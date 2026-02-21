using System.Collections.Generic;

namespace VikingJamGame.Models.GameEvents.Runtime;

public sealed class EventResults
{
    private readonly List<GameEventOption> _resolvedOptions = [];

    public IReadOnlyList<GameEventOption> ResolvedOptions => _resolvedOptions;

    public void Add(GameEventOption option) => _resolvedOptions.Add(option);
}
