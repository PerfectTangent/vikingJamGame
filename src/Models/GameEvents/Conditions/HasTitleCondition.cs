namespace VikingJamGame.Models.GameEvents.Conditions;

/// <summary>Passes when the player holds the specified title. Not yet implemented always returns true.</summary>
public sealed record HasTitleCondition(string Title) : IGameEventCondition
{
    public bool Evaluate(GameEventContext context) => true;
}
