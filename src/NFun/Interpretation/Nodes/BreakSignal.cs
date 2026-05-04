namespace NFun.Interpretation.Nodes;

/// <summary>Sentinel for break -- exits innermost loop. ThreadStatic, zero-alloc.</summary>
internal sealed class BreakSignal {
    [System.ThreadStatic] private static BreakSignal _instance;
    public static BreakSignal Instance => _instance ??= new BreakSignal();
    private BreakSignal() { }
}
