using System.Collections.Generic;
using System.Linq;
using NFun.Tic.SolvingStates;

namespace NFun.Tic
{
    public class FinalizationResults
    {
        private readonly HashSet<TicNode> _typeVariables;

        private readonly IList<TicNode> _namedNodes;

        private readonly IList<TicNode> _syntaxNodes;
        public FinalizationResults(HashSet<TicNode> typeVariables, IList<TicNode> namedNodes, IList<TicNode> syntaxNodes)
        {
            _typeVariables = typeVariables;
            _namedNodes = namedNodes;
            _syntaxNodes = syntaxNodes;
        }

        public TicNode GetVariableNode(string variableName) =>
            _namedNodes.First(n => n.Name == "T" + variableName);
        public ITicNodeState GetVariable(string variableName) =>
            _namedNodes.First(n => n.Name == "T" + variableName).State;
        public TicNode GetSyntaxNodeOrNull(int syntaxNode) =>
            _syntaxNodes.FirstOrDefault(n => n?.Name == syntaxNode.ToString());

        /// <summary>
        /// Gap for tests
        /// </summary>
        public IEnumerable<TicNode> GenericNodes => 
            _typeVariables
                .Union(_namedNodes)
                .Union(_syntaxNodes)
                .Where(t => t?.State is ConstrainsState);

        public bool HasGenerics
        {
            get
            {
                //For perfomance optimization
                foreach (var node in _typeVariables)
                {
                    if (node?.State is ConstrainsState c)
                        return true;
                }
                foreach (var node in _namedNodes)
                {
                    if (node?.State is ConstrainsState c)
                        return true;
                }
                foreach (var node in _syntaxNodes)
                {
                    if (node?.State is ConstrainsState c)
                        return true;
                }

                return false;
            }
        }
        /// <summary>
        /// GAP for tests
        /// </summary>
        public int GenericsCount => GenericsStates.Count();
        public IEnumerable<TicNode> TypeVariables => _typeVariables;
        public IEnumerable<TicNode> NamedNodes => _namedNodes;

        public  IEnumerable<TicNode> SyntaxNodes => _syntaxNodes;

        public IEnumerable<ConstrainsState> GenericsStates
        {
            get
            {
                foreach (var node in _typeVariables)
                {
                    if (node?.State is ConstrainsState c)
                        yield return c;
                }
                foreach (var node in _namedNodes)
                {
                    if (node?.State is ConstrainsState c)
                        yield return c;
                }
                foreach (var node in _syntaxNodes)
                {
                    if (node?.State is ConstrainsState c)
                        yield return c;
                }
            }
        }

        public ITicNodeState[] GetSyntaxNodeStates() => _syntaxNodes.SelectToArray(s => s?.State);

        public Dictionary<string, ITicNodeState> GetAllNamedNodeStates()
        {
            return _namedNodes.ToDictionary(
                n =>
                {
                    if (n.Name.StartsWith("T"))
                        return n.Name.Substring(1);
                    return n.Name;
                },
                n => n.State
            );
        }
    }
}