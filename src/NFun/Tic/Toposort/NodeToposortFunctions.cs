using System;
using System.Collections.Generic;
using System.Linq;
using NFun.Tic.SolvingStates;

namespace NFun.Tic.Toposort
{
    public class NodeSortResult
    {
        public NodeSortResult(SolvingNode[] order, SolvingNode[] refs, SortStatus status)
        {
            Order = order;
            Refs = refs;
            Status = status;
        }

        public SolvingNode[] Order { get; }
        public SolvingNode[] Refs { get; }
        public SortStatus Status { get; }
    }

    public enum SortStatus
    {
        Sorted,
        AncestorCycle,
        MemebershipCycle
    }
    public static class NodeToposortFunctions
    {
        public static NodeSortResult Toposort(SolvingNode[] nodes)
        {
            //The node list probably contains reference loops
            //We need to remove these loops to avoid Stackoverflow exceptions 
            RemoveRefenceLoops(nodes);

            //Need to remove all reference nodes from the graph. They interfere with the toposort and can be considered solved
            MergeAllReferences(nodes, out var referenceNodes, out var nonReferenceNOdes);
            
            //Do toposort
            var graph = ConvertToArrayGraph(nonReferenceNOdes);
            var sorted = GraphTools.SortTopology(graph);

            
            //var order = sorted.NodeNames.Select(n => nonReferenceNOdes[n.To]).Reverse().ToArray();
            var order = new SolvingNode[sorted.NodeNames.Count];
            int pos = 0;
            for (int i = sorted.NodeNames.Count - 1; i >= 0; i--)
            {
                var nodeName = sorted.NodeNames[i];
                order[pos] = nonReferenceNOdes[nodeName.To];
                pos++;
            }
            
            return new NodeSortResult(order, referenceNodes, 
                sorted.HasLoop
                ? sorted.NodeNames.Any(n => n.Type == EdgeType.Member)
                    ? SortStatus.MemebershipCycle // if any edge is of membership type, than recursive type defenition found. Example t = t[0]
                    : SortStatus.AncestorCycle    // Ancestor cycle found.  Example: x = x+1. 
                : SortStatus.Sorted);
        }

        private static void MergeAllReferences(SolvingNode[] nodes, out SolvingNode[] refs, out SolvingNode[] concretes)
        {
            refs = null;
            concretes = null;
            var references = new LinkedList<SolvingNode>();
            var concretesBuffer = new LinkedList<SolvingNode>();
            foreach (var node in nodes)
            {
                if (node.State is RefTo refTo)
                {
                    references.AddLast(node);
                    var real = node.GetNonReference();
                    var nrAncestors = GetNonReferenced(node.Ancestors);
                    real.Ancestors.AddRange(nrAncestors);
                    node.Ancestors.Clear();

                    if (refTo.Node != real)
                        node.State = new RefTo(real);
                }
                else
                {
                    concretesBuffer.AddLast(node);
                    if (node.State is ICompositeType composite)
                    {
                        if (composite.Members.Any(m => m.State is RefTo))
                            node.State = composite.GetNonReferenced();
                    }

                    var nrAncestors = GetNonReferenced(node.Ancestors);
                    node.Ancestors.Clear();
                    node.Ancestors.AddRange(nrAncestors);
                }
            }

            refs = references.ToArray();
            concretes = concretesBuffer.ToArray();
        }

        private static SolvingNode[] GetNonReferenced(IList<SolvingNode> nodes)
        {
            SolvingNode[] ans =new SolvingNode[nodes.Count];
            for (int i = 0; i < nodes.Count; i++)
            {
                var node = nodes[i];
                if (node.State is RefTo)
                    ans[i] = node.GetNonReference();
                else
                    ans[i] = node;
            }

            return ans;
        }


        private static void RemoveRefenceLoops(SolvingNode[] nodes)
        {
            while (true)
            {
                var refList = FindRefNodesGraph(nodes);

                var refGraph = ConvertToRefArrayGraph(refList);
                var refTopology = GraphTools.SortTopology(refGraph);
                if (!refTopology.HasLoop)
                    return;

                //var refCycle = refTopology.NodeNames.Select(n => nodes[n.To]).ToArray();
                var refCycle = new SolvingNode[refTopology.NodeNames.Count];
                for (int i = 0; i < refCycle.Length; i++)
                {
                    var to = refTopology.NodeNames[i].To;
                    refCycle[i] = nodes[to];
                }
                SolvingFunctions.MergeGroup(refCycle);
            }
        }

        private static LinkedList<SolvingNode> FindRefNodesGraph(SolvingNode[] nodes)
        {
            var refList = new LinkedList<SolvingNode>();
            foreach (var node in nodes)
            {
                if (node.State is RefTo refTo)
                {
                    refList.AddLast(node);
                    if (!(refTo.Node.State is RefTo))
                        refList.AddLast(refTo.Node);
                }
            }

            return refList;
        }

        public static Edge[][] ConvertToRefArrayGraph(LinkedList<SolvingNode> allNodes)
        {
            var graph = new LinkedList<Edge>[allNodes.Count];

            int i = 0;
            foreach (var solvingNode in allNodes)
            {
                solvingNode.GraphId = i;
                i++;
            }

            foreach (var node in allNodes)
            {
                if (node.State is RefTo reference)
                {
                    var from = node.GraphId;
                    var to = reference.Node.GraphId;
                    if (graph[to] == null)
                        graph[to] = new LinkedList<Edge>();
                    if (graph[from] == null)
                        graph[from] = new LinkedList<Edge>();

                    //Two nodes references each other
                    graph[from].AddLast(Edge.ReferenceTo(to));
                    graph[to].AddLast(Edge.ReferenceTo(from));
                }
            }
            return graph.Select(g => g?.ToArray()).ToArray();
        }
        public static Edge[][] ConvertToArrayGraph(SolvingNode[] allNodes)
        {
            var graph = new LinkedList<Edge>[allNodes.Length];
            for (int i = 0; i < allNodes.Length; i++)
                allNodes[i].GraphId = i;

            foreach (var node in allNodes)
            {
                var from = node.GraphId;
                if (from < 0)
                    throw new InvalidOperationException();

                if (node.State is RefTo)
                    throw new InvalidOperationException();
                
                if (graph[@from] == null)
                    graph[@from] = new LinkedList<Edge>();

                foreach (var anc in node.Ancestors)
                {
                    var ancId = anc.GetNonReference().GraphId;
                    graph[from].AddLast(Edge.AncestorTo(ancId));
                }
                
                if (node.State is ICompositeType composite)
                {
                    foreach (var member in composite.Members)
                    {
                        if(member.State is RefTo)
                            throw new InvalidOperationException();
                        
                        var mfrom = member.GraphId;
                        
                        if (mfrom < 0)
                            continue;
                            //throw new InvalidOperationException();

                        if (graph[mfrom] == null)
                            graph[mfrom] = new LinkedList<Edge>();
                        graph[mfrom].AddLast(Edge.MemberOf(from));
                    }
                }
            }

            return graph.Select(g => g?.ToArray()).ToArray();
        }
    }
}
