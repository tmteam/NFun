using System.Collections.Generic;
using System.Linq;
using NFun.Tic.SolvingStates;

namespace NFun.Tic
{
    public class TicResultsWithoutGenerics : ITicResults
    {
        private readonly List<TicNode> _namedNodes;
        private readonly List<TicNode> _syntaxNodes;

        public TicResultsWithoutGenerics(IEnumerable<TicNode> nodes, int syntaxNodeCapacity)
        {
            _syntaxNodes = new List<TicNode>(syntaxNodeCapacity);
             _namedNodes = new List<TicNode>();
            foreach (var node in nodes)
            {
                if (node.Type == TicNodeType.Named)
                    _namedNodes.Add(node);
                else if (node.Type == TicNodeType.SyntaxNode)
                    _syntaxNodes.EnlargeAndSet((int)node.Name, node);
            }
        }

        public TicResultsWithoutGenerics(List<TicNode> namedNodes, List<TicNode> syntaxNodes)
        {
            _namedNodes = namedNodes;
            _syntaxNodes = syntaxNodes;
        }

        public TicNode GetVariableNode(string variableName) 
            => _namedNodes.First(n => n.Name.Equals(variableName));

        public TicNode GetSyntaxNodeOrNull(int syntaxNode)
        {
            if (syntaxNode >= _syntaxNodes.Count)
                return null;
            return _syntaxNodes[syntaxNode];
        }

        public IEnumerable<TicNode> GenericNodes { get; } = new TicNode[0];
        public bool HasGenerics => false;
        public int GenericsCount => 0;
        public ConstrainsState[] GenericsStates { get; } = new ConstrainsState[0];
    }
}