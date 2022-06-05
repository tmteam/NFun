namespace NFun.Tic.Errors; 

internal class TicIncompatibleAncestorSyntaxNodeException : TicException {
    public TicNode Ancestor { get; }
    public TicNode Descendant { get; }

    public TicIncompatibleAncestorSyntaxNodeException(TicNode ancestor, TicNode descendant)
        : base($"Incompatible ancestor {ancestor}=>{descendant}") {
        Ancestor = ancestor;
        Descendant = descendant;
    }
}