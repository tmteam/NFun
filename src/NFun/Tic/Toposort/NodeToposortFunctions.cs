using System;
using System.Collections.Generic;
using System.Linq;
using NFun.Tic.SolvingStates;

namespace NFun.Tic.Toposort
{
    public class NodeSortResult
    {
        public NodeSortResult(SolvingNode[] order, IList<SolvingNode> refs, SortStatus status)
        {
            Order = order;
            Refs = refs;
            Status = status;
        }

        public SolvingNode[] Order { get; }
        public IList<SolvingNode> Refs { get; }
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

        private static void MergeAllReferences(SolvingNode[] nodes, out IList<SolvingNode> refs, out IList<SolvingNode> concretes)
        {
            refs = null;
            concretes = null;
            var references = new List<SolvingNode>();
            var concretesBuffer = new List<SolvingNode>();
            foreach (var node in nodes)
            {
                if (node.State is RefTo refTo)
                {
                    references.Add(node);
                    var real = node.GetNonReference();
                    AppendNonReferencedToList(node.Ancestors, real.Ancestors);
                    node.Ancestors.Clear();

                    if (refTo.Node != real)
                        node.State = new RefTo(real);
                }
                else
                {
                    concretesBuffer.Add(node);
                    if (node.State is ICompositeType composite)
                    {
                        if (composite.HasAnyReferenceMember)
                            node.State = composite.GetNonReferenced();
                    }
                    ReplaceWithNonReferences(node.Ancestors);
                }
            }

            refs = references;
            concretes = concretesBuffer;
        }
    
        
        private static void AppendNonReferencedToList(IList<SolvingNode> nodes, List<SolvingNode> targetList)
        {
            foreach (var node in nodes)
            {
                if (node.State is RefTo)
                    targetList.Add(node.GetNonReference());
                else
                    targetList.Add(node);
            }
        }
        
        /// <summary>
        /// Replaces every item in the list with it non referenced node
        /// </summary>
        private static void ReplaceWithNonReferences(List<SolvingNode> nodes)
        {
            for (int i = 0; i < nodes.Count; i++)
            {
                var node = nodes[i];
                if (node.State is RefTo)
                    nodes[i] = node.GetNonReference();
            }
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

                var refCycle = new SolvingNode[refTopology.NodeNames.Count];
                for (int i = 0; i < refCycle.Length; i++)
                {
                    var to = refTopology.NodeNames[i].To;
                    refCycle[i] = nodes[to];
                }
                SolvingFunctions.MergeGroup(refCycle);
            }
        }

        private static List<SolvingNode> FindRefNodesGraph(SolvingNode[] nodes)
        {
            var refList = new List<SolvingNode>();
            foreach (var node in nodes)
            {
                if (node.State is RefTo refTo)
                {
                    refList.Add(node);
                    if (!(refTo.Node.State is RefTo))
                        refList.Add(refTo.Node);
                }
            }

            return refList;
        }

        public static Edge[][] ConvertToRefArrayGraph(List<SolvingNode> allNodes)
        {
            var graph = new List<Edge>[allNodes.Count];

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
                        graph[to] = new List<Edge>();
                    if (graph[from] == null)
                        graph[from] = new List<Edge>();

                    //Two nodes references each other
                    graph[from].Add(Edge.ReferenceTo(to));
                    graph[to].Add(Edge.ReferenceTo(from));
                }
            }
            return ToTwinArray(graph);
        }
        public static Edge[][] ConvertToArrayGraph(IList<SolvingNode> allNodes)
        {
            var graph = new List<Edge>[allNodes.Count];
            for (int i = 0; i < allNodes.Count; i++)
                allNodes[i].GraphId = i;

            foreach (var node in allNodes)
            {
                var from = node.GraphId;
                if (from < 0)
                    throw new InvalidOperationException();

                if (node.State is RefTo)
                    throw new InvalidOperationException();
                
                if (graph[@from] == null)
                    graph[@from] = new List<Edge>();

                foreach (var anc in node.Ancestors)
                {
                    var ancId = anc.GetNonReference().GraphId;
                    graph[from].Add(Edge.AncestorTo(ancId));
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

                        if (graph[mfrom] == null)
                            graph[mfrom] = new List<Edge>();
                        graph[mfrom].Add(Edge.MemberOf(from));
                    }
                }
            }

            return ToTwinArray(graph);
        }


        private static Edge[][] ToTwinArray(List<Edge>[] graph)
        {
            Edge[][] ans =new Edge[graph.Length][];
            for (int i = 0; i < graph.Length; i++)
            {
                ans[i] = graph[i].ToArray();
            }
            return ans;
        }
    }
}
