using VikingJamGame.Models.Navigation;
using VikingJamGame.Repositories.Navigation;

namespace VikingJamGame.Tests.Models.Navigation.Repository;

public sealed class InMemoryMapNodeRepositoryTests
{
    [Fact]
    public void Constructor_ThrowsWhenNodeKindsAreDuplicated()
    {
        MapNodeDefinition[] nodes =
        [
            new() { Kind = "forest" },
            new() { Kind = "forest" }
        ];

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() =>
            new InMemoryMapNodeRepository(nodes));

        Assert.Contains("Duplicate map node kind 'forest'", exception.Message);
    }

    [Fact]
    public void GetByKind_ReturnsExpectedNode()
    {
        var repository = new InMemoryMapNodeRepository(
        [
            new MapNodeDefinition { Kind = "start" },
            new MapNodeDefinition { Kind = "forest" }
        ]);

        MapNodeDefinition mapNode = repository.GetByKind("forest");

        Assert.Equal("forest", mapNode.Kind);
    }

    [Fact]
    public void TryGetByKind_ReturnsFalseWhenNodeIsMissing()
    {
        var repository = new InMemoryMapNodeRepository(
        [
            new MapNodeDefinition { Kind = "start" }
        ]);

        var found = repository.TryGetByKind("river", out _);

        Assert.False(found);
    }
}
