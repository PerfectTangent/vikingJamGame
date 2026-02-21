namespace VikingJamGame.Models.GameEvents;

public sealed class GameEventContext
{
    public required PlayerInfo PlayerInfo { get; init; }
    public required GameResources GameResources { get; init; }
    public string? CurrentNodeKind { get; init; }
}
