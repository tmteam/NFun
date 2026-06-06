namespace NFun.Tic.Algebra;

using SolvingStates;
using static SolvingStates.StatePrimitive;

/// <summary>
/// Layer-0 algebra (symmetric content) operators for
/// <see cref="StateCompositeConstraints"/> per
/// <c>Specs/Tic/Algebra_CompositeConstraints.md</c> v6.1 §3.
///
/// <para><b>Operator inventory:</b>
/// <list type="bullet">
/// <item><see cref="LcaCompCs"/> — §3.1 (same-class + cross-class LCA dispatch).</item>
/// <item><see cref="GcdCompCs"/> — §3.2 cross-class GCD; same-class delegates to Unify per §3.2.</item>
/// <item><see cref="UnifyCompCs"/> — §3.3 (`[LCA(D₁,D₂)..GCD(A₁,A₂)]` identity).</item>
/// <item><see cref="ConcretestCompCs"/> — §3.4 (Descendant if present, else ConstructorLattice.Concretest(Ancestor), else self-as-residual).</item>
/// <item><see cref="AbstractestCompCs"/> — §3.5 (drop floor, keep cap).</item>
/// </list></para>
///
/// <para>All operators use cycle guards from <see cref="StateCompositeConstraints"/>
/// per §3.8 (-59000..-59600 mark range).</para>
///
/// <para>Operations have <b>no commit</b> — they return states; the Apply layer
/// (Pull/Push/Destruction in C.3) wires one-sided writes to <c>node.State</c>.
/// Element-node side-effects (<c>MergeInplace</c>) DO fire inside Layer 0
/// because element invariance forces unification regardless of which side
/// commits — that's a Layer-0 invariant.</para>
/// </summary>
public static partial class StateExtensions {

    // ─────────────────────────────────────────────────────────────────────
    // §3.1 LCA (symmetric join, widens)

    internal static ITicNodeState LcaCompCs(this StateCompositeConstraints a, ITicNodeState b) {
        // Symmetric dispatch by RHS type.
        if (b is StateCompositeConstraints bcc)
            return LcaCompCsSameClass(a, bcc);
        if (b is StateCollection sc)
            return LcaCompCsXColl(a, sc);
        if (b is StateArray sa)
            return LcaCompCsXArray(a, sa);
        if (b is StatePrimitive prim)
            return LcaCompCsXPrim(a, prim);
        if (b is StateOptional opt)
            return LcaWithOptional(opt, a);
        if (b is ConstraintsState cs)
            return LcaCompCsXCs(a, cs);
        // Fun / Struct: incompatible shapes → join is Any (top).
        return Any;
    }

    private static ITicNodeState LcaCompCsSameClass(StateCompositeConstraints a, StateCompositeConstraints b) {
        var guard = StateCompositeConstraints._compCsLcaInProgress ??= new();
        if (!guard.Add((a, b)) && !guard.Add((b, a)))
            return a; // coinductive: cycle hit
        try {
            // null-as-identity convention (v6.1 §3.1 — mirror StateExtensions.Lca.cs:19-24).
            var newDesc = (a.Descendant, b.Descendant) switch {
                (null, null) => (ConstructorKind?)null,
                (null, var d) => d,
                (var d, null) => d,
                (var d1, var d2) => (ConstructorKind?)ConstructorLattice.Lca(d1.Value, d2.Value)
            };
            var newAnc = (a.Ancestor, b.Ancestor) switch {
                (null, null) => (ConstructorKind?)null,
                (null, var x) => x,
                (var x, null) => x,
                (var x1, var x2) => (ConstructorKind?)ConstructorLattice.Lca(x1.Value, x2.Value)
            };
            // Element unification (invariance). MergeInplace fuses identities.
            if (!ReferenceEquals(a.ElementNode, b.ElementNode))
                SolvingFunctions.MergeInplace(a.ElementNode, b.ElementNode);
            var result = StateCompositeConstraints.Create(
                elementNode: a.ElementNode,
                ancestor: newAnc,
                descendant: newDesc,
                isOptional: a.IsOptional || b.IsOptional);
            // LCA widens — should never produce an empty interval if both inputs were valid.
            // Defensive: fall through to Any if Create rejected (shouldn't happen).
            return result ?? (ITicNodeState)Any;
        } finally {
            guard.Remove((a, b));
            guard.Remove((b, a));
        }
    }

    private static ITicNodeState LcaCompCsXColl(StateCompositeConstraints a, StateCollection sc) {
        var guard = StateCompositeConstraints._compCsXCollLcaInProgress ??= new();
        if (!guard.Add((a, sc)))
            return a;
        try {
            var K = sc.Constructor;
            // null-as-identity: point K becomes the floor/cap when CompCS side is null.
            var newDesc = a.HasDescendant
                ? (ConstructorKind?)ConstructorLattice.Lca(a.Descendant.Value, K)
                : K;
            var newAnc = a.HasAncestor
                ? (ConstructorKind?)ConstructorLattice.Lca(a.Ancestor.Value, K)
                : K;
            if (!ReferenceEquals(a.ElementNode, sc.ElementNode))
                SolvingFunctions.MergeInplace(a.ElementNode, sc.ElementNode);
            var result = StateCompositeConstraints.Create(
                elementNode: a.ElementNode,
                ancestor: newAnc,
                descendant: newDesc,
                isOptional: a.IsOptional);
            return result ?? (ITicNodeState)Any;
        } finally {
            guard.Remove((a, sc));
        }
    }

    private static ITicNodeState LcaCompCsXArray(StateCompositeConstraints a, StateArray sa) {
        var guard = StateCompositeConstraints._compCsXArrayLcaInProgress ??= new();
        if (!guard.Add((a, sa)))
            return a;
        try {
            // Element merge under invariance/covariance — StateArray is covariant
            // but cross-family with invariant CompCS uses identity unification.
            if (!ReferenceEquals(a.ElementNode, sa.ElementNode))
                SolvingFunctions.MergeInplace(a.ElementNode, sa.ElementNode);
            // Cross-family LCA defaults to StateArray with merged element
            // (matches existing StateExtensions.Lca behavior at :84-95).
            return StateArray.Of(a.ElementNode.State);
        } finally {
            guard.Remove((a, sa));
        }
    }

    private static ITicNodeState LcaCompCsXPrim(StateCompositeConstraints a, StatePrimitive prim) {
        if (prim == Any)
            return a.IsOptional ? (ITicNodeState)Any : Any; // opt(any) = any per Algebra.md
        // composite ∨ primitive non-Any → Any top
        return Any;
    }

    private static ITicNodeState LcaCompCsXCs(StateCompositeConstraints a, ConstraintsState cs) {
        // CS-with-composite-Desc: coerce CS to CompCS-view (§4.0.3) — but at Layer 0 we
        // can take a simpler route: if CS's Desc is a StateCollection, dispatch to
        // CompCS-vs-SC. Otherwise CS is primitive-only → join is Any.
        if (cs.HasDescendant && cs.Descendant is StateCollection scDesc)
            return LcaCompCsXColl(a, scDesc);
        if (cs.HasDescendant && cs.Descendant is StateArray saDesc)
            return LcaCompCsXArray(a, saDesc);
        return Any;
    }

    // ─────────────────────────────────────────────────────────────────────
    // §3.2 GCD — same-class delegates to Unify (§3.3); cross-class is unique.

    internal static ITicNodeState GcdCompCs(this StateCompositeConstraints a, ITicNodeState b) {
        // Same-class: delegate to Unify per spec §3.2.
        if (b is StateCompositeConstraints bcc)
            return UnifyCompCsSameClass(a, bcc);
        if (b is StateCollection sc)
            return GcdCompCsXColl(a, sc);
        if (b is StateArray sa)
            return GcdCompCsXArray(a, sa);
        if (b is StatePrimitive prim) {
            // Any ∧ X = X
            if (prim == Any) return a;
            // primitive non-Any ∧ composite = null (shape mismatch)
            return null;
        }
        if (b is StateOptional opt)
            return GcdCompCsXOptional(a, opt);
        if (b is ConstraintsState cs)
            return GcdCompCsXCs(a, cs);
        return null;
    }

    private static ITicNodeState GcdCompCsXColl(StateCompositeConstraints a, StateCollection sc) {
        var guard = StateCompositeConstraints._compCsXCollGcdInProgress ??= new();
        if (!guard.Add((a, sc)))
            return a;
        try {
            var K = sc.Constructor;
            // K must fit inside CompCS's interval.
            if (a.HasDescendant && !ConstructorLattice.IsSubtypeOf(a.Descendant.Value, K))
                return null;
            if (a.HasAncestor && !ConstructorLattice.IsSubtypeOf(K, a.Ancestor.Value))
                return null;
            if (!ReferenceEquals(a.ElementNode, sc.ElementNode))
                SolvingFunctions.MergeInplace(a.ElementNode, sc.ElementNode);
            // Collapse to concrete SC(K) — point intersection with interval.
            ITicNodeState result = new StateCollection(K, a.ElementNode);
            if (a.IsOptional) result = StateOptional.Of(result);
            return result;
        } finally {
            guard.Remove((a, sc));
        }
    }

    private static ITicNodeState GcdCompCsXArray(StateCompositeConstraints a, StateArray sa) {
        var guard = StateCompositeConstraints._compCsXArrayGcdInProgress ??= new();
        if (!guard.Add((a, sa)))
            return a;
        try {
            // Narrower-wins (v6.1 R1 fix — mirror SolvingFunctions.cs:74-78):
            // If CompCS has a lattice cap (Anc non-null), it represents a NARROWER
            // kind than StateArray's nominal position. Collapse to StateCollection(Anc, e).
            if (!ReferenceEquals(a.ElementNode, sa.ElementNode))
                SolvingFunctions.MergeInplace(a.ElementNode, sa.ElementNode);
            if (a.HasAncestor)
                return new StateCollection(a.Ancestor.Value, a.ElementNode);
            // Otherwise unconstrained — fall back to StateArray.
            return StateArray.Of(a.ElementNode.State);
        } finally {
            guard.Remove((a, sa));
        }
    }

    private static ITicNodeState GcdCompCsXOptional(StateCompositeConstraints a, StateOptional opt) {
        var innerMeet = a.GcdCompCs(opt.Element);
        if (innerMeet == null) return null;
        // RHS is Optional — result wraps in Optional (v6.1 fix to §3.2 ellipsis).
        return StateOptional.Of(innerMeet);
    }

    private static ITicNodeState GcdCompCsXCs(StateCompositeConstraints a, ConstraintsState cs) {
        if (cs.HasDescendant && cs.Descendant is StateCollection scDesc)
            return GcdCompCsXColl(a, scDesc);
        if (cs.HasDescendant && cs.Descendant is StateArray saDesc)
            return GcdCompCsXArray(a, saDesc);
        // CS without composite Desc → primitive-only → composite meet is null
        return null;
    }

    // ─────────────────────────────────────────────────────────────────────
    // §3.3 Unify — symmetric meet via interval identity [LCA(D₁,D₂)..GCD(A₁,A₂)]

    internal static ITicNodeState UnifyCompCs(this StateCompositeConstraints a, ITicNodeState b) {
        if (b is StateCompositeConstraints bcc)
            return UnifyCompCsSameClass(a, bcc);
        // Cross-class: delegate to GCD (which handles narrower-wins and collapse).
        return a.GcdCompCs(b);
    }

    private static ITicNodeState UnifyCompCsSameClass(StateCompositeConstraints a, StateCompositeConstraints b) {
        var guard = StateCompositeConstraints._compCsUnifyInProgress ??= new();
        if (!guard.Add((a, b)) && !guard.Add((b, a)))
            return a; // coinductive
        try {
            // Per Algebra.md identity: Desc = LCA(D₁,D₂), Anc = GCD(A₁,A₂).
            // null-as-identity convention.
            var newDesc = (a.Descendant, b.Descendant) switch {
                (null, null) => (ConstructorKind?)null,
                (null, var d) => d,
                (var d, null) => d,
                (var d1, var d2) => (ConstructorKind?)ConstructorLattice.Lca(d1.Value, d2.Value)
            };
            ConstructorKind? newAnc;
            switch ((a.Ancestor, b.Ancestor)) {
                case (null, null): newAnc = null; break;
                case (null, var x): newAnc = x; break;
                case (var x, null): newAnc = x; break;
                default:
                    var gcd = ConstructorLattice.Gcd(a.Ancestor.Value, b.Ancestor.Value);
                    if (gcd == null) return null; // disjoint branches
                    newAnc = gcd;
                    break;
            }
            if (newDesc.HasValue && newAnc.HasValue
                && !ConstructorLattice.IsSubtypeOf(newDesc.Value, newAnc.Value))
                return null; // empty interval
            if (!ReferenceEquals(a.ElementNode, b.ElementNode))
                SolvingFunctions.MergeInplace(a.ElementNode, b.ElementNode);
            var result = StateCompositeConstraints.Create(
                elementNode: a.ElementNode,
                ancestor: newAnc,
                descendant: newDesc,
                isOptional: a.IsOptional || b.IsOptional);
            if (result == null) return null;
            return result.TryCollapseToPoint() ?? (ITicNodeState)result;
        } finally {
            guard.Remove((a, b));
            guard.Remove((b, a));
        }
    }

    // ─────────────────────────────────────────────────────────────────────
    // §3.4 Concretest — resolve to concrete StateCollection.

    internal static ITicNodeState ConcretestCompCs(this StateCompositeConstraints a) {
        var guard = StateCompositeConstraints._compCsConcretestInProgress ??= new();
        if (!guard.Add(a.ElementNode))
            return a; // coinductive: same node already being concretised
        try {
            ConstructorKind? pickedKind = null;
            if (a.HasDescendant) {
                pickedKind = a.Descendant;
            } else if (a.HasAncestor && a.Ancestor.Value != ConstructorKind.Any) {
                pickedKind = ConstructorLattice.Concretest(a.Ancestor.Value);
            }
            if (!pickedKind.HasValue)
                return a; // abstract residual — dialect default handles (§5)
            // Element node identity preserved (invariance).
            ITicNodeState result = new StateCollection(pickedKind.Value, a.ElementNode);
            if (a.IsOptional) result = StateOptional.Of(result);
            return result;
        } finally {
            guard.Remove(a.ElementNode);
        }
    }

    // ─────────────────────────────────────────────────────────────────────
    // §3.5 Abstractest — drop floor, keep cap.

    internal static ITicNodeState AbstractestCompCs(this StateCompositeConstraints a) {
        var guard = StateCompositeConstraints._compCsAbstractestInProgress ??= new();
        if (!guard.Add(a.ElementNode))
            return a;
        try {
            var widened = StateCompositeConstraints.Create(
                elementNode: a.ElementNode,
                ancestor: a.Ancestor,
                descendant: null, // drop floor
                isOptional: a.IsOptional);
            // Widening always succeeds when input was valid (asserted at C.2 unit-test).
            return widened ?? (ITicNodeState)a;
        } finally {
            guard.Remove(a.ElementNode);
        }
    }
}
