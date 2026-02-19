using System;
using System.Collections.Generic;
using Godot;
using VikingJamGame.Models.Navigation;
using VikingJamGame.Repositories.Navigation;

namespace VikingJamGame.GameLogic.Nodes;

[GlobalClass]
public partial class MapNodeRepositoryNode : Node, IMapNodeRepository
{
    [Export]
    public string EditorMapNodesResourceDirectory { get; set; } =
        MapNodeDirectoryResolver.EDITOR_MAP_NODES_RESOURCE_DIRECTORY;

    [Export]
    public string BuildMapNodesRelativeDirectory { get; set; } =
        MapNodeDirectoryResolver.BUILD_MAP_NODES_RELATIVE_DIRECTORY;

    private IMapNodeRepository? _innerRepository;

    public IReadOnlyCollection<MapNodeDefinition> All => RequireLoaded().All;

    public void Reload()
    {
        var mapNodesDirectory = MapNodeDirectoryResolver.ResolveForRuntime(
            EditorMapNodesResourceDirectory,
            BuildMapNodesRelativeDirectory);

        _innerRepository = TomlMapNodeRepositoryLoader.LoadFromDirectory(mapNodesDirectory);
    }

    public void ReloadFromDirectory(string directoryPath)
    {
        _innerRepository = TomlMapNodeRepositoryLoader.LoadFromDirectory(directoryPath);
    }

    public MapNodeDefinition GetByKind(string kind) => RequireLoaded().GetByKind(kind);

    public bool TryGetByKind(string kind, out MapNodeDefinition mapNodeDefinition) =>
        RequireLoaded().TryGetByKind(kind, out mapNodeDefinition);

    private IMapNodeRepository RequireLoaded()
    {
        return _innerRepository ?? throw new InvalidOperationException(
            "MapNodeRepositoryNode is not loaded. Call Reload() first.");
    }
}
