using System.Collections.Generic;
using System.Linq;
using NFun.Tic;
using NFun.Tic.SolvingStates;

namespace NFun.TypeInferenceCalculator
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

        private IEnumerable<SolvingNode> AllNodes => _typeVariables.Union(_namedNodes).Union(_syntaxNodes);
        
        
        public IEnumerable<SolvingNode> Generics => AllNodes.Where(t => t?.State is Constrains);
        public int GenericsCount => AllNodes.Count(t => t?.State is Constrains);
        public IEnumerable<SolvingNode> TypeVariables => _typeVariables;
        public IEnumerable<SolvingNode> NamedNodes => _namedNodes;

        public  IEnumerable<SolvingNode> SyntaxNodes => _syntaxNodes;

        public IEnumerable<Constrains> GetAllGenerics => AllNodes.Select(a => a?.State).OfType<Constrains>();
        public IState[] GetSyntaxNodes() => _syntaxNodes.Select(s => s?.State).ToArray();

        public Dictionary<string, IState> GetAllNamedNodes()
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