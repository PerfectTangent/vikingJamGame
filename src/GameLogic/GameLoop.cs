using Chickensoft.Introspection;
using Chickensoft.LogicBlocks;

namespace VikingJamGame.GameLogic;


[Meta, LogicBlock(typeof(State))]
public partial class GameLoop : LogicBlock<GameLoop.State>
{
    public override Transition GetInitialState() => To<State.Prologue>();

    public static class Input
    {
        public readonly record struct Start;

        // MovementPhase -> EventResolution
        public readonly record struct MovementDone;

        // EventResolution -> ResourceAdjustment
        public readonly record struct EventResolved;

        // ResourceAdjustment -> MovementPhase
        public readonly record struct ResourcesAdjusted;

        // Allowed exits 
        public readonly record struct Exit;     // e.g. quit/leave loop
        public readonly record struct GameOver; // end the run
    }

    public abstract record State : StateLogic<State>
    {

        public record Prologue : State, IGet<Input.Start>
        {
            public Transition On(in Input.Start input) => To<MovementPhase>();
        }

        public record MovementPhase : State, IGet<Input.MovementDone>
        {
            public Transition On(in Input.MovementDone input) => To<EventResolution>();
        }

        public record EventResolution : State,
            IGet<Input.EventResolved>,
            IGet<Input.Exit>,
            IGet<Input.GameOver>
        {

            public Transition On(in Input.EventResolved input) => To<ResourceAdjustment>();
            public Transition On(in Input.Exit input) => To<Ended>();
            public Transition On(in Input.GameOver input) => To<Ended>();
        }

        public record ResourceAdjustment : State,
            IGet<Input.ResourcesAdjusted>,
            IGet<Input.Exit>,
            IGet<Input.GameOver>
        {

            public Transition On(in Input.ResourcesAdjusted input) => To<MovementPhase>();
            public Transition On(in Input.Exit input) => To<Ended>();
            public Transition On(in Input.GameOver input) => To<Ended>();
        }

        // Terminal state
        public record Ended : State { }
    }
}