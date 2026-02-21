using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Tomlyn;
using Tomlyn.Model;
using Tomlyn.Syntax;
using VikingJamGame.Models.Navigation;

namespace VikingJamGame.Repositories.Navigation;

public static class TomlMapNodeRepositoryLoader
{
    public const string DEFAULT_MAP_NODES_DIRECTORY = "src/definitions/mapNodes";

    public static IMapNodeRepository LoadFromDefaultDirectory() =>
        LoadFromDirectory(DEFAULT_MAP_NODES_DIRECTORY);

    public static IMapNodeRepository LoadFromDirectory(string directoryPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(directoryPath);

        var fullDirectoryPath = Path.GetFullPath(directoryPath);
        if (!Directory.Exists(fullDirectoryPath))
        {
            throw new DirectoryNotFoundException(
                $"Map node definitions directory was not found: '{fullDirectoryPath}'.");
        }

        var nodeFiles = Directory.GetFiles(fullDirectoryPath, "*.toml", SearchOption.TopDirectoryOnly)
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase);

        var nodes = new List<MapNodeDefinition>();
        foreach (var nodeFilePath in nodeFiles)
        {
            nodes.Add(ReadDefinition(nodeFilePath));
        }

        ValidateNeighbourKinds(nodes);

        return new InMemoryMapNodeRepository(nodes);
    }

    private static MapNodeDefinition ReadDefinition(string filePath)
    {
        var fileContents = File.ReadAllText(filePath);
        DocumentSyntax parseResult = Toml.Parse(fileContents);
        if (parseResult.HasErrors)
        {
            var errors = string.Join(
                Environment.NewLine,
                parseResult.Diagnostics.Select(diagnostic => diagnostic.ToString()));

            throw new InvalidOperationException(
                $"Invalid TOML in '{filePath}'.{Environment.NewLine}{errors}");
        }

        if (Toml.ToModel(fileContents) is not TomlTable root)
        {
            throw new InvalidOperationException(
                $"TOML file '{filePath}' root value must be a table.");
        }

        return new MapNodeDefinition
        {
            Kind = GetRequiredString(root, nameof(MapNodeDefinition.Kind), filePath),
            Name = GetRequiredString(root, nameof(MapNodeDefinition.Name), filePath),
            Description = GetRequiredString(root, nameof(MapNodeDefinition.Description), filePath),
            PossibleNeighbours = GetPossibleNeighbours(root, nameof(MapNodeDefinition.PossibleNeighbours), filePath),
            ForcedFirstEvent = GetOptionalString(root, nameof(MapNodeDefinition.ForcedFirstEvent), filePath),
            EventsPool = GetOptionalStringArray(root, nameof(MapNodeDefinition.EventsPool), filePath),
            EventsOnConsecutiveVisits = GetOptionalConsecutiveVisitEvents(
                root,
                nameof(MapNodeDefinition.EventsOnConsecutiveVisits),
                filePath),
            Art = GetOptionalString(root, nameof(MapNodeDefinition.Art), filePath) ?? ""
        };
    }

    private static string GetRequiredString(TomlTable table, string key, string filePath)
    {
        if (!table.TryGetValue(key, out var rawValue))
        {
            throw new InvalidOperationException(
                $"TOML file '{filePath}' is missing required key '{key}'.");
        }

        if (rawValue is not string value)
        {
            throw new InvalidOperationException(
                $"TOML file '{filePath}' key '{key}' must be a string.");
        }

        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException(
                $"TOML file '{filePath}' key '{key}' cannot be empty.");
        }

        return value;
    }

    private static string? GetOptionalString(TomlTable table, string key, string filePath)
    {
        if (!table.TryGetValue(key, out var rawValue))
        {
            return null;
        }

        if (rawValue is not string value)
        {
            throw new InvalidOperationException(
                $"TOML file '{filePath}' key '{key}' must be a string.");
        }

        return string.IsNullOrWhiteSpace(value) ? null : value;
    }

    private static List<string> GetOptionalStringArray(TomlTable table, string key, string filePath)
    {
        if (!table.TryGetValue(key, out var rawValue))
        {
            return [];
        }

        if (rawValue is not TomlArray values)
        {
            throw new InvalidOperationException(
                $"TOML file '{filePath}' key '{key}' must be an array.");
        }

        var result = new List<string>(values.Count);
        foreach (var rawValueEntry in values)
        {
            if (rawValueEntry is not string value)
            {
                throw new InvalidOperationException(
                    $"TOML file '{filePath}' key '{key}' must contain only strings.");
            }

            if (!string.IsNullOrWhiteSpace(value))
            {
                result.Add(value);
            }
        }

        return result;
    }

    private static Dictionary<string, float> GetPossibleNeighbours(TomlTable table, string key, string filePath)
    {
        if (!table.TryGetValue(key, out var rawValue))
        {
            return new Dictionary<string, float>(StringComparer.Ordinal);
        }

        if (rawValue is not TomlTable neighboursTable)
        {
            throw new InvalidOperationException(
                $"TOML file '{filePath}' key '{key}' must be a table.");
        }

        var neighbours = new Dictionary<string, float>(StringComparer.Ordinal);
        foreach (var entry in neighboursTable)
        {
            float weight = entry.Value switch
            {
                int value => value,
                long value => checked((float)value),
                float value => value,
                double value => checked((float)value),
                _ => throw new InvalidOperationException(
                    $"TOML file '{filePath}' neighbour '{entry.Key}' weight must be numeric.")
            };

            if (weight < 0)
            {
                throw new InvalidOperationException(
                    $"TOML file '{filePath}' neighbour '{entry.Key}' weight must be >= 0.");
            }

            neighbours[entry.Key] = weight;
        }

        return neighbours;
    }

    private static Dictionary<int, string> GetOptionalConsecutiveVisitEvents(
        TomlTable table,
        string key,
        string filePath)
    {
        if (!table.TryGetValue(key, out var rawValue))
        {
            return new Dictionary<int, string>();
        }

        if (rawValue is not TomlTable eventsTable)
        {
            throw new InvalidOperationException(
                $"TOML file '{filePath}' key '{key}' must be a table.");
        }

        var eventsByConsecutiveVisitCount = new Dictionary<int, string>();
        foreach (var entry in eventsTable)
        {
            if (!int.TryParse(entry.Key, NumberStyles.Integer, CultureInfo.InvariantCulture, out int consecutiveVisitCount))
            {
                throw new InvalidOperationException(
                    $"TOML file '{filePath}' key '{key}' contains non-integer threshold '{entry.Key}'.");
            }

            if (consecutiveVisitCount < 1)
            {
                throw new InvalidOperationException(
                    $"TOML file '{filePath}' key '{key}' threshold '{entry.Key}' must be >= 1.");
            }

            if (entry.Value is not string eventId || string.IsNullOrWhiteSpace(eventId))
            {
                throw new InvalidOperationException(
                    $"TOML file '{filePath}' key '{key}' threshold '{entry.Key}' must map to a non-empty string.");
            }

            eventsByConsecutiveVisitCount[consecutiveVisitCount] = eventId;
        }

        return eventsByConsecutiveVisitCount;
    }

    private static void ValidateNeighbourKinds(IReadOnlyCollection<MapNodeDefinition> nodes)
    {
        var knownKinds = nodes
            .Select(node => node.Kind)
            .ToHashSet(StringComparer.Ordinal);

        foreach (MapNodeDefinition node in nodes)
        {
            foreach (string neighbourKind in node.PossibleNeighbours.Keys)
            {
                if (!knownKinds.Contains(neighbourKind))
                {
                    throw new InvalidOperationException(
                        $"Node kind '{node.Kind}' references unknown neighbour kind '{neighbourKind}'.");
                }
            }
        }
    }
}
