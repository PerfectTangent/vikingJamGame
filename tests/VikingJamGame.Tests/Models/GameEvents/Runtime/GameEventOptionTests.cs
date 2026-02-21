using VikingJamGame.Models;
using VikingJamGame.Models.GameEvents;
using VikingJamGame.Models.GameEvents.Conditions;
using VikingJamGame.Models.GameEvents.Effects;
using VikingJamGame.Models.GameEvents.Runtime;
using VikingJamGame.Models.GameEvents.Stats;
using VikingJamGame.Tests.TestDoubles;

namespace VikingJamGame.Tests.Models.GameEvents.Runtime;

public sealed class GameEventOptionTests
{
    private readonly GameEventEvaluator _evaluator = new();

    [Fact]
    public void IsVisible_ReturnsFalseWhenConditionsAreNotMet()
    {
        var playerInfo = new PlayerInfo();
        var gameResources = new GameResources();
        gameResources.AddFood(1);

        GameEventOption option = CreateOption(
            visibilityConditions: [new StatThresholdCondition(StatId.Food, 2)],
            costs: []);

        var context = CreateContext(playerInfo, gameResources);

        Assert.False(_evaluator.IsVisible(option, context));
    }

    [Fact]
    public void IsAffordable_ReturnsFalseWhenCostsCannotBePaid()
    {
        var playerInfo = new PlayerInfo();
        var gameResources = new GameResources();
        gameResources.AddGold(1);

        GameEventOption option = CreateOption(
            visibilityConditions: [],
            costs: [new StatAmount(StatId.Gold, 2)]);

        var context = CreateContext(playerInfo, gameResources);

        Assert.False(_evaluator.IsAffordable(option, context));
    }

    [Fact]
    public void Apply_PaysCostsAndRunsEffects()
    {
        var playerInfo = new PlayerInfo();
        var gameResources = new GameResources();
        gameResources.AddFood(5);
        gameResources.AddGold(4);
        var command = new RecordingCommand();

        GameEventOption option = CreateOption(
            visibilityConditions: [],
            costs: [new StatAmount(StatId.Food, 2), new StatAmount(StatId.Gold, 1)],
            effects: [new CustomCommandEffect(command)]);

        _evaluator.Apply(option, CreateContext(playerInfo, gameResources));

        Assert.Equal(3, gameResources.Food);
        Assert.Equal(3, gameResources.Gold);
        Assert.Equal(1, command.ExecuteCalls);
    }

    private static GameEventOption CreateOption(
        IReadOnlyList<IGameEventCondition> visibilityConditions,
        IReadOnlyList<StatAmount> costs,
        IReadOnlyList<IGameEventEffect>? effects = null) =>
        new()
        {
            DisplayText = "Option",
            ResolutionText = "Resolved",
            Order = 1,
            DisplayCosts = true,
            VisibilityConditions = visibilityConditions,
            Costs = costs,
            Effects = effects ?? []
        };

    private static GameEventContext CreateContext(PlayerInfo playerInfo, GameResources gameResources) =>
        new() { PlayerInfo = playerInfo, GameResources = gameResources };
}
