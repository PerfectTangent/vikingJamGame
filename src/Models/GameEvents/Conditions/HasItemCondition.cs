namespace VikingJamGame.Models.GameEvents.Conditions;

/// <summary>Passes when the player has the specified item. Not yet implemented always returns true.</summary>
public sealed record HasItemCondition(string ItemId) : IGameEventCondition
{
    public bool Evaluate(GameEventContext context) => true;
}
