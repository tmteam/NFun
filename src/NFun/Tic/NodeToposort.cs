using System;
using System.Collections.Generic;
using System.Linq;
using NFun.Tic.Errors;
using NFun.Tic.SolvingStates;

// ReSharper disable ForeachCanBeConvertedToQueryUsingAnotherGetEnumerator
// ReSharper disable ConvertIfStatementToSwitchStatement
// ReSharper disable LoopCanBeConvertedToQuery

namespace NFun.Tic
{
    public class NodeToposort
    {
        public TicNode[] NonReferenceOrdered { get; private set; }
        //public TicNode[] References { get; private set; }

        private Stack<TicNode> _path;
        private int _refenceNodesCount = 0;

        public NodeToposort(int capacity)
        {
            _path = new Stack<TicNode>(capacity);            
        }
        
        public void OptimizeTopology()
        {
            //at this moment we have garanties that graph has no cycles
            NonReferenceOrdered = new TicNode[_path.Count- _refenceNodesCount];
            //var refs = new TicNode[_refenceNodesCount];
            var nonRefId = 0;
            //var refId = 0;
            foreach (var node in _path)
            {
                for (var i = 0; i < node.Ancestors.Count; i++)
                {
                    var ancestor = node.Ancestors[i];
                    if (ancestor.State is StateRefTo ancrRefTo) 
                        node.Ancestors[i] = ancrRefTo.Node.GetNonReference();
                }

                if (node.State is StateRefTo refTo)
                {
                    foreach (var refAncestor in node.Ancestors) 
                        refTo.Node.Ancestors.Add(refAncestor);
                    node.Ancestors.Clear();
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
        
        private const int InProcess = 42;
        private const int IsVisited = -42;
        private const int NotVisited = 0;

        public bool AddToTopology(TicNode node)
        {
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
                if (!AddToTopology(refTo.Node))
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
                    if (!AddToTopology(member)) 
                        ThrowRecursiveTypeDefenition(node);

            }
                
            foreach (var ancestor in node.Ancestors) 
                if(!AddToTopology(ancestor)) 
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
                
            SolvingFunctions.MergeGroup(_cycle.Reverse());
                
            // Cycle is merged
            _cycle = null;
            _cycleInitiator = null;
            // continue toposort algorithm
            return AddToTopology(node);

            // Whole cycle is not found yet            
            // step back
        }
    }
}