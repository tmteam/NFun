using System.Collections.Generic;
using System.Linq;
using NFun.Tic.SolvingStates;

namespace NFun.Tic {

public class TicResultsWithGenerics : ITicResults {
    private readonly IReadOnlyList<TicNode> _typeVariables;

    private readonly Dictionary<string, TicNode> _namedNodes;

    private readonly IReadOnlyList<TicNode> _syntaxNodes;

    public TicResultsWithGenerics(
        IReadOnlyList<TicNode> typeVariables,
        Dictionary<string, TicNode> namedNodes,
        IReadOnlyList<TicNode> syntaxNodes) {
        _typeVariables = typeVariables;
        _namedNodes = namedNodes;
        _syntaxNodes = syntaxNodes;
    }

    public TicNode GetVariableNode(string variableName) =>
        _namedNodes[variableName];

    public TicNode GetSyntaxNodeOrNull(int syntaxNode) {
        if (syntaxNode >= _syntaxNodes.Count)
            return null;
        return _syntaxNodes[syntaxNode];
    }

    /// <summary>
    /// Gap for tests
    /// </summary>
    public IEnumerable<TicNode> GenericNodes =>
        _typeVariables
            .Union(_namedNodes.Values)
            .Union(_syntaxNodes)
            .Where(t => t?.State is ConstrainsState);
    /// <summary>
    /// GAP for tests
    /// </summary>
    public int GenericsCount => GenericsStates.Length;

    public bool HasGenerics => GenericsStates.Length > 0;

    private ConstrainsState[] _genericsStates = null;

    public ConstrainsState[] GenericsStates
    {
        get
        {
            if (_genericsStates != null) return _genericsStates;

            var states = new List<ConstrainsState>();
            foreach (var node in _typeVariables)
            {
                if (node?.State is ConstrainsState c)
                    states.Add(c);
            }

            foreach (var node in _namedNodes)
            {
                if (node.Value.State is ConstrainsState c)
                    states.Add(c);
            }

            foreach (var node in _syntaxNodes)
            {
                if (node?.State is ConstrainsState c)
                    states.Add(c);
            }

            _genericsStates = states.ToArray();
            return _genericsStates;
        }
    }
}

}