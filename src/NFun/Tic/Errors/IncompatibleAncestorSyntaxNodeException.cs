namespace NFun.Tic.Errors {

internal class IncompatibleAncestorSyntaxNodeException : TicException {
    public TicNode Ancestor { get; }
    public TicNode Descendant { get; }

    public IncompatibleAncestorSyntaxNodeException(TicNode ancestor, TicNode descendant)
        : base($"Incompatible ancestor {ancestor}=>{descendant}") {
        Ancestor = ancestor;
        Descendant = descendant;
    }
}

}