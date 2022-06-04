using System;
using System.Collections.Generic;

namespace NFun.SyntaxParsing; 

public class CycleTopologySorting {
    /// <summary>
    /// Gets topology sorted in form of indexes [ParentNodeName ->  ChildrenNames[] ]
    /// O(N)
    /// If Circular dependencies found -  returns circle route insead of sorted order
    /// Simple dependencies from node to itself are ignored
    /// </summary>
    /// <returns>topology sorted node indexes from source to drain or first cycle route</returns>
    public static TopologySortResults Sort(int[][] graph)
        => new CycleTopologySorting(graph).Sort();

    private readonly int[][] _graph;
    private readonly NodeState[] _nodeStates;
    private readonly int[] _route;
    private readonly Queue<int> _cycleRoute = new();

    private int _processedCount = 0;

    private CycleTopologySorting(int[][] graph) {
        _graph = graph;
        _nodeStates = new NodeState[graph.Length];
        _route = new int[graph.Length];
    }

    public TopologySortResults Sort() {
        for (int i = 0; i < _graph.Length; i++)
        {
            if (!RecSort(i))
                return new TopologySortResults(_cycleRoute.ToArray(), true);
        }

        return new TopologySortResults(_route, false);
    }

    private bool RecSort(int node) {
        switch (_nodeStates[node])
        {
            case NodeState.Checked:
                return true;
            case NodeState.Checking:
                return false;
            case NodeState.NotProcessed:
                _nodeStates[node] = NodeState.Checking;
                for (int child = 0; child < _graph[node].Length; child++)
                {
                    var childId = _graph[node][child];

                    //ignore self dependencies
                    if (childId == node)
                        continue;

                    if (!RecSort(childId))
                    {
                        _cycleRoute.Enqueue(_graph[node][child]);
                        return false;
                    }
                }

                _nodeStates[node] = NodeState.Checked;

                _route[_processedCount] = node;
                _processedCount++;
                return true;
            default:
                throw new ArgumentOutOfRangeException($"node state {_nodeStates[node]}");
        }
    }
    
    enum NodeState {
        NotProcessed,
        Checked,
        Checking
    }
}