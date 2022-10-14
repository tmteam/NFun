using System;
using System.Collections.Generic;
using NFun.Tic.SolvingStates;

namespace NFun.Tic; 

public class TicResultsWithoutGenerics : ITicResults {
    private readonly Dictionary<string, TicNode> _namedNodes;
    private readonly IReadOnlyList<TicNode> _syntaxNodes;

    public TicResultsWithoutGenerics(Dictionary<string, TicNode> namedNodes, IReadOnlyList<TicNode> syntaxNodes) {
        _namedNodes = namedNodes;
        _syntaxNodes = syntaxNodes;
    }

    public TicNode GetVariableNode(string variableName) => _namedNodes[variableName];

    public TicNode GetVariableNodeOrNull(string variableName) => _namedNodes.GetValueOrDefault(variableName);

    public TicNode GetSyntaxNodeOrNull(int syntaxNode) {
        if (syntaxNode >= _syntaxNodes.Count)
            return null;
        return _syntaxNodes[syntaxNode];
    }

    public IEnumerable<TicNode> GenericNodes { get; } = Array.Empty<TicNode>();
    public bool HasGenerics => false;
    public int GenericsCount => 0;
    public IReadOnlyList<ConstrainsState> GenericsStates { get; } = Array.Empty<ConstrainsState>();
}