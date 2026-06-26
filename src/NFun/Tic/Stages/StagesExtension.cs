namespace NFun.Tic.Stages;

using System;
using System.Collections.Generic;
using SolvingStates;

public static class StagesExtension {

    /// <summary>Coinductive visited-pair guard (Amadio-Cardelli '93 §3) for Apply
    /// over cyclic constraint graphs: re-encountered (nodeA, nodeB) ⇒ assume compatibility.</summary>
    [ThreadStatic]
    private static HashSet<(TicNode, TicNode)> _invokeVisitedPairs;

    /// <summary>Set by GraphBuilder.SolveCore. False ⇒ no μ-recursive types possible
    /// ⇒ skip visited-pair guard entirely.</summary>
    [ThreadStatic]
    internal static bool _isRecursion;

    public static bool Invoke<TFunction>(this TFunction function, TicNode nodeA, TicNode nodeB) where TFunction : IStateFunction {
        // Fast paths skipping the visited-pair guard:
        //   (1) !_isRecursion — graph cycle-free (set pre-TIC by GraphBuilder).
        //   (2) primitive × primitive — InvokeCore can't recurse through Invoke.
        // Optional × non-Optional is NOT fast-path even when !_isRecursion: the
        // Wrap*InOptional unwrap-then-Invoke can re-enter the same pair after a
        // state mutation re-establishes the original shape. The guard is the only
        // termination signal there.
        bool hasOptional = nodeA.State is StateOptional || nodeB.State is StateOptional;
        if ((!_isRecursion && !hasOptional) || (nodeA.State is StatePrimitive && nodeB.State is StatePrimitive))
            return InvokeCore(function, nodeA, nodeB);

        // Coinductive visited-pair guard — required for cycle members, not just heads.
        var pairs = _invokeVisitedPairs;
        if (pairs == null) {
            pairs = new HashSet<(TicNode, TicNode)>();
            _invokeVisitedPairs = pairs;
        }
        var pair = (nodeA, nodeB);
        if (!pairs.Add(pair))
            return true; // cycle re-entered — coinductive assumption
        try {
            return InvokeCore(function, nodeA, nodeB);
        }
        finally {
            pairs.Remove(pair);
        }
    }

    private static bool InvokeCore<TFunction>(TFunction function, TicNode nodeA, TicNode nodeB) where TFunction : IStateFunction {
        if (nodeB.State is StateRefTo bRef)
            return function.Invoke(nodeA, bRef.Node);
        if (nodeA.State is StatePrimitive p)
        {
            return nodeB.State switch {
                StatePrimitive bp => function.Apply(p, bp, nodeA, nodeB),
                StateCompositeConstraints bcc => function.Apply(p, bcc, nodeA, nodeB),
                ICompositeState bc => function.Apply(p, bc, nodeA, nodeB),
                ConstraintsState bcon => function.Apply(p, bcon, nodeA, nodeB),
                _ => throw new NotSupportedException($"State {nodeA.State.GetType()} is not supported")
            };
        }
        if (nodeA.State is ConstraintsState con)
        {
            return nodeB.State switch {
                StatePrimitive bp => function.Apply(con, bp, nodeA, nodeB),
                StateCompositeConstraints bcc => function.Apply(con, bcc, nodeA, nodeB),
                ICompositeState bc => function.Apply(con, bc, nodeA, nodeB),
                ConstraintsState bcon => function.Apply(con, bcon, nodeA, nodeB),
                _ => throw new NotSupportedException($"State {nodeA.State.GetType()} is not supported")
            };
        }
        if (nodeA.State is StateCompositeConstraints acc)
        {
            return nodeB.State switch {
                StatePrimitive bp => function.Apply(acc, bp, nodeA, nodeB),
                ConstraintsState bcon => function.Apply(acc, bcon, nodeA, nodeB),
                StateCompositeConstraints bcc => function.Apply(acc, bcc, nodeA, nodeB),
                ICompositeState bc => function.Apply(acc, bc, nodeA, nodeB),
                _ => throw new NotSupportedException($"State {nodeA.State.GetType()} is not supported")
            };
        }
        else if (nodeA.State is ICompositeState c)
        {
            return nodeB.State switch {
                StatePrimitive bp => function.Apply(c, bp, nodeA, nodeB),
                StateCompositeConstraints bcc => function.Apply(c, bcc, nodeA, nodeB),
                ConstraintsState bcon => function.Apply(c, bcon, nodeA, nodeB),
                ICompositeState bc => c switch {
                    StateArray arrA => bc switch {
                        StateArray arrB => function.Apply(arrA, arrB, nodeA, nodeB),
                        // Cross-family Array-branch StateCollection ≤ StateArray.
                        StateCollection collB => function.Apply(arrA, collB, nodeA, nodeB),
                        // LCA(F, Opt(F)) = Opt(F): wrap ancestor.
                        StateOptional optB => WrapAncestorInOptional(function, nodeA, nodeB, optB),
                        _ => false
                    },
                    // Unified single-arg invariant collections; cross-kind rejected by Apply.
                    StateCollection collA => bc switch {
                        StateCollection collB => function.Apply(collA, collB, nodeA, nodeB),
                        // Reverse direction: StateArray ≤ Array-branch StateCollection.
                        StateArray arrB => function.Apply(collA, arrB, nodeA, nodeB),
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
                    // LCA(Opt(T), T) = Opt(T): wrap descendant.
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

    /// <summary>LCA(F, Opt(F)) = Opt(F): wrap ancestor in Optional.
    /// Pinned nodes (SyntaxNode literals, signature params, solved primitives) reject.</summary>
    private static bool WrapAncestorInOptional<TFunction>(
        TFunction function, TicNode nodeA, TicNode nodeB, StateOptional optB) where TFunction : IStateFunction {
        TraceLog.WriteLine($"  WrapAncestorInOptional: nodeA={nodeA.Name}({nodeA.Type}):{nodeA.State} nodeB={nodeB.Name}:{nodeB.State}");

        // Contractivity: T ≤ opt(T) is trivial when T aliases opt's element —
        // wrapping would unfold to opt(opt(...)) and stack-overflow.
        if (ReferenceEquals(optB.ElementNode.GetNonReference(), nodeA))
            return true;
        // WORKAROUND (specs_tic/TicTechnicalDebt.md #5 — stale Pull snapshots):
        // Pinned nodes reject the lift; TypeVariable+composite-state is NOT pinned
        // (identity travels via innerNode).
        // Pinned = SyntaxNode literal | Named user variable | IsSignatureParam |
        //          IsSolved primitive.
        bool isPinned = nodeA.Type != TicNodeType.TypeVariable
                     || nodeA.IsSignatureParam
                     || (nodeA.IsSolved && nodeA.State is StatePrimitive);
        if (isPinned)
            throw Errors.TicErrors.IncompatibleNodes(nodeA, nodeB);
        var innerNode = TicNode.CreateTypeVariableNode("ow" + nodeA.Name, nodeA.State);
        innerNode.IsOptionalElement = true;
        nodeA.State = new StateOptional(innerNode);
        nodeA.IsOptionalElement = true;
        return function.Apply((StateOptional)nodeA.State, optB, nodeA, nodeB);
    }

    /// <summary>LCA(Opt(T), T) = Opt(T): wrap descendant.
    /// SyntaxNode literals fall back to unwrap.</summary>
    private static bool WrapDescendantInOptional<TFunction>(
        TFunction function, TicNode nodeA, TicNode nodeB, StateOptional optA) where TFunction : IStateFunction {
        TraceLog.WriteLine($"  WrapDescendantInOptional: nodeA={nodeA.Name}:{nodeA.State} nodeB={nodeB.Name}({nodeB.Type}):{nodeB.State}");
        if (nodeB.Type == TicNodeType.SyntaxNode || nodeB.IsSolved)
            return Invoke(function, optA.ElementNode, nodeB);
        // Contractive μ-cycle head: μX.F(X) is a single fixed-point (Pierce TAPL §20.2).
        // Don't wrap — Optional already absorbed by μ.
        if (nodeB.IsContractiveCycleHead && nodeB.Type == TicNodeType.TypeVariable)
            return Invoke(function, optA.ElementNode, nodeB);
        // Non-Optional composite desc: wrapping would change shared/external identity.
        // Use implicit lift T ≤ opt(T) — connect desc to optA's inner element instead.
        // Mirrors WrapAncestorInOptional's pinned-Named protection.
        if (nodeB.Type != TicNodeType.SyntaxNode && nodeB.State is ICompositeState && !nodeB.IsOptionalElement)
            return Invoke(function, optA.ElementNode, nodeB);
        var innerNode = TicNode.CreateTypeVariableNode("ow" + nodeB.Name, nodeB.State);
        innerNode.IsOptionalElement = true;
        nodeB.State = new StateOptional(innerNode);
        nodeB.IsOptionalElement = true;
        return function.Apply(optA, (StateOptional)nodeB.State, nodeA, nodeB);
    }
}
