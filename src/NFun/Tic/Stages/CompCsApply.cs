namespace NFun.Tic.Stages;

using NFun.Tic.Algebra;
using NFun.Tic.SolvingStates;

/// <summary>
/// Stage C.3 — shared helpers for the directional Apply cells per
/// <c>Specs/Tic/Algebra_CompositeConstraints.md</c> §4. Used by
/// <see cref="PullConstraintsFunctions"/>, <see cref="PushConstraintsFunctions"/>,
/// <see cref="DestructionFunctions"/>.
///
/// <para>The 4 critical cells of spec §4.1 (forward Pull/Push + reverse Pull/Push
/// with StateCollection) live here as static helpers. The owning IStateFunction
/// methods are thin wrappers per-stage.</para>
/// </summary>
internal static class CompCsApply {

    /// <summary>§4.1.1 Forward Pull `Apply(CompCS anc, StateCollection desc)`.
    /// LCA on CompCS.Desc only; CompCS.Anc untouched. Element invariance: merge
    /// element nodes (identity unification) then eagerly Pull on the CompCS
    /// element so its existing ancestor edges (e.g. predicate input positions)
    /// pick up the merged element type. The eager Pull is needed because
    /// MergeInplace mutates state after the streaming toposort has already
    /// processed CompCS.ElementNode.</summary>
    public static bool ForwardPullCompCsSc(
        StateCompositeConstraints ancestor, StateCollection sc,
        TicNode ancestorNode, TicNode descendantNode) {
        var K = sc.Constructor;
        if (ancestor.HasAncestor && !ConstructorLattice.IsSubtypeOf(K, ancestor.Ancestor.Value))
            return false;
        var newDescKind = ancestor.HasDescendant
            ? ConstructorLattice.Lca(ancestor.Descendant.Value, K)
            : K;
        var newState = ancestor.WithDescendant(newDescKind);
        if (newState == null) return false;
        // Element propagation: try MergeInplace first (strict identity merge —
        // preserves type-precision for back-propagation through covariant
        // composites like StateArray covariance), fall back to AddAncestor
        // (subtyping edge) when merge would fail on not-yet-resolved composite
        // shapes (e.g. lambda input `(int)->T1` vs list element `(Any)->U24`
        // where function-arg contravariance means strict equality fails but
        // Pull/Push chain reconciles correctly). Mirrors the same-kind
        // Apply(StateCollection, StateCollection) AddAncestor fallback used at
        // PullConstraintsFunctions line 417.
        if (descendantNode != ancestorNode && sc.ElementNode != ancestor.ElementNode) {
            if (CanMergeStates(ancestor.ElementNode, sc.ElementNode)) {
                SolvingFunctions.MergeInplace(ancestor.ElementNode, sc.ElementNode);
            } else {
                sc.ElementNode.AddAncestor(ancestor.ElementNode);
                // Restore P3 Monotonicity (TicProofs.md §3): the AddAncestor
                // fallback edge is one-way (sc.elem → ancestor.elem) but the
                // bidirectional Preferred-copy rule (TicPreferred.md §3) is only
                // applied by Apply(CS, CS) at edge processing time. Streaming
                // Pull may have processed sc.elem before this edge existed —
                // its Preferred metadata never reaches ancestor.elem via the
                // standard path. Explicit copy here closes the gap. Debt #16.
                PropagatePreferredAcrossFallback(sc.ElementNode, ancestor.ElementNode);
            }
            // Eager re-Pull: streaming toposort already processed the element
            // before this Apply changed its constraints/ancestors.
            SolvingFunctions.PullConstraintsForNode(ancestor.ElementNode);
        }
        ancestorNode.State = newState;
        descendantNode.RemoveAncestor(ancestorNode);
        return true;
    }

    /// <summary>True if MergeInplace on these two nodes' states would succeed
    /// without throwing IncompatibleNodes. Used as a guard before strict
    /// element-merge in CompCs cross-cells.</summary>
    private static bool CanMergeStates(TicNode a, TicNode b) {
        var sa = a.GetNonReference().State;
        var sb = b.GetNonReference().State;
        return SolvingFunctions.GetMergedStateOrNull(sa, sb) != null;
    }

    /// <summary>
    /// Restore P3 Monotonicity of Pull (TicProofs.md §3) at the CompCs cross-Apply
    /// AddAncestor-fallback boundary: when MergeInplace can't fire, the descendant's
    /// Preferred metadata would otherwise be lost. Mirror the bidirectional
    /// Preferred-copy rule from Apply(CS, CS) (PullConstraintsFunctions line 33-36 /
    /// TicPreferred.md §3) so it holds at this cross-cell too. Closes debt #16.
    /// </summary>
    private static void PropagatePreferredAcrossFallback(TicNode source, TicNode target) {
        if (target.GetNonReference().State is ConstraintsState targetCs
            && source.GetNonReference().State is ConstraintsState sourceCs
            && targetCs.Preferred == null && sourceCs.Preferred != null) {
            targetCs.Preferred = sourceCs.Preferred;
        }
    }

    /// <summary>§4.1.2 Forward Push `Apply(CompCS anc, StateCollection desc)`.
    /// Precondition check only; no state mutation. Element-axis push.</summary>
    public static bool ForwardPushCompCsSc(
        StateCompositeConstraints ancestor, StateCollection sc,
        TicNode ancestorNode, TicNode descendantNode) {
        var K = sc.Constructor;
        if (ancestor.HasAncestor && !ConstructorLattice.IsSubtypeOf(K, ancestor.Ancestor.Value))
            return false;
        // No state mutation on either side. Element invariance still needs unification
        // (Layer-0 invariant) — element-node merge happens once during Pull side; Push
        // just checks compatibility.
        return true;
    }

    /// <summary>§4.1.3 Reverse Pull `Apply(StateCollection anc, CompCS desc)`.
    /// Collapse CompCS_d to concrete SC(K); commit on descendant.</summary>
    public static bool ReversePullScCompCs(
        StateCollection sc, StateCompositeConstraints descendant,
        TicNode ancestorNode, TicNode descendantNode) {
        var K = sc.Constructor;
        if (descendant.HasAncestor && !ConstructorLattice.IsSubtypeOf(K, descendant.Ancestor.Value))
            return false;
        if (descendant.HasDescendant && !ConstructorLattice.IsSubtypeOf(descendant.Descendant.Value, K))
            return false;
        // Invariance: element merge.
        if (!System.Object.ReferenceEquals(descendant.ElementNode, sc.ElementNode))
            SolvingFunctions.MergeInplace(descendant.ElementNode, sc.ElementNode);
        // Collapse CompCS_d to SC(K, descendant.ElementNode).
        ITicNodeState collapsed = new StateCollection(K, descendant.ElementNode);
        if (descendant.IsOptional) collapsed = StateOptional.Of(collapsed);
        descendantNode.State = collapsed;
        descendantNode.RemoveAncestor(ancestorNode);
        return true;
    }

    /// <summary>§4.1.4 Reverse Push `Apply(StateCollection anc, CompCS desc)`.
    /// GCD on CompCS_d.Anc with null-guard (cross-branch K rejects); CompCS_d.Desc untouched.</summary>
    public static bool ReversePushScCompCs(
        StateCollection sc, StateCompositeConstraints descendant,
        TicNode ancestorNode, TicNode descendantNode) {
        var K = sc.Constructor;
        if (descendant.HasDescendant && !ConstructorLattice.IsSubtypeOf(descendant.Descendant.Value, K))
            return false;
        ConstructorKind newAnc;
        if (descendant.HasAncestor) {
            var narrowed = ConstructorLattice.Gcd(descendant.Ancestor.Value, K);
            if (narrowed == null) return false; // cross-branch (e.g. list vs set)
            newAnc = narrowed.Value;
        } else {
            newAnc = K;
        }
        var newState = descendant.WithAncestor(newAnc);
        if (newState == null) return false;
        if (!System.Object.ReferenceEquals(descendant.ElementNode, sc.ElementNode))
            SolvingFunctions.MergeInplace(descendant.ElementNode, sc.ElementNode);
        descendantNode.State = newState;
        return true;
    }

    /// <summary>Same-class Apply via UnifyOrNull (spec §3.3 + §4.2 centralised collapse).</summary>
    public static bool ApplySameClass(
        StateCompositeConstraints ancestor, StateCompositeConstraints descendant,
        TicNode ancestorNode, TicNode descendantNode) {
        var merged = ancestor.UnifyCompCs(descendant);
        if (merged == null) return false;
        ancestorNode.State = merged;
        descendantNode.RemoveAncestor(ancestorNode);
        return true;
    }

    /// <summary>
    /// Stage 5 / Map.3 — DELETED. Map now flows as <c>StateCollection(Map,
    /// structNode)</c> where structNode IS the frozen <c>{key, value}</c>
    /// pair-struct. The CompCS×StateCollection cells (ForwardPullCompCsSc,
    /// ForwardPushCompCsSc, ReversePullScCompCs, ReversePushScCompCs) handle
    /// Map uniformly via the SC dispatch — the pair-struct synthesis is now
    /// built ONCE at <c>StateCollection.OfMap</c> factory time (identity-stable
    /// across all paths) instead of being re-synthesised per cell with fresh
    /// nodes. Closes the dual-representation bug class.
    /// </summary>

    /// <summary>Cross with StateArray, forward direction — Layer-0 cross-rule §3.10.
    /// Try strict MergeInplace first (preserves element-node identity for
    /// primitive/struct elements; this was the original behaviour), fall back
    /// to AddAncestor when merge would fail on not-yet-resolved composite
    /// shapes (function types with contravariant arg slots). ee-mode closure-
    /// in-array cases hit the AddAncestor path and lose preferred-Real
    /// propagation precision — see TicTechnicalDebt.md #16.</summary>
    public static bool ForwardCompCsStateArray(
        StateCompositeConstraints ancestor, StateArray sa,
        TicNode ancestorNode, TicNode descendantNode, bool isPull) {
        // Cross-kind narrowing: CompCs imposes a stricter shape than ee-mode
        // StateArray (which conceptually sits at FixedArray). When the CompCs
        // cap (Anc) is in the lattice and narrower than/at FixedArray (Array,
        // List, FixedArray, or any Clearable kind), narrow the descendant
        // StateArray to a concrete StateCollection of the right kind. Mirror
        // of `MergeOrNull` cross-kind rule (StateArray + StateCollection =
        // StateCollection). With IsClearable=true, pick List (the unique
        // Clearable kind in the Array branch).
        if (ancestor.HasAncestor && ancestor.Ancestor.Value != ConstructorKind.Any
            && ancestor.Ancestor.Value != ConstructorKind.Enumerable) {
            var kind = ancestor.IsClearable
                ? ConstructorKind.List
                : ConstructorLattice.Concretest(ancestor.Ancestor.Value);
            ITicNodeState narrowed = new StateCollection(kind, sa.ElementNode);
            if (ancestor.IsOptional) narrowed = StateOptional.Of(narrowed);
            descendantNode.State = narrowed;
            if (isPull) descendantNode.RemoveAncestor(ancestorNode);
            return true;
        }
        if (ancestor.IsClearable) {
            ITicNodeState narrowed = new StateCollection(ConstructorKind.List, sa.ElementNode);
            if (ancestor.IsOptional) narrowed = StateOptional.Of(narrowed);
            descendantNode.State = narrowed;
            if (isPull) descendantNode.RemoveAncestor(ancestorNode);
            return true;
        }
        if (isPull && sa.ElementNode != ancestor.ElementNode) {
            if (CanMergeStates(ancestor.ElementNode, sa.ElementNode)) {
                SolvingFunctions.MergeInplace(ancestor.ElementNode, sa.ElementNode);
            } else {
                sa.ElementNode.AddAncestor(ancestor.ElementNode);
                // P3 Monotonicity restore — see PropagatePreferredAcrossFallback. Debt #16.
                PropagatePreferredAcrossFallback(sa.ElementNode, ancestor.ElementNode);
            }
            SolvingFunctions.PullConstraintsForNode(ancestor.ElementNode);
        }
        if (isPull)
            descendantNode.RemoveAncestor(ancestorNode);
        return true;
    }

    /// <summary>Cross with StateArray, reverse direction.</summary>
    public static bool ReverseCompCsStateArray(
        StateArray sa, StateCompositeConstraints descendant,
        TicNode ancestorNode, TicNode descendantNode, bool isPull) {
        if (isPull && descendant.ElementNode != sa.ElementNode) {
            if (CanMergeStates(descendant.ElementNode, sa.ElementNode)) {
                SolvingFunctions.MergeInplace(descendant.ElementNode, sa.ElementNode);
            } else {
                descendant.ElementNode.AddAncestor(sa.ElementNode);
                // P3 Monotonicity restore — see PropagatePreferredAcrossFallback. Debt #16.
                PropagatePreferredAcrossFallback(descendant.ElementNode, sa.ElementNode);
            }
            SolvingFunctions.PullConstraintsForNode(descendant.ElementNode);
        }
        if (isPull)
            descendantNode.RemoveAncestor(ancestorNode);
        return true;
    }
}
