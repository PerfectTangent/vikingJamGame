using System.Linq;
using VikingJamGame.Models;
using VikingJamGame.Models.GameEvents.Commands;
using VikingJamGame.Models.GameEvents.Compilation;
using VikingJamGame.Models.GameEvents.Conditions;
using VikingJamGame.Models.GameEvents.Definitions;
using VikingJamGame.Models.GameEvents.Effects;
using VikingJamGame.Models.GameEvents.Runtime;
using VikingJamGame.Models.GameEvents.Stats;
using VikingJamGame.TemplateUtils;
using VikingJamGame.Tests.TestDoubles;

namespace VikingJamGame.Tests.Models.GameEvents.Compilation;

public sealed class GameEventCompilerTests
{
    [Fact]
    public void Compile_SortsOptionsByOrder_AndSeparatesConditionsFromCosts()
    {
        var definition = new GameEventDefinition
        {
            Id = "event.alpha",
            Name = "Alpha",
            Description = "desc",
            OptionDefinitions =
            [
                new GameEventOptionDefinition
                {
                    DisplayText = "Second",
                    ResolutionText = "Second",
                    Order = 2
                },
                new GameEventOptionDefinition
                {
                    DisplayText = "First",
                    ResolutionText = "First",
                    Order = 1,
                    Condition = "food:1",
                    Cost = "food:3;gold:2"
                }
            ]
        };

        GameEvent compiled = GameEventCompiler.Compile(definition, new RecordingCommandRegistry());

        Assert.Equal([1, 2], compiled.Options.Select(option => option.Order).ToArray());
        GameEventOption first = compiled.Options.First();

        // Condition parsed as visibility condition only
        Assert.Single(first.VisibilityConditions);
        Assert.Contains(first.VisibilityConditions, c => c is StatThresholdCondition { Stat: StatId.Food, MinAmount: 1 });

        // Costs are separate and not merged with conditions
        Assert.Equal(2, first.Costs.Count);
        Assert.Contains(first.Costs, amount => amount is { Stat: StatId.Food, Amount: 3 });
        Assert.Contains(first.Costs, amount => amount is { Stat: StatId.Gold, Amount: 2 });
    }

    [Fact]
    public void Compile_ThrowsWhenOptionOrderIsDuplicated()
    {
        var definition = new GameEventDefinition
        {
            Id = "event.dup",
            Name = "Dup",
            Description = "desc",
            OptionDefinitions =
            [
                new GameEventOptionDefinition
                {
                    DisplayText = "A",
                    ResolutionText = "A",
                    Order = 1
                },
                new GameEventOptionDefinition
                {
                    DisplayText = "B",
                    ResolutionText = "B",
                    Order = 1
                }
            ]
        };

        InvalidOperationException exception = Assert
            .Throws<InvalidOperationException>(() => GameEventCompiler.Compile(definition, new RecordingCommandRegistry()));

        Assert.Contains("duplicate option Order=1", exception.Message);
    }

    [Fact]
    public void Compile_UsesCommandRegistryForCustomCommand()
    {
        var markerCommand = new RecordingCommand();
        var commandRegistry = new RecordingCommandRegistry(markerCommand);
        var definition = new GameEventDefinition
        {
            Id = "event.command",
            Name = "Command",
            Description = "desc",
            OptionDefinitions =
            [
                new GameEventOptionDefinition
                {
                    DisplayText = "Do",
                    ResolutionText = "Done",
                    Order = 1,
                    CustomCommand = "ApplyDebuff:Fear"
                }
            ]
        };

        GameEvent compiled = GameEventCompiler.Compile(definition, commandRegistry);

        Assert.Single(commandRegistry.CreatedCommands);
        Assert.Equal(("ApplyDebuff", "Fear"), commandRegistry.CreatedCommands[0]);
        CustomCommandEffect customEffect = Assert.IsType<CustomCommandEffect>(compiled.Options.Single().Effects.Single());
        Assert.Same(markerCommand, customEffect.Command);
    }

    [Fact]
    public void Compile_HasNoEffectsWhenCustomCommandIsMissing()
    {
        var commandRegistry = new RecordingCommandRegistry();
        var definition = new GameEventDefinition
        {
            Id = "event.noop",
            Name = "Noop",
            Description = "desc",
            OptionDefinitions =
            [
                new GameEventOptionDefinition
                {
                    DisplayText = "Wait",
                    ResolutionText = "Waited",
                    Order = 1
                }
            ]
        };

        GameEvent compiled = GameEventCompiler.Compile(definition, commandRegistry);

        Assert.Empty(commandRegistry.CreatedCommands);
        Assert.Empty(compiled.Options.Single().Effects);
    }

    [Fact]
    public void Compile_ThrowsWhenConditionUsesUnknownStat()
    {
        var definition = new GameEventDefinition
        {
            Id = "event.badstat",
            Name = "Bad Stat",
            Description = "desc",
            OptionDefinitions =
            [
                new GameEventOptionDefinition
                {
                    DisplayText = "X",
                    ResolutionText = "Y",
                    Order = 1,
                    Condition = "mystery:2"
                }
            ]
        };

        InvalidOperationException exception = Assert.
            Throws<InvalidOperationException>(() => GameEventCompiler.Compile(definition, new RecordingCommandRegistry()));

        Assert.Contains("unknown condition key 'mystery' in Condition", exception.Message);
    }

    [Fact]
    public void Compile_ParsesSignedEffectPairs()
    {
        var definition = new GameEventDefinition
        {
            Id = "event.effects",
            Name = "Effects",
            Description = "desc",
            OptionDefinitions =
            [
                new GameEventOptionDefinition
                {
                    DisplayText = "Plunder",
                    ResolutionText = "Plundered",
                    Order = 1,
                    Effect = "food:+5;honor:-1"
                }
            ]
        };

        GameEvent compiled = GameEventCompiler.Compile(definition, new RecordingCommandRegistry());

        GameEventOption option = compiled.Options.Single();
        Assert.Equal(2, option.Effects.Count);
        Assert.Contains(option.Effects, e => e is StatChangeEffect { Stat: StatId.Food, Amount: 5 });
        Assert.Contains(option.Effects, e => e is StatChangeEffect { Stat: StatId.Honor, Amount: -1 });
    }

    [Fact]
    public void Compile_RendersTemplatedTextWithPronouns()
    {
        var definition = new GameEventDefinition
        {
            Id = "event.templated",
            Name = "{Title}",
            Description = "{He} sailed into the storm.",
            OptionDefinitions =
            [
                new GameEventOptionDefinition
                {
                    DisplayText = "Follow {him}",
                    ResolutionText = "{His} legend grows.",
                    Order = 1
                }
            ]
        };

        var templateContext = new GameEventTemplateContext(
            BirthChoice.Girl,
            "Astrid",
            "Stormborn");

        GameEvent compiled = GameEventCompiler.Compile(
            definition,
            new RecordingCommandRegistry(),
            templateContext);

        Assert.Equal("Stormborn", compiled.Name);
        Assert.Equal("She sailed into the storm.", compiled.Description);
        Assert.Equal("Follow her", compiled.Options[0].DisplayText);
        Assert.Equal("Her legend grows.", compiled.Options[0].ResolutionText);
    }
}
