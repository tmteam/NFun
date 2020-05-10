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
            //На данном этапе - в nodes лежат все узлы участвующие в вычислениях
            //нужно убрать все  циклы с ребрами "равно", что-бы можно было спокойно работать далее
            //не опасаясь Stackoverflow
            RemoveRefenceCycles(nodes);
            //Теперь из графа можно исключить все ребра "равно". Для этого нужно перекинуть
            //все взаимодействия (Ancestor и Member) на оригинальные узлы
            //
            //Это нужно для того что бы можно было провести направленный топосорт

            MergeAllReferences(nodes, out var refs, out var concretes);
            
            var graph = ConvertToArrayGraph(concretes);
            var sorted = GraphTools.SortTopology(graph);
            var order = sorted.NodeNames.Select(n => concretes[n.To]).Reverse().ToArray();
            return new NodeSortResult(order, refs, 
                sorted.HasCycle
                ? sorted.NodeNames.Any(n => n.Type == EdgeType.Member)
                    ? SortStatus.MemebershipCycle
                    : SortStatus.AncestorCycle
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


        private static void RemoveRefenceCycles(SolvingNode[] nodes)
        {
            while (true)
            {
                var refList = FindRefNodesGraph(nodes);

                var arrayOfRefList = refList.ToArray();
                var refGraph = ConvertToRefArrayGraph(arrayOfRefList);
                var refTopology = GraphTools.SortTopology(refGraph);
                if (!refTopology.HasCycle)
                    return;
                
                var refCycle = refTopology.NodeNames.Select(n => nodes[n.To]).ToArray();
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

        public static Edge[][] ConvertToRefArrayGraph(SolvingNode[] allNodes)
        {
            var graph = new LinkedList<Edge>[allNodes.Length];
            for (int i = 0; i < allNodes.Length; i++)
                allNodes[i].GraphId = i;

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
