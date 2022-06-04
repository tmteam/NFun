using System.Collections.Generic;
using NFun.SyntaxParsing.SyntaxNodes;

namespace NFun.SyntaxParsing.Visitors; 

public class FindFunctionDependenciesVisitor : EnterVisitorBase {
    private readonly string _functionAlias;
    private readonly Dictionary<string, int> _userFunctionsNames;
    private readonly List<int> _dependencies;
   
    public FindFunctionDependenciesVisitor(string functionAlias, Dictionary<string, int> userFunctionsNames) {
        _functionAlias = functionAlias;
        _userFunctionsNames = userFunctionsNames;
        _dependencies = new List<int>(userFunctionsNames.Count);
    }
    public bool HasSelfRecursion { get; private set; } = false;
    public int[] GetFoundDependencies() => _dependencies.ToArray();

    public override DfsEnterResult Visit(FunCallSyntaxNode node) {
        var nodeName = node.Id + "(" + node.Args.Length + ")";
        if (nodeName == _functionAlias)
            HasSelfRecursion = true;
        else if (_userFunctionsNames.TryGetValue(nodeName, out int id))
            _dependencies.Add(id);
        return DfsEnterResult.Continue;
    }
}