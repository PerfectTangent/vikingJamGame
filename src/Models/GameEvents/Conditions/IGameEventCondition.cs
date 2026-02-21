namespace VikingJamGame.Models.GameEvents.Conditions;

public interface IGameEventCondition
{
    bool Evaluate(GameEventContext context);
}
