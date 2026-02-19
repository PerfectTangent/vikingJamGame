using System.Collections.Generic;
using VikingJamGame.Models.Navigation;

namespace VikingJamGame.Repositories.Navigation;

public interface IMapNodeRepository
{
    IReadOnlyCollection<MapNodeDefinition> All { get; }

    MapNodeDefinition GetByKind(string kind);

    bool TryGetByKind(string kind, out MapNodeDefinition mapNodeDefinition);
}
