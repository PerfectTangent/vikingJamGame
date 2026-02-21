using System;
using System.Collections.Generic;
using Chickensoft.AutoInject;
using Chickensoft.Introspection;
using Godot;
using VikingJamGame.Models.Navigation;

namespace VikingJamGame.GameLogic.Nodes;

[GlobalClass]
[Meta(typeof(IAutoNode))]
public partial class GodotNavigationSession : Node
{
    [Dependency] public GodotMapGenerator Generator => this.DependOn<GodotMapGenerator>();

    [ExportCategory("Configuration")]
    [Export] private bool AutoInitializeOnReady { get; set; } = true;

    private NavigationSession _session = null!;
    private bool _isInitialized;

    public bool IsInitialized => _isInitialized;
    public int CurrentNodeId => RequireSession().CurrentNodeId;
    public NavigationMapNode CurrentNode => RequireSession().CurrentNode;
    public IReadOnlySet<int> VisitedNodeIds => RequireSession().VisitedNodeIds;
    public int ConsecutiveNodesOfSameType => RequireSession().ConsecutiveNodesOfSameType;

    public IReadOnlyList<int> GetForwardNodeIds() => RequireSession().GetForwardNodeIds();
    public IReadOnlyList<int> GetBackwardNodeIds() => RequireSession().GetBackwardNodeIds();
    public IReadOnlyList<int> GetAvailableMoveNodeIds() => RequireSession().GetAvailableMoveNodeIds();

    public bool IsForwardMove(int nodeId) => RequireSession().IsForwardMove(nodeId);
    public bool IsBackwardMove(int nodeId) => RequireSession().IsBackwardMove(nodeId);
    public bool CanMoveTo(int nodeId) => RequireSession().CanMoveTo(nodeId);
    public bool TryMoveTo(int nodeId) => RequireSession().TryMoveTo(nodeId);
    public void MoveTo(int nodeId) => RequireSession().MoveTo(nodeId);

    public void OnResolved()
    {
        if (!AutoInitializeOnReady)
        {
            return;
        }

        Initialize();
    }

    public void Initialize(int? startNodeId = null)
    {
        GodotMapGenerator generator = RequireGenerator();
        NavigationMap map = generator.CurrentMap ?? throw new NullReferenceException(
            "Map generator current map was null. Ensure generation happened before session initialization.");

        _session = new NavigationSession(map, startNodeId);
        _isInitialized = true;
    }

    public bool TryInitialize(int? startNodeId = null)
    {
        if (Generator.CurrentMap is null)
        {
            return false;
        }

        Initialize(startNodeId);
        return true;
    }

    private NavigationSession RequireSession()
    {
        return _isInitialized
            ? _session
            : throw new InvalidOperationException(
                $"{nameof(GodotNavigationSession)} is not initialized. Call {nameof(Initialize)} first.");
    }

    private GodotMapGenerator RequireGenerator()
    {
        return Generator
               ?? throw new InvalidOperationException(
                   $"{nameof(Generator)} must be assigned.");
    }
    
    public override void _Notification(int what) => this.Notify(what);
}
