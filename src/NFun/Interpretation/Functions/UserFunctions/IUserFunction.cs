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
    /// <summary>
    /// Function participates in a mutually-recursive cycle of two or more
    /// user functions (e.g. <c>isEven</c> ↔ <c>isOdd</c>). Solved together
    /// via SCC in one TIC graph; runtime depth bounded by a shared counter
    /// across the group.
    /// </summary>
    MutualRecursion,
}