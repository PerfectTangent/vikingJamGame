using Chickensoft.AutoInject;
using Chickensoft.Introspection;
using Godot;
using VikingJamGame.GameLogic.Nodes;

namespace VikingJamGame.GameLogic;

[GlobalClass]
[Meta(typeof(IAutoNode))]
public partial class CameraController : Node
{
    [Signal] public delegate void IntroPanFinishedEventHandler();
    public override void _Notification(int what) => this.Notify(what);

    [Export] private Camera2D Camera2D { get; set; } = null!;
    [Export] private Node2D StartingVillage { get; set; } = null!;

    [Export(PropertyHint.Range, "0.1,4.0,0.01")] private float MinZoom { get; set; } = 0.5f;
    [Export(PropertyHint.Range, "0.1,6.0,0.01")] private float MaxZoom { get; set; } = 2.5f;
    [Export(PropertyHint.Range, "0.01,1.0,0.01")] private float ZoomStepRatio { get; set; } = 0.1f;
    [Export(PropertyHint.Range, "0.0,10.0,0.1")] private float IntroPanDelay { get; set; } = 2.0f;
    [Export(PropertyHint.Range, "0.1,10.0,0.1")] private float IntroPanDuration { get; set; } = 2.5f;

    [Dependency] private GodotMapGenerator MapGenerator => this.DependOn<GodotMapGenerator>();

    private bool _isDragging;
    private bool _isPanning;

    public void OnResolved()
    {
        Camera2D.GlobalPosition = StartingVillage.GlobalPosition;
        SetProcessUnhandledInput(true);
        MapGenerator.MapGenerated += OnMapGenerated;
    }

    public override void _ExitTree()
    {
        MapGenerator.MapGenerated -= OnMapGenerated;
    }

    private void OnMapGenerated(Vector2 startGlobalPosition, Vector2 endGlobalPosition)
    {
        _isPanning = true;
        Camera2D.GlobalPosition = endGlobalPosition;

        Tween tween = CreateTween();
        tween.TweenInterval(IntroPanDelay);
        tween.TweenProperty(Camera2D, "global_position", startGlobalPosition, IntroPanDuration)
            .SetEase(Tween.EaseType.InOut)
            .SetTrans(Tween.TransitionType.Sine);
        tween.TweenCallback(Callable.From(() =>
        {
            _isPanning = false;
            EmitSignal(SignalName.IntroPanFinished);
        }));
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event is InputEventMouseButton mouseButton)
        {
            if (mouseButton is { ButtonIndex: MouseButton.WheelUp, Pressed: true })
            {
                ApplyZoom(zoomIn: true);
                return;
            }

            if (mouseButton is { ButtonIndex: MouseButton.WheelDown, Pressed: true })
            {
                ApplyZoom(zoomIn: false);
                return;
            }

            if (mouseButton.ButtonIndex == MouseButton.Left)
            {
                if (!_isPanning)
                {
                    _isDragging = mouseButton.Pressed;
                }
                return;
            }
        }

        if (_isPanning || !_isDragging || @event is not InputEventMouseMotion mouseMotion)
        {
            return;
        }

        Vector2 zoom = Camera2D.Zoom;
        var zoomSafe = new Vector2(
            Mathf.IsZeroApprox(zoom.X) ? 1f : zoom.X,
            Mathf.IsZeroApprox(zoom.Y) ? 1f : zoom.Y);

        Camera2D.GlobalPosition -= mouseMotion.Relative / zoomSafe;
    }

    private void ApplyZoom(bool zoomIn)
    {
        float zoomFactor = 1f + ZoomStepRatio;
        Vector2 targetZoom = zoomIn ? Camera2D.Zoom / zoomFactor : Camera2D.Zoom * zoomFactor;
        float clampedZoom = Mathf.Clamp(targetZoom.X, MinZoom, MaxZoom);
        Camera2D.Zoom = new Vector2(clampedZoom, clampedZoom);
    }
}
