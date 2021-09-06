namespace NFun.SyntaxParsing {

public struct TopologySortResults {
    /// <summary>
    /// Topological sort order if has no cycle
    /// First cycle route otherwise
    /// </summary>
    public readonly int[] NodeNames;

    public readonly bool HasCycle;

    public TopologySortResults(int[] nodeNames, bool hasCycle) {
        NodeNames = nodeNames;
        HasCycle = hasCycle;
    }
}

}