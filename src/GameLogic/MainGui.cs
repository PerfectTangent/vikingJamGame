using Chickensoft.AutoInject;
using Chickensoft.Introspection;
using Godot;
using VikingJamGame.GameLogic.Nodes;
using VikingJamGame.Models;

namespace VikingJamGame.GameLogic;

[GlobalClass][Meta(typeof(IAutoNode))]
public partial class MainGui : CanvasLayer
{
    [Dependency] private GameResources GameResources => this.DependOn<GameResources>();
    [Dependency] private PlayerInfo PlayerInfo => this.DependOn<PlayerInfo>();
    [Dependency] private ToolTipHandler ToolTipHandler => this.DependOn<ToolTipHandler>();
    [Dependency] private GodotNavigationSession Navigation => this.DependOn<GodotNavigationSession>();
    [Dependency] private GodotMapGenerator MapGenerator => this.DependOn<GodotMapGenerator>();

    [ExportGroup("Topbar")]
    [ExportCategory("Labels")]
    [Export] private Label PopulationLabel { get; set; } = null!;
    [Export] private Label FoodLabel { get; set; } = null!;
    [Export] private Label CoinLabel { get; set; } = null!;

    [ExportCategory("Containers for tooltips")]
    [Export] private HBoxContainer PopulationContainer { get; set; } = null!;
    [Export] private HBoxContainer FoodContainer { get; set; } = null!;
    [Export] private HBoxContainer CoinContainer { get; set; } = null!;

    [ExportGroup("Player info")]
    [ExportCategory("Labels")]
    [Export] private Label NameLabel { get; set; } = null!;
    [Export] private Label TitleLabel { get; set; } = null!;
    [ExportCategory("Containers")]
    [Export] private VBoxContainer NameContainer { get; set; } = null!;

    [ExportGroup("Stats")]
    [ExportCategory("Bars")]
    [Export] private ProgressBar StrBar { get; set; } = null!;
    [Export] private ProgressBar HonorBar { get; set; } = null!;
    [Export] private ProgressBar FeatsBar { get; set; } = null!;

    [ExportCategory("Value Labels")]
    [Export] private Label StrValue { get; set; } = null!;
    [Export] private Label HonorValue { get; set; } = null!;
    [Export] private Label FeatsValue { get; set; } = null!;

    private bool _resolved;

    private void OnNameClicked(InputEvent @event)
    {
        if (@event is not InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left })
        {
            return;
        }

        if (!MapGenerator.TryGetVisualNode(Navigation.CurrentNodeId, out Node2D visualNode))
        {
            return;
        }

        Camera2D camera = GetViewport().GetCamera2D();
        Tween tween = CreateTween();
        tween.TweenProperty(camera, "global_position",  visualNode.GlobalPosition, 1d)
            .SetEase(Tween.EaseType.InOut)
            .SetTrans(Tween.TransitionType.Sine);
    }

    private Vector2 GetMousePositionWithOffset() =>
        new(GetViewport().GetMousePosition().X, GetViewport().GetMousePosition().Y + 50);

    private void OnHoveringPop() => ToolTipHandler.SetToolTip("Population", GetMousePositionWithOffset());
    private void OnHoveringFood() => ToolTipHandler.SetToolTip("Food", GetMousePositionWithOffset());
    private void OnHoveringCoin() => ToolTipHandler.SetToolTip("Gold", GetMousePositionWithOffset());

    private void OnExitHover() => ToolTipHandler.ClearToolTip();

    private void UpdatedValues()
    {
        if (!_resolved) return;

        PopulationLabel.Text = GameResources.Population.ToString();
        FoodLabel.Text = GameResources.Food.ToString();
        CoinLabel.Text = GameResources.Gold.ToString();
        NameLabel.Text = PlayerInfo.Name;
        TitleLabel.Text = PlayerInfo.Title;

        StrBar.MaxValue = PlayerInfo.MaxStrength;
        StrBar.Value = PlayerInfo.Strength;
        HonorBar.MaxValue = PlayerInfo.MaxHonor;
        HonorBar.Value = PlayerInfo.Honor;
        FeatsBar.MaxValue = PlayerInfo.MaxFeats;
        FeatsBar.Value = PlayerInfo.Feats;
        StrValue.Text = $"{PlayerInfo.Strength}/{PlayerInfo.MaxStrength}";
        HonorValue.Text = $"{PlayerInfo.Honor}/{PlayerInfo.MaxHonor}";
        FeatsValue.Text = $"{PlayerInfo.Feats}/{PlayerInfo.MaxFeats}";
    }

    public void OnResolved()
    {
        _resolved = true;
        UpdatedValues();

        GameResources.GameResourcesChanged += UpdatedValues;
        PlayerInfo.PlayerInfoChanged += UpdatedValues;
        PopulationContainer.MouseEntered += OnHoveringPop;
        FoodContainer.MouseEntered += OnHoveringFood;
        CoinContainer.MouseEntered += OnHoveringCoin;
        PopulationContainer.MouseExited += OnExitHover;
        FoodContainer.MouseExited += OnExitHover;
        CoinContainer.MouseExited += OnExitHover;
        NameContainer.GuiInput += OnNameClicked;
    }

    public override void _ExitTree()
    {
        GameResources.GameResourcesChanged -= UpdatedValues;
        PlayerInfo.PlayerInfoChanged -= UpdatedValues;
        PopulationContainer.MouseEntered -= OnHoveringPop;
        FoodContainer.MouseEntered -= OnHoveringFood;
        CoinContainer.MouseEntered -= OnHoveringCoin;
        PopulationContainer.MouseExited -= OnExitHover;
        FoodContainer.MouseExited -= OnExitHover;
        CoinContainer.MouseExited -= OnExitHover;
        NameContainer.GuiInput -= OnNameClicked;
    }

    public override void _Notification(int what) => this.Notify(what);
}
