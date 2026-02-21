using VikingJamGame.Models.GameEvents.Stats;

namespace VikingJamGame.Models.GameEvents.Effects;

/// <summary>
/// Adds or removes a stat amount. Positive = gain, negative = loss.
/// </summary>
public sealed record StatChangeEffect(StatId Stat, int Amount) : IGameEventEffect
{
    public void Apply(GameEventContext context)
    {
        if (Amount >= 0)
        {
            GameStateStats.Add(context.PlayerInfo, context.GameResources, Stat, Amount);
        }
        else
        {
            GameStateStats.Spend(context.PlayerInfo, context.GameResources, Stat, -Amount);
        }
    }
}
