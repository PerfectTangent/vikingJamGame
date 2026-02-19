using System.Collections.Generic;

namespace VikingJamGame.Models.Navigation;

public sealed class NavigationMap
{
    public required int StartNodeId { get; init; }
    public required IReadOnlyDictionary<int, NavigationMapNode> NodesById { get; init; }

    public NavigationMapNode StartNode => NodesById[StartNodeId];
}
