using System.Collections.Generic;

namespace VikingJamGame.Models.Navigation;

public sealed record NavigationMapNode
{
    public required int Id { get; init; }
    public required string Kind { get; init; }
    public int Depth { get; init; }
    public required IReadOnlyList<int> NeighbourIds { get; init; }
}
