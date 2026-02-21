using VikingJamGame.Models.GameEvents.Commands;

namespace VikingJamGame.Models.GameEvents.Effects;

public sealed record CustomCommandEffect(IEventCommand Command) : IGameEventEffect
{
    public void Apply(GameEventContext context) =>
        Command.Execute(context.PlayerInfo, context.GameResources);
}
