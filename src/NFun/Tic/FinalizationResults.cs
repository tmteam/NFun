using System.Collections.Generic;
using System.Linq;
using NFun.Tic.SolvingStates;

namespace NFun.Tic
{
    public class FinalizationResults
    {
        private readonly HashSet<SolvingNode> _typeVariables;

        private readonly IList<SolvingNode> _namedNodes;

        private readonly IList<SolvingNode> _syntaxNodes;
        public FinalizationResults(HashSet<SolvingNode> typeVariables, IList<SolvingNode> namedNodes, IList<SolvingNode> syntaxNodes)
        {
            _typeVariables = typeVariables;
            _namedNodes = namedNodes;
            _syntaxNodes = syntaxNodes;
        }

        public SolvingNode GetVariableNode(string variableName) =>
            _namedNodes.First(n => n.Name == "T" + variableName);
        public IState GetVariable(string variableName) =>
            _namedNodes.First(n => n.Name == "T" + variableName).State;
        public SolvingNode GetSyntaxNodeOrNull(int syntaxNode) =>
            _syntaxNodes.FirstOrDefault(n => n?.Name == syntaxNode.ToString());

        /// <summary>
        /// Gap for tests
        /// </summary>
        public IEnumerable<SolvingNode> GenericNodes => 
            _typeVariables
                .Union(_namedNodes)
                .Union(_syntaxNodes)
                .Where(t => t?.State is Constrains);

        public bool HasGenerics
        {
            get
            {
                //For perfomance optimization
                foreach (var node in _typeVariables)
                {
                    if (node?.State is Constrains c)
                        return true;
                }
                foreach (var node in _namedNodes)
                {
                    if (node?.State is Constrains c)
                        return true;
                }
                foreach (var node in _syntaxNodes)
                {
                    if (node?.State is Constrains c)
                        return true;
                }

                return false;
            }
        }
        /// <summary>
        /// GAP for tests
        /// </summary>
        public int GenericsCount => GenericsStates.Count();
        public IEnumerable<SolvingNode> TypeVariables => _typeVariables;
        public IEnumerable<SolvingNode> NamedNodes => _namedNodes;

        public  IEnumerable<SolvingNode> SyntaxNodes => _syntaxNodes;

        public IEnumerable<Constrains> GenericsStates
        {
            get
            {
                foreach (var node in _typeVariables)
                {
                    if (node?.State is Constrains c)
                        yield return c;
                }
                foreach (var node in _namedNodes)
                {
                    if (node?.State is Constrains c)
                        yield return c;
                }
                foreach (var node in _syntaxNodes)
                {
                    if (node?.State is Constrains c)
                        yield return c;
                }
            }
        }

        public IState[] GetSyntaxNodeStates() => _syntaxNodes.SelectToArray(s => s?.State);

        public Dictionary<string, IState> GetAllNamedNodeStates()
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