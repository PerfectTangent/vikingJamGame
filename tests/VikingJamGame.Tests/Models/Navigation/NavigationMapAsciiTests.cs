using System.Text;
using VikingJamGame.Models.Navigation;
using Xunit.Abstractions;

namespace VikingJamGame.Tests.Models.Navigation;

public sealed class NavigationMapAsciiTests(ITestOutputHelper output)
{
    [Fact]
    public void Explore_Maps_AsAsciiForVisualInspection()
    {
        var scenarios = new[]
        {
            new Scenario(
                "Balanced branching",
                CreateBalancedDefinitions(),
                "start",
                "start",
                20,
                7),
            new Scenario(
                "Linear-ish chain",
                CreateLinearDefinitions(),
                "start",
                "start",
                20,
                21),
            new Scenario(
                "High fan-out",
                CreateHighFanoutDefinitions(),
                "start",
                "start",
                25,
                99)
        };

        var generator = new NavigationMapGenerator();
        foreach (Scenario scenario in scenarios)
        {
            output.WriteLine($"===== {scenario.Name} =====");

            for (var variation = 0; variation < 3; variation++)
            {
                var seed = scenario.Seed + variation;
                output.WriteLine(
                    $"-- variation={variation + 1}, seed={seed}, start={scenario.StartKind}, maxNodes={scenario.MaxNodes}");

                try
                {
                    var random = new SystemNavigationRandom(new Random(seed));
                    NavigationMap map = generator.Generate(
                        scenario.Definitions,
                        scenario.StartKind,
                        scenario.EndKind,
                        scenario.MaxNodes,
                        random);
                    string ascii = ToAsciiGraph(map);
                    output.WriteLine(ascii);
                }
                catch (Exception exception)
                {
                    output.WriteLine(
                        $"Generation failed: {exception.GetType().Name}: {exception.Message}");
                }

                output.WriteLine(string.Empty);
            }
        }
    }

    private static Dictionary<string, MapNodeDefinition> CreateBalancedDefinitions() =>
        new(StringComparer.Ordinal)
        {
            ["start"] = new()
            {
                Kind = "start",
                PossibleNeighbours = new Dictionary<string, float>
                {
                    ["forest"] = 0.5f,
                    ["river"] = 0.3f,
                    ["cave"] = 0.2f
                }
            },
            ["forest"] = new()
            {
                Kind = "forest",
                PossibleNeighbours = new Dictionary<string, float>
                {
                    ["forest"] = 0.4f,
                    ["river"] = 0.4f,
                    ["village"] = 0.2f
                }
            },
            ["river"] = new()
            {
                Kind = "river",
                PossibleNeighbours = new Dictionary<string, float>
                {
                    ["river"] = 0.7f,
                    ["village"] = 0.3f
                }
            },
            ["cave"] = new()
            {
                Kind = "cave",
                PossibleNeighbours = new Dictionary<string, float>
                {
                    ["cave"] = 0.5f,
                    ["forest"] = 0.5f
                }
            },
            ["village"] = new()
            {
                Kind = "village",
                PossibleNeighbours = new Dictionary<string, float>
                {
                    ["forest"] = 0.6f,
                    ["river"] = 0.4f
                }
            }
        };

    private static Dictionary<string, MapNodeDefinition> CreateLinearDefinitions() =>
        new(StringComparer.Ordinal)
        {
            ["start"] = new()
            {
                Kind = "start",
                PossibleNeighbours = new Dictionary<string, float>
                {
                    ["path"] = 1.0f
                }
            },
            ["path"] = new()
            {
                Kind = "path",
                PossibleNeighbours = new Dictionary<string, float>
                {
                    ["path"] = 0.8f,
                    ["camp"] = 0.2f
                }
            },
            ["camp"] = new()
            {
                Kind = "camp",
                PossibleNeighbours = new Dictionary<string, float>
                {
                    ["path"] = 1.0f
                }
            }
        };

    private static Dictionary<string, MapNodeDefinition> CreateHighFanoutDefinitions() =>
        new(StringComparer.Ordinal)
        {
            ["start"] = new()
            {
                Kind = "start",
                PossibleNeighbours = new Dictionary<string, float>
                {
                    ["forest"] = 0.2f,
                    ["river"] = 0.2f,
                    ["cave"] = 0.2f,
                    ["ruins"] = 0.2f,
                    ["hill"] = 0.2f
                }
            },
            ["forest"] = new() { Kind = "forest", PossibleNeighbours = new Dictionary<string, float> { ["start"] = 1.0f } },
            ["river"] = new() { Kind = "river", PossibleNeighbours = new Dictionary<string, float> { ["start"] = 1.0f } },
            ["cave"] = new() { Kind = "cave", PossibleNeighbours = new Dictionary<string, float> { ["start"] = 1.0f } },
            ["ruins"] = new() { Kind = "ruins", PossibleNeighbours = new Dictionary<string, float> { ["start"] = 1.0f } },
            ["hill"] = new() { Kind = "hill", PossibleNeighbours = new Dictionary<string, float> { ["start"] = 1.0f } }
        };

    private static string ToAsciiGraph(NavigationMap map)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Graph");
        sb.AppendLine($"{map.StartNode.Id}:{map.StartNode.Kind}");

        var expandedNodes = new HashSet<int> { map.StartNodeId };
        RenderChildren(
            map,
            map.StartNodeId,
            prefix: "",
            path: [map.StartNodeId],
            expandedNodes,
            sb);

        sb.AppendLine();
        sb.AppendLine("Adjacency");
        foreach (NavigationMapNode node in map.NodesById.OrderBy(entry => entry.Key).Select(entry => entry.Value))
        {
            sb.Append("  ");
            sb.Append(node.Id);
            sb.Append(':');
            sb.Append(node.Kind);
            sb.Append(" -> [");
            sb.Append(string.Join(", ", node.NeighbourIds));
            sb.AppendLine("]");
        }

        return sb.ToString().TrimEnd();
    }

    private static void RenderChildren(
        NavigationMap map,
        int parentId,
        string prefix,
        HashSet<int> path,
        HashSet<int> expandedNodes,
        StringBuilder sb)
    {
        NavigationMapNode parent = map.NodesById[parentId];
        for (var i = 0; i < parent.NeighbourIds.Count; i++)
        {
            var childId = parent.NeighbourIds[i];
            var isLastChild = i == parent.NeighbourIds.Count - 1;
            var connector = isLastChild ? "\\-- " : "+-- ";

            sb.Append(prefix);
            sb.Append(connector);

            if (!map.NodesById.TryGetValue(childId, out NavigationMapNode? child))
            {
                sb.AppendLine($"{childId}:<missing>");
                continue;
            }

            sb.Append(child.Id);
            sb.Append(':');
            sb.Append(child.Kind);

            if (path.Contains(childId))
            {
                sb.AppendLine(" (cycle)");
                continue;
            }

            if (expandedNodes.Contains(childId))
            {
                sb.AppendLine(" (seen)");
                continue;
            }

            sb.AppendLine();

            expandedNodes.Add(childId);
            path.Add(childId);

            var childPrefix = prefix + (isLastChild ? "    " : "|   ");
            RenderChildren(map, childId, childPrefix, path, expandedNodes, sb);

            path.Remove(childId);
        }
    }

    private sealed record Scenario(
        string Name,
        Dictionary<string, MapNodeDefinition> Definitions,
        string StartKind,
        string EndKind,
        int MaxNodes,
        int Seed);
}
