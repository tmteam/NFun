using System.Collections.Generic;
using NFun.SyntaxParsing.SyntaxNodes;

namespace NFun.SyntaxParsing.Visitors
{
    public class FindFunctionDependenciesVisitor: EnterVisitorBase
    {
        private readonly Dictionary<string, int> _userFunctionsNames;
        private readonly List<int> _dependencies = new List<int>();
        
        public int[] GetFoundDependencies() => _dependencies.ToArray();
        public FindFunctionDependenciesVisitor(Dictionary<string, int> userFunctionsNames)
        {
            _userFunctionsNames = userFunctionsNames;
        }
        
        public override VisitorEnterResult Visit(FunCallSyntaxNode node)
        {
            var nodeName = node.Id + "(" + node.Args.Length + ")";
            if(!_userFunctionsNames.TryGetValue(nodeName, out int id))
                return VisitorEnterResult.Continue;
            _dependencies.Add(id);
            return VisitorEnterResult.Continue;
        }
    }
}