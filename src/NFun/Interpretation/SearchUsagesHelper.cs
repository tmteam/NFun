using System;
using System.Collections.Generic;
using System.Linq;
using NFun.Interpretation.Nodes;
using NFun.Runtime;

namespace NFun.Interpretation;

internal static class SearchUsagesHelper {

    internal static VariableExpressionNode FindFirstUsageOrThrow(this IExpressionNode node, VariableSource source)
        => node.FindFirstUsageOrNull(source) ?? throw new InvalidOperationException("Sequence contains no matching element");

    internal static VariableExpressionNode FindFirstUsageOrNull(this IExpressionNode node, VariableSource source)
        => node.Dfs(v => (v as VariableExpressionNode)?.Source == source) as VariableExpressionNode;

    internal static VariableExpressionNode FindFirstUsageOrNull(this IList<Equation> equations, VariableSource source)
        => equations
            .Select(e => e.Expression.FindFirstUsageOrNull(source))
            .FirstOrDefault(e => e != null);

    internal static VariableExpressionNode FindFirstUsageOrThrow(this IList<Equation> equations, VariableSource source)
        => equations.FindFirstUsageOrNull(source) ?? throw new InvalidOperationException("Sequence contains no matching element");

    private static IRuntimeNode Dfs(this IRuntimeNode root, Func<IRuntimeNode, bool> condition)
    {
        if (condition(root))
            return root;
        foreach (var child in root.Children)
        {
            var result = Dfs(child, condition);
            if (result != null)
                return result;
        }
        return null;
    }
}
