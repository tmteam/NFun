using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using NFun.Tic.SolvingStates;

namespace NFun.Tic; 

public class TicResultsWithGenerics : ITicResults {
    private readonly IReadOnlyList<TicNode> _typeVariables;

    private readonly Dictionary<string, TicNode> _namedNodes;

    private readonly TicNode[] _syntaxNodes;

    public TicResultsWithGenerics(
        IReadOnlyList<TicNode> typeVariables,
        Dictionary<string, TicNode> namedNodes,
        TicNode[] syntaxNodes) {
        _typeVariables = typeVariables;
        _namedNodes = namedNodes;
        _syntaxNodes = syntaxNodes;
    }

    public TicNode GetVariableNode(string variableName) =>
        _namedNodes[variableName];

    public TicNode GetVariableNodeOrNull(string variableName) => CollectionExtensions.GetValueOrDefault(_namedNodes, variableName);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public TicNode GetSyntaxNodeOrNull(int syntaxNode) {
        if ((uint)syntaxNode >= (uint)_syntaxNodes.Length)
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
            .Where(t => t?.State is ConstraintsState);
    /// <summary>
    /// GAP for tests
    /// </summary>
    public int GenericsCount => GenericsStates.Count;

    public bool HasGenerics => GenericsStates.Count > 0;

    private  IReadOnlyList<ConstraintsState> _genericsStates = null;

    public IReadOnlyList<ConstraintsState> GenericsStates
    {
        get
        {
            if (_genericsStates != null) return _genericsStates;
            
            var states = new List<ConstraintsState>();
            foreach (var node in _typeVariables)
            {
                if (node?.State is ConstraintsState c)
                    states.Add(c);
            }

            foreach (var node in _namedNodes)
            {
                if (node.Value.State is ConstraintsState c)
                    states.Add(c);
            }

            foreach (var node in _syntaxNodes)
            {
                if (node?.State is ConstraintsState c)
                    states.Add(c);
            }

            _genericsStates = states;
            return _genericsStates;
        }
    }
}