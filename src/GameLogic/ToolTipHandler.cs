using Godot;

namespace VikingJamGame.GameLogic;

[GlobalClass]
public partial class ToolTipHandler : CanvasLayer
{
    [Export] private Control Container { get; set; } = null!;
    [Export] private Label ToolTipLabel { get; set; } = null!;

    public void SetToolTip(string text, Vector2? position = null)
    {
        ToolTipLabel.Text = text;
        Container.ResetSize();
        Visible = true;

        Vector2 desiredPos = position ?? GetViewport().GetMousePosition();
        Vector2 viewportSize = GetViewport().GetVisibleRect().Size;
        Vector2 containerSize = Container.Size;
        Vector2 centeredPos = desiredPos - containerSize / 2;

        Container.Position = new Vector2(
            Mathf.Clamp(centeredPos.X, 0, viewportSize.X - containerSize.X),
            Mathf.Clamp(centeredPos.Y, 0, viewportSize.Y - containerSize.Y)
        );
    }

    public override void _Ready() => Visible = false;
    public void ClearToolTip() => Visible = false;
}