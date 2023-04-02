namespace NFun.Tic.Stages;

using System;
using SolvingStates;

public static class StagesExtension {
    public static bool Invoke(this IStateFunction function, TicNode nodeA, TicNode nodeB) {
        if (nodeB.State is StateRefTo bRef)
            Invoke(function, nodeA, bRef.Node);
        if (nodeA.State is StatePrimitive p)
        {
            if (nodeB.State is StatePrimitive bp)
                return function.Apply(p, bp, nodeA, nodeB);
            else if (nodeB.State is ICompositeState bc)
                return function.Apply(p, bc, nodeA, nodeB);
            else if (nodeB.State is ConstrainsState bcon)
                return function.Apply(p, bcon, nodeA, nodeB);
            else
                throw new NotSupportedException($"State {nodeA.State.GetType()} is not supported");
        }
        else if (nodeA.State is ConstrainsState con)
        {
            if (nodeB.State is StatePrimitive bp)
                return function.Apply(con, bp, nodeA, nodeB);
            else if (nodeB.State is ICompositeState bc)
                return function.Apply(con, bc, nodeA, nodeB);
            else if (nodeB.State is ConstrainsState bcon)
                return function.Apply(con, bcon, nodeA, nodeB);
            else
                throw new NotSupportedException($"State {nodeA.State.GetType()} is not supported");
        }
        else if (nodeA.State is ICompositeState c)
        {
            if (nodeB.State is StatePrimitive bp)
                return function.Apply(c, bp, nodeA, nodeB);
            else if (nodeB.State is ConstrainsState bcon)
                return function.Apply(c, bcon, nodeA, nodeB);
            else if (nodeB.State is ICompositeState bc)
                return c switch {
                    StateArray arrA => bc is StateArray arrB && function.Apply(arrA, arrB, nodeA, nodeB),
                    StateFun funA => bc is StateFun funB && function.Apply(funA, funB, nodeA, nodeB),
                    StateStruct strA => bc is StateStruct strB && function.Apply(strA, strB, nodeA, nodeB),
                    _ => throw new NotSupportedException($"State {nodeA.State.GetType()} is not supported")
                };
            else
                throw new NotSupportedException($"State {nodeA.State.GetType()} is not supported");
        }
        else if (nodeA.State is StateRefTo r)
            return Invoke(function, r.Node, nodeB);
        else
            throw new NotSupportedException($"State {nodeA.State.GetType()} is not supported");
    }
}
