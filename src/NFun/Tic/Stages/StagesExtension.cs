namespace NFun.Tic.Stages;

using System;
using SolvingStates;

public static class StagesExtension {
    public static bool Invoke<TFunction>(this TFunction function, TicNode nodeA, TicNode nodeB) where TFunction : IStateFunction {
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
        if (nodeA.State is ConstraintsState con)
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
                        // LCA(F<...>, Opt(F<...>)) = Opt(F<...>): wrap ancestor.
                        StateOptional optB => WrapAncestorInOptional(function, nodeA, nodeB, optB),
                        _ => false
                    },
                    StateFun funA => bc switch {
                        StateFun funB => function.Apply(funA, funB, nodeA, nodeB),
                        StateOptional optB => WrapAncestorInOptional(function, nodeA, nodeB, optB),
                        _ => false
                    },
                    StateStruct strA => bc switch {
                        StateStruct strB => function.Apply(strA, strB, nodeA, nodeB),
                        StateOptional optB when nodeA.IsOptionalElement => Invoke(function, nodeA, optB.ElementNode),
                        StateOptional optB => WrapAncestorInOptional(function, nodeA, nodeB, optB),
                        _ => false
                    },
                    StateOptional optA when bc is StateOptional optB => function.Apply(optA, optB, nodeA, nodeB),
                    // LCA(Opt(T), T) = Opt(T): wrap descendant in Optional.
                    StateOptional optA => WrapDescendantInOptional(function, nodeA, nodeB, optA),
                    _ => throw new NotSupportedException($"State {nodeA.State.GetType()} is not supported")
                },
                _ => throw new NotSupportedException($"State {nodeA.State.GetType()} is not supported")
            };
        }
        else if (nodeA.State is StateRefTo aRefFallback)
            return Invoke(function, aRefFallback.Node, nodeB);
        else
            throw new NotSupportedException($"State {nodeA.State.GetType()} is not supported");
    }

    /// <summary>
    /// LCA(F&lt;...&gt;, Opt(F&lt;...&gt;)) = Opt(F&lt;...&gt;): wrap ancestor in Optional.
    /// This is a standard LCA operation — ancestor widens to accommodate Optional descendant.
    /// Only wraps TypeVariable/Named nodes — SyntaxNodes (literals) indicate a type error.
    /// </summary>
    private static bool WrapAncestorInOptional<TFunction>(
        TFunction function, TicNode nodeA, TicNode nodeB, StateOptional optB) where TFunction : IStateFunction {
        TraceLog.WriteLine($"  WrapAncestorInOptional: nodeA={nodeA.Name}({nodeA.Type}):{nodeA.State} nodeB={nodeB.Name}:{nodeB.State}");
        if (nodeA.Type == TicNodeType.SyntaxNode || nodeA.IsSolved || nodeA.IsSignatureParam)
            throw Errors.TicErrors.IncompatibleNodes(nodeA, nodeB); // opt(T) ≤ T is invalid
        var innerNode = TicNode.CreateTypeVariableNode("ow" + nodeA.Name, nodeA.State);
        innerNode.IsOptionalElement = true;
        nodeA.State = new StateOptional(innerNode);
        nodeA.IsOptionalElement = true;
        return function.Apply((StateOptional)nodeA.State, optB, nodeA, nodeB);
    }

    /// <summary>
    /// LCA(Opt(T), T) = Opt(T): wrap descendant in Optional.
    /// Only wraps TypeVariable/Named nodes — SyntaxNodes (literals) use fallback unwrap.
    /// </summary>
    private static bool WrapDescendantInOptional<TFunction>(
        TFunction function, TicNode nodeA, TicNode nodeB, StateOptional optA) where TFunction : IStateFunction {
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
