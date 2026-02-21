using System;
using Chickensoft.AutoInject;
using Chickensoft.Introspection;
using Godot;
using VikingJamGame.GameLogic;
using VikingJamGame.GameLogic.Nodes;
using VikingJamGame.Models;
using VikingJamGame.TemplateUtils;

namespace VikingJamGame;

[GlobalClass][Meta(typeof(IProvider))]
public partial class Game: Node2D,
    IProvide<PlayerInfo>, IProvide<GameResources>, IProvide<GameStateMachine>
{
    [Export] private GodotSceneSwitcher SceneSwitcher { get; set; } = null!;
    [Export] private GameStateMachine.InitialState ForceInitialState { get; set; } =
        GameStateMachine.InitialState.Prologue;


    private readonly PlayerInfo _playerInfo = new();
    private readonly GameResources _gameResources = new();
    private readonly GameStateMachine _gameStateMachine = new();
    PlayerInfo IProvide<PlayerInfo>.Value() => _playerInfo;
    GameResources IProvide<GameResources>.Value() => _gameResources;
    GameStateMachine IProvide<GameStateMachine>.Value() => _gameStateMachine;
    
    public override void _Ready()
    {
        if (ForceInitialState != GameStateMachine.InitialState.Prologue)
        {
            EnsurePlayerDataInitialized();
        }

        _gameStateMachine.SetInitialState(ForceInitialState);
        _gameStateMachine.Start();
        this.Provide();
    }

    private void EnsurePlayerDataInitialized()
    {
        if (!string.IsNullOrEmpty(_playerInfo.Name)) return;

        GameDataWrapper data = InitialResourcesFactory.FromPrologueData(
            BirthChoice.Boy, "Debug Viking");

        _playerInfo.SetInitialInfo(
            data.PlayerInfo.Name,
            data.PlayerInfo.BirthChoice,
            data.PlayerInfo.Title,
            data.PlayerInfo.Strength, data.PlayerInfo.MaxStrength,
            data.PlayerInfo.Honor, data.PlayerInfo.MaxHonor,
            data.PlayerInfo.Feats, data.PlayerInfo.MaxFeats);

        _gameResources.SetInitialResources(
            data.GameResources.Population,
            data.GameResources.Food,
            data.GameResources.Gold);
    }

    public override void _Notification(int what) => this.Notify(what);
}
