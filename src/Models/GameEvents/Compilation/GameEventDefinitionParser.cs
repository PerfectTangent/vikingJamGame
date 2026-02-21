using System;
using System.Collections.Generic;
using System.Globalization;
using VikingJamGame.Models.GameEvents.Commands;
using VikingJamGame.Models.GameEvents.Conditions;
using VikingJamGame.Models.GameEvents.Effects;
using VikingJamGame.Models.GameEvents.Stats;
using VikingJamGame.TemplateUtils;

namespace VikingJamGame.Models.GameEvents.Compilation;

internal static class GameEventDefinitionParser
{
    public static string ParseTemplatedText(
        string text,
        GameEventTemplateContext? templateContext = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(text);

        GameEventTemplateContext context = templateContext ?? GameEventTemplateContext.Default;
        string name = string.IsNullOrWhiteSpace(context.Name) ? "{Name}" : context.Name;
        string title = string.IsNullOrWhiteSpace(context.Title) ? "{Title}" : context.Title;

        return Template.Render(text, context.BirthChoice, name, title);
    }

    /// <summary>
    /// Parses "stat:amount" pairs where amounts must be non-negative. Used for costs.
    /// </summary>
    public static IReadOnlyList<StatAmount> ParsePairs(
        string eventId,
        int order,
        string fieldName,
        string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return [];
        }

        var segments = text.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var parsed = new List<StatAmount>(segments.Length);

        foreach (var segment in segments)
        {
            var (stat, amount) = ParseStatPair(eventId, order, fieldName, segment, allowNegative: false);
            parsed.Add(new StatAmount(stat, amount));
        }

        return parsed;
    }

    /// <summary>
    /// Parses "key:value" condition pairs. Dispatches to the appropriate condition type by key prefix.
    /// Stat names (food, gold, ...) → StatThresholdCondition.
    /// "item", "title", "node_kind" → respective condition stubs.
    /// </summary>
    public static IReadOnlyList<IGameEventCondition> ParseConditionPairs(
        string eventId,
        int order,
        string fieldName,
        string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return Array.Empty<IGameEventCondition>();
        }

        var segments = text.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var parsed = new List<IGameEventCondition>(segments.Length);

        foreach (var segment in segments)
        {
            var pair = segment.Split(':', 2, StringSplitOptions.TrimEntries);
            if (pair.Length != 2)
            {
                throw new InvalidOperationException(
                    $"Event '{eventId}' option {order}: bad {fieldName} segment '{segment}'. Expected key:value.");
            }

            parsed.Add(ParseCondition(eventId, order, fieldName, pair[0], pair[1]));
        }

        return parsed;
    }

    private static IGameEventCondition ParseCondition(
        string eventId, int order, string fieldName, string key, string value)
    {
        switch (key.Trim().ToLowerInvariant())
        {
            case "item":
                return new HasItemCondition(value.Trim());
            case "title":
                return new HasTitleCondition(value.Trim());
            case "node_kind":
                return new NodeKindCondition(value.Trim());
            default:
                if (TryParseStatId(key, out var stat))
                {
                    if (!int.TryParse(value.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var amount)
                        || amount < 0)
                    {
                        throw new InvalidOperationException(
                            $"Event '{eventId}' option {order}: bad amount '{value}' in {fieldName}.");
                    }
                    return new StatThresholdCondition(stat, amount);
                }
                throw new InvalidOperationException(
                    $"Event '{eventId}' option {order}: unknown condition key '{key}' in {fieldName}.");
        }
    }

    /// <summary>
    /// Parses signed "stat:+amount" or "stat:-amount" pairs as effects. Positive = gain, negative = loss.
    /// </summary>
    public static IReadOnlyList<StatChangeEffect> ParseEffectPairs(
        string eventId,
        int order,
        string fieldName,
        string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return Array.Empty<StatChangeEffect>();
        }

        var segments = text.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var parsed = new List<StatChangeEffect>(segments.Length);

        foreach (var segment in segments)
        {
            var (stat, amount) = ParseStatPair(eventId, order, fieldName, segment, allowNegative: true);
            parsed.Add(new StatChangeEffect(stat, amount));
        }

        return parsed;
    }

    /// <summary>
    /// Parses a custom command string into a <see cref="CustomCommandEffect"/>.
    /// Returns null when no custom command is defined.
    /// </summary>
    public static CustomCommandEffect? ParseCommand(
        string eventId,
        int order,
        string? customCommand,
        ICommandRegistry commands)
    {
        if (string.IsNullOrWhiteSpace(customCommand))
        {
            return null;
        }

        var parts = customCommand.Split(':', 2, StringSplitOptions.TrimEntries);
        var name = parts[0];
        var arg = parts.Length == 2 ? parts[1] : null;

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new InvalidOperationException($"Event '{eventId}' option {order}: empty CustomCommand.");
        }

        return new CustomCommandEffect(commands.Create(name, arg));
    }

    private static (StatId stat, int amount) ParseStatPair(
        string eventId,
        int order,
        string fieldName,
        string segment,
        bool allowNegative)
    {
        var pair = segment.Split(':', 2, StringSplitOptions.TrimEntries);
        if (pair.Length != 2)
        {
            throw new InvalidOperationException(
                $"Event '{eventId}' option {order}: bad {fieldName} segment '{segment}'. Expected key:value.");
        }

        if (!TryParseStatId(pair[0], out var stat))
        {
            throw new InvalidOperationException(
                $"Event '{eventId}' option {order}: unknown stat '{pair[0]}' in {fieldName}.");
        }

        var valueStr = pair[1].TrimStart('+');
        if (!int.TryParse(valueStr, NumberStyles.Integer, CultureInfo.InvariantCulture, out var amount))
        {
            throw new InvalidOperationException(
                $"Event '{eventId}' option {order}: bad amount '{pair[1]}' in {fieldName}.");
        }

        if (!allowNegative && amount < 0)
        {
            throw new InvalidOperationException(
                $"Event '{eventId}' option {order}: negative amount '{amount}' in {fieldName}.");
        }

        return (stat, amount);
    }

    private static bool TryParseStatId(string key, out StatId id)
    {
        switch (key.Trim().ToLowerInvariant())
        {
            case "population":
                id = StatId.Population;
                return true;
            case "food":
                id = StatId.Food;
                return true;
            case "gold":
                id = StatId.Gold;
                return true;
            case "strength":
                id = StatId.Strength;
                return true;
            case "honor":
                id = StatId.Honor;
                return true;
            case "feats":
                id = StatId.Feats;
                return true;
            default:
                id = default;
                return false;
        }
    }
}
