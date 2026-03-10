namespace NFun.Types;

/// <summary>
/// Sentinel object representing the 'none' value at runtime.
/// Used instead of null to avoid ambiguity with .NET null semantics.
/// </summary>
public sealed class FunnyNone {
    public static readonly FunnyNone Instance = new();
    private FunnyNone() { }
    public override string ToString() => "none";
}
