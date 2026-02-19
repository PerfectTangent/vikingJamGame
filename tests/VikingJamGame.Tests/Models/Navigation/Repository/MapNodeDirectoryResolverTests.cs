using VikingJamGame.Repositories.Navigation;

namespace VikingJamGame.Tests.Models.Navigation.Repository;

public sealed class MapNodeDirectoryResolverTests
{
    [Fact]
    public void Resolve_EditorMode_UsesEditorAbsoluteDirectory()
    {
        var editorAbsoluteDirectory = Path.Combine(
            Path.GetTempPath(),
            "VikingJamGame.Tests",
            "mapNodes");
        var expected = Path.GetFullPath(editorAbsoluteDirectory);

        var resolved = MapNodeDirectoryResolver.Resolve(
            isEditor: true,
            editorMapNodesAbsoluteDirectory: editorAbsoluteDirectory,
            executablePath: string.Empty);

        Assert.Equal(expected, resolved, StringComparer.OrdinalIgnoreCase);
    }

    [Fact]
    public void Resolve_BuildMode_UsesExecutableDirectoryPlusRelativeDefinitionsFolder()
    {
        var executablePath = Path.Combine("C:\\Games\\VikingJam", "VikingJamGame.exe");
        var expected = Path.GetFullPath(Path.Combine(
            Path.GetDirectoryName(executablePath)!,
            "definitions",
            "mapNodes"));

        var resolved = MapNodeDirectoryResolver.Resolve(
            isEditor: false,
            editorMapNodesAbsoluteDirectory: string.Empty,
            executablePath: executablePath);

        Assert.Equal(expected, resolved, StringComparer.OrdinalIgnoreCase);
    }

    [Fact]
    public void Resolve_EditorMode_ThrowsWhenEditorDirectoryIsMissing()
    {
        var exception = Assert.Throws<InvalidOperationException>(() =>
            MapNodeDirectoryResolver.Resolve(
                isEditor: true,
                editorMapNodesAbsoluteDirectory: "",
                executablePath: ""));

        Assert.Contains("Editor map node directory is empty", exception.Message);
    }

    [Fact]
    public void Resolve_BuildMode_ThrowsWhenExecutablePathIsMissing()
    {
        var exception = Assert.Throws<InvalidOperationException>(() =>
            MapNodeDirectoryResolver.Resolve(
                isEditor: false,
                editorMapNodesAbsoluteDirectory: "",
                executablePath: ""));

        Assert.Contains("Executable path is empty", exception.Message);
    }
}
