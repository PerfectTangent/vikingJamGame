using VikingJamGame.Models.GameEvents.Stats;

namespace VikingJamGame.Models.GameEvents.Conditions;

public sealed record StatThresholdCondition(StatId Stat, int MinAmount) : IGameEventCondition
{
    public bool Evaluate(GameEventContext context) =>
        GameStateStats.Get(context.PlayerInfo, context.GameResources, Stat) >= MinAmount;
}
