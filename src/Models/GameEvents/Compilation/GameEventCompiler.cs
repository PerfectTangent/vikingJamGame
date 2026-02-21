using System;
using System.Collections.Generic;
using System.Linq;
using VikingJamGame.Models.GameEvents.Commands;
using VikingJamGame.Models.GameEvents.Conditions;
using VikingJamGame.Models.GameEvents.Definitions;
using VikingJamGame.Models.GameEvents.Effects;
using VikingJamGame.Models.GameEvents.Runtime;

namespace VikingJamGame.Models.GameEvents.Compilation;

public static class GameEventCompiler
{
    public static GameEvent Compile(
        GameEventDefinition definition,
        ICommandRegistry commands,
        GameEventTemplateContext? templateContext = null)
    {
        if (string.IsNullOrWhiteSpace(definition.Id))
        {
            throw new InvalidOperationException("Event Id is required.");
        }

        var options = definition.OptionDefinitions
            .Select(optionDefinition => CompileOption(
                definition.Id,
                optionDefinition,
                commands,
                templateContext))
            .OrderBy(option => option.Order)
            .ToList();

        var duplicateOrder = options
            .GroupBy(option => option.Order)
            .FirstOrDefault(group => group.Count() > 1);

        if (duplicateOrder is not null)
        {
            throw new InvalidOperationException(
                $"Event '{definition.Id}' has duplicate option Order={duplicateOrder.Key}.");
        }

        return new GameEvent
        {
            Id = definition.Id,
            Name = GameEventDefinitionParser.ParseTemplatedText(definition.Name, templateContext),
            Description = GameEventDefinitionParser.ParseTemplatedText(definition.Description, templateContext),
            Options = options
        };
    }

    private static GameEventOption CompileOption(
        string eventId,
        GameEventOptionDefinition optionDefinition,
        ICommandRegistry commands,
        GameEventTemplateContext? templateContext)
    {
        var visibilityConditions = GameEventDefinitionParser.ParseConditionPairs(
            eventId,
            optionDefinition.Order,
            "Condition",
            optionDefinition.Condition);

        var costs = GameEventDefinitionParser.ParsePairs(
            eventId,
            optionDefinition.Order,
            "Cost",
            optionDefinition.Cost);

        List<IGameEventEffect> effects = GameEventDefinitionParser.ParseEffectPairs(
                eventId,
                optionDefinition.Order,
                "Effect",
                optionDefinition.Effect)
            .Cast<IGameEventEffect>()
            .ToList();

        var customCommand = GameEventDefinitionParser.ParseCommand(
            eventId,
            optionDefinition.Order,
            optionDefinition.CustomCommand,
            commands);

        if (customCommand is not null)
        {
            effects.Add(customCommand);
        }

        return new GameEventOption
        {
            DisplayText = GameEventDefinitionParser.ParseTemplatedText(
                optionDefinition.DisplayText,
                templateContext),
            ResolutionText = GameEventDefinitionParser.ParseTemplatedText(
                optionDefinition.ResolutionText,
                templateContext),
            Order = optionDefinition.Order,
            DisplayCosts = optionDefinition.DisplayCosts,
            VisibilityConditions = visibilityConditions,
            Costs = costs,
            Effects = effects,
            NextEventId = NormalizeNextEventId(optionDefinition.NextEventId)
        };
    }

    private static string? NormalizeNextEventId(string? nextEventId) =>
        string.IsNullOrWhiteSpace(nextEventId) ? null : nextEventId.Trim();
}
