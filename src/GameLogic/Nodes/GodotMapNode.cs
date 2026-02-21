using Chickensoft.AutoInject;
using Chickensoft.Introspection;
using Godot;
using VikingJamGame.Models.Navigation;

namespace VikingJamGame.GameLogic.Nodes;

[GlobalClass][Meta(typeof(IAutoNode))]
public partial class GodotMapNode : Node2D
{

    private const string UNKNOWN_LABEL_TEXT = "Unknown";

    [Export] private Sprite2D Sprite { get; set; } = null!;
    [Export] private Label Label { get; set; } = null!;
    [Export] private Texture2D UnknownTexture { get; set; } = null!;
    [Export] private Control InternalControl { get; set; } = null!;
    [Export] private Marker2D ToolTipPosition { get; set; } = null!;
    
    [Dependency] private ToolTipHandler ToolTipHandler => this.DependOn<ToolTipHandler>();
    [Dependency] private GameLoopMachine GameLoop => this.DependOn<GameLoopMachine>();

    private NavigationMapNode MapNode { get; set; } = null!;
    private MapNodeDefinition Definition { get; set; } = null!;

    private string _knownLabelText = string.Empty;
    private Texture2D _knownTexture = null!;
    private bool _hasKnownIdentity;
    private bool _isHovering;
    private bool _isRevealedToPlayer;

    public void Initialize(NavigationMapNode mapNode, MapNodeDefinition definition, Texture2D texture)
    {
        MapNode = mapNode;
        Definition = definition;
        Sprite.Texture = texture;
        Label.Text = definition.Name;
        _knownTexture = Sprite.Texture;
        _knownLabelText = Label.Text;
        _hasKnownIdentity = true;
        SetIdentityKnown(false);
    }

    public void SetIdentityKnown(bool isKnown)
    {
        _isRevealedToPlayer = isKnown;
        if (!_hasKnownIdentity)
        {
            CaptureCurrentIdentity();
        }

        if (isKnown)
        {
            Sprite.Texture = _knownTexture;
            Label.Text = _knownLabelText;
            return;
        }
        Sprite.Texture = UnknownTexture;
        Label.Text = UNKNOWN_LABEL_TEXT;
    }

    public void SetIsCurrentNode(bool isCurrent)
    {
        Sprite.Modulate = isCurrent ? new Color(0.4f, 1f, 0.3f) : Colors.White;
        Sprite.Scale = isCurrent ? new Vector2(1.3f, 1.3f) : Vector2.One;
    }

    private void CaptureCurrentIdentity()
    {
        _knownTexture = Sprite.Texture;
        _knownLabelText = Label.Text;
        _hasKnownIdentity = true;
    }

    private void OnHover()
    {
        if (!_isRevealedToPlayer) return;
        if (_isHovering) return;
        Vector2 pos = GetViewport().GetMousePosition();
        _isHovering = true;
        ToolTipHandler.SetToolTip(Definition.Description, new Vector2(pos.X, pos.Y + 50));
    }

    private void OnGuiInput(InputEvent @event)
    {
        if (@event is not InputEventMouseButton mouseEvent) return;
        if (!mouseEvent.Pressed || mouseEvent.ButtonIndex != MouseButton.Left) return;
        GetViewport().SetInputAsHandled();
        GameLoop.Input(new GameLoopMachine.Input.DestinationSelected(MapNode.Id));
    }

    private void OnMouseExit()
    {
        _isHovering = false;
        ToolTipHandler.ClearToolTip();
    }

    public void OnResolved()
    {
        InternalControl.MouseEntered += OnHover;
        InternalControl.MouseExited += OnMouseExit;
        InternalControl.GuiInput += OnGuiInput;
    }

    public override void _ExitTree()
    {
        InternalControl.MouseEntered -= OnHover;
        InternalControl.MouseExited -= OnMouseExit;
        InternalControl.GuiInput -= OnGuiInput;
    }
    public override void _Notification(int what) => this.Notify(what);
}
