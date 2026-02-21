using System;
using System.Collections.Generic;
using System.Linq;

namespace VikingJamGame.Models.Navigation;

public sealed class NavigationSession
{
    private readonly NavigationMap _map;
    private readonly IReadOnlyDictionary<int, IReadOnlyList<int>> _parentIdsByNodeId;

    public int CurrentNodeId { get; private set; }
    public IReadOnlySet<int> VisitedNodeIds => _visitedNodeIds;
    private readonly HashSet<int> _visitedNodeIds = [];

    public NavigationMapNode CurrentNode => _map.NodesById[CurrentNodeId];

    public int ConsecutiveNodesOfSameType { get; private set; }

    public NavigationSession(NavigationMap map, int? startNodeId = null)
    {
        ArgumentNullException.ThrowIfNull(map);
        _map = map;

        ValidateGraph(_map);

        int initialNodeId = startNodeId ?? _map.StartNodeId;
        if (!_map.NodesById.ContainsKey(initialNodeId))
        {
            throw new InvalidOperationException(
                $"Start node id '{initialNodeId}' is not present in map.");
        }

        CurrentNodeId = initialNodeId;
        _visitedNodeIds.Add(initialNodeId);
        ConsecutiveNodesOfSameType = 1;
        _parentIdsByNodeId = BuildParentIdsByNodeId(_map);
    }

    public IReadOnlyList<int> GetForwardNodeIds() => CurrentNode.NeighbourIds;

    public IReadOnlyList<int> GetBackwardNodeIds()
    {
        return _parentIdsByNodeId.TryGetValue(CurrentNodeId, out IReadOnlyList<int>? parentIds)
            ? parentIds
            : [];
    }

    public IReadOnlyList<int> GetAvailableMoveNodeIds()
    {
        var result = new List<int>(GetForwardNodeIds().Count + GetBackwardNodeIds().Count);
        var seen = new HashSet<int>();

        foreach (int nodeId in GetForwardNodeIds())
        {
            if (seen.Add(nodeId))
            {
                result.Add(nodeId);
            }
        }

        foreach (int nodeId in GetBackwardNodeIds())
        {
            if (seen.Add(nodeId))
            {
                result.Add(nodeId);
            }
        }

        return result;
    }

    public bool IsForwardMove(int nodeId) => CurrentNode.NeighbourIds.Contains(nodeId);

    public bool IsBackwardMove(int nodeId) => GetBackwardNodeIds().Contains(nodeId);

    public bool CanMoveTo(int nodeId) => IsForwardMove(nodeId) || IsBackwardMove(nodeId);

    public bool TryMoveTo(int nodeId)
    {
        if (!CanMoveTo(nodeId))
        {
            return false;
        }

        string currentKind = CurrentNode.Kind;
        string destinationKind = _map.NodesById[nodeId].Kind;

        CurrentNodeId = nodeId;
        _visitedNodeIds.Add(nodeId);
        ConsecutiveNodesOfSameType = string.Equals(
            currentKind,
            destinationKind,
            StringComparison.Ordinal)
            ? ConsecutiveNodesOfSameType + 1
            : 1;
        return true;
    }

    public void MoveTo(int nodeId)
    {
        if (!TryMoveTo(nodeId))
        {
            throw new InvalidOperationException(
                $"Node '{nodeId}' is not reachable from current node '{CurrentNodeId}'.");
        }
    }

    private static void ValidateGraph(NavigationMap map)
    {
        if (!map.NodesById.ContainsKey(map.StartNodeId))
        {
            throw new InvalidOperationException(
                $"Map start node id '{map.StartNodeId}' is not present in map.");
        }

        foreach (NavigationMapNode node in map.NodesById.Values)
        {
            foreach (int neighbourId in node.NeighbourIds)
            {
                if (!map.NodesById.ContainsKey(neighbourId))
                {
                    throw new InvalidOperationException(
                        $"Node '{node.Id}' references missing neighbour id '{neighbourId}'.");
                }
            }
        }
    }

    private static IReadOnlyDictionary<int, IReadOnlyList<int>> BuildParentIdsByNodeId(NavigationMap map)
    {
        var parentIdsByNodeId = map.NodesById.Keys
            .ToDictionary(nodeId => nodeId, _ => new List<int>());

        foreach (NavigationMapNode node in map.NodesById.Values)
        {
            foreach (int neighbourId in node.NeighbourIds)
            {
                List<int> parentIds = parentIdsByNodeId[neighbourId];
                if (!parentIds.Contains(node.Id))
                {
                    parentIds.Add(node.Id);
                }
            }
        }

        return parentIdsByNodeId.ToDictionary(
            pair => pair.Key,
            pair =>
            {
                pair.Value.Sort();
                return (IReadOnlyList<int>)pair.Value.ToArray();
            });
    }
}
