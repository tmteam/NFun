using System;

namespace Funny.Interpritation
{
    public struct TopologySortResults
    {
        public readonly int[] Order;
        public readonly bool HasCycle;

        public TopologySortResults(int[] order, bool hasCycle)
        {
            Order = order;
            HasCycle = hasCycle;
        }
    }
    public static class GraphTools
    {

        /// <summary>
        /// Gets topology sorted in form of indexes [childrenNodeName -> parentsNodeNames[]]
        /// O(N)
        /// Circular dependencies means parsing failures
        /// </summary>
        /// <returns>topology sorted node indexes from source to drain. nodeNames[]</returns>
        public static TopologySortResults SortTopology(int[][] graph)
        {
            var res =  new TopologySort(graph).Sort();
            return new TopologySortResults(res, false);
        }

        class TopologySort
        {
            private readonly int[][] _graph;
            private readonly NodeState[] _nodeStates;
            private readonly int[] _route;
            private int _processedCount = 0;
            public TopologySort(int[][] graph)
            {
                _graph = graph;
                _nodeStates= new NodeState[graph.Length];
                _route= new int[graph.Length];
            }

            public int[] Sort()
            {
                for (int i = 0; i < _graph.Length; i++)
                {
                    if (!RecSort(i))
                        return null;
                }

                return _route;
            }
            private bool RecSort(int node)
            {
                switch (_nodeStates[node])
                {
                    case NodeState.Checked:
                        return true;
                    case NodeState.Checking:
                        return false;
                    default:
                        _nodeStates[node] = NodeState.Checking;
                        for (int child = 0; child < _graph[node].Length; child++)
                            RecSort(_graph[node][child]);
                        _nodeStates[node] = NodeState.Checked;

                        _route[_processedCount] = node;
                        _processedCount++;
                        return true;
                }
            }

            enum NodeState
            {
                NotProcessed,
                Checked,
                Checking
            }
        }
    }
}