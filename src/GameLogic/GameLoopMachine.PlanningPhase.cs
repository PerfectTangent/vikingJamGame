using Chickensoft.LogicBlocks;
using Godot;
using VikingJamGame.GameLogic.Nodes;

namespace VikingJamGame.GameLogic;

public partial class GameLoopMachine
{
    public abstract partial record State
    {
        public record PlanningPhase : State, IGet<Input.DestinationSelected>
        {
            public PlanningPhase() => this.OnEnter(() => GD.Print("PlanningPhase"));

            public Transition On(in Input.DestinationSelected input)
            {
                GodotNavigationSession nav = Get<GodotNavigationSession>();

                if (!nav.CanMoveTo(input.NodeId))
                {
                    GD.Print($"Cannot move to node {input.NodeId}");
                    return ToSelf();
                }

                bool isRevisit = nav.VisitedNodeIds.Contains(input.NodeId);
                nav.MoveTo(input.NodeId);

                if (ResolveConsecutiveVisitEventId() is not null)
                {
                    return To<EventPhase>();
                }

                return isRevisit ? To<PlanningPhase>() : To<EventPhase>();
            }
        }
    }
}
