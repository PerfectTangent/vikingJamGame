using System;
using System.Collections.Generic;
using System.Linq;

namespace VikingJamGame.Models.Navigation;

public sealed class NavigationMapGenerator
{
    public NavigationMap Generate(
        IReadOnlyDictionary<string, MapNodeDefinition> nodeDefinitions,
        string startKind,
        string endKind,
        int maxNodes,
        INavigationRandom? random = null,
        NavigationMapGenerationParameters? parameters = null)
    {
        ArgumentNullException.ThrowIfNull(nodeDefinitions);
        ArgumentException.ThrowIfNullOrWhiteSpace(startKind);

        if (maxNodes < 1)
        {
            throw new ArgumentOutOfRangeException(
                nameof(maxNodes),
                maxNodes,
                "maxNodes must be at least 1.");
        }

        if (!nodeDefinitions.ContainsKey(startKind))
        {
            throw new InvalidOperationException(
                $"Starting node kind '{startKind}' is not defined.");
        }

        random ??= new SystemNavigationRandom();
        parameters ??= NavigationMapGenerationParameters.Default;

        return new MapGenerationSession(nodeDefinitions, maxNodes, random, parameters).Generate(startKind, endKind);
    }

    private static string PickWeightedNeighbour(
        IReadOnlyList<KeyValuePair<string, float>> weightedNeighbours,
        INavigationRandom random)
    {
        var totalWeight = weightedNeighbours.Sum(entry => (double)entry.Value);
        if (totalWeight <= 0)
        {
            throw new InvalidOperationException("Weighted neighbours must have a positive total weight.");
        }

        var roll = random.NextDouble() * totalWeight;
        double cumulative = 0;

        foreach (var entry in weightedNeighbours)
        {
            cumulative += entry.Value;
            if (roll < cumulative) return entry.Key;
        }

        return weightedNeighbours[^1].Key;
    }

    private sealed class MapGenerationSession(
        IReadOnlyDictionary<string, MapNodeDefinition> nodeDefinitions,
        int maxNodes,
        INavigationRandom random,
        NavigationMapGenerationParameters parameters)
    {
        private const int START_CONNECTIONS = 1;
        private const int NON_START_MIN_CONNECTIONS = 2;
        private const int NON_START_MAX_CONNECTIONS = 3;
        private const double BASE_MERGE_PROBABILITY = 0.35;
        private const double NEAR_TARGET_MERGE_PROBABILITY = 0.75;
        private const double AT_TARGET_MERGE_PROBABILITY = 0.95;

        private readonly List<MutableNode> _mutableNodes = new(capacity: maxNodes);
        private int _startNodeId;

        public NavigationMap Generate(string startKind, string endKind)
        {
            _startNodeId = AddNode(startKind, depth: 0);

            var currentLayer = new List<MutableNode> { _mutableNodes[_startNodeId] };

            while (currentLayer.Count > 0 && _mutableNodes.Count < maxNodes)
            {
                currentLayer = ProcessLayer(currentLayer);
            }

            ConnectLooseEnds(endKind);

            return CreateMap();
        }

        private List<MutableNode> ProcessLayer(List<MutableNode> currentLayer)
        {
            var nextLayer = new List<MutableNode>();
            int minPlanarIndex = 0;
            int targetNextLayerWidth = GetTargetNextLayerWidth();

            foreach (MutableNode mapNode in currentLayer)
            {
                ProcessNode(
                    mapNode,
                    nextLayer,
                    ref minPlanarIndex,
                    targetNextLayerWidth);
            }

            if (nextLayer.Count == 0 && _mutableNodes.Count < maxNodes)
            {
                RecoverFromDeadEnd(nextLayer);
            }

            return nextLayer;
        }

        private void ProcessNode(
            MutableNode mapNode,
            List<MutableNode> nextLayer,
            ref int minPlanarIndex,
            int targetNextLayerWidth)
        {
            MapNodeDefinition definition = nodeDefinitions[mapNode.Kind];
            List<KeyValuePair<string, float>> weightedNeighbours = definition.PossibleNeighbours
                .Where(entry => entry.Value > 0)
                .ToList();

            if (weightedNeighbours.Count == 0)
            {
                return;
            }

            int minConnections = GetMinConnectionsForNode(mapNode);
            int maxConnections = GetMaxConnectionsForNode(mapNode);
            maxConnections = Math.Max(minConnections, maxConnections);

            var requestedConnections = random.NextInt(minConnections, maxConnections + 1);

            for (var i = 0; i < requestedConnections && weightedNeighbours.Count > 0; i++)
            {
                var neighbourKind = PickWeightedNeighbour(weightedNeighbours, random);

                if (!nodeDefinitions.ContainsKey(neighbourKind))
                {
                    throw new InvalidOperationException(
                        $"Node kind '{mapNode.Kind}' references unknown neighbour kind '{neighbourKind}'.");
                }

                TryAddConnection(
                    mapNode,
                    neighbourKind,
                    nextLayer,
                    ref minPlanarIndex,
                    targetNextLayerWidth,
                    allowAnyKindMergeFallback: false);
            }

            int safety = 0;
            while (mapNode.NeighbourIds.Count < minConnections && safety < 8 && weightedNeighbours.Count > 0)
            {
                safety++;
                string neighbourKind = PickWeightedNeighbour(weightedNeighbours, random);
                TryAddConnection(
                    mapNode,
                    neighbourKind,
                    nextLayer,
                    ref minPlanarIndex,
                    targetNextLayerWidth,
                    allowAnyKindMergeFallback: true);
            }
        }

        private void TryAddConnection(
            MutableNode mapNode,
            string neighbourKind,
            List<MutableNode> nextLayer,
            ref int minPlanarIndex,
            int targetNextLayerWidth,
            bool allowAnyKindMergeFallback)
        {
            var possibleMerges = GetPossibleMerges(neighbourKind, nextLayer, minPlanarIndex, ignoreDistance: false);

            if (ShouldPreferMerge(nextLayer.Count, targetNextLayerWidth) && possibleMerges.Count > 0)
            {
                PerformMerge(mapNode, possibleMerges, ref minPlanarIndex);
                return;
            }

            if (AttemptRandomMerge(
                    mapNode,
                    possibleMerges,
                    nextLayer.Count,
                    targetNextLayerWidth,
                    ref minPlanarIndex))
            {
                return;
            }

            bool canCreateNewNode = _mutableNodes.Count < maxNodes && nextLayer.Count < targetNextLayerWidth;
            if (canCreateNewNode)
            {
                minPlanarIndex = AddNewNeighbourNode(mapNode, neighbourKind, nextLayer);
            }
            else
            {
                AttemptForcedMerge(
                    mapNode,
                    neighbourKind,
                    nextLayer,
                    ref minPlanarIndex,
                    allowAnyKindMergeFallback);
            }
        }

        private List<(MutableNode Node, int Index)> GetPossibleMerges(string neighbourKind, List<MutableNode> nextLayer, int minPlanarIndex, bool ignoreDistance)
        {
            var result = new List<(MutableNode, int)>();
            int maxIndex = ignoreDistance ? nextLayer.Count : Math.Min(nextLayer.Count, minPlanarIndex + parameters.MaxMergeDistance);

            for (int i = minPlanarIndex; i < maxIndex; i++)
            {
                if (nextLayer[i].Kind == neighbourKind)
                {
                    result.Add((nextLayer[i], i));
                }
            }
            return result;
        }

        private bool AttemptRandomMerge(
            MutableNode mapNode,
            List<(MutableNode Node, int Index)> possibleMerges,
            int nextLayerCount,
            int targetNextLayerWidth,
            ref int minPlanarIndex)
        {
            if (possibleMerges.Count == 0) return false;

            double mergeProbability = GetMergeProbability(nextLayerCount, targetNextLayerWidth);
            if (random.NextDouble() >= mergeProbability) return false;

            return PerformMerge(mapNode, possibleMerges, ref minPlanarIndex);
        }

        private bool ShouldPreferMerge(int nextLayerWidth, int targetNextLayerWidth)
        {
            if (targetNextLayerWidth <= 1)
            {
                return nextLayerWidth >= 1;
            }

            return nextLayerWidth >= targetNextLayerWidth;
        }

        private void AttemptForcedMerge(
            MutableNode mapNode,
            string neighbourKind,
            List<MutableNode> nextLayer,
            ref int minPlanarIndex,
            bool allowAnyKindMergeFallback)
        {
            var possibleMerges = GetPossibleMerges(neighbourKind, nextLayer, minPlanarIndex, ignoreDistance: false);
            if (possibleMerges.Count > 0)
            {
                PerformMerge(mapNode, possibleMerges, ref minPlanarIndex);
                return;
            }

            // Fallback: ignore distance limit if forced to merge
            possibleMerges = GetPossibleMerges(neighbourKind, nextLayer, minPlanarIndex, ignoreDistance: true);
            if (possibleMerges.Count > 0)
            {
                PerformMerge(mapNode, possibleMerges, ref minPlanarIndex);
                return;
            }

            if (!allowAnyKindMergeFallback)
            {
                return;
            }

            possibleMerges = GetPossibleMergesAnyKind(nextLayer, minPlanarIndex, ignoreDistance: false);
            if (possibleMerges.Count > 0)
            {
                PerformMerge(mapNode, possibleMerges, ref minPlanarIndex);
                return;
            }

            possibleMerges = GetPossibleMergesAnyKind(nextLayer, minPlanarIndex, ignoreDistance: true);
            if (possibleMerges.Count > 0)
            {
                PerformMerge(mapNode, possibleMerges, ref minPlanarIndex);
            }
        }

        private List<(MutableNode Node, int Index)> GetPossibleMergesAnyKind(
            List<MutableNode> nextLayer,
            int minPlanarIndex,
            bool ignoreDistance)
        {
            var result = new List<(MutableNode, int)>();
            int maxIndex = ignoreDistance ? nextLayer.Count : Math.Min(nextLayer.Count, minPlanarIndex + parameters.MaxMergeDistance);

            for (int i = minPlanarIndex; i < maxIndex; i++)
            {
                result.Add((nextLayer[i], i));
            }

            return result;
        }

        private bool PerformMerge(MutableNode mapNode, List<(MutableNode Node, int Index)> possibleMerges, ref int minPlanarIndex)
        {
            var mergeTarget = possibleMerges[random.NextInt(0, possibleMerges.Count)];

            if (!mapNode.NeighbourIds.Contains(mergeTarget.Node.Id))
            {
                mapNode.NeighbourIds.Add(mergeTarget.Node.Id);
                minPlanarIndex = mergeTarget.Index;
                return true;
            }
            return false;
        }

        private int AddNewNeighbourNode(MutableNode mapNode, string neighbourKind, List<MutableNode> nextLayer)
        {
            var neighbourId = AddNode(neighbourKind, mapNode.Depth + 1);
            mapNode.NeighbourIds.Add(neighbourId);
            nextLayer.Add(_mutableNodes[neighbourId]);
            return nextLayer.Count - 1;
        }

        private void ConnectLooseEnds(string endKind)
        {
            var looseEnds = _mutableNodes.Where(n => n.NeighbourIds.Count == 0 && n.Id != _startNodeId).ToList();

            if (looseEnds.Count > 0)
            {
                int deepestLooseDepth = looseEnds.Max(node => node.Depth);
                var incomingCountByNodeId = _mutableNodes
                    .SelectMany(
                        node => node.NeighbourIds,
                        (node, neighbourId) => neighbourId)
                    .GroupBy(neighbourId => neighbourId)
                    .ToDictionary(group => group.Key, group => group.Count());

                MutableNode finalDestination = looseEnds
                    .Where(node => node.Depth == deepestLooseDepth)
                    .OrderBy(node => incomingCountByNodeId.GetValueOrDefault(node.Id))
                    .ThenBy(node => node.Id)
                    .First();

                finalDestination.Kind = endKind;

                List<MutableNode> deepestOtherLooseEnds = looseEnds
                    .Where(node => node.Depth == deepestLooseDepth && node.Id != finalDestination.Id)
                    .OrderBy(node => node.Id)
                    .ToList();

                List<MutableNode> currentParentsOfFinal = _mutableNodes
                    .Where(node => node.NeighbourIds.Contains(finalDestination.Id))
                    .OrderBy(node => node.Id)
                    .ToList();

                int finalDepth = deepestLooseDepth;
                if (deepestOtherLooseEnds.Count > 0)
                {
                    finalDepth = deepestLooseDepth + 2;
                    finalDestination.Depth = finalDepth;

                    int gatewayCount = Math.Min(3, deepestOtherLooseEnds.Count);
                    List<MutableNode> gateways = deepestOtherLooseEnds
                        .Take(gatewayCount)
                        .ToList();

                    foreach (MutableNode gateway in gateways)
                    {
                        gateway.Depth = finalDepth - 1;
                        if (!gateway.NeighbourIds.Contains(finalDestination.Id))
                        {
                            gateway.NeighbourIds.Add(finalDestination.Id);
                        }
                    }

                    List<MutableNode> extraDeepLooseEnds = deepestOtherLooseEnds
                        .Skip(gatewayCount)
                        .ToList();
                    ConnectRoundRobin(extraDeepLooseEnds, gateways);

                    for (int i = 0; i < currentParentsOfFinal.Count; i++)
                    {
                        MutableNode parent = currentParentsOfFinal[i];
                        while (parent.NeighbourIds.Remove(finalDestination.Id))
                        {
                        }

                        MutableNode gateway = gateways[i % gateways.Count];
                        if (!parent.NeighbourIds.Contains(gateway.Id))
                        {
                            parent.NeighbourIds.Add(gateway.Id);
                        }
                    }
                }

                for (int depth = finalDepth - 1; depth >= 0; depth--)
                {
                    ConnectLooseEndsAtDepth(depth, finalDestination.Id);
                }

                EnforceConnectionBounds(finalDestination.Id);
            }
        }

        private static void ConnectRoundRobin(List<MutableNode> sources, List<MutableNode> targets)
        {
            if (sources.Count == 0 || targets.Count == 0)
            {
                return;
            }

            for (int i = 0; i < sources.Count; i++)
            {
                MutableNode source = sources[i];
                MutableNode target = targets[i % targets.Count];
                if (!source.NeighbourIds.Contains(target.Id))
                {
                    source.NeighbourIds.Add(target.Id);
                }
            }
        }

        private void ConnectLooseEndsAtDepth(int depth, int finalNodeId)
        {
            List<MutableNode> looseAtDepth = _mutableNodes
                .Where(node => node.Id != finalNodeId)
                .Where(node => node.Depth == depth)
                .Where(node => node.NeighbourIds.Count == 0)
                .OrderBy(node => node.Id)
                .ToList();

            if (looseAtDepth.Count == 0)
            {
                return;
            }

            List<MutableNode> targets = _mutableNodes
                .Where(node => node.Depth == depth + 1)
                .Where(node => node.Id == finalNodeId || node.NeighbourIds.Count > 0)
                .OrderBy(node => node.Id)
                .ToList();

            if (targets.Count == 0)
            {
                return;
            }

            ConnectRoundRobin(looseAtDepth, targets);
        }

        private void EnforceConnectionBounds(int finalNodeId)
        {
            foreach (MutableNode node in _mutableNodes.OrderByDescending(node => node.Depth).ThenBy(node => node.Id))
            {
                if (node.Id == _startNodeId || node.Id == finalNodeId)
                {
                    continue;
                }

                int minConnections = NON_START_MIN_CONNECTIONS;
                int maxConnections = NON_START_MAX_CONNECTIONS;

                if (node.NeighbourIds.Count > maxConnections)
                {
                    node.NeighbourIds.RemoveRange(maxConnections, node.NeighbourIds.Count - maxConnections);
                }

                if (node.NeighbourIds.Count >= minConnections)
                {
                    continue;
                }

                List<MutableNode> forwardTargets = _mutableNodes
                    .Where(target => target.Depth > node.Depth)
                    .Where(target => target.Id == finalNodeId || target.NeighbourIds.Count > 0)
                    .OrderBy(target => target.Depth)
                    .ThenBy(target => target.Id)
                    .ToList();

                foreach (MutableNode target in forwardTargets)
                {
                    if (node.NeighbourIds.Count >= minConnections || node.NeighbourIds.Count >= maxConnections)
                    {
                        break;
                    }

                    if (node.NeighbourIds.Contains(target.Id))
                    {
                        continue;
                    }

                    node.NeighbourIds.Add(target.Id);
                }
            }
        }

        private NavigationMap CreateMap()
        {
            var frozenNodes = _mutableNodes.ToDictionary(
                node => node.Id,
                node => new NavigationMapNode
                {
                    Id = node.Id,
                    Kind = node.Kind,
                    Depth = node.Depth,
                    NeighbourIds = node.NeighbourIds.ToArray()
                });

            return new NavigationMap
            {
                StartNodeId = _startNodeId,
                NodesById = frozenNodes
            };
        }

        private int AddNode(string kind, int depth)
        {
            var id = _mutableNodes.Count;
            _mutableNodes.Add(new MutableNode(id, kind, depth));
            return id;
        }

        private int GetMinConnectionsForNode(MutableNode node)
        {
            return node.Id == _startNodeId
                ? START_CONNECTIONS
                : NON_START_MIN_CONNECTIONS;
        }

        private int GetMaxConnectionsForNode(MutableNode node)
        {
            return node.Id == _startNodeId
                ? START_CONNECTIONS
                : NON_START_MAX_CONNECTIONS;
        }

        private double GetMergeProbability(int nextLayerWidth, int targetNextLayerWidth)
        {
            if (nextLayerWidth >= targetNextLayerWidth)
            {
                return AT_TARGET_MERGE_PROBABILITY;
            }

            if (nextLayerWidth == targetNextLayerWidth - 1)
            {
                return NEAR_TARGET_MERGE_PROBABILITY;
            }

            return BASE_MERGE_PROBABILITY;
        }

        private void RecoverFromDeadEnd(List<MutableNode> nextLayerNodes)
        {
            MutableNode? parent = _mutableNodes
                .Where(node => node.NeighbourIds.Count < GetMaxConnectionsForNode(node))
                .Where(node => nodeDefinitions[node.Kind].PossibleNeighbours.Any(entry => entry.Value > 0))
                .OrderByDescending(node => node.Depth)
                .ThenBy(node => node.NeighbourIds.Count)
                .FirstOrDefault();

            if (parent is null) return;

            List<KeyValuePair<string, float>> weightedNeighbours = nodeDefinitions[parent.Kind]
                .PossibleNeighbours.Where(entry => entry.Value > 0).ToList();

            if (weightedNeighbours.Count == 0) return;

            string neighbourKind = PickWeightedNeighbour(weightedNeighbours, random);

            if (!nodeDefinitions.ContainsKey(neighbourKind))
            {
                throw new InvalidOperationException($"Node kind '{parent.Kind}' references unknown neighbour kind '{neighbourKind}'.");
            }

            int neighbourId = AddNode(neighbourKind, parent.Depth + 1);
            parent.NeighbourIds.Add(neighbourId);
            nextLayerNodes.Add(_mutableNodes[neighbourId]);
        }

        private int GetTargetNextLayerWidth()
        {
            int remainingSlots = maxNodes - _mutableNodes.Count;
            if (remainingSlots <= 1)
            {
                return 1;
            }

            if (_mutableNodes.Count == 1)
            {
                // First expansion always comes from start, which has exactly one connection.
                return 1;
            }

            int minWidth = 2;
            int configuredPeak = Math.Max(parameters.PeakFrontierWidth, minWidth + 1);
            int heuristicPeak = Math.Max(configuredPeak, Math.Min(configuredPeak + 1, Math.Max(4, maxNodes / 8)));
            int peakWidth = Math.Min(heuristicPeak, remainingSlots);

            double progress = (double)_mutableNodes.Count / Math.Max(1, maxNodes - 1);
            progress = Math.Clamp(progress, 0d, 1d);

            // Triangle envelope: 0 at start/end, 1 in the middle.
            double envelope = 1d - Math.Abs(2d * progress - 1d);
            int desired = (int)Math.Round(minWidth + (peakWidth - minWidth) * envelope);

            if (progress >= 0.90d)
            {
                desired = Math.Min(desired, 2);
            }
            else if (progress >= 0.78d)
            {
                desired = Math.Min(desired, 3);
            }

            return Math.Clamp(desired, minWidth, remainingSlots);
        }
    }

    private sealed class MutableNode(int id, string kind, int depth)
    {
        public int Id { get; } = id;
        public string Kind { get; set; } = kind;
        public int Depth { get; set; } = depth;
        public List<int> NeighbourIds { get; } = [];
    }
}
