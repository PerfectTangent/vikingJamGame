using System;
using System.Collections.Generic;
using VikingJamGame.Models.Navigation;

namespace VikingJamGame.Repositories.Navigation;

public sealed class InMemoryMapNodeRepository : IMapNodeRepository
{
    private readonly Dictionary<string, MapNodeDefinition> _nodesByKind;

    public InMemoryMapNodeRepository(IEnumerable<MapNodeDefinition> nodeDefinitions)
    {
        ArgumentNullException.ThrowIfNull(nodeDefinitions);

        var map = new Dictionary<string, MapNodeDefinition>(StringComparer.Ordinal);
        foreach (MapNodeDefinition nodeDefinition in nodeDefinitions)
        {
            if (!map.TryAdd(nodeDefinition.Kind, nodeDefinition))
            {
                throw new InvalidOperationException(
                    $"Duplicate map node kind '{nodeDefinition.Kind}' found while creating repository.");
            }
        }

        _nodesByKind = map;
    }

    public IReadOnlyCollection<MapNodeDefinition> All => _nodesByKind.Values;

    public MapNodeDefinition GetByKind(string kind)
    {
        if (TryGetByKind(kind, out MapNodeDefinition nodeDefinition))
        {
            return nodeDefinition;
        }

        throw new KeyNotFoundException($"No map node found with kind '{kind}'.");
    }

    public bool TryGetByKind(string kind, out MapNodeDefinition mapNodeDefinition)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(kind);

        return _nodesByKind.TryGetValue(kind, out mapNodeDefinition!);
    }
}
