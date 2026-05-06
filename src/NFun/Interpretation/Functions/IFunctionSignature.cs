namespace NFun.Interpretation.Functions; 

public interface IFunctionSignature {
    string Name { get; }
    FunnyType[] ArgTypes { get; }
    FunnyType ReturnType { get; }
    /// <summary>Optional parameter metadata (names, defaults, params). Null for most built-ins.</summary>
    FunArgProperty[] ArgProperties => null;
    /// <summary>
    /// True when this function was defined with extension syntax (x.f() = expr).
    /// Extension functions are only callable via piped syntax when ExtensionFunctionsSeparation is enabled.
    /// Default: false (regular function or built-in).
    /// </summary>
    bool IsExtension => false;
    /// <summary>
    /// True for user-defined functions (both regular and extension).
    /// Used by extension function separation to distinguish user functions from built-ins.
    /// Built-in functions always remain accessible via piped calls.
    /// Default: false (built-in).
    /// </summary>
    bool IsUserDefined => false;
}