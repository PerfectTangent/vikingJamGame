using Chickensoft.AutoInject;
using Chickensoft.Introspection;
using Godot;
using VikingJamGame.GameLogic.Nodes;

namespace VikingJamGame.GameLogic;

[GlobalClass]
[Meta(typeof(IProvider))]
public partial class PlayingScript : Node2D,
    IProvide<GameLoopMachine>, IProvide<GodotMapGenerator>, IProvide<GodotMapNodeRepository>,
    IProvide<ToolTipHandler>, IProvide<GodotNavigationSession>, IProvide<Camera2D>, IProvide<CameraController>
{
    [Export] private GodotMapGenerator Generator { get; set; } = null!;
    [Export] private GodotMapLinkRenderer LinkRenderer { get; set; } = null!;
    [Export] private GodotNavigationSession NavigationSession { get; set; } = null!;
    [Export] private GodotMapNodeRepository NodeRepository { get; set; } = null!;
    [Export] private ToolTipHandler ToolTipHandler { get; set; } = null!;
    [Export] private Camera2D Camera { get; set; } = null!;
    [Export] private CameraController CameraController { get; set; } = null!;

    private readonly GameLoopMachine _machine = new();

    GameLoopMachine IProvide<GameLoopMachine>.Value() => _machine;
    GodotMapGenerator IProvide<GodotMapGenerator>.Value() => Generator;
    GodotMapNodeRepository IProvide<GodotMapNodeRepository>.Value() => NodeRepository;
    ToolTipHandler IProvide<ToolTipHandler>.Value() => ToolTipHandler;
    GodotNavigationSession IProvide<GodotNavigationSession>.Value() => NavigationSession;
    Camera2D IProvide<Camera2D>.Value() => Camera;
    CameraController IProvide<CameraController>.Value() => CameraController;

    public void OnProvided()
    {
        _machine.Set(Generator);
        _machine.Set(LinkRenderer);
        _machine.Set(NavigationSession);
        _machine.Set(NodeRepository);
        _machine.Set(CameraController);
        _machine.Start();
    }

    public override void _Ready() => this.Provide();
    public override void _Notification(int what) => this.Notify(what);
}