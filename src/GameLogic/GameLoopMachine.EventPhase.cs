using Chickensoft.LogicBlocks;
using Godot;
using VikingJamGame.GameLogic.Nodes;
using VikingJamGame.Models.GameEvents.Runtime;

namespace VikingJamGame.GameLogic;

public partial class GameLoopMachine
{
    public abstract partial record State
    {
        public record EventPhase : State, IGet<Input.EventResolved>
        {
            public EventPhase() => this.OnEnter(() =>
            {
                GD.Print("EventPhase");
                GodotNavigationSession nav = Get<GodotNavigationSession>();

                UpdateVisibility();

                string? consecutiveVisitEventId = ResolveConsecutiveVisitEventId();
                if (consecutiveVisitEventId is not null)
                {
                    GD.Print(
                        $"Triggering consecutive-visit event '{consecutiveVisitEventId}' for kind '{nav.CurrentNode.Kind}' on streak {nav.ConsecutiveNodesOfSameType}.");
                    // TODO: Route this event id through the in-game event presentation flow.
                    return;
                }

                // TODO: trigger actual event; for now, auto-resolve
            });

            // we trigger the event at the current Node, player has to resolve it
            // and the resources amount is adjusted accordingly
            public Transition On(in Input.EventResolved input) => To<PlanningPhase>();
        }
    }
}
