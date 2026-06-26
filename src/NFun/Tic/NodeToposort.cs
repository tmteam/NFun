using System;
using System.Collections.Generic;
using NFun.Exceptions;
using NFun.Tic.Errors;
using NFun.Tic.SolvingStates;

// ReSharper disable ConvertIfStatementToSwitchStatement
// ReSharper disable LoopCanBeConvertedToQuery

namespace NFun.Tic; 

public class NodeToposort {
    public TicNode[] NonReferenceOrdered { get; private set; }

    private Stack<TicNode> _path;
    private readonly List<TicNode> _allNodes;

    private int _referenceNodesCount = 0;

    public NodeToposort(int capacity) {
        _allNodes = new List<TicNode>(capacity);
        _searchNonReferenceAlgorithm = new RefCycleSearchAlgorithm(NodeInListMark);
    }

    private int _visitDepth = 0;

    /// <summary>Topological sort; if <paramref name="onNodeReady"/> is non-null, invokes it on each non-reference node in order (streaming Pull fusion).</summary>
    public void OptimizeTopology(Action<TicNode> onNodeReady = null) {
        _path = new Stack<TicNode>(_allNodes.Count);

        foreach (var nonReferenceNode in _allNodes)
            Visit(nonReferenceNode);

        // Post-process: dereference ancestors, transfer RefTo edges, build result array.
        // _referenceNodesCount only counts RefTo nodes encountered live during Visit;
        // MergeGroup creates additional RefTos post-Visit, so we size optimistically and
        // trim with Array.Resize.
        NonReferenceOrdered = new TicNode[_path.Count - _referenceNodesCount];
        var nonRefId = 0;
        foreach (var node in _path)
        {
            for (var i = 0; i < node.Ancestors.Count; i++)
            {
                var ancestor = node.Ancestors[i];
                if (ancestor.State is StateRefTo ancrRefTo)
                {
                    var deref = ancrRefTo.Node.GetNonReference();
                    // Identity-share via IsLiveSnapshotableFun can leave a RefTo ancestor whose
                    // deref-target is `node` itself. Drop the stale self-edge — `T ≤ T` is the
                    // identity ordering element and must be elided structurally.
                    if (deref == node)
                    {
                        node.RemoveAncestor(ancestor);
                        i--;
                        continue;
                    }
                    node.SetAncestor(i, deref);
                }
            }

            if (node.State is StateRefTo refTo)
            {
                foreach (var refAncestor in node.Ancestors)
                {
                    // Skip self-edges that arise when an ancestor IS the RefTo target — happens
                    // for SetCall(F-bounded fun) where return state RefTo(fun.RetNode) and
                    // fun.RetNode is already in the chain.
                    if (refAncestor == refTo.Node) continue;
                    refTo.Node.AddAncestor(refAncestor);
                }
                node.ClearAncestors();
            }
            else
            {
                NonReferenceOrdered[nonRefId] = node;
                nonRefId++;

                if (node.State is ICompositeState composite)
                    node.State = composite.GetNonReferenced();

                // Streaming Pull (when onNodeReady is set).
                onNodeReady?.Invoke(node);
            }
        }

        if (nonRefId < NonReferenceOrdered.Length) {
            var trimmed = new TicNode[nonRefId];
            Array.Copy(NonReferenceOrdered, trimmed, nonRefId);
            NonReferenceOrdered = trimmed;
        }
    }

    private Stack<TicNode> _cycle = null;
    private TicNode _cycleInitiator = null;
    private readonly RefCycleSearchAlgorithm _searchNonReferenceAlgorithm;

    private const int InProcess = 42;
    private const int IsVisited = -42;
    private const int NotVisited = 0;

    public void AddMany(params TicNode[] nodes) {
        foreach (var node in nodes)
        {
            AddToTopology(node);
        }
    }

    private const int NodeInListMark = TicVisitMarks.NodeInList;

    public void AddToTopology(TicNode node) {
        if (node == null)
            return;
        if (node.VisitMark == NodeInListMark)
            return;
        var nonReference
            = _searchNonReferenceAlgorithm.GetNonReferenceMergedOrNull(node);

        if (nonReference != null && nonReference.VisitMark != NodeInListMark)
        {
            nonReference.VisitMark = NodeInListMark;
            if(nonReference.State is StateRefTo)
                AssertChecks.Panic($"Toposort adds reference node to list: {node}");
            _allNodes.Add(nonReference);
        }
    }


    private bool Visit(TicNode node) {
        _visitDepth++;
        if (_visitDepth > 1000)
            throw new InvalidOperationException($"Toposort stack overflow. Node: {node}");

        try
        {
            if (node == null)
                return true;
            if (node.VisitMark == IsVisited)
                return true;

            if (node.VisitMark == InProcess)
            {
                // Re-entry — start collecting the cycle.
                _cycle = new Stack<TicNode>(_path.Count + 1);
                _cycleInitiator = node;
                return false;
            }

            node.VisitMark = InProcess;

            if (node.State is StateRefTo refTo)
            {
                _referenceNodesCount++;
                if (!Visit(refTo.Node))
                {
                    _referenceNodesCount--; // VisitNodeInCycle rolls back graph
                    return VisitNodeInCycle(node);
                }
            }
            else if (node.State is ICompositeState composite)
            {
                for (int mi = 0; mi < composite.MemberCount; mi++)
                    if (!Visit(composite.GetMember(mi)))
                    {
                        // Composite-member edge is contractive (Cardelli-Mitchell '89 §3) — mark
                        // initiator and continue.
                        if (_cycleInitiator != null)
                        {
                            _cycleInitiator.IsContractiveCycleHead = true;
                            _cycle = null;
                            _cycleInitiator = null;
                        }
                        else
                        {
                            ThrowRecursiveTypeDefinition(node);
                        }
                    }
            }

            // ReSharper disable once ForCanBeConvertedToForeach
            for (var i = 0; i < node.Ancestors.Count; i++)
            {
                var ancestor = node.Ancestors[i];
                if (!Visit(ancestor))
                    return VisitNodeInCycle(node);
            }

            _path.Push(node);
            node.VisitMark = IsVisited;
            return true;
        }
        finally
        {
            _visitDepth--;
        }
    }

    private void ThrowRecursiveTypeDefinition(TicNode node) {
        _cycle.Push(node);
        throw TicErrors.RecursiveTypeDefinition(_cycle.ToArray());
    }

    private bool VisitNodeInCycle(TicNode node) {
        node.VisitMark = NotVisited;
        _cycle.Push(node);

        if (_cycleInitiator != node)
            return false; // keep collecting

        // Full cycle collected — merge: (a ≤ b ≤ c = a) ⇒ (a = b = c).
        var cycleArray = _cycle.ToArray();
        Array.Reverse(cycleArray);
        var merged = SolvingFunctions.MergeGroup(cycleArray);

        _cycle = null;
        _cycleInitiator = null;

        return Visit(merged);
    }
}