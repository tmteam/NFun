namespace NFun.Interpretation.Functions;

/// <summary>
/// Declares how a function is reachable from call sites.
/// Built-ins default to <see cref="Both"/> for backward compatibility.
/// User-defined functions default to <see cref="Extension"/> or <see cref="Direct"/>
/// based on their declaration syntax (<c>x.f()</c> vs <c>f(x)</c>).
/// </summary>
public enum CallStyle {
    /// <summary>Callable only as <c>f(x)</c>.</summary>
    Direct,
    /// <summary>Callable only as <c>x.f()</c>.</summary>
    Extension,
    /// <summary>Callable both as <c>f(x)</c> and <c>x.f()</c>.</summary>
    Both
}

public interface IFunctionSignature {
    string Name { get; }
    FunnyType[] ArgTypes { get; }
    FunnyType ReturnType { get; }
    /// <summary>Optional parameter metadata (names, defaults, params). Null for most built-ins.</summary>
    FunArgProperty[] ArgProperties => null;
    /// <summary>
    /// Declares the calling convention(s) under which the function is reachable.
    /// Built-ins default to <see cref="CallStyle.Both"/>; user-defined are tagged
    /// per declaration syntax. Built-ins that opt into extension-only behavior
    /// pass <c>isExtension: true</c> via <see cref="FunctionSignatureDescription"/>;
    /// the base class derives <see cref="CallStyle.Extension"/>.
    /// </summary>
    CallStyle CallStyle => CallStyle.Both;
    /// <summary>
    /// True for user-defined functions (both regular and extension).
    /// Used by extension function separation to distinguish user functions from built-ins.
    /// Built-in functions always remain accessible via piped calls.
    /// Default: false (built-in).
    /// </summary>
    bool IsUserDefined => false;
}
