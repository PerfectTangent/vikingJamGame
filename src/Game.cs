using Godot;

namespace VikingJamGame;

[GlobalClass]
public partial class Game: Node2D
{
    [Export] private PackedScene? InitialScene { get; set; }

    public override void _Ready()
    {
        if (InitialScene is null) return;
        Node instance = InitialScene.Instantiate<Node>();
        AddChild(instance);
    }
}