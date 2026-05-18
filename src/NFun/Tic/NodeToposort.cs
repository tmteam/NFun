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

    /// <summary>
    /// Topological sort + optional per-node callback (streaming Pull fusion).
    /// If onNodeReady is provided, it is called for each non-reference node
    /// immediately after post-processing, in toposort order.
    /// </summary>
    public void OptimizeTopology(Action<TicNode> onNodeReady = null) {
        // Cycle handling relies on Stages.Invoke's visited-pair guard; no pre-pass marking is
        // needed here. The in-Visit cycle initiator detection below still sets
        // IsContractiveCycleHead for the paths that consume it.

        _path = new Stack<TicNode>(_allNodes.Count);

        foreach (var nonReferenceNode in _allNodes)
            Visit(nonReferenceNode);

        // Post-process: dereference ancestors, transfer RefTo edges, build result array.
        // `_referenceNodesCount` only tracks RefTo nodes encountered live during Visit;
        // MergeGroup (called from VisitNodeInCycle) ALSO converts cycle members to RefTo
        // post-Visit, and those don't increment the counter. Pre-sizing the array off
        // that counter would leave trailing null entries when a cycle merged >1 node
        // (BugHunt-stmt #51: multiple `p.x = N` field mutations on an anonymous-typed
        // struct produced 2+ cycles, each merging into one node). Size optimistically
        // then Array.Resize if MergeGroup created extra RefTos.
        NonReferenceOrdered = new TicNode[_path.Count - _referenceNodesCount];
        var nonRefId = 0;
        foreach (var node in _path)
        {
            for (var i = 0; i < node.Ancestors.Count; i++)
            {
                var ancestor = node.Ancestors[i];
                if (ancestor.State is StateRefTo ancrRefTo)
                    node.SetAncestor(i, ancrRefTo.Node.GetNonReference());
            }

            if (node.State is StateRefTo refTo)
            {
                foreach (var refAncestor in node.Ancestors)
                {
                    // Skip self-edges that would arise when transferring
                    // ancestors of a RefTo'd node where one ancestor IS the
                    // refTo target. Happens when SetCall(F-bounded fun)
                    // produces a return node with State=RefTo(fun.RetNode)
                    // AND fun.RetNode is in the ancestor chain (cycle).
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

                // Streaming: process node immediately in toposort order
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

    private const int NodeInListMark = -33753;

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
                // Node is visiting, that means cycle found
                // initialize cycle collecting process
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
                    // VisitNodeInCycle rolls back graph
                    // so we need to decrement counter
                    _referenceNodesCount--;
                    // this node is part of cycle
                    return VisitNodeInCycle(node);
                }
            }
            else if (node.State is ICompositeState composite)
            {
                for (int mi = 0; mi < composite.MemberCount; mi++)
                    if (!Visit(composite.GetMember(mi)))
                    {
                        // A composite-member edge is contractive by construction (Cardelli-Mitchell
                        // '89 §3). Mark cycle initiator and continue toposort.
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
        {
            //continue to collect cycle route
            return false;
        }

        // Ref and/or ancestor cycle found
        // That means all elements in cycle have to be merged

        // (a<= b <= c = a)  =>  (a = b = c) 

        // Reverse cycle in-place (avoid LINQ Reverse() allocation)
        var cycleArray = _cycle.ToArray();
        Array.Reverse(cycleArray);
        var merged = SolvingFunctions.MergeGroup(cycleArray);

        // Cycle is merged
        _cycle = null;
        _cycleInitiator = null;

        // continue toposort algorithm
        return Visit(merged);

        // Whole cycle is not found yet            
        // step back
    }
}