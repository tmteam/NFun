using System.Collections.Generic;
using NFun.Tic.SolvingStates;

namespace NFun.Tic
{
    public interface ITicResults
    {
        TicNode GetVariableNode(string variableName);
        TicNode GetSyntaxNodeOrNull(int syntaxNode);
        /// <summary>
        /// Gap for tests
        /// </summary>
        IEnumerable<TicNode> GenericNodes { get; }
        bool HasGenerics { get; }
        /// <summary>
        /// GAP for tests
        /// </summary>
        int GenericsCount { get; }

        ConstrainsState[] GenericsStates { get; }
    }
}