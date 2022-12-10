namespace NFun.Tic.Errors; 

public class TicRecursiveTypeDefinitionException : TicException {
    public TicRecursiveTypeDefinitionException(TicNode[] nodes) : base(
        $"Recursive type definition {string.Join("->", nodes.SelectToArray(s => s.ToString()))}") =>
        Nodes = nodes;

    public TicNode[] Nodes { get; }
}