using VikingJamGame.Models.Navigation;

namespace VikingJamGame.Tests.Models.Navigation;

public sealed class NavigationSessionTests
{
    [Fact]
    public void Constructor_StartsAtMapStartNodeByDefault()
    {
        NavigationMap map = CreateSampleMap();

        var session = new NavigationSession(map);

        Assert.Equal(map.StartNodeId, session.CurrentNodeId);
    }

    [Fact]
    public void BackwardOptions_AreDerivedFromIncomingLinks()
    {
        NavigationMap map = CreateSampleMap();
        var session = new NavigationSession(map);
        session.MoveTo(1);
        session.MoveTo(3);

        Assert.Equal([4], session.GetForwardNodeIds());
        Assert.Equal([1, 2], session.GetBackwardNodeIds());
    }

    [Fact]
    public void TryMoveTo_AllowsForwardAndBackwardMoves()
    {
        NavigationMap map = CreateSampleMap();
        var session = new NavigationSession(map);

        Assert.True(session.TryMoveTo(1)); // forward from start
        Assert.True(session.TryMoveTo(0)); // backward to start
        Assert.Equal(0, session.CurrentNodeId);
    }

    [Fact]
    public void TryMoveTo_ReturnsFalseForUnreachableNode()
    {
        NavigationMap map = CreateSampleMap();
        var session = new NavigationSession(map);

        bool moved = session.TryMoveTo(4);

        Assert.False(moved);
        Assert.Equal(map.StartNodeId, session.CurrentNodeId);
    }

    [Fact]
    public void AvailableMoves_DeDuplicatesForwardBackwardOverlap()
    {
        NavigationMap map = CreateBidirectionalEdgeMap();
        var session = new NavigationSession(map);

        session.MoveTo(1);
        IReadOnlyList<int> availableMoves = session.GetAvailableMoveNodeIds();

        Assert.Equal([0], availableMoves);
    }

    [Fact]
    public void Constructor_InitializesConsecutiveNodesOfSameTypeToOne()
    {
        NavigationMap map = CreateSampleMap();

        var session = new NavigationSession(map);

        Assert.Equal(1, session.ConsecutiveNodesOfSameType);
    }

    [Fact]
    public void MoveTo_TracksConsecutiveNodesOfSameTypeAcrossForwardAndBackwardMoves()
    {
        NavigationMap map = CreateConsecutiveKindMap();
        var session = new NavigationSession(map);

        session.MoveTo(1); // start -> a
        Assert.Equal(1, session.ConsecutiveNodesOfSameType);

        session.MoveTo(2); // a -> a
        Assert.Equal(2, session.ConsecutiveNodesOfSameType);

        session.MoveTo(3); // a -> b
        Assert.Equal(1, session.ConsecutiveNodesOfSameType);

        session.MoveTo(2); // b -> a (backward)
        Assert.Equal(1, session.ConsecutiveNodesOfSameType);

        session.MoveTo(1); // a -> a (backward)
        Assert.Equal(2, session.ConsecutiveNodesOfSameType);
    }

    private static NavigationMap CreateSampleMap()
    {
        var nodes = new Dictionary<int, NavigationMapNode>
        {
            [0] = new()
            {
                Id = 0,
                Kind = "start",
                Depth = 0,
                NeighbourIds = [1, 2]
            },
            [1] = new()
            {
                Id = 1,
                Kind = "a",
                Depth = 1,
                NeighbourIds = [3]
            },
            [2] = new()
            {
                Id = 2,
                Kind = "b",
                Depth = 1,
                NeighbourIds = [3]
            },
            [3] = new()
            {
                Id = 3,
                Kind = "c",
                Depth = 2,
                NeighbourIds = [4]
            },
            [4] = new()
            {
                Id = 4,
                Kind = "end",
                Depth = 3,
                NeighbourIds = []
            }
        };

        return new NavigationMap
        {
            StartNodeId = 0,
            NodesById = nodes
        };
    }

    private static NavigationMap CreateBidirectionalEdgeMap()
    {
        var nodes = new Dictionary<int, NavigationMapNode>
        {
            [0] = new()
            {
                Id = 0,
                Kind = "start",
                Depth = 0,
                NeighbourIds = [1]
            },
            [1] = new()
            {
                Id = 1,
                Kind = "a",
                Depth = 1,
                NeighbourIds = [0]
            }
        };

        return new NavigationMap
        {
            StartNodeId = 0,
            NodesById = nodes
        };
    }

    private static NavigationMap CreateConsecutiveKindMap()
    {
        var nodes = new Dictionary<int, NavigationMapNode>
        {
            [0] = new()
            {
                Id = 0,
                Kind = "start",
                Depth = 0,
                NeighbourIds = [1]
            },
            [1] = new()
            {
                Id = 1,
                Kind = "a",
                Depth = 1,
                NeighbourIds = [2]
            },
            [2] = new()
            {
                Id = 2,
                Kind = "a",
                Depth = 2,
                NeighbourIds = [3]
            },
            [3] = new()
            {
                Id = 3,
                Kind = "b",
                Depth = 3,
                NeighbourIds = []
            }
        };

        return new NavigationMap
        {
            StartNodeId = 0,
            NodesById = nodes
        };
    }
}
