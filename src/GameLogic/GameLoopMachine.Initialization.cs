using Chickensoft.LogicBlocks;
using Godot;

namespace VikingJamGame.GameLogic;

public partial class GameLoopMachine
{
    public abstract partial record State
    {
        public record Initialization : State, IGet<Input.InitializationDone>
        {
            public Initialization()
            {
                this.OnEnter(() =>
                {
                    GD.Print("Initialization");
                    UpdateVisibility();

                    CameraController camera = Get<CameraController>();
                    camera.IntroPanFinished += OnIntroPanFinished;
                });

                this.OnExit(() =>
                {
                    CameraController camera = Get<CameraController>();
                    camera.IntroPanFinished -= OnIntroPanFinished;
                });
            }

            private void OnIntroPanFinished()
            {
                // trigger the first event here!
                Input(new Input.InitializationDone());
            }

            public Transition On(in Input.InitializationDone input) => To<PlanningPhase>();
        }
    }
}
