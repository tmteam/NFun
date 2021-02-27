using System;
using System.Collections.Generic;
using System.Linq;
using NFun.Exceptions;
using NFun.Tic.Errors;
using NFun.Tic.SolvingStates;

// ReSharper disable ForeachCanBeConvertedToQueryUsingAnotherGetEnumerator
// ReSharper disable ConvertIfStatementToSwitchStatement
// ReSharper disable LoopCanBeConvertedToQuery

namespace NFun.Tic
{
    class RefCycleSearchAlgorithm
    {
        private readonly int _nodeInListMark;

        private const int RefVisitingMark = 6782341;
        private const int RefVisitedMark = 672901236;

        public TicNode GetNonReferenceMergedOrNull(TicNode node)
        {
            if (node.VisitMark == RefVisitedMark)
                return null;
            
            _refRoute = new Stack<TicNode>();
            var nonReference = GetNonReferenceNodeOrNull(node);
            if (nonReference != null) 
                return nonReference;
            
            // ref cycle found!
            // the node becomes one non reference node with no constrains
            node.State = new ConstrainsState();
            foreach (var refNode in _refRoute)
            {
                if (refNode == node)
                    continue;
                if(refNode.VisitMark== RefVisitedMark)
                    continue;
                node.AddAncestors(refNode.Ancestors);
                refNode.ClearAncestors();
                refNode.VisitMark = RefVisitedMark;
                if (refNode.IsMemberOfAnything)
                    refNode.IsMemberOfAnything = false;
            }
            return node;
        }

        private Stack<TicNode> _refRoute = null;

        public RefCycleSearchAlgorithm(int nodeInListMark)
        {
            _nodeInListMark = nodeInListMark;
        }

        private TicNode GetNonReferenceNodeOrNull(TicNode node)
        {
            if (node.VisitMark == _nodeInListMark)
                return node;
            if (!(node.State is StateRefTo refTo)) 
                return node;
            if (node.VisitMark == RefVisitingMark)
                return null;
            node.VisitMark = RefVisitingMark;
            var res =  GetNonReferenceNodeOrNull(refTo.Node);
            if(res==null)
                _refRoute.Push(node);
            else
            {
                node.VisitMark = -1;
                //merge
                if (node.Ancestors.Any())
                {
                    res.AddAncestors(node.Ancestors);
                    node.ClearAncestors();
                }
            }
            return res;
        }

    }
    public class NodeToposort2
    {
        public TicNode[] NonReferenceOrdered { get; private set; }

        private Stack<TicNode> _path;
        private readonly List<TicNode> _allNodes;

        private int _refenceNodesCount = 0;

        public NodeToposort2(int capacity)
        {
            _allNodes = new List<TicNode>(capacity);
            _searchNonReferenceAlgorithm = new RefCycleSearchAlgorithm(NodeInListMark);
        }

        private int _visitDepth = 0;
        public void OptimizeTopology()
        {
            //Trying to find ancestor cycles
            
            _path = new Stack<TicNode>(_allNodes.Count);
            
            foreach (var nonReferenceNode in _allNodes) {
                Visit(nonReferenceNode);
            }
            //at this moment we have garanties that graph has no cycles
            NonReferenceOrdered = new TicNode[_path.Count- _refenceNodesCount];
            var nonRefId = 0;
            foreach (var node in _path)
            {
                for (var i = 0; i < node.Ancestors.Count; i++)
                {
                    var ancestor = node.Ancestors[i];
                    if (ancestor.State is StateRefTo ancrRefTo) 
                        node.SetAncestor(i,ancrRefTo.Node.GetNonReference());
                }

                if (node.State is StateRefTo refTo)
                { 
                    foreach (var refAncestor in node.Ancestors) 
                        refTo.Node.AddAncestor(refAncestor);
                    node.ClearAncestors();
                }
                else
                {
                    NonReferenceOrdered[nonRefId] =  node;
                    nonRefId++;

                    if (node.State is ICompositeState composite) 
                        node.State = composite.GetNonReferenced();
                }
            }
        }

        private Stack<TicNode> _cycle = null;
        private TicNode _cycleInitiator = null;
        private readonly RefCycleSearchAlgorithm _searchNonReferenceAlgorithm;

        private const int InProcess = 42;
        private const int IsVisited = -42;
        private const int NotVisited = 0;

        public void AddMany(params TicNode[] nodes)
        {
            foreach (var node in nodes)
            {
                AddToTopology(node);
            }
        }

        private const int NodeInListMark = -33753;
       

        public void AddToTopology(TicNode node)
        {
            if(node==null)
                return;
            if (node.VisitMark == NodeInListMark)
                return;
            var nonReference 
                = _searchNonReferenceAlgorithm.GetNonReferenceMergedOrNull(node);
            
            if (nonReference!=null && nonReference.VisitMark != NodeInListMark) {
                nonReference.VisitMark = NodeInListMark;
                if (nonReference.State is StateRefTo)
                    throw new ImpossibleException($"Toposort adds reference node to list: {node}");
                _allNodes.Add(nonReference);
            }
        }

        

        private bool Visit(TicNode node)
        {
            _visitDepth++;
            if (_visitDepth > 1000)
                throw new InvalidOperationException($"Toposort stack overflow. Node: {node}");
            
            if (node == null)
                return true;
            if (node.VisitMark == IsVisited)
            {
                // if node is already visited then skip it
                return true;
            }  
            if (node.VisitMark == InProcess)
            {
                // Node is visiting, that means cycle found
                // initialize cycle collecting process
                _cycle = new Stack<TicNode>(_path.Count+1);
                _cycleInitiator = node;
                return false;
            }
            
            
            node.VisitMark = InProcess;

            if (node.State is StateRefTo refTo)
            {
                _refenceNodesCount++;
                if (!Visit(refTo.Node))
                {
                    // VisitNodeInCycle rolls back graph
                    // so we need to decrement counter
                    _refenceNodesCount--;
                    // this node is part of cycle
                    return VisitNodeInCycle(node);
                }
            }
            else if (node.State is ICompositeState composite)
            {
                foreach (var member in composite.Members)
                    if (!Visit(member)) 
                        ThrowRecursiveTypeDefenition(node);
            }
                
            foreach (var ancestor in node.Ancestors) 
                if(!Visit(ancestor)) 
                    return VisitNodeInCycle(node);
                
            _path.Push(node);
            node.VisitMark = IsVisited;
            return true;
        }

        private void ThrowRecursiveTypeDefenition(TicNode node)
        {
            _cycle.Push(node);
            throw TicErrors.RecursiveTypeDefinition(_cycle.ToArray());
        }
        private bool VisitNodeInCycle(TicNode node)
        {
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
                
            var merged = SolvingFunctions.MergeGroup(_cycle.Reverse());
            //foreach (var item in _cycle)
            //    item.VisitMark = IsVisited;
            //merged.VisitMark = NotVisited;

            // Cycle is merged
            _cycle = null;
            _cycleInitiator = null;
            
            // continue toposort algorithm
            return Visit(merged);

            // Whole cycle is not found yet            
            // step back
        }
    }
}