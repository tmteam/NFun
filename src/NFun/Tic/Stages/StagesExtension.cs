namespace NFun.Tic.Stages;

using System;
using System.Collections.Generic;
using SolvingStates;

public static class StagesExtension {

    /// <summary>
    /// Coinductive visited-pair guard for Pull/Push Apply over cyclic constraint graphs.
    /// When (nodeA, nodeB) is re-encountered during a recursive Invoke, we assume compatibility
    /// (return true) per Amadio-Cardelli '93 §3 coinductive subtyping: μ-recursive types are
    /// equal/sub iff their unfoldings are equal/sub up to the visited-pair fixpoint.
    /// </summary>
    [ThreadStatic]
    private static HashSet<(TicNode, TicNode)> _invokeVisitedPairs;

    /// <summary>
    /// Set by GraphBuilder.SolveCore: when false, the graph cannot contain
    /// μ-recursive types (no SafeFieldAccess, no named-type registry, no
    /// recursive user function — checked deterministically pre-TIC). The
    /// visited-pair guard exists ONLY for cycle re-entry; with no cycles
    /// possible, skip it entirely.
    /// </summary>
    [ThreadStatic]
    internal static bool _isRecursion;

    public static bool Invoke<TFunction>(this TFunction function, TicNode nodeA, TicNode nodeB) where TFunction : IStateFunction {
        // Fast paths to skip the visited-pair guard:
        // (1) graph cannot contain cycles (`!_isRecursion`) — single bool check, the
        //     dominant case in non-recursive expressions. Set deterministically pre-TIC
        //     by GraphBuilder (no SafeFieldAccess, no named-type registry, no recursive
        //     user fn).
        // (2) both states primitive — even in recursive graphs, primitive↔primitive pairs
        //     cannot re-enter Invoke (InvokeCore dispatches to a single Apply with no
        //     further recursion through this method).
        //
        // Optional × non-Optional pairs are NOT a fast path even when not flagged
        // recursive: WrapDescendantInOptional / WrapAncestorInOptional's
        // unwrap-then-Invoke pattern can re-enter the same (nodeA, nodeB) pair
        // when state mutations (e.g. opt-wrap of the descendant's CS during
        // Destruction) re-establish the original Optional × non-Optional shape.
        // The visited-pair guard is the only termination signal. (StmtBug80:
        // `try: oops(...) catch e: return e.message` looped infinitely through
        // WrapDescendantInOptional on (V0:opt(V2), arr(Ch)) without it.)
        bool hasOptional = nodeA.State is StateOptional || nodeB.State is StateOptional;
        if ((!_isRecursion && !hasOptional) || (nodeA.State is StatePrimitive && nodeB.State is StatePrimitive))
            return InvokeCore(function, nodeA, nodeB);

        // Coinductive visited-pair guard. Required for the cycle members
        // (not just the cycle head) during Apply recursion through composite
        // elements. Per Amadio-Cardelli '93 §3 coinductive subtyping: when
        // (nodeA, nodeB) is re-encountered, assume compatibility (return true).
        var pairs = _invokeVisitedPairs;
        if (pairs == null) {
            pairs = new HashSet<(TicNode, TicNode)>();
            _invokeVisitedPairs = pairs;
        }
        var pair = (nodeA, nodeB);
        if (!pairs.Add(pair))
            return true; // coinductive assumption: cycle re-entered
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
        // Stage C.3: CompCS as ancestor — peer of ConstraintsState.
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
                        // Stage 0 hierarchy: list<T> ≤ T[]. ee-mode LINQ keyed on T[] can
                        // accept lang-mode list<T> values via this cross-family edge.
                        StateCollection collB => function.Apply(arrA, collB, nodeA, nodeB),
                        // LCA(F<...>, Opt(F<...>)) = Opt(F<...>): wrap ancestor.
                        StateOptional optB => WrapAncestorInOptional(function, nodeA, nodeB, optB),
                        _ => false
                    },
                    // Unified single-arg invariant collections (Stage 2.1b).
                    // Cross-kind pairs route here too — Apply rejects them internally.
                    StateCollection collA => bc switch {
                        StateCollection collB => function.Apply(collA, collB, nodeA, nodeB),
                        // Reverse direction subtyping (ancestor=lang collection,
                        // descendant=legacy ee-mode T[]). Lets `out:int[]` slots
                        // in lang accept results from ee-mode LINQ built-ins.
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
                    // StateMap deleted — Map dispatches as StateCollection(Map, …).
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
    /// Only wraps TypeVariable composite-state nodes — SyntaxNodes (literals), function-signature
    /// params, and pinned primitive types reject the lift.
    /// </summary>
    private static bool WrapAncestorInOptional<TFunction>(
        TFunction function, TicNode nodeA, TicNode nodeB, StateOptional optB) where TFunction : IStateFunction {
        TraceLog.WriteLine($"  WrapAncestorInOptional: nodeA={nodeA.Name}({nodeA.Type}):{nodeA.State} nodeB={nodeB.Name}:{nodeB.State}");

        // Wrapping nodeA in
        // StateOptional(innerNode) immediately makes nodeB.State (which references nodeA
        // via opt(nodeA)) point to opt(opt(innerNode))-equivalent. The next Apply(opt,opt)
        // calls PushConstraints(optB.ElementNode = nodeA, innerNode) which re-enters
        // WrapAncestorInOptional on (innerNode, nodeA) since nodeA is now opt-stated.
        // Unbounded re-wrap chain: arr(Ch) → opt(owarr) → opt(owowarr) → … (stack overflow).
        //
        // Algebraically: the lift `T ≤ opt(T)` is trivially satisfied when T's identity is
        // the same as opt's element — wrapping is unnecessary, the inclusion already holds.
        // Return true coinductively (Amadio-Cardelli '93 §3): the constraint is consistent.
        if (ReferenceEquals(optB.ElementNode.GetNonReference(), nodeA))
            return true;
        // WORKAROUND (TicTechnicalDebt #5 — stale Pull snapshots): the algebraic postulate
        // T ≤ opt(T) is universal — wrapping a TypeVariable's composite state in
        // StateOptional(innerNode) preserves all structural identity (including TypeName for
        // named structs, since the StateStruct moves verbatim into innerNode). The
        // previous `nodeA.IsSolved` guard rejected legal lifts on named-struct returns from
        // F-bound recursive functions (GH #126 followup: `loop(x, acc) = if(x==none) acc
        // else loop(x?.next, n{next=acc})` — V0 inherits the bare-struct snapshot from
        // Phase 1 before Phase 2 None propagation lifts acc; the late lift then needs to
        // widen V0 to opt(n)).
        //
        // Pinned-identity rule: widening is rejected only when the node has an external
        // identity commitment:
        //   - SyntaxNode literals (the value has a fixed apparent type)
        //   - Named nodes (user-declared variables — `y:text = x` must not silently widen
        //     y to opt(text); same for function I/O vars after SetVarType)
        //   - IsSignatureParam (function-signature shape is rigid by contract)
        //   - IsSolved primitive (covers anonymous primitive-typed TypeVariables — e.g.
        //     intermediate nodes pinned to I32 by ConvertType)
        // TypeVariable + composite state (struct/array/fun) is NOT pinned — identity
        // travels with the inner state via innerNode.
        bool isPinned = nodeA.Type != TicNodeType.TypeVariable  // SyntaxNode literal OR Named user variable
                     || nodeA.IsSignatureParam
                     || (nodeA.IsSolved && nodeA.State is StatePrimitive);
        if (isPinned)
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
        // Do not wrap in Optional if nodeB is on a certified contractive μ-cycle. The cycle
        // already represents the fixed-point μX. opt(...); each additional wrap would extend the
        // chain unnecessarily, leading to exponential opt(opt(...)) explosion before the
        // visited-pair guard catches the recursion. Per Pierce TAPL §20.2: μX. F(X) is a SINGLE
        // fixed-point, not a tower of unfoldings. When descendant is also a TypeVariable
        // (synthetic cycle marker, not a syntax node literal), unwrap (Optional absorbed by μ).
        if (nodeB.IsContractiveCycleHead && nodeB.Type == TicNodeType.TypeVariable)
            return Invoke(function, optA.ElementNode, nodeB);
        // When nodeB carries a non-Optional composite state, wrapping it in opt() would
        // change its shared/external identity. Use implicit lift T ≤ opt(T) instead:
        // connect descendant directly to the inner element of optA so its composite state
        // flows up, leaving nodeB itself non-Optional.
        //
        // Applies to both:
        //   - TypeVariable composite (e.g., generic param shared with another signature
        //     position via field access in a predicate body) — preserves lambda predicate's
        //     signature (T)->Bool across filter/first chains with T? annotation (MBug4)
        //   - Named composite (user-declared variable with concrete struct/array state from
        //     a literal binding) — `a = {b=1}; y = a?.b` must not silently widen `a` to
        //     opt(struct) just because `?.b` happens to set up an opt-struct ancestor on it
        //     (MR5Bug5). Mirrors WrapAncestorInOptional's pinned-Named protection (line ~162).
        if (nodeB.Type != TicNodeType.SyntaxNode && nodeB.State is ICompositeState && !nodeB.IsOptionalElement)
            return Invoke(function, optA.ElementNode, nodeB);
        var innerNode = TicNode.CreateTypeVariableNode("ow" + nodeB.Name, nodeB.State);
        innerNode.IsOptionalElement = true;
        nodeB.State = new StateOptional(innerNode);
        nodeB.IsOptionalElement = true;
        return function.Apply(optA, (StateOptional)nodeB.State, nodeA, nodeB);
    }
}
