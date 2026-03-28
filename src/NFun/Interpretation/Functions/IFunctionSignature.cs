namespace NFun.Interpretation.Functions; 

public interface IFunctionSignature {
    string Name { get; }
    FunnyType[] ArgTypes { get; }
    FunnyType ReturnType { get; }
    /// <summary>Optional parameter metadata (names, defaults, params). Null for most built-ins.</summary>
    FunArgProperty[] ArgProperties => null;
}