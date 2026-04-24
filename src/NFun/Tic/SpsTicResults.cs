using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using NFun.Tic.SolvingStates;

namespace NFun.Tic;

/// <summary>
/// Lightweight ITicResults for SimplePrimitiveSolver.
/// No namedNodes dictionary — SPS applies variable types directly via OutputType/VariableType.
/// Only syntaxNodes array is needed (ExpressionBuilder reads GetSyntaxNodeOrNull for constants/comparisons).
/// </summary>
internal class SpsTicResults : ITicResults {
    private readonly TicNode[] _syntaxNodes;

    public SpsTicResults(TicNode[] syntaxNodes) => _syntaxNodes = syntaxNodes;

    // Falls back to creating a node on-the-fly from the syntax node singleton.
    // This is called by ApplyTiResultEnterVisitor for typed equations when SPS succeeds.
    public TicNode GetVariableNode(string variableName) =>
        throw new InvalidOperationException(
            $"SPS: GetVariableNode('{variableName}') called unexpectedly. This indicates SPS returned results but typesApplied=false somewhere.");

    public TicNode GetVariableNodeOrNull(string variableName) => null;

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
