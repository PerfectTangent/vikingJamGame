using VikingJamGame.Models.Navigation;

namespace VikingJamGame.Tests.Models.Navigation;

public sealed class NavigationMapGeneratorTests
{
    [Fact]
    public void Generate_RecoversFromDeadLayerAndKeepsGrowingUntilMaxNodes()
    {
        var definitions = new Dictionary<string, MapNodeDefinition>(StringComparer.Ordinal)
        {
            ["starting_village"] = new()
            {
                Kind = "starting_village",
                PossibleNeighbours = new Dictionary<string, float>
                {
                    ["path"] = 1f
                }
            },
            ["path"] = new()
            {
                Kind = "path",
                PossibleNeighbours = new Dictionary<string, float>
                {
                    ["path"] = 1f
                }
            }
        };

        var generator = new NavigationMapGenerator();
        const int maxNodes = 8;

        NavigationMap map = generator.Generate(
            definitions,
            "starting_village",
            "final_boss",
            maxNodes,
            new MinRandom());

        Assert.Equal(maxNodes, map.NodesById.Count);
    }

    [Fact]
    public void Generate_AvoidsSinglePathCollapse_WhenThereIsRoomToBranch()
    {
        var definitions = new Dictionary<string, MapNodeDefinition>(StringComparer.Ordinal)
        {
            ["starting_village"] = new()
            {
                Kind = "starting_village",
                PossibleNeighbours = new Dictionary<string, float>
                {
                    ["a"] = 1f,
                    ["b"] = 1f,
                    ["c"] = 1f
                }
            },
            ["a"] = new()
            {
                Kind = "a",
                PossibleNeighbours = new Dictionary<string, float>
                {
                    ["a"] = 1f,
                    ["b"] = 1f,
                    ["c"] = 1f
                }
            },
            ["b"] = new()
            {
                Kind = "b",
                PossibleNeighbours = new Dictionary<string, float>
                {
                    ["a"] = 1f,
                    ["b"] = 1f,
                    ["c"] = 1f
                }
            },
            ["c"] = new()
            {
                Kind = "c",
                PossibleNeighbours = new Dictionary<string, float>
                {
                    ["a"] = 1f,
                    ["b"] = 1f,
                    ["c"] = 1f
                }
            }
        };

        var generator = new NavigationMapGenerator();
        const int maxNodes = 18;

        NavigationMap map = generator.Generate(
            definitions,
            "starting_village",
            "final_boss",
            maxNodes,
            new MinRandom());

        var breadthByDepth = CalculateBreadthByDepth(map);
        int depthsWithAtLeastTwoNodes = breadthByDepth.Count(entry => entry.Value >= 2);

        Assert.True(
            depthsWithAtLeastTwoNodes >= 2,
            $"Expected at least 2 non-trivial layers, got: {string.Join(", ", breadthByDepth.OrderBy(e => e.Key).Select(e => $"{e.Key}:{e.Value}"))}");
    }

    [Fact]
    public void Generate_LimitsDirectFanInToFinalNode_WhenLastFrontierIsLarge()
    {
        var definitions = new Dictionary<string, MapNodeDefinition>(StringComparer.Ordinal)
        {
            ["starting_village"] = new()
            {
                Kind = "starting_village",
                PossibleNeighbours = new Dictionary<string, float>
                {
                    ["a"] = 1f,
                    ["b"] = 1f,
                    ["c"] = 1f
                }
            },
            ["a"] = new()
            {
                Kind = "a",
                PossibleNeighbours = new Dictionary<string, float>
                {
                    ["a"] = 1f,
                    ["b"] = 1f,
                    ["c"] = 1f
                }
            },
            ["b"] = new()
            {
                Kind = "b",
                PossibleNeighbours = new Dictionary<string, float>
                {
                    ["a"] = 1f,
                    ["b"] = 1f,
                    ["c"] = 1f
                }
            },
            ["c"] = new()
            {
                Kind = "c",
                PossibleNeighbours = new Dictionary<string, float>
                {
                    ["a"] = 1f,
                    ["b"] = 1f,
                    ["c"] = 1f
                }
            }
        };

        var generator = new NavigationMapGenerator();
        const int maxNodes = 50;
        const string endKind = "final_boss";

        NavigationMap map = generator.Generate(
            definitions,
            "starting_village",
            endKind,
            maxNodes,
            new MaxRandom());

        NavigationMapNode finalNode = map.NodesById.Values.Single(node => node.Kind == endKind);
        int directIncomingToFinal = map.NodesById.Values.Count(node => node.NeighbourIds.Contains(finalNode.Id));
        int maxOtherDepth = map.NodesById.Values
            .Where(node => node.Id != finalNode.Id)
            .Max(node => node.Depth);

        Assert.True(
            directIncomingToFinal <= 3,
            $"Expected at most 3 direct links into final node, got {directIncomingToFinal}.");
        Assert.True(
            finalNode.Depth > maxOtherDepth,
            $"Expected final node to be strictly deeper than all other nodes, but final depth was {finalNode.Depth} and max other depth was {maxOtherDepth}.");
    }

    [Fact]
    public void Generate_ProducesFrontierProfile_FewManyFew()
    {
        var definitions = new Dictionary<string, MapNodeDefinition>(StringComparer.Ordinal)
        {
            ["starting_village"] = new()
            {
                Kind = "starting_village",
                PossibleNeighbours = new Dictionary<string, float>
                {
                    ["a"] = 1f,
                    ["b"] = 1f,
                    ["c"] = 1f
                }
            },
            ["a"] = new()
            {
                Kind = "a",
                PossibleNeighbours = new Dictionary<string, float>
                {
                    ["a"] = 1f,
                    ["b"] = 1f,
                    ["c"] = 1f
                }
            },
            ["b"] = new()
            {
                Kind = "b",
                PossibleNeighbours = new Dictionary<string, float>
                {
                    ["a"] = 1f,
                    ["b"] = 1f,
                    ["c"] = 1f
                }
            },
            ["c"] = new()
            {
                Kind = "c",
                PossibleNeighbours = new Dictionary<string, float>
                {
                    ["a"] = 1f,
                    ["b"] = 1f,
                    ["c"] = 1f
                }
            }
        };

        var generator = new NavigationMapGenerator();
        const int maxNodes = 60;

        NavigationMap map = generator.Generate(
            definitions,
            "starting_village",
            "final_boss",
            maxNodes,
            new MaxRandom());

        Dictionary<int, int> breadthByDepth = map.NodesById.Values
            .GroupBy(node => node.Depth)
            .ToDictionary(group => group.Key, group => group.Count());

        int maxDepth = breadthByDepth.Keys.Max();
        int earlyUpperDepth = Math.Max(1, maxDepth / 4);
        int midLowerDepth = Math.Max(earlyUpperDepth + 1, maxDepth / 3);
        int midUpperDepth = Math.Max(midLowerDepth, (2 * maxDepth) / 3);
        int lateLowerDepth = Math.Max(midUpperDepth + 1, maxDepth - 2);

        int earlyMaxBreadth = breadthByDepth
            .Where(entry => entry.Key <= earlyUpperDepth)
            .Select(entry => entry.Value)
            .DefaultIfEmpty(0)
            .Max();
        int midMaxBreadth = breadthByDepth
            .Where(entry => entry.Key >= midLowerDepth && entry.Key <= midUpperDepth)
            .Select(entry => entry.Value)
            .DefaultIfEmpty(0)
            .Max();
        int lateMaxBreadth = breadthByDepth
            .Where(entry => entry.Key >= lateLowerDepth)
            .Select(entry => entry.Value)
            .DefaultIfEmpty(0)
            .Max();

        Assert.True(
            midMaxBreadth > earlyMaxBreadth,
            $"Expected middle frontier breadth ({midMaxBreadth}) to exceed early breadth ({earlyMaxBreadth}).");
        Assert.True(
            midMaxBreadth > lateMaxBreadth,
            $"Expected middle frontier breadth ({midMaxBreadth}) to exceed late breadth ({lateMaxBreadth}).");
    }

    [Fact]
    public void Generate_UsesFixedConnectionBounds_StartIsOne_OthersAreTwoToThree()
    {
        var definitions = new Dictionary<string, MapNodeDefinition>(StringComparer.Ordinal)
        {
            ["starting_village"] = new()
            {
                Kind = "starting_village",
                PossibleNeighbours = new Dictionary<string, float>
                {
                    ["a"] = 1f,
                    ["b"] = 1f,
                    ["c"] = 1f
                }
            },
            ["a"] = new()
            {
                Kind = "a",
                PossibleNeighbours = new Dictionary<string, float>
                {
                    ["a"] = 1f,
                    ["b"] = 1f,
                    ["c"] = 1f
                }
            },
            ["b"] = new()
            {
                Kind = "b",
                PossibleNeighbours = new Dictionary<string, float>
                {
                    ["a"] = 1f,
                    ["b"] = 1f,
                    ["c"] = 1f
                }
            },
            ["c"] = new()
            {
                Kind = "c",
                PossibleNeighbours = new Dictionary<string, float>
                {
                    ["a"] = 1f,
                    ["b"] = 1f,
                    ["c"] = 1f
                }
            }
        };

        var generator = new NavigationMapGenerator();
        NavigationMap map = generator.Generate(
            definitions,
            "starting_village",
            "final_boss",
            maxNodes: 45,
            new MaxRandom());

        NavigationMapNode startNode = map.StartNode;
        Assert.Single(startNode.NeighbourIds);

        NavigationMapNode finalNode = map.NodesById.Values.Single(node => node.Kind == "final_boss");
        foreach (NavigationMapNode node in map.NodesById.Values)
        {
            if (node.Id == startNode.Id || node.Id == finalNode.Id)
            {
                continue;
            }

            int availableForwardTargets = map.NodesById.Values
                .Where(target => target.Depth > node.Depth)
                .Select(target => target.Id)
                .Distinct()
                .Count();

            if (availableForwardTargets < 2)
            {
                // End-of-map closure can leave only one feasible forward target.
                continue;
            }

            Assert.InRange(node.NeighbourIds.Count, 2, 3);
        }
    }

    private static Dictionary<int, int> CalculateBreadthByDepth(NavigationMap map)
    {
        var depthByNodeId = new Dictionary<int, int> { [map.StartNodeId] = 0 };
        var queue = new Queue<int>();
        queue.Enqueue(map.StartNodeId);

        while (queue.Count > 0)
        {
            int current = queue.Dequeue();
            int currentDepth = depthByNodeId[current];
            NavigationMapNode node = map.NodesById[current];

            foreach (int neighbourId in node.NeighbourIds)
            {
                if (!map.NodesById.ContainsKey(neighbourId))
                {
                    continue;
                }

                int nextDepth = currentDepth + 1;
                if (depthByNodeId.TryGetValue(neighbourId, out int knownDepth) && knownDepth <= nextDepth)
                {
                    continue;
                }

                depthByNodeId[neighbourId] = nextDepth;
                queue.Enqueue(neighbourId);
            }
        }

        return depthByNodeId
            .GroupBy(pair => pair.Value)
            .ToDictionary(group => group.Key, group => group.Count());
    }

    private sealed class MinRandom : INavigationRandom
    {
        public int NextInt(int minInclusive, int maxExclusive) => minInclusive;

        public double NextDouble() => 0;
    }

    private sealed class MaxRandom : INavigationRandom
    {
        public int NextInt(int minInclusive, int maxExclusive) => maxExclusive - 1;

        public double NextDouble() => 0.999999;
    }
}
