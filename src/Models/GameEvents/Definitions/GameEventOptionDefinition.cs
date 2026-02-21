namespace VikingJamGame.Models.GameEvents.Definitions;

public sealed record GameEventOptionDefinition
{
    public required string DisplayText { get; init; }
    public required string ResolutionText { get; init; }
    public required int Order { get; init; }

    // format: "food:3;gold:10" meaning required >=
    public string? Condition { get; init; }

    // format: "food:3;honor:1". The amount will be discounted from game state
    public string? Cost { get; init; }
    public bool DisplayCosts { get; init; } = false;

    // format: "food:+3;honor:-1". Signed amounts: positive = gain, negative = loss.
    public string? Effect { get; init; }

    // format: "ApplyDebuff:Famine"
    public string? CustomCommand { get; init; }

    // ID of the next event in the chain when this option is chosen.
    public string? NextEventId { get; init; }
}
