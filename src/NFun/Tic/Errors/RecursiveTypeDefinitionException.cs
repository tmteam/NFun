namespace NFun.Tic.Errors {

public class RecursiveTypeDefinitionException : TicException {
    public string[] NodeNames { get; }
    public int[] NodeIds { get; }

    public RecursiveTypeDefinitionException(string[] nodeNames, int[] nodeIds) : base(
        $"Recursive type definition {string.Join("->", nodeNames)}") {
        NodeNames = nodeNames;
        NodeIds = nodeIds;
    }
}

}