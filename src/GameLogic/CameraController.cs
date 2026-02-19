using Godot;

namespace VikingJamGame.GameLogic;

[GlobalClass]
public partial class CameraController : Node
{
    [Export] private Camera2D Camera2D { get; set; } = null!;
    [Export] private Node2D StartingVillage { get; set; } = null!;
    [Export(PropertyHint.Range, "0.1,4.0,0.01")] private float MinZoom { get; set; } = 0.5f;
    [Export(PropertyHint.Range, "0.1,6.0,0.01")] private float MaxZoom { get; set; } = 2.5f;
    [Export(PropertyHint.Range, "0.01,1.0,0.01")] private float ZoomStepRatio { get; set; } = 0.1f;

    private bool _isDragging;

    public override void _Ready()
    {
        Camera2D.GlobalPosition = StartingVillage.GlobalPosition;
        SetProcessUnhandledInput(true);
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
                _isDragging = mouseButton.Pressed;
                return;
            }
        }

        if (!_isDragging || @event is not InputEventMouseMotion mouseMotion)
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
        var zoomFactor = 1f + ZoomStepRatio;
        var targetZoom = zoomIn ? Camera2D.Zoom / zoomFactor : Camera2D.Zoom * zoomFactor;
        var clampedZoom = Mathf.Clamp(targetZoom.X, MinZoom, MaxZoom);
        Camera2D.Zoom = new Vector2(clampedZoom, clampedZoom);
    }
}
