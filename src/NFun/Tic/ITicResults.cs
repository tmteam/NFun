using System.Collections.Generic;
using NFun.Tic.SolvingStates;

namespace NFun.Tic;

public interface ITicResults {
    TicNode GetVariableNode(string variableName);
    TicNode GetVariableNodeOrNull(string variableName);
    TicNode GetSyntaxNodeOrNull(int syntaxNode);
    IEnumerable<TicNode> GenericNodes { get; }
    bool HasGenerics { get; }
    int GenericsCount { get; }
    IReadOnlyList<ConstraintsState> GenericsStates { get; }
}
