namespace VikingJamGame.Models.GameEvents.Effects;

public interface IGameEventEffect
{
    void Apply(GameEventContext context);
}
