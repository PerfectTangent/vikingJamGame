using Chickensoft.LogicBlocks;
using Godot;

namespace VikingJamGame.GameLogic;

public partial class GameLoopMachine
{
    public abstract partial record State
    {
        public record MovementCostPhase : State, IGet<Input.MovementCostDone>
        {
            public MovementCostPhase() => this.OnEnter(() =>
            {
                GD.Print("MovementCostPhase");
                // TODO: consume movement cost (skip on first turn)
                Input(new Input.MovementCostDone());
            });

            public Transition On(in Input.MovementCostDone input) => To<PlanningPhase>();
        }
    }
}
