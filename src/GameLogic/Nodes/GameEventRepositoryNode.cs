using System;
using System.Collections.Generic;
using Godot;
using VikingJamGame.Models.GameEvents.Commands;
using VikingJamGame.Models.GameEvents.Runtime;
using VikingJamGame.Repositories.GameEvents;

namespace VikingJamGame.GameLogic.Nodes;

[GlobalClass]
public partial class GameEventRepositoryNode : Node, IGameEventRepository
{
    [Export]
    public string EditorEventsResourceDirectory { get; set; } =
        GameEventDirectoryResolver.EDITOR_EVENTS_RESOURCE_DIRECTORY;

    [Export]
    public string BuildEventsRelativeDirectory { get; set; } =
        GameEventDirectoryResolver.BUILD_EVENTS_RELATIVE_DIRECTORY;

    private IGameEventRepository? _innerRepository;

    public IReadOnlyCollection<GameEvent> All => RequireLoaded().All;

    public void Reload(ICommandRegistry commands)
    {
        var eventsDirectory = GameEventDirectoryResolver.ResolveForRuntime(
            EditorEventsResourceDirectory,
            BuildEventsRelativeDirectory);

        _innerRepository = TomlGameEventRepositoryLoader.LoadFromDirectory(eventsDirectory, commands);
    }

    public void ReloadFromDirectory(string directoryPath, ICommandRegistry commands)
    {
        _innerRepository = TomlGameEventRepositoryLoader.LoadFromDirectory(directoryPath, commands);
    }

    public GameEvent GetById(string eventId) => RequireLoaded().GetById(eventId);

    public bool TryGetById(string eventId, out GameEvent gameEvent) =>
        RequireLoaded().TryGetById(eventId, out gameEvent);

    public bool TryGetNextEvent(GameEventOption selectedOption, out GameEvent nextEvent) =>
        RequireLoaded().TryGetNextEvent(selectedOption, out nextEvent);

    private IGameEventRepository RequireLoaded()
    {
        return _innerRepository ?? throw new InvalidOperationException(
            "GameEventRepositoryNode is not loaded. Call Reload(...) first.");
    }
}
