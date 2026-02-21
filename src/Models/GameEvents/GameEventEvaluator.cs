using System.Linq;
using VikingJamGame.Models.GameEvents.Runtime;
using VikingJamGame.Models.GameEvents.Stats;

namespace VikingJamGame.Models.GameEvents;

public sealed class GameEventEvaluator
{
    /// <summary>Returns true if all visibility conditions on the option pass.</summary>
    public bool IsVisible(GameEventOption option, GameEventContext context) =>
        option.VisibilityConditions.All(c => c.Evaluate(context));

    /// <summary>Returns true if the player can pay all costs for this option.</summary>
    public bool IsAffordable(GameEventOption option, GameEventContext context) =>
        GameStateStats.CanPayAll(context.PlayerInfo, context.GameResources, option.Costs);

    /// <summary>Pays costs and applies all effects for a single resolved option.</summary>
    public void Apply(GameEventOption option, GameEventContext context)
    {
        GameStateStats.PayAll(context.PlayerInfo, context.GameResources, option.Costs);

        foreach (var effect in option.Effects)
        {
            effect.Apply(context);
        }
    }

    /// <summary>Applies all options collected during an event chain in resolution order.</summary>
    public void ApplyAll(EventResults results, GameEventContext context)
    {
        foreach (var option in results.ResolvedOptions)
        {
            Apply(option, context);
        }
    }
}
