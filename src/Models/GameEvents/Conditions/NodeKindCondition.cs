namespace VikingJamGame.Models.GameEvents.Conditions;

/// <summary>Passes when the current map node matches the specified kind. Not yet implemented always returns true.</summary>
public sealed record NodeKindCondition(string Kind) : IGameEventCondition
{
    public bool Evaluate(GameEventContext context) => true;
}
