namespace NFun.Tic.Algebra;

using SolvingStates;
using static SolvingStates.StatePrimitive;

/// <summary>
/// Layer-0 algebra for <see cref="StateCompositeConstraints"/>.
/// See <c>specs_tic/Algebra/CompositeConstraints.md</c>.
/// Element invariance forces <c>MergeInplace</c> side-effects inside Layer 0
/// regardless of which side commits.
/// </summary>
public static partial class StateExtensions {

    internal static ITicNodeState LcaCompCs(this StateCompositeConstraints a, ITicNodeState b) {
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
        return Any;
    }

    private static ITicNodeState LcaCompCsSameClass(StateCompositeConstraints a, StateCompositeConstraints b) {
        // Cycle guard — coinductive return on revisit.
        const int mark = TicVisitMarks.CompCsLcaSame;
        if (a.ElementNode.VisitMark == mark || b.ElementNode.VisitMark == mark)
            return a;
        var prevA = a.ElementNode.VisitMark;
        var prevB = b.ElementNode.VisitMark;
        a.ElementNode.VisitMark = mark;
        b.ElementNode.VisitMark = mark;
        try {
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
            if (!ReferenceEquals(a.ElementNode, b.ElementNode))
                SolvingFunctions.MergeInplace(a.ElementNode, b.ElementNode);
            var result = StateCompositeConstraints.Create(
                elementNode: a.ElementNode,
                ancestor: newAnc,
                descendant: newDesc,
                isOptional: a.IsOptional || b.IsOptional,
                // Clearable: LCA narrows the typeclass set, so AND.
                isClearable: a.IsClearable && b.IsClearable);
            return result ?? (ITicNodeState)Any;
        } finally {
            a.ElementNode.VisitMark = prevA;
            b.ElementNode.VisitMark = prevB;
        }
    }

    private static ITicNodeState LcaCompCsXColl(StateCompositeConstraints a, StateCollection sc) {
        const int mark = TicVisitMarks.CompCsXCollLca;
        if (a.ElementNode.VisitMark == mark || sc.ElementNode.VisitMark == mark)
            return a;
        var prevA = a.ElementNode.VisitMark;
        var prevSc = sc.ElementNode.VisitMark;
        a.ElementNode.VisitMark = mark;
        sc.ElementNode.VisitMark = mark;
        try {
            var K = sc.Constructor;
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
                isOptional: a.IsOptional,
                // Clearable: AND on widen.
                isClearable: a.IsClearable && ConstructorLattice.IsClearable(K));
            return result ?? (ITicNodeState)Any;
        } finally {
            a.ElementNode.VisitMark = prevA;
            sc.ElementNode.VisitMark = prevSc;
        }
    }

    private static ITicNodeState LcaCompCsXArray(StateCompositeConstraints a, StateArray sa) {
        const int mark = TicVisitMarks.CompCsXArrayLca;
        if (a.ElementNode.VisitMark == mark || sa.ElementNode.VisitMark == mark)
            return a;
        var prevA = a.ElementNode.VisitMark;
        var prevSa = sa.ElementNode.VisitMark;
        a.ElementNode.VisitMark = mark;
        sa.ElementNode.VisitMark = mark;
        try {
            // Cross-family: invariant CompCS forces identity unification even though StateArray is covariant.
            if (!ReferenceEquals(a.ElementNode, sa.ElementNode))
                SolvingFunctions.MergeInplace(a.ElementNode, sa.ElementNode);
            ITicNodeState result = StateArray.Of(a.ElementNode.State);
            // Optional axis survives the join (T ≤ opt(T)) — mirrors LcaCompCsXColl.
            if (a.IsOptional) result = StateOptional.Of(result);
            return result;
        } finally {
            a.ElementNode.VisitMark = prevA;
            sa.ElementNode.VisitMark = prevSa;
        }
    }

    private static ITicNodeState LcaCompCsXPrim(StateCompositeConstraints a, StatePrimitive prim) {
        if (prim == Any)
            return Any; // opt(any) collapses to any
        return Any;
    }

    private static ITicNodeState LcaCompCsXCs(StateCompositeConstraints a, ConstraintsState cs) {
        // Dispatch via CS's Desc when composite; primitive-only CS joins to Any.
        if (cs.HasDescendant && cs.Descendant is StateCollection scDesc)
            return LcaCompCsXColl(a, scDesc);
        if (cs.HasDescendant && cs.Descendant is StateArray saDesc)
            return LcaCompCsXArray(a, saDesc);
        return Any;
    }

    internal static ITicNodeState GcdCompCs(this StateCompositeConstraints a, ITicNodeState b) {
        if (b is StateCompositeConstraints bcc)
            return UnifyCompCsSameClass(a, bcc);
        if (b is StateCollection sc)
            return GcdCompCsXColl(a, sc);
        if (b is StateArray sa)
            return GcdCompCsXArray(a, sa);
        if (b is StatePrimitive prim) {
            if (prim == Any) return a;
            return null;
        }
        if (b is StateOptional opt)
            return GcdCompCsXOptional(a, opt);
        if (b is ConstraintsState cs)
            return GcdCompCsXCs(a, cs);
        return null;
    }

    private static ITicNodeState GcdCompCsXColl(StateCompositeConstraints a, StateCollection sc) {
        const int mark = TicVisitMarks.CompCsXCollGcd;
        if (a.ElementNode.VisitMark == mark || sc.ElementNode.VisitMark == mark)
            return a;
        var prevA = a.ElementNode.VisitMark;
        var prevSc = sc.ElementNode.VisitMark;
        a.ElementNode.VisitMark = mark;
        sc.ElementNode.VisitMark = mark;
        try {
            var K = sc.Constructor;
            // K must fit inside CompCS's interval.
            if (a.HasDescendant && !ConstructorLattice.IsSubtypeOf(a.Descendant.Value, K))
                return null;
            if (a.HasAncestor && !ConstructorLattice.IsSubtypeOf(K, a.Ancestor.Value))
                return null;
            // K must satisfy Clearable typeclass when CompCS requires it.
            if (a.IsClearable && !ConstructorLattice.IsClearable(K))
                return null;
            if (!ReferenceEquals(a.ElementNode, sc.ElementNode))
                SolvingFunctions.MergeInplace(a.ElementNode, sc.ElementNode);
            // Collapse to concrete SC(K) — point intersection with interval.
            ITicNodeState result = new StateCollection(K, a.ElementNode);
            if (a.IsOptional) result = StateOptional.Of(result);
            return result;
        } finally {
            a.ElementNode.VisitMark = prevA;
            sc.ElementNode.VisitMark = prevSc;
        }
    }

    private static ITicNodeState GcdCompCsXArray(StateCompositeConstraints a, StateArray sa) {
        const int mark = TicVisitMarks.CompCsXArrayGcd;
        if (a.ElementNode.VisitMark == mark || sa.ElementNode.VisitMark == mark)
            return a;
        var prevA = a.ElementNode.VisitMark;
        var prevSa = sa.ElementNode.VisitMark;
        a.ElementNode.VisitMark = mark;
        sa.ElementNode.VisitMark = mark;
        try {
            if (!ReferenceEquals(a.ElementNode, sa.ElementNode))
                SolvingFunctions.MergeInplace(a.ElementNode, sa.ElementNode);
            // CompCS cap narrows StateArray's nominal position: collapse to SC(Anc, e)
            // when the Ancestor cap is a concrete constructor. Any/Enumerable caps
            // cannot materialize as StateCollection (ctor rejects them) — there the
            // array side IS the concrete shape of the meet. Mirrors GcdCompCsXColl's
            // Descendant/Clearable guards and IsOptional wrap.
            ITicNodeState result;
            if (a.HasAncestor
                && a.Ancestor.Value != ConstructorKind.Any
                && a.Ancestor.Value != ConstructorKind.Enumerable)
            {
                var k = a.Ancestor.Value;
                if (a.HasDescendant && !ConstructorLattice.IsSubtypeOf(a.Descendant.Value, k))
                    return null;
                if (a.IsClearable && !ConstructorLattice.IsClearable(k))
                    return null;
                result = new StateCollection(k, a.ElementNode);
            }
            else if (a.IsClearable)
            {
                // Clearable typeclass with no concrete cap: T[] is not clearable; the
                // narrowest array-branch clearable constructor is List.
                result = new StateCollection(ConstructorKind.List, a.ElementNode);
            }
            else
            {
                result = StateArray.Of(a.ElementNode);
            }
            if (a.IsOptional) result = StateOptional.Of(result);
            return result;
        } finally {
            a.ElementNode.VisitMark = prevA;
            sa.ElementNode.VisitMark = prevSa;
        }
    }

    private static ITicNodeState GcdCompCsXOptional(StateCompositeConstraints a, StateOptional opt) {
        var innerMeet = a.GcdCompCs(opt.Element);
        if (innerMeet == null) return null;
        // RHS Optional wraps result; T ≤ opt(T).
        return StateOptional.Of(innerMeet);
    }

    private static ITicNodeState GcdCompCsXCs(StateCompositeConstraints a, ConstraintsState cs) {
        if (cs.HasDescendant && cs.Descendant is StateCollection scDesc)
            return GcdCompCsXColl(a, scDesc);
        if (cs.HasDescendant && cs.Descendant is StateArray saDesc)
            return GcdCompCsXArray(a, saDesc);
        return null;
    }

    internal static ITicNodeState UnifyCompCs(this StateCompositeConstraints a, ITicNodeState b) {
        if (b is StateCompositeConstraints bcc)
            return UnifyCompCsSameClass(a, bcc);
        return a.GcdCompCs(b);
    }

    private static ITicNodeState UnifyCompCsSameClass(StateCompositeConstraints a, StateCompositeConstraints b) {
        const int mark = TicVisitMarks.CompCsUnify;
        if (a.ElementNode.VisitMark == mark || b.ElementNode.VisitMark == mark)
            return a;
        var prevA = a.ElementNode.VisitMark;
        var prevB = b.ElementNode.VisitMark;
        a.ElementNode.VisitMark = mark;
        b.ElementNode.VisitMark = mark;
        try {
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
                    if (gcd == null) return null;
                    newAnc = gcd;
                    break;
            }
            if (newDesc.HasValue && newAnc.HasValue
                && !ConstructorLattice.IsSubtypeOf(newDesc.Value, newAnc.Value))
                return null;
            if (!ReferenceEquals(a.ElementNode, b.ElementNode))
                SolvingFunctions.MergeInplace(a.ElementNode, b.ElementNode);
            var result = StateCompositeConstraints.Create(
                elementNode: a.ElementNode,
                ancestor: newAnc,
                descendant: newDesc,
                isOptional: a.IsOptional || b.IsOptional,
                // Clearable: OR on narrow.
                isClearable: a.IsClearable || b.IsClearable);
            if (result == null) return null;
            return result.TryCollapseToPoint() ?? (ITicNodeState)result;
        } finally {
            a.ElementNode.VisitMark = prevA;
            b.ElementNode.VisitMark = prevB;
        }
    }

    internal static ITicNodeState ConcretestCompCs(this StateCompositeConstraints a) {
        const int mark = TicVisitMarks.CompCsConcretest;
        if (a.ElementNode.VisitMark == mark)
            return a;
        var prev = a.ElementNode.VisitMark;
        a.ElementNode.VisitMark = mark;
        try {
            ConstructorKind? pickedKind = null;
            if (a.HasDescendant) {
                // SimplifyOrNull guarantees Desc satisfies IsClearable when set.
                pickedKind = a.Descendant;
            } else if (a.HasAncestor && a.Ancestor.Value != ConstructorKind.Any) {
                var concretest = ConstructorLattice.Concretest(a.Ancestor.Value);
                // IsClearable narrows to {List, Set, Map}; fall back to List if cap-Concretest escapes.
                pickedKind = (a.IsClearable && !ConstructorLattice.IsClearable(concretest))
                    ? ConstructorKind.List
                    : concretest;
            } else if (a.IsClearable) {
                pickedKind = ConstructorKind.List;
            }
            if (!pickedKind.HasValue)
                return a; // dialect default resolves the residual
            ITicNodeState result = new StateCollection(pickedKind.Value, a.ElementNode);
            if (a.IsOptional) result = StateOptional.Of(result);
            return result;
        } finally {
            a.ElementNode.VisitMark = prev;
        }
    }

    internal static ITicNodeState AbstractestCompCs(this StateCompositeConstraints a) {
        const int mark = TicVisitMarks.CompCsAbstractest;
        if (a.ElementNode.VisitMark == mark)
            return a;
        var prev = a.ElementNode.VisitMark;
        a.ElementNode.VisitMark = mark;
        try {
            var widened = StateCompositeConstraints.Create(
                elementNode: a.ElementNode,
                ancestor: a.Ancestor,
                descendant: null,
                isOptional: a.IsOptional,
                isClearable: a.IsClearable); // typeclass marker is independent of floor/cap
            return widened ?? (ITicNodeState)a;
        } finally {
            a.ElementNode.VisitMark = prev;
        }
    }
}
