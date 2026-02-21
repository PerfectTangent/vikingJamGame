using System.Collections.Generic;
using VikingJamGame.Models.GameEvents.Conditions;
using VikingJamGame.Models.GameEvents.Effects;
using VikingJamGame.Models.GameEvents.Stats;

namespace VikingJamGame.Models.GameEvents.Runtime;

public sealed class GameEventOption
{
    public required string DisplayText { get; init; }
    public required string ResolutionText { get; init; }
    public required int Order { get; init; }
    public required bool DisplayCosts { get; init; }

    /// <summary>All must pass for this option to be visible.</summary>
    public required IReadOnlyList<IGameEventCondition> VisibilityConditions { get; init; }

    /// <summary>Resource costs: must be payable for the option to be selectable; deducted on resolve.</summary>
    public required IReadOnlyList<StatAmount> Costs { get; init; }

    /// <summary>Applied after costs are paid when this option is resolved.</summary>
    public required IReadOnlyList<IGameEventEffect> Effects { get; init; }

    public string? NextEventId { get; init; }
}
