using System.Collections.Generic;
using System.Linq;
using Chickensoft.AutoInject;
using Chickensoft.Introspection;
using Godot;
using VikingJamGame.Models.Navigation;

namespace VikingJamGame.GameLogic.Nodes;

[GlobalClass]
[Meta(typeof(IAutoNode))]
public partial class GodotMapLinkRenderer : Node
{
    [Dependency] public GodotMapGenerator MapGenerator => this.DependOn<GodotMapGenerator>();

    [ExportCategory("General")]
    [Export] private string ConnectionsLayerName { get; set; } = "ConnectionsLayer";
    [Export] private bool DebugRendering { get; set; } = false;

    [ExportCategory("Forward Link Style")]
    [Export] private Color ForwardConnectionDotColor { get; set; } = new(0.12f, 0.12f, 0.12f, 0.85f);
    [Export] private float ForwardConnectionDotRadius { get; set; } = 3.5f;
    [Export] private float ForwardConnectionDotSpacing { get; set; } = 18f;
    [Export] private float ForwardConnectionInsetFromNode { get; set; } = 28f;

    [ExportCategory("Backward Link Style")]
    [Export] private Color BackConnectionDotColor { get; set; } = new(0.65f, 0.35f, 0.20f, 0.85f);
    [Export] private float BackConnectionDotRadius { get; set; } = 3.5f;
    [Export] private float BackConnectionDotSpacing { get; set; } = 18f;
    [Export] private float BackConnectionInsetFromNode { get; set; } = 28f;

    public void OnResolved()
    {
        if (DebugRendering) RenderAllConnections();
    }

    public void ClearConnections()
    {
        if (!TryGetRenderContext(out _, out Node2D nodesRoot, out _))
        {
            return;
        }

        RemoveExistingConnectionsLayer(nodesRoot);
    }

    public void RenderAllConnections()
    {
        if (!TryGetRenderContext(out NavigationMap map, out Node2D nodesRoot, out IReadOnlyDictionary<int, Vector2> nodePositionsById))
        {
            return;
        }

        RemoveExistingConnectionsLayer(nodesRoot);

        var layer = new Node2D
        {
            Name = ConnectionsLayerName
        };

        Texture2D forwardDotTexture = CreateDotTexture(ForwardConnectionDotRadius);
        foreach (NavigationMapNode node in map.NodesById.Values)
        {
            if (!nodePositionsById.TryGetValue(node.Id, out Vector2 from))
            {
                continue;
            }

            foreach (int neighbourId in node.NeighbourIds)
            {
                if (!nodePositionsById.TryGetValue(neighbourId, out Vector2 to))
                {
                    continue;
                }

                AddDotsForConnection(
                    layer,
                    from,
                    to,
                    forwardDotTexture,
                    ForwardConnectionDotColor,
                    ForwardConnectionDotSpacing,
                    ForwardConnectionInsetFromNode);
            }
        }

        nodesRoot.AddChild(layer);
    }

    public void RenderConnectionsForCurrentNode(int currentNodeId, IReadOnlySet<int>? visibleNodeIds = null)
    {
        if (!TryGetRenderContext(out NavigationMap map, out Node2D nodesRoot, out IReadOnlyDictionary<int, Vector2> nodePositionsById))
        {
            return;
        }

        if (!map.NodesById.ContainsKey(currentNodeId))
        {
            GD.PushWarning(
                $"Cannot render current-node connections. Node id '{currentNodeId}' does not exist in map.");
            return;
        }

        if (!nodePositionsById.TryGetValue(currentNodeId, out Vector2 currentPosition))
        {
            GD.PushWarning(
                $"Cannot render current-node connections. Node id '{currentNodeId}' has no visual position.");
            return;
        }

        RemoveExistingConnectionsLayer(nodesRoot);

        var layer = new Node2D
        {
            Name = ConnectionsLayerName
        };

        Texture2D forwardDotTexture = CreateDotTexture(ForwardConnectionDotRadius);
        Texture2D backDotTexture = CreateDotTexture(BackConnectionDotRadius);

        NavigationMapNode currentNode = map.NodesById[currentNodeId];

        var forwardTargets = new HashSet<int>();
        foreach (int neighbourId in currentNode.NeighbourIds)
        {
            if (!nodePositionsById.TryGetValue(neighbourId, out Vector2 to))
            {
                continue;
            }

            AddDotsForConnection(
                layer,
                currentPosition,
                to,
                forwardDotTexture,
                ForwardConnectionDotColor,
                ForwardConnectionDotSpacing,
                ForwardConnectionInsetFromNode);

            forwardTargets.Add(neighbourId);
        }

        foreach (int parentId in GetParentIds(map, currentNodeId))
        {
            // If node has explicit bidirectional edge, prefer forward style once.
            if (forwardTargets.Contains(parentId))
            {
                continue;
            }

            // Only draw backward links to nodes that are actually visible.
            if (visibleNodeIds is not null && !visibleNodeIds.Contains(parentId))
            {
                continue;
            }

            if (!nodePositionsById.TryGetValue(parentId, out Vector2 parentPosition))
            {
                continue;
            }

            AddDotsForConnection(
                layer,
                currentPosition,
                parentPosition,
                backDotTexture,
                BackConnectionDotColor,
                BackConnectionDotSpacing,
                BackConnectionInsetFromNode);
        }

        nodesRoot.AddChild(layer);
    }

    public void RenderConnectionsBetweenVisibleNodes(IReadOnlySet<int> visibleNodeIds)
    {
        if (!TryGetRenderContext(out NavigationMap map, out Node2D nodesRoot, out IReadOnlyDictionary<int, Vector2> nodePositionsById))
        {
            return;
        }

        RemoveExistingConnectionsLayer(nodesRoot);

        var layer = new Node2D
        {
            Name = ConnectionsLayerName
        };

        Texture2D dotTexture = CreateDotTexture(ForwardConnectionDotRadius);

        foreach (int nodeId in visibleNodeIds)
        {
            if (!map.NodesById.TryGetValue(nodeId, out NavigationMapNode? node)) continue;
            if (!nodePositionsById.TryGetValue(nodeId, out Vector2 from)) continue;

            foreach (int neighbourId in node.NeighbourIds)
            {
                if (!visibleNodeIds.Contains(neighbourId)) continue;
                if (!nodePositionsById.TryGetValue(neighbourId, out Vector2 to)) continue;

                AddDotsForConnection(
                    layer,
                    from,
                    to,
                    dotTexture,
                    ForwardConnectionDotColor,
                    ForwardConnectionDotSpacing,
                    ForwardConnectionInsetFromNode);
            }
        }

        nodesRoot.AddChild(layer);
    }

    private bool TryGetRenderContext(
        out NavigationMap map,
        out Node2D nodesRoot,
        out IReadOnlyDictionary<int, Vector2> nodePositionsById)
    {
        map = null!;
        nodesRoot = null!;
        nodePositionsById = new Dictionary<int, Vector2>();

        if (MapGenerator is null)
        {
            GD.PushWarning(
                $"{nameof(GodotMapLinkRenderer)} has no {nameof(MapGenerator)} assigned.");
            return false;
        }

        if (!MapGenerator.TryGetRenderContext(out map, out nodesRoot, out nodePositionsById))
        {
            GD.PushWarning(
                $"{nameof(GodotMapLinkRenderer)} cannot render connections because map context is not ready.");
            return false;
        }

        return true;
    }

    private void RemoveExistingConnectionsLayer(Node2D nodesRoot)
    {
        Node? existingLayer = nodesRoot.GetNodeOrNull(ConnectionsLayerName);
        if (existingLayer is null)
        {
            return;
        }

        nodesRoot.RemoveChild(existingLayer);
        existingLayer.QueueFree();
    }

    private static IEnumerable<int> GetParentIds(NavigationMap map, int nodeId)
    {
        foreach (NavigationMapNode node in map.NodesById.Values)
        {
            if (node.NeighbourIds.Contains(nodeId))
            {
                yield return node.Id;
            }
        }
    }

    private static void AddDotsForConnection(
        Node2D layer,
        Vector2 from,
        Vector2 to,
        Texture2D dotTexture,
        Color dotColor,
        float dotSpacing,
        float insetFromNode)
    {
        Vector2 rawDirection = to - from;
        float rawLength = rawDirection.Length();
        if (rawLength <= Mathf.Epsilon)
        {
            return;
        }

        Vector2 direction = rawDirection / rawLength;
        float maxInset = rawLength / 2f - 1f;
        float inset = Mathf.Min(insetFromNode, Mathf.Max(0f, maxInset));
        Vector2 start = from + direction * inset;
        Vector2 end = to - direction * inset;
        float length = start.DistanceTo(end);

        if (length <= Mathf.Epsilon)
        {
            return;
        }

        float spacing = Mathf.Max(6f, dotSpacing);
        int dotCount = Mathf.Max(2, Mathf.FloorToInt(length / spacing) + 1);
        for (var i = 0; i < dotCount; i++)
        {
            float distance = Mathf.Min(i * spacing, length);
            Vector2 position = start + direction * distance;
            layer.AddChild(CreateConnectionDot(dotTexture, dotColor, position));
        }

        Vector2 lastDot = start + direction * Mathf.Min((dotCount - 1) * spacing, length);
        if (lastDot.DistanceTo(end) > spacing * 0.5f)
        {
            layer.AddChild(CreateConnectionDot(dotTexture, dotColor, end));
        }
    }

    private static Sprite2D CreateConnectionDot(Texture2D dotTexture, Color dotColor, Vector2 position)
    {
        return new Sprite2D
        {
            Texture = dotTexture,
            Centered = true,
            Position = position,
            Modulate = dotColor
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
    
    public override void _Notification(int what) => this.Notify(what);
}
