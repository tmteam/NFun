using System.Collections.Generic;
using System.Linq;
using NFun.Tic;
using NFun.Tic.SolvingStates;

namespace NFun.TypeInferenceCalculator
{
    public class FinalizationResults
    {
        public FinalizationResults(HashSet<SolvingNode> typeVariables, IList<SolvingNode> namedNodes, IList<SolvingNode> syntaxNodes)
        {
            TypeVariables = typeVariables;
            NamedNodes = namedNodes;
            SyntaxNodes = syntaxNodes;
        }

        public SolvingNode GetVariableNode(string variableName) =>
            NamedNodes.First(n => n.Name == "T" + variableName);
        public IState GetVariable(string variableName) =>
            NamedNodes.First(n => n.Name == "T" + variableName).State;
        public SolvingNode GetSyntaxNodeOrNull(int syntaxNode) =>
            SyntaxNodes.FirstOrDefault(n => n?.Name == syntaxNode.ToString());

        private IEnumerable<SolvingNode> AllNodes => TypeVariables.Union(NamedNodes).Union(SyntaxNodes);
        public IEnumerable<SolvingNode> Generics => AllNodes.Where(t => t?.State is Constrains);
        public int GenericsCount => AllNodes.Count(t => t?.State is Constrains);

        private HashSet<SolvingNode> TypeVariables { get; }
        private IList<SolvingNode> NamedNodes { get; }
        private IList<SolvingNode> SyntaxNodes { get; }
        public IEnumerable<Constrains> GetAllGenerics => AllNodes.Select(a => a?.State).OfType<Constrains>();
        public IState[] GetSyntaxNodes() => SyntaxNodes.Select(s => s?.State).ToArray();

        public Dictionary<string, IState> GetAllNamedNodes()
        {
            return NamedNodes.ToDictionary(
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