using System.Collections.Generic;

namespace NFun.Tic.Toposort
{
    enum NodeState
    {
        NotVisited,
        Checked,
        Checking,
    }

    
    public static class GraphTools
    {

        /// <summary>
        /// Gets topology sorted in form of indexes [ParentNodeName ->  ChildrenNames[] ]
        /// O(N)
        /// If Circular dependencies found -  returns circle route insead of sorted order
        /// </summary>
        /// <returns>topology sorted node indexes from source to drain or first cycle route</returns>
        public static TopologySortResults SortTopology(Edge[][] graph)
            => new TopologySort(graph).Sort();

        class TopologySort
        {
            private readonly Edge[][] _graph;
            private readonly NodeState[] _nodeStates;
            private readonly List<Edge> _route;
            private readonly Queue<Edge> _cycleRoute = new Queue<Edge>();

            public TopologySort(Edge[][] graph)
            {
                _graph = graph;
                _nodeStates = new NodeState[graph.Length];
                _route = new List<Edge>(graph.Length);
            }

            public TopologySortResults Sort()
            {
                for (int i = 0; i < _graph.Length; i++)
                {
                    if (!RecSort(new Edge(i, EdgeType.Root)))
                        return new TopologySortResults(_cycleRoute.ToArray(), null, true);
                }

                return new TopologySortResults(_route, null, false);
            }

            private bool RecSort(Edge edge, int from = -1)
            {
                //Console.Write($"S: {from} : {edge} ");
                var node = edge.To;
                if (_graph[node] == null)
                {
                  //  Console.Write($"  --null\r\n");
                    _nodeStates[node] = NodeState.Checked;
                    return true;
                }

                switch (_nodeStates[node])
                {
                    case NodeState.Checked:
                    {
                       // Console.Write($"  --Checked\r\n");
                        return true;}
                    case NodeState.Checking:
                    {
                        //Console.Write($"  --Cycle\r\n");
                        return false;
                    }
                    default:
                        _nodeStates[node] = NodeState.Checking;
                        //Console.Write($"--Checking\r\n");

                        var hasSelfCycle = false;
                        for (int child = 0; child < _graph[node].Length; child++)
                        {
                            var to = _graph[node][child];

                            //if node a equals to b, then b should ref to a.
                            //it is not a cycle. Skip it.
                            //But we can do it only once.
                            if (from == to.To
                                && edge.Type == EdgeType.Equal
                                && to.Type == EdgeType.Equal)
                            {
                                if (!hasSelfCycle)
                                {
                                    //Console.Write($"--SKIPEQ\r\n");
                                    //Can skip only one back reference
                                    //Second one is a cycle
                                    hasSelfCycle = true;
                                    continue; //skip edge
                                }
                            }

                            if (!RecSort(edge: to, @from: node))
                            {
                                _cycleRoute.Enqueue(_graph[node][child]);
                                return false;
                            }
                        }

                        _nodeStates[node] = NodeState.Checked;

                        _route.Add(edge);
                        return true;
                }
            }
        }
    }
}