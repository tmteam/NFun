namespace NFun.Interpretation.Functions; 

public interface IFunctionSignature {
    string Name { get; }
    FunnyType[] ArgTypes { get; }
    FunnyType ReturnType { get; }
}