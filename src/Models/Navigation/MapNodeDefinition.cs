using System.Collections.Generic;

namespace VikingJamGame.Models.Navigation;

public record MapNodeDefinition
{
    public string Kind { get; init; } = "";
    // kind of node and weight, from 0 to 1
    public Dictionary<string, float> PossibleNeighbours { get; init; } = new();

    public string? ForcedFirstEvent { get; init; }
    public List<string> EventsPool { get; set; } = [];

    // name of the image and extension. Path is known
    public string Art { get; set; } = "";
}