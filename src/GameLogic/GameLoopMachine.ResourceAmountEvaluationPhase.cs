namespace VikingJamGame.GameLogic;

public partial class GameLoopMachine
{
    public abstract partial record State
    {
        public record ResourceEvaluationPhase: State
        {
            
        }
    }
}