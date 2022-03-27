namespace NFun.Tic.Errors {

public class RecursiveTypeDefinitionException : TicException {
    public RecursiveTypeDefinitionException(TicNode[] nodes) : base(
        $"Recursive type definition {string.Join("->", nodes.SelectToArray(s => s.ToString()))}") {
        Nodes = nodes;
    }

    public TicNode[] Nodes { get; }
}

}