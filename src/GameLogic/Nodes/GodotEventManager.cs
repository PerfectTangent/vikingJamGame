using Chickensoft.AutoInject;
using Chickensoft.Introspection;
using Godot;

namespace VikingJamGame.GameLogic.Nodes;

[GlobalClass][Meta(typeof(IAutoNode))]
public partial class GodotEventManager: CanvasLayer
{
    
    
    public override void _Notification(int what) => this.Notify(what);
}