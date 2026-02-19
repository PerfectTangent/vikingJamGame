using VikingJamGame.Models.Navigation;
using VikingJamGame.Repositories.Navigation;

namespace VikingJamGame.Tests.Models.Navigation.Repository;

public sealed class TomlMapNodeRepositoryLoaderTests
{
    [Fact]
    public void LoadFromDirectory_LoadsMapNodeDefinitions()
    {
        var tempDirectory = CreateTempDirectory();
        try
        {
            WriteToml(
                tempDirectory,
                "start.toml",
                """
                Kind = "start"
                Art = "start.png"
                ForcedFirstEvent = "intro_event"
                EventsPool = ["event.a", "event.b"]

                [PossibleNeighbours]
                forest = 0.75
                river = 0.25
                """);

            WriteToml(
                tempDirectory,
                "forest.toml",
                """
                Kind = "forest"
                Art = "forest.png"
                EventsPool = ["forest.event"]

                [PossibleNeighbours]
                forest = 1.0
                """);

            WriteToml(
                tempDirectory,
                "river.toml",
                """
                Kind = "river"
                Art = "river.png"

                [PossibleNeighbours]
                river = 1
                """);

            IMapNodeRepository repository = TomlMapNodeRepositoryLoader.LoadFromDirectory(tempDirectory);
            MapNodeDefinition start = repository.GetByKind("start");

            Assert.Equal(3, repository.All.Count);
            Assert.Equal("start.png", start.Art);
            Assert.Equal("intro_event", start.ForcedFirstEvent);
            Assert.Equal(["event.a", "event.b"], start.EventsPool);
            Assert.Equal(0.75f, start.PossibleNeighbours["forest"]);
            Assert.Equal(0.25f, start.PossibleNeighbours["river"]);
        }
        finally
        {
            DeleteDirectory(tempDirectory);
        }
    }

    [Fact]
    public void LoadFromDirectory_ThrowsWhenRequiredKindIsMissing()
    {
        var tempDirectory = CreateTempDirectory();
        try
        {
            WriteToml(
                tempDirectory,
                "broken.toml",
                """
                Art = "broken.png"
                """);

            InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() =>
                TomlMapNodeRepositoryLoader.LoadFromDirectory(tempDirectory));

            Assert.Contains("missing required key 'Kind'", exception.Message);
        }
        finally
        {
            DeleteDirectory(tempDirectory);
        }
    }

    [Fact]
    public void LoadFromDirectory_ThrowsWhenNeighbourWeightIsNegative()
    {
        var tempDirectory = CreateTempDirectory();
        try
        {
            WriteToml(
                tempDirectory,
                "start.toml",
                """
                Kind = "start"

                [PossibleNeighbours]
                forest = -0.1
                """);

            WriteToml(
                tempDirectory,
                "forest.toml",
                """
                Kind = "forest"
                """);

            InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() =>
                TomlMapNodeRepositoryLoader.LoadFromDirectory(tempDirectory));

            Assert.Contains("must be >= 0", exception.Message);
        }
        finally
        {
            DeleteDirectory(tempDirectory);
        }
    }

    [Fact]
    public void LoadFromDirectory_ThrowsWhenNeighbourKindIsUnknown()
    {
        var tempDirectory = CreateTempDirectory();
        try
        {
            WriteToml(
                tempDirectory,
                "start.toml",
                """
                Kind = "start"

                [PossibleNeighbours]
                missing = 1
                """);

            InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() =>
                TomlMapNodeRepositoryLoader.LoadFromDirectory(tempDirectory));

            Assert.Contains("unknown neighbour kind 'missing'", exception.Message);
        }
        finally
        {
            DeleteDirectory(tempDirectory);
        }
    }

    private static string CreateTempDirectory()
    {
        var path = Path.Combine(
            Path.GetTempPath(),
            "VikingJamGame.Tests",
            Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }

    private static void WriteToml(string directoryPath, string fileName, string contents)
    {
        var filePath = Path.Combine(directoryPath, fileName);
        File.WriteAllText(filePath, contents);
    }

    private static void DeleteDirectory(string directoryPath)
    {
        if (Directory.Exists(directoryPath))
        {
            Directory.Delete(directoryPath, recursive: true);
        }
    }
}
