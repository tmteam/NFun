namespace NFun.Interpretation.Functions;

public interface IUserFunction: IFunctionSignature {
    bool IsGeneric { get; }
    FunctionRecursionKind RecursionKind { get; }
}

public enum FunctionRecursionKind {
    /// <summary>
    /// Function does not call itself
    /// </summary>
    NoRecursion,
    // Function call itself
    SelfRecursion,
}