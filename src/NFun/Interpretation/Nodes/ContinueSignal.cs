namespace NFun.Interpretation.Nodes;

/// <summary>Sentinel for continue -- skips to next iteration. ThreadStatic, zero-alloc.</summary>
internal sealed class ContinueSignal {
    [System.ThreadStatic] private static ContinueSignal _instance;
    public static ContinueSignal Instance => _instance ??= new ContinueSignal();
    private ContinueSignal() { }
}
