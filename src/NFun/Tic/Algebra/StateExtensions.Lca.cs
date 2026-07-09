namespace NFun.Tic.Algebra;

using System;
using System.Collections.Generic;
using SolvingStates;
using static SolvingStates.StatePrimitive;

public static partial class StateExtensions {
    public static ITicNodeState Lca(this ITicNodeState a, ITicNodeState b) => a.Lca(b, null);

    // ∨ with an explicit coinductive context (see AlgebraCycleContext): allocated lazily by
    // the cycle-capable arms (struct fields, S-axis join) and threaded through the whole
    // mutually-recursive family — Lca ↔ Gcd (Fun args), struct fields → Unify → Merge →
    // interval Lca, bound joins.
    internal static ITicNodeState Lca(this ITicNodeState a, ITicNodeState b, AlgebraCycleContext ctx) {
        if (a is StateRefTo aref)
            return aref.Element.Lca(b, ctx);
        if (b is StateRefTo bref)
            return a.Lca(bref.Element, ctx);
        if (a is ConstraintsState ac && b is ConstraintsState bc)
        {
            // Both are constraints — compute LCA of their concretest forms
            var descA = ac.HasDescendant ? ac.Descendant : null;
            var descB = bc.HasDescendant ? bc.Descendant : null;
            // One-sided desc uses the SNAPSHOT projection (↓ₛ, not pure ↓): the result is
            // stored as the joined CS's descendant, and LCA's transport rule requires
            // hints to survive the join — pure ↓ would strip nested element Preferred
            // that AddDescendant snapshots deliberately preserved (debt #19).
            var lcaDesc = (descA, descB) switch {
                (null, null) => null,
                (null, _)    => descB.ConcretestSnapshot(),
                (_, null)    => descA.ConcretestSnapshot(),
                _            => descA.Lca(descB, ctx)
            };
            var comparable = ac.IsComparable && bc.IsComparable;
            var isOptional = ac.IsOptional || bc.IsOptional;
            // Propagate Preferred hint through LCA (not algebraic, just a resolution hint).
            var preferred = PreferredHintLcaOrNull(ac.Preferred, bc.Preferred);
            // S axis (debt #12, Algebra_LCA.md §ConstraintsState): S = S₁ ∩ S₂ — field-name
            // intersection with recursive LCA on common field types; a missing bound on
            // either side absorbs to "no bound" (join keeps only common obligations).
            var lcaBound = ac.HasStructBound && bc.HasStructBound
                ? IntersectBoundsOrNull(ac.StructBound, bc.StructBound, ctx)
                : null;
            if (lcaDesc == null) {
                var result = ConstraintsState.Of(isComparable: comparable, isOptional: isOptional);
                result.Preferred = preferred;
                result.StructBound = lcaBound;
                return result;
            }
            if (!comparable && !isOptional && preferred == null && lcaBound == null && lcaDesc is ITypeState { IsSolved: true } and (ICompositeState or StatePrimitive { Name: PrimitiveTypeName.Any }))
                return lcaDesc;
            var cs = ConstraintsState.Of(lcaDesc, null, comparable, isOptional);
            cs.Preferred = preferred;
            cs.StructBound = lcaBound;
            return cs;
        }
        if (b is ConstraintsState bc2) {
            var inner = bc2.HasDescendant ? a.Lca(bc2.Descendant, ctx) : a.Concretest();
            // Propagate IsOptional: LCA(T, C[.., opt=true]) = C[T.., opt=true]
            if (bc2.IsOptional && !inner.Equals(Any)) {
                // Canonical flag form: the IsOptional flag already carries the axis,
                // so the stored Descendant must be opt-free — [opt(X)..]? is opt(opt(X))
                // and dies in interval checks ([F32..Re] vs [opt(F32)..]).
                var innerNoOpt = inner is StateOptional innerOpt2 ? innerOpt2.Element : inner;
                var cs = ConstraintsState.Of(innerNoOpt, isOptional: true);
                cs.Preferred = bc2.Preferred; // hints survive the join
                return cs;
            }
            // opt(P) ∨ CS[D..A](Pref) = CS[P..A]?(Pref): while the CS side carries a
            // Preferred hint, the join stays an unresolved interval (desc = P keeps the
            // left side's contribution). The former eager `StateOptional.Of(Preferred)`
            // here baked the hint into a concrete primitive — violating TicPreferred P3
            // and blocking Push from narrowing the target down (`{v:float32?}` field
            // pushes F32, but pre-baked opt(Re) can't accept it → FU719, Bug#6).
            // Ported from lang-mutable-collections.
            if (bc2.Preferred != null
                && inner is StateOptional { IsSolved: true } optInner
                && optInner.Element is StatePrimitive innerP) {
                var lifted = ConstraintsState.Of(
                    innerP,
                    bc2.HasAncestor ? bc2.Ancestor : null,
                    bc2.IsComparable,
                    isOptional: true);
                lifted.Preferred = bc2.Preferred;
                return lifted;
            }
            return inner;
        }
        if (a is ConstraintsState)
            return b.Lca(a, ctx); // symmetric: hits bc2 branch above

        // None: LCA(None, T) = Opt(T), LCA(None, None) = None, LCA(None, Opt(T)) = Opt(T)
        if (a == None)
            return LcaWithNone(b);
        if (b == None)
            return LcaWithNone(a);
        // Optional: covariant wrapper
        if (a is StateOptional aopt)
            return LcaWithOptional(aopt, b, ctx);
        if (b is StateOptional bopt)
            return LcaWithOptional(bopt, a, ctx);

        if (a is StatePrimitive ap)
            return b is StatePrimitive bp ? ap.GetLastCommonPrimitiveAncestor(bp) : Any;
        if (b is StatePrimitive)
            return Any;
        return a switch {
            StateArray aarr => b is StateArray barr ? StateArray.Of(aarr.Element.Lca(barr.Element, ctx)) : Any,
            StateFun af => b is StateFun bf ? af.Lca(bf, ctx) : Any,
            StateStruct astruct => b is StateStruct bstruct ? astruct.Lca(bstruct, ctx) : Any,
            _ => Any
        };
    }

    /// <summary>
    /// Commutative transport of the Preferred resolution hint (metadata, Sat-neutral) through
    /// binary operators (∨ and ⊓ — Algebra_Merge.md §Preferred, debt #14).
    /// Rule: if both agree, keep; if only one is set, keep it; if both differ, take their
    /// primitive LCA — the wider one preserves the resolution intent (e.g. hint-LCA of
    /// int-literal P=I32 and real-literal P=Real is Real: any int lifts losslessly, and the
    /// user wrote a real literal in one branch so Real is the intended resolution).
    /// LCA = Any means the hints share no information (unrelated families) — drop.
    /// </summary>
    internal static StatePrimitive PreferredHintLcaOrNull(StatePrimitive a, StatePrimitive b) =>
        (a, b) switch {
            (null, null)  => null,
            (null, var p) => p,
            (var p, null) => p,
            var (pa, pb)  => pa.Equals(pb)
                ? pa
                : pa.GetLastCommonPrimitiveAncestor(pb) is { } lca && !lca.Equals(Any) ? lca : null
        };

    /// <summary>
    /// True iff the state is (or wraps, along the RefTo/Optional/Array spine) a
    /// recursion-variable position — a ConstraintsState carrying its own StructBound (the
    /// CS-encoded μ-back-edge; see SolvingFunctions.IsBoundCarrierCs). Struct layers are NOT
    /// descended: struct×struct positions recurse per-field in their own operators.
    /// </summary>
    internal static bool ContainsRecursionVariable(ITicNodeState state) {
        // Contractive spines are finite; the cap only fires on malformed cyclic spines —
        // treat those as recursive (conservative: caller preserves identity).
        for (int safety = 0; safety < 100; safety++) {
            switch (state) {
                case StateRefTo r: state = r.Node.GetNonReference().State; continue;
                case StateOptional o: state = o.ElementNode.GetNonReference().State; continue;
                case StateArray arr: state = arr.ElementNode.GetNonReference().State; continue;
                case ConstraintsState cs: return cs.HasStructBound;
                default: return false;
            }
        }
        return true;
    }

    /// <summary>
    /// S₁ ∩ S₂ — the join (weaker bound) of two F-bound structs (Algebra_LCA.md): keep only
    /// fields present in BOTH by name; common field types join via LCA (covariant — every
    /// satisfier of either bound satisfies the joined field). Recursion-variable positions are
    /// dropped from the intersection: a pure join cannot name the joint recursion variable, and
    /// dropping a required field only weakens the bound (sound for ∨). An empty intersection
    /// is "no bound" — null.
    /// Coinductive arm: μ-bounds can reach the same bound pair again through field LCA →
    /// CS∨CS → IntersectBoundsOrNull; the re-entry answer for a JOIN is "drop the bound" —
    /// weakening is always sound for ∨ (the result only accepts more).
    /// </summary>
    private static StateStruct IntersectBoundsOrNull(StateStruct a, StateStruct b, AlgebraCycleContext ctx) {
        if (ReferenceEquals(a, b))
            return a;
        ctx ??= new AlgebraCycleContext();
        if (ctx.BoundJoinPairInProgress(a, b))
            return null; // cycle re-entered — drop the bound (sound weakening for a join)
        ctx.EnterBoundJoinPair(a, b);
        try {
            var fields = new Dictionary<string, TicNode>(StringComparer.OrdinalIgnoreCase);
            foreach (var (name, valA) in a.Fields) {
                var valB = b.GetFieldOrNull(name);
                if (valB == null) continue; // join keeps only common obligations
                if (ReferenceEquals(valA, valB)) {
                    fields.Add(name, valA);
                    continue;
                }
                var sA = valA.GetNonReference().State;
                var sB = valB.GetNonReference().State;
                if (ContainsRecursionVariable(sA) || ContainsRecursionVariable(sB))
                    continue; // μ-position — drop (see xmldoc)
                fields.Add(name, TicNode.CreateInvisibleNode(sA.Lca(sB, ctx)));
            }
            if (fields.Count == 0)
                return null; // empty bound = no bound
            return new StateStruct(fields, isFrozen: a.IsFrozen && b.IsFrozen, isOpen: a.IsOpen && b.IsOpen) {
                TypeName = StateStruct.LcaTypeName(a.TypeName, b.TypeName),
                IsOptionalSourced = StateStruct.MergedIsOptionalSourced(a.IsOptionalSourced, b.IsOptionalSourced),
            };
        } finally {
            ctx.ExitBoundJoinPair();
        }
    }

    private static ITicNodeState LcaWithNone(ITicNodeState other) {
        if (other.Equals(None))
            return None;
        if (other.Equals(Any))
            return Any; // None ≤ Any → LCA = Any
        if (other is StateOptional opt)
            return opt.Element.Equals(Any) ? Any : other; // None ≤ Opt(T) → LCA = Opt(T); opt(any) = any
        // None ^ T = Opt(T) for non-Any types
        return StateOptional.Of(other);
    }

    private static ITicNodeState LcaWithOptional(StateOptional opt, ITicNodeState other, AlgebraCycleContext ctx) {
        ITicNodeState inner;
        if (other is StateOptional otherOpt)
            inner = opt.Element.Lca(otherOpt.Element, ctx);
        else
            inner = opt.Element.Lca(other, ctx);
        // opt(any) = any (collapses)
        if (inner.Equals(Any))
            return Any;
        return StateOptional.Of(inner);
    }

    // Coinductive arm of recursive struct LCA. Two layers:
    //   1. Named-type short-circuit: if both sides have the same TypeName, the μ-cycle
    //      reached itself through Lca/Gcd interleaving (Lca creates new struct snapshots
    //      every level, so ref-equality misses). Coinductively return one side.
    //   2. Reference-equals fallback for anonymous structs (no TypeName).
    private static ITicNodeState Lca(this StateStruct a, StateStruct b, AlgebraCycleContext ctx) {
        if (a.TypeName != null && a.TypeName == b.TypeName) {
            ctx ??= new AlgebraCycleContext();
            if (ctx.LcaNameInProgress(a.TypeName))
                return a;
            ctx.EnterLcaName(a.TypeName);
            try {
                return LcaStructFields(a, b, ctx);
            } finally {
                ctx.ExitLcaName();
            }
        }
        // Anonymous struct ref-equality arm (pre-existing).
        ctx ??= new AlgebraCycleContext();
        if (ctx.LcaPairInProgress(a, b))
            return a;
        ctx.EnterLcaPair(a, b);
        try {
            return LcaStructFields(a, b, ctx);
        } finally {
            ctx.ExitLcaPair();
        }
    }

    private static ITicNodeState LcaStructFields(StateStruct a, StateStruct b, AlgebraCycleContext ctx) {
        bool bothMutable = a is StateMutableStruct && b is StateMutableStruct;
        bool eitherMutable = a is StateMutableStruct || b is StateMutableStruct;

        // MutStruct x MutStruct: try invariant Unify per field; if any fails, downgrade to immutable Struct.
        // MutStruct x Struct (or vice versa): always upcast to immutable Struct with covariant LCA.
        // Struct x Struct: covariant LCA per field (existing behavior).
        var nodes = new Dictionary<string, TicNode>(StringComparer.OrdinalIgnoreCase);
        bool unifyFailed = false;
        foreach (var aField in a.Fields)
        {
            var bField = b.GetFieldOrNull(aField.Key);
            if (bField == null) continue;

            var aState = aField.Value.GetNonReference().State;
            var bState = bField.GetNonReference().State;

            ITicNodeState fieldType;
            if (bothMutable && !unifyFailed)
            {
                // Invariant: try Unify first
                fieldType = aState.UnifyOrNull(bState, ctx);
                if (fieldType == null)
                {
                    // Unify failed — downgrade entire result to immutable Struct, use LCA
                    unifyFailed = true;
                    fieldType = aState.Lca(bState, ctx);
                }
            }
            else if (aState is ConstraintsState && bState is ConstraintsState)
                fieldType = aState.UnifyOrNull(bState, ctx);
            else
                fieldType = aState.Lca(bState, ctx);

            if (fieldType == null) continue;
            nodes.Add(aField.Key, TicNode.CreateInvisibleNode(fieldType));
        }

        // Both MutStruct and all fields unified → result is MutStruct.
        // Otherwise → immutable Struct (mixed mutability or Unify failure).
        if (bothMutable && !unifyFailed)
            return new StateMutableStruct(nodes, true);
        // Note: IsOptionalSourced is NOT propagated through Lca. Lca produces
        // the structurally-narrowest common ancestor — its identity should not
        // inherit a marker that originated only on one side. The marker
        // matters for cycle-rescue gating, where Pull/Push merges share the
        // identity (handled in PullConstraintsFunctions.Apply(Struct,Struct)).
        return new StateStruct(nodes, true) {
            TypeName = StateStruct.LcaTypeName(a.TypeName, b.TypeName),
        };
    }

    private static ITicNodeState Lca(this StateFun a, StateFun b, AlgebraCycleContext ctx) {
        if (a.ArgsCount != b.ArgsCount)
            return Any;
        var returnState = a.ReturnType.Lca(b.ReturnType, ctx);
        var argNodes = new TicNode[a.ArgsCount];

        for (var i = 0; i < a.ArgNodes.Length; i++)
        {
            var aNode = a.ArgNodes[i];
            var bNode = b.ArgNodes[i];
            var gcd = aNode.State.Gcd(bNode.State, ctx);
            if (gcd == null)
                return Any;
            argNodes[i] = TicNode.CreateInvisibleNode(gcd);
        }

        return StateFun.Of(argNodes, TicNode.CreateInvisibleNode(returnState));
    }
}
