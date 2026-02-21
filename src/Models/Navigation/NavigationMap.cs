using System.Collections.Generic;

namespace VikingJamGame.Models.Navigation;

public sealed class NavigationMap
{
    public required int StartNodeId { get; init; }
    public int? EndNodeId { get; init; }
    public required IReadOnlyDictionary<int, NavigationMapNode> NodesById { get; init; }

    public NavigationMapNode StartNode => NodesById[StartNodeId];

    /// <summary>
    /// BFS traversing only forward links (NeighbourIds) up to maxDistance hops.
    /// </summary>
    public HashSet<int> GetNodesWithinDistance(int originNodeId, int maxDistance)
    {
        var result = new HashSet<int> { originNodeId };
        var frontier = new Queue<(int nodeId, int distance)>();
        frontier.Enqueue((originNodeId, 0));

        while (frontier.Count > 0)
        {
            var (nodeId, distance) = frontier.Dequeue();
            if (distance >= maxDistance) continue;
            if (!NodesById.TryGetValue(nodeId, out var node)) continue;

            foreach (int neighbourId in node.NeighbourIds)
            {
                if (result.Add(neighbourId))
                {
                    frontier.Enqueue((neighbourId, distance + 1));
                }
            }
        }

        return result;
    }
}
