using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using NFun.Tic.SolvingStates;

namespace NFun.Tic; 

public class TicResultsWithoutGenerics : ITicResults {
    private readonly Dictionary<string, TicNode> _namedNodes;
    private readonly TicNode[] _syntaxNodes;

    public TicResultsWithoutGenerics(Dictionary<string, TicNode> namedNodes, TicNode[] syntaxNodes) {
        _namedNodes = namedNodes;
        _syntaxNodes = syntaxNodes;
    }

    public TicNode GetVariableNode(string variableName) => _namedNodes[variableName];

    public TicNode GetVariableNodeOrNull(string variableName) => CollectionExtensions.GetValueOrDefault(_namedNodes, variableName);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public TicNode GetSyntaxNodeOrNull(int syntaxNode) {
        if ((uint)syntaxNode >= (uint)_syntaxNodes.Length)
            return null;
        return _syntaxNodes[syntaxNode];
    }

    public IEnumerable<TicNode> GenericNodes { get; } = Array.Empty<TicNode>();
    public bool HasGenerics => false;
    public int GenericsCount => 0;
    public IReadOnlyList<ConstraintsState> GenericsStates { get; } = Array.Empty<ConstraintsState>();
}