using System.Collections.Generic;
using NFun.SyntaxParsing.SyntaxNodes;
using NFun.Tic;

namespace NFun.SyntaxParsing.Visitors
{
    public class FindFunctionDependenciesVisitor: EnterVisitorBase
    {
        private readonly string _functionAlias;
        private readonly SmallStringDictionary<int> _userFunctionsNames;
        private readonly List<int> _dependencies;
        public bool HasSelfRecursion { get; private set; } = false;
        public int[] GetFoundDependencies() => _dependencies.ToArray();
        public FindFunctionDependenciesVisitor(string functionAlias, SmallStringDictionary<int> userFunctionsNames)
        {
            _functionAlias = functionAlias;
            _userFunctionsNames = userFunctionsNames;
            _dependencies = new List<int>(userFunctionsNames.Count);
        }
        
        public override VisitorEnterResult Visit(FunCallSyntaxNode node)
        {
            var nodeName = node.Id + "(" + node.Args.Length + ")";
            if (nodeName == _functionAlias)
                HasSelfRecursion = true;
            else if (_userFunctionsNames.TryGetValue(nodeName, out int id)) 
                _dependencies.Add(id);
            return VisitorEnterResult.Continue;
        }
    }
}