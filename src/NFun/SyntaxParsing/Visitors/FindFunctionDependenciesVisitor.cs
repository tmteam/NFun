using System.Collections.Generic;
using NFun.SyntaxParsing.SyntaxNodes;

namespace NFun.SyntaxParsing.Visitors
{
    public class FindFunctionDependenciesVisitor: EnterVisitorBase
    {
        private readonly Dictionary<string, int> _userFunctionsNames;
        private List<int> _dependencies = new List<int>();
        
        public int[] GetFoundDependencies() => _dependencies.ToArray();
        public FindFunctionDependenciesVisitor(Dictionary<string, int> userFunctionsNames)
        {
            _userFunctionsNames = userFunctionsNames;
        }
        
        public override VisitorResult Visit(FunCallSyntaxNode node)
        {
            var nodeName = node.Id + "(" + node.Args.Length + ")";
            if(!_userFunctionsNames.TryGetValue(nodeName, out int id))
                return VisitorResult.Continue;
            _dependencies.Add(id);
            return VisitorResult.Continue;
        }
    }
}