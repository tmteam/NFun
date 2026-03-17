namespace NFun.Tic.Stages;

using System;
using SolvingStates;

public static class StagesExtension {
    public static bool Invoke(this IStateFunction function, TicNode nodeA, TicNode nodeB) {
        if (nodeB.State is StateRefTo bRef)
            return function.Invoke(nodeA, bRef.Node);
        if (nodeA.State is StatePrimitive p)
        {
            return nodeB.State switch {
                StatePrimitive bp => function.Apply(p, bp, nodeA, nodeB),
                ICompositeState bc => function.Apply(p, bc, nodeA, nodeB),
                ConstraintsState bcon => function.Apply(p, bcon, nodeA, nodeB),
                _ => throw new NotSupportedException($"State {nodeA.State.GetType()} is not supported")
            };
        }
        else if (nodeA.State is ConstraintsState con)
        {
            return nodeB.State switch {
                StatePrimitive bp => function.Apply(con, bp, nodeA, nodeB),
                ICompositeState bc => function.Apply(con, bc, nodeA, nodeB),
                ConstraintsState bcon => function.Apply(con, bcon, nodeA, nodeB),
                _ => throw new NotSupportedException($"State {nodeA.State.GetType()} is not supported")
            };
        }
        else if (nodeA.State is ICompositeState c)
        {
            return nodeB.State switch {
                StatePrimitive bp => function.Apply(c, bp, nodeA, nodeB),
                ConstraintsState bcon => function.Apply(c, bcon, nodeA, nodeB),
                ICompositeState bc => c switch {
                    StateArray arrA => bc switch {
                        StateArray arrB => function.Apply(arrA, arrB, nodeA, nodeB),
                        // During Destruction: wrap ancestor to converge types.
                        StateOptional optB when function is DestructionFunctions => WrapAncestorInOptional(function,
                            nodeA, nodeB, optB),
                        // During Pull: opt(arr(T)) can't satisfy arr(T) — no implicit unwrap
                        StateOptional when function is PullConstraintsFunctions => false,
                        // During Push: unwrap optional descendant to propagate inner info
                        // (Destruction will wrap the ancestor later if needed)
                        StateOptional optB => Invoke(function, nodeA, optB.ElementNode),
                        _ => false
                    },
                    StateFun funA => bc switch {
                        StateFun funB => function.Apply(funA, funB, nodeA, nodeB),
                        StateOptional optB when function is DestructionFunctions => WrapAncestorInOptional(function,
                            nodeA, nodeB, optB),
                        StateOptional when function is PullConstraintsFunctions => false,
                        StateOptional optB => Invoke(function, nodeA, optB.ElementNode),
                        _ => false
                    },
                    StateStruct strA => bc switch {
                        StateStruct strB => function.Apply(strA, strB, nodeA, nodeB),
                        // When nodeA is an optional element (inner node of opt(T) from SetSafeFieldAccess),
                        // unwrap the optional descendant instead of wrapping/rejecting.
                        // This handles chained ?.b?.c where field b is optional:
                        // T_b gets struct constraint from ?.c, but source field is opt(struct).
                        // Unwrap is safe because T_b is already wrapped in opt() by the result node.
                        StateOptional optB when nodeA.IsOptionalElement => Invoke(function, nodeA, optB.ElementNode),
                        StateOptional optB when function is DestructionFunctions => WrapAncestorInOptional(function,
                            nodeA, nodeB, optB),
                        StateOptional when function is PullConstraintsFunctions => false,
                        StateOptional optB => Invoke(function, nodeA, optB.ElementNode),
                        _ => false
                    },
                    StateOptional optA when bc is StateOptional optB => function.Apply(optA, optB, nodeA, nodeB),
                    // Implicit lift: opt(T) ancestor, non-optional T descendant.
                    // During Destruction: wrap descendant to converge types.
                    StateOptional optA when function is DestructionFunctions => WrapDescendantInOptional(function,
                        nodeA, nodeB, optA),
                    StateOptional optA => Invoke(function, optA.ElementNode, nodeB),
                    _ => throw new NotSupportedException($"State {nodeA.State.GetType()} is not supported")
                },
                _ => throw new NotSupportedException($"State {nodeA.State.GetType()} is not supported")
            };
        }
        else if (nodeA.State is StateRefTo r)
            return Invoke(function, r.Node, nodeB);
        else
            throw new NotSupportedException($"State {nodeA.State.GetType()} is not supported");
    }

    /// <summary>
    /// During Destruction, when ancestor is non-Optional composite (struct/array/fun) and
    /// descendant is Optional, wrap the ancestor in Optional to make types converge.
    /// Only wraps TypeVariable/Named nodes — SyntaxNodes (literals) are left as-is.
    /// </summary>
    private static bool WrapAncestorInOptional(
        IStateFunction function, TicNode nodeA, TicNode nodeB, StateOptional optB) {
        TraceLog.WriteLine($"  WrapAncestorInOptional: nodeA={nodeA.Name}({nodeA.Type}):{nodeA.State} nodeB={nodeB.Name}:{nodeB.State}");
        if (nodeA.Type == TicNodeType.SyntaxNode || nodeA.IsSolved)
            throw Errors.TicErrors.IncompatibleNodes(nodeA, nodeB); // opt(T) ≤ T is invalid
        var innerNode = TicNode.CreateTypeVariableNode("ow" + nodeA.Name, nodeA.State);
        innerNode.IsOptionalElement = true;
        nodeA.State = new StateOptional(innerNode);
        nodeA.IsOptionalElement = true;
        return function.Apply((StateOptional)nodeA.State, optB, nodeA, nodeB);
    }

    /// <summary>
    /// During Destruction, when ancestor is Optional and descendant is non-Optional composite,
    /// wrap the descendant in Optional to make types converge.
    /// Only wraps TypeVariable/Named nodes — SyntaxNodes (literals) are left as-is.
    /// </summary>
    private static bool WrapDescendantInOptional(
        IStateFunction function, TicNode nodeA, TicNode nodeB, StateOptional optA) {
        TraceLog.WriteLine($"  WrapDescendantInOptional: nodeA={nodeA.Name}:{nodeA.State} nodeB={nodeB.Name}({nodeB.Type}):{nodeB.State}");
        if (nodeB.Type == TicNodeType.SyntaxNode || nodeB.IsSolved)
            return Invoke(function, optA.ElementNode, nodeB); // fallback: unwrap
        var innerNode = TicNode.CreateTypeVariableNode("ow" + nodeB.Name, nodeB.State);
        innerNode.IsOptionalElement = true;
        nodeB.State = new StateOptional(innerNode);
        nodeB.IsOptionalElement = true;
        return function.Apply(optA, (StateOptional)nodeB.State, nodeA, nodeB);
    }
}
