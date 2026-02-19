using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using VikingJamGame.Models.Navigation;

namespace VikingJamGame.GameLogic.Nodes;

[GlobalClass]
public partial class NavigationMapGeneratorNode : Node
{
    [ExportCategory("Dependencies")]
    [Export] private Button? GenerateButton { get; set; }
    [Export] private MapNodeRepositoryNode? NodeRepository { get; set; }
    [Export] private Node2D? StartingVillage { get; set; }
    [Export] private Node2D? NodesRoot { get; set; }
    [Export(PropertyHint.Dir)] private string NodeArtDirectory { get; set; } = "res://resources/map-generator-test";
    [Export] private Color ConnectionDotColor { get; set; } = new(0.12f, 0.12f, 0.12f, 0.85f);
    [Export] private float ConnectionDotRadius { get; set; } = 3.5f;
    [Export] private float ConnectionDotSpacing { get; set; } = 18f;
    [Export] private float ConnectionInsetFromNode { get; set; } = 28f;
    
    [ExportCategory("FTL Layout Settings")]
    
    //Horizontal distance between layers
    [Export] private float ColumnSpacingX { get; set; } = 200f;
    // Vertical distance between nodes in the same column
    [Export] private float RowSpacingY { get; set; } = 120f;    
    // Random offset so it doesn't look like a rigid spreadsheet
    [Export] private float JitterAmount { get; set; } = 25f;    

    [ExportCategory("Generation Parameters")]
    [Export] private int MaxNodesCount { get; set; } = 25;
    [Export] private int PeakFrontierWidth { get; set; } = 6;
    [Export] private int MaxMergeDistance { get; set; } = 3;

    private readonly NavigationMapGenerator _generator = new();
    private NavigationMap? _map;
    private IReadOnlyDictionary<string, MapNodeDefinition> _definitionsByKind =
        new Dictionary<string, MapNodeDefinition>(StringComparer.Ordinal);
    private readonly Dictionary<int, Vector2> _nodePositionsById = [];

    private const string START_KIND = "starting_village";
    private const string END_KIND = "final_boss";
    private const string CONNECTIONS_LAYER_NAME = "ConnectionsLayer";

    private void CreateMap()
    {
        if (_map is null)
        {
            return;
        }

        Node2D root = RequireNodesRoot();
        Node2D startNode = RequireStartingVillage();

        _nodePositionsById.Clear();
        Vector2 startPosition = root.ToLocal(startNode.GlobalPosition);
        _nodePositionsById[_map.StartNodeId] = startPosition;

        foreach (Node child in root.GetChildren())
        {
            if (ReferenceEquals(child, startNode))
            {
                continue;
            }

            child.QueueFree();
        }

        BuildNodeLayout(_map);
        CreateConnectionLayer(_map, root);

        foreach (NavigationMapNode mapNode in _map.NodesById.Values.OrderBy(node => node.Id))
        {
            if (mapNode.Id == _map.StartNodeId)
            {
                continue;
            }

            if (!_nodePositionsById.TryGetValue(mapNode.Id, out Vector2 position))
            {
                continue;
            }

            Sprite2D visualNode = CreateVisualNode(mapNode);
            visualNode.Position = position;
            root.AddChild(visualNode);
        }
    }

    private void CreateConnectionLayer(NavigationMap map, Node2D root)
    {
        var layer = new Node2D
        {
            Name = CONNECTIONS_LAYER_NAME
        };

        Texture2D dotTexture = CreateDotTexture(ConnectionDotRadius);
        foreach (NavigationMapNode node in map.NodesById.Values)
        {
            if (!_nodePositionsById.TryGetValue(node.Id, out Vector2 from))
            {
                continue;
            }

            foreach (int neighbourId in node.NeighbourIds)
            {
                if (!_nodePositionsById.TryGetValue(neighbourId, out Vector2 to))
                {
                    continue;
                }

                AddDotsForConnection(layer, from, to, dotTexture);
            }
        }

        root.AddChild(layer);
    }
    
    private void AddDotsForConnection(Node2D layer, Vector2 from, Vector2 to, Texture2D dotTexture)
    {
        Vector2 rawDirection = to - from;
        float rawLength = rawDirection.Length();
        if (rawLength <= Mathf.Epsilon)
        {
            return;
        }

        Vector2 direction = rawDirection / rawLength;
        float maxInset = rawLength / 2f - 1f;
        float inset = Mathf.Min(ConnectionInsetFromNode, Mathf.Max(0f, maxInset));
        Vector2 start = from + direction * inset;
        Vector2 end = to - direction * inset;
        float length = start.DistanceTo(end);

        if (length <= Mathf.Epsilon)
        {
            return;
        }

        float spacing = Mathf.Max(6f, ConnectionDotSpacing);
        int dotCount = Mathf.Max(2, Mathf.FloorToInt(length / spacing) + 1);
        for (var i = 0; i < dotCount; i++)
        {
            float distance = Mathf.Min(i * spacing, length);
            Vector2 position = start + direction * distance;
            layer.AddChild(CreateConnectionDot(dotTexture, position));
        }

        Vector2 lastDot = start + direction * Mathf.Min((dotCount - 1) * spacing, length);
        if (lastDot.DistanceTo(end) > spacing * 0.5f)
        {
            layer.AddChild(CreateConnectionDot(dotTexture, end));
        }
    }

    private Sprite2D CreateConnectionDot(Texture2D dotTexture, Vector2 position)
    {
        return new Sprite2D
        {
            Texture = dotTexture,
            Centered = true,
            Position = position,
            Modulate = ConnectionDotColor
        };
    }

    private static Texture2D CreateDotTexture(float radius)
    {
        float safeRadius = Mathf.Max(1f, radius);
        int diameter = Mathf.Max(2, Mathf.CeilToInt(safeRadius * 2f));
        Image image = Image.CreateEmpty(diameter, diameter, false, Image.Format.Rgba8);

        Vector2 center = new((diameter - 1) * 0.5f, (diameter - 1) * 0.5f);
        float radiusSquared = safeRadius * safeRadius;

        for (var y = 0; y < diameter; y++)
        {
            for (var x = 0; x < diameter; x++)
            {
                Vector2 delta = new(x, y);
                delta -= center;
                image.SetPixel(
                    x,
                    y,
                    delta.LengthSquared() <= radiusSquared ? Colors.White : Colors.Transparent);
            }
        }

        return ImageTexture.CreateFromImage(image);
    }

    private void BuildNodeLayout(NavigationMap map)
    {
        Dictionary<int, int> depthsByNodeId = GetDepthsByNodeId(map);
        Dictionary<int, List<int>> layers = GroupNodesIntoLayers(depthsByNodeId);
        Dictionary<int, List<int>> parentIdsByNodeId = BuildParentIdsByNodeId(map);
        
        PositionNodesOnScreen(map, layers, depthsByNodeId, parentIdsByNodeId);
    }

    private Dictionary<int, int> GetDepthsByNodeId(NavigationMap map)
    {
        bool hasExplicitDepths = map.NodesById.Values
            .Any(node => node.Id != map.StartNodeId && node.Depth > 0);

        if (!hasExplicitDepths)
        {
            return CalculateNodeDepths(map);
        }

        var depthsByNodeId = new Dictionary<int, int>(map.NodesById.Count);
        foreach (NavigationMapNode node in map.NodesById.Values)
        {
            depthsByNodeId[node.Id] = Math.Max(0, node.Depth);
        }

        return depthsByNodeId;
    }

    private Dictionary<int, int> CalculateNodeDepths(NavigationMap map)
    {
        var depthsByNodeId = new Dictionary<int, int>();
        var queue = new Queue<(int nodeId, int depth)>();
        queue.Enqueue((map.StartNodeId, 0));

        while (queue.Count > 0)
        {
            var (nodeId, depth) = queue.Dequeue();

            // Fallback for maps without explicit depth: keep the shortest path depth.
            if (ShouldUpdateDepth(depthsByNodeId, nodeId, depth))
            {
                depthsByNodeId[nodeId] = depth;
                EnqueueNeighbours(queue, map, nodeId, depth);
            }
        }

        return depthsByNodeId;
    }

    private void EnqueueNeighbours(Queue<(int nodeId, int depth)> queue, NavigationMap map, int nodeId, int depth)
    {
        foreach (int neighbourId in map.NodesById[nodeId].NeighbourIds)
        {
            queue.Enqueue((neighbourId, depth + 1));
        }
    }

    private bool ShouldUpdateDepth(Dictionary<int, int> depthsByNodeId, int nodeId, int depth)
    {
        return !depthsByNodeId.TryGetValue(nodeId, out int currentDepth) || depth < currentDepth;
    }

    private Dictionary<int, List<int>> GroupNodesIntoLayers(Dictionary<int, int> depthsByNodeId)
    {
        var layers = new Dictionary<int, List<int>>();
        foreach (var kvp in depthsByNodeId)
        {
            int depth = kvp.Value;
            if (!layers.ContainsKey(depth))
            {
                layers[depth] = [];
            }
            layers[depth].Add(kvp.Key);
        }

        return layers;
    }

    private static Dictionary<int, List<int>> BuildParentIdsByNodeId(NavigationMap map)
    {
        var parentIdsByNodeId = new Dictionary<int, List<int>>();
        foreach (NavigationMapNode node in map.NodesById.Values)
        {
            foreach (int neighbourId in node.NeighbourIds)
            {
                if (!parentIdsByNodeId.TryGetValue(neighbourId, out List<int>? parentIds))
                {
                    parentIds = [];
                    parentIdsByNodeId[neighbourId] = parentIds;
                }

                parentIds.Add(node.Id);
            }
        }

        return parentIdsByNodeId;
    }

    private void PositionNodesOnScreen(
        NavigationMap map,
        Dictionary<int, List<int>> layers,
        IReadOnlyDictionary<int, int> depthsByNodeId,
        IReadOnlyDictionary<int, List<int>> parentIdsByNodeId)
    {
        Vector2 startPos = _nodePositionsById[map.StartNodeId];
        int maxDepth = layers.Keys.Max();

        for (int depth = 0; depth <= maxDepth; depth++)
        {
            if (!layers.TryGetValue(depth, out List<int>? layerNodes))
            {
                continue;
            }
            
            int nodeCount = layerNodes.Count;
            List<int> orderedNodeIds = OrderLayerNodes(layerNodes, depth, startPos.Y, depthsByNodeId, parentIdsByNodeId);
            float[] yPositions = BuildLayerYPositions(
                orderedNodeIds,
                depth,
                startPos.Y,
                depthsByNodeId,
                parentIdsByNodeId);

            float actualJitter = JitterAmount * GetLayerJitterScale(depth, maxDepth, nodeCount);

            for (int i = 0; i < nodeCount; i++)
            {
                int nodeId = orderedNodeIds[i];
                
                if (nodeId == map.StartNodeId)
                {
                    continue;
                }

                float baseX = startPos.X + (depth * ColumnSpacingX);
                float baseY = yPositions[i];

                float offsetX = (float)GD.RandRange(-actualJitter, actualJitter);
                float offsetY = (float)GD.RandRange(-actualJitter, actualJitter);

                _nodePositionsById[nodeId] = new Vector2(baseX + offsetX, baseY + offsetY);
            }
        }
    }

    private List<int> OrderLayerNodes(
        IReadOnlyList<int> layerNodes,
        int depth,
        float fallbackY,
        IReadOnlyDictionary<int, int> depthsByNodeId,
        IReadOnlyDictionary<int, List<int>> parentIdsByNodeId)
    {
        return layerNodes
            .OrderBy(nodeId => GetParentAnchorY(nodeId, depth, fallbackY, depthsByNodeId, parentIdsByNodeId))
            .ThenBy(nodeId => nodeId)
            .ToList();
    }

    private float[] BuildLayerYPositions(
        IReadOnlyList<int> orderedNodeIds,
        int depth,
        float fallbackCenterY,
        IReadOnlyDictionary<int, int> depthsByNodeId,
        IReadOnlyDictionary<int, List<int>> parentIdsByNodeId)
    {
        if (orderedNodeIds.Count == 0)
        {
            return [];
        }

        var yPositions = new float[orderedNodeIds.Count];
        for (var i = 0; i < orderedNodeIds.Count; i++)
        {
            yPositions[i] = GetParentAnchorY(
                orderedNodeIds[i],
                depth,
                fallbackCenterY,
                depthsByNodeId,
                parentIdsByNodeId);
        }

        float minSpacing = Mathf.Max(8f, RowSpacingY);
        for (var i = 1; i < yPositions.Length; i++)
        {
            float minimumAllowedY = yPositions[i - 1] + minSpacing;
            if (yPositions[i] < minimumAllowedY)
            {
                yPositions[i] = minimumAllowedY;
            }
        }

        float layerCenterY = (yPositions[0] + yPositions[^1]) * 0.5f;
        float centerShift = fallbackCenterY - layerCenterY;
        for (var i = 0; i < yPositions.Length; i++)
        {
            yPositions[i] += centerShift;
        }

        return yPositions;
    }

    private float GetParentAnchorY(
        int nodeId,
        int depth,
        float fallbackY,
        IReadOnlyDictionary<int, int> depthsByNodeId,
        IReadOnlyDictionary<int, List<int>> parentIdsByNodeId)
    {
        if (depth <= 0 || !parentIdsByNodeId.TryGetValue(nodeId, out List<int>? parentIds))
        {
            return fallbackY;
        }

        float totalY = 0f;
        int parentCount = 0;

        foreach (int parentId in parentIds)
        {
            if (!depthsByNodeId.TryGetValue(parentId, out int parentDepth) || parentDepth != depth - 1)
            {
                continue;
            }

            if (!_nodePositionsById.TryGetValue(parentId, out Vector2 parentPosition))
            {
                continue;
            }

            totalY += parentPosition.Y;
            parentCount++;
        }

        return parentCount == 0 ? fallbackY : totalY / parentCount;
    }

    private static float GetLayerJitterScale(int depth, int maxDepth, int nodeCount)
    {
        float scale = 1f;

        if (depth >= maxDepth - 1)
        {
            scale *= 0.35f;
        }

        if (nodeCount <= 1)
        {
            scale *= 0.15f;
        }
        else if (nodeCount == 2)
        {
            scale *= 0.35f;
        }
        else if (nodeCount == 3)
        {
            scale *= 0.6f;
        }

        return scale;
    }

    private static float[] BuildNeighbourAngles(float baseAngleRadians, int neighbourCount)
    {
        if (neighbourCount <= 0)
        {
            return [];
        }

        if (neighbourCount == 1)
        {
            return [baseAngleRadians];
        }

        float spreadDegrees = Mathf.Min(170f, 34f + neighbourCount * 24f);
        float spreadRadians = Mathf.DegToRad(spreadDegrees);
        float firstAngle = baseAngleRadians - spreadRadians / 2f;
        float step = spreadRadians / (neighbourCount - 1);

        var angles = new float[neighbourCount];
        for (var i = 0; i < neighbourCount; i++)
        {
            angles[i] = firstAngle + step * i;
        }

        return angles;
    }
    
    private Sprite2D CreateVisualNode(NavigationMapNode mapNode)
    {
        var sprite = new Sprite2D
        {
            Name = $"MapNode_{mapNode.Id}_{mapNode.Kind}",
            Centered = true
        };

        if (_definitionsByKind.TryGetValue(mapNode.Kind, out MapNodeDefinition? definition))
        {
            sprite.Texture = LoadTextureForDefinition(definition);
        }
        else
        {
            GD.PushWarning($"No map definition found for node kind '{mapNode.Kind}'.");
        }

        return sprite;
    }

    private Texture2D? LoadTextureForDefinition(MapNodeDefinition definition)
    {
        if (string.IsNullOrWhiteSpace(definition.Art))
        {
            return null;
        }

        string directory = NodeArtDirectory.TrimEnd('/', '\\');
        string texturePath = $"{directory}/{definition.Art}";
        if (!ResourceLoader.Exists(texturePath))
        {
            GD.PushWarning($"Map-node art not found at '{texturePath}' for kind '{definition.Kind}'.");
            return null;
        }

        return ResourceLoader.Load<Texture2D>(texturePath);
    }

    public void Generate()
    {
        MapNodeRepositoryNode repository = RequireNodeRepository();
        _definitionsByKind = repository.All
            .ToDictionary(definition => definition.Kind, definition => definition, StringComparer.Ordinal);

        var parameters = new NavigationMapGenerationParameters
        {
            PeakFrontierWidth = PeakFrontierWidth,
            MaxMergeDistance = MaxMergeDistance
        };

        _map = _generator.Generate(_definitionsByKind, START_KIND, END_KIND, MaxNodesCount, parameters: parameters);
        CreateMap();
    }

    public override void _Ready()
    {
        MapNodeRepositoryNode repository = RequireNodeRepository();
        repository.Reload();

        if (GenerateButton is not null)
        {
            GenerateButton.Pressed += Generate;
        }

        Generate();
    }

    public override void _ExitTree()
    {
        if (GenerateButton is not null)
        {
            GenerateButton.Pressed -= Generate;
        }
    }

    private MapNodeRepositoryNode RequireNodeRepository() => 
        NodeRepository
        ?? throw new InvalidOperationException($"{nameof(NodeRepository)} must be assigned.");

    private Node2D RequireStartingVillage() => 
        StartingVillage
        ?? throw new InvalidOperationException($"{nameof(StartingVillage)} must be assigned.");

    private Node2D RequireNodesRoot() => 
        NodesRoot
        ?? throw new InvalidOperationException($"{nameof(NodesRoot)} must be assigned.");

}
