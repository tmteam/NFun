using System.Collections.Generic;

namespace NFun.Tic.Toposort
{
    public readonly struct TopologySortResults
    {
        /// <summary>
        /// Topological sort order if has no cycle
        /// First cycle route otherwise
        /// </summary>
        public readonly IList<Edge> NodeNames;
        public readonly bool HasLoop;
        /// <summary>
        /// List of recursive nodes or null if there are no one
        /// </summary>
        public readonly int[] RecursionsOrNull;

        public TopologySortResults(IList<Edge> nodeNames, int[] recursionsOrNull, bool hasLoop)
        {
            NodeNames = nodeNames;
            HasLoop = hasLoop;
            RecursionsOrNull = recursionsOrNull;
        }
    }
}