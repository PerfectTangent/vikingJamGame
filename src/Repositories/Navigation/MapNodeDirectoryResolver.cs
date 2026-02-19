using System;
using System.IO;
using Godot;

namespace VikingJamGame.Repositories.Navigation;

public static class MapNodeDirectoryResolver
{
    public const string EDITOR_MAP_NODES_RESOURCE_DIRECTORY = "res://src/definitions/mapNodes";
    public const string BUILD_MAP_NODES_RELATIVE_DIRECTORY = "definitions/mapNodes";

    public static string ResolveForRuntime(
        string editorMapNodesResourceDirectory = EDITOR_MAP_NODES_RESOURCE_DIRECTORY,
        string buildMapNodesRelativeDirectory = BUILD_MAP_NODES_RELATIVE_DIRECTORY)
    {
        var isEditor = OS.HasFeature("editor");
        var editorAbsoluteDirectory = isEditor
            ? ProjectSettings.GlobalizePath(editorMapNodesResourceDirectory)
            : string.Empty;
        var executablePath = isEditor ? string.Empty : OS.GetExecutablePath();

        return Resolve(
            isEditor,
            editorAbsoluteDirectory,
            executablePath,
            buildMapNodesRelativeDirectory);
    }

    public static string Resolve(
        bool isEditor,
        string editorMapNodesAbsoluteDirectory,
        string executablePath,
        string buildMapNodesRelativeDirectory = BUILD_MAP_NODES_RELATIVE_DIRECTORY)
    {
        if (isEditor)
        {
            if (string.IsNullOrWhiteSpace(editorMapNodesAbsoluteDirectory))
            {
                throw new InvalidOperationException(
                    "Editor map node directory is empty. Expected an absolute path for 'res://src/definitions/mapNodes'.");
            }

            return Path.GetFullPath(editorMapNodesAbsoluteDirectory);
        }

        if (string.IsNullOrWhiteSpace(executablePath))
        {
            throw new InvalidOperationException(
                "Executable path is empty. Cannot resolve build map node directory.");
        }

        var executableDirectory = Path.GetDirectoryName(executablePath);
        if (string.IsNullOrWhiteSpace(executableDirectory))
        {
            throw new InvalidOperationException(
                $"Could not determine executable directory from path '{executablePath}'.");
        }

        return Path.GetFullPath(Path.Combine(executableDirectory, buildMapNodesRelativeDirectory));
    }
}
