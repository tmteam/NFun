namespace NFun.Tic.Stages;

using NFun.Tic.Algebra;
using NFun.Tic.SolvingStates;

/// <summary>Shared directional Apply cells for StateCompositeConstraints.
/// See specs_tic/Algebra/CompositeConstraints.md §4.</summary>
internal static class CompCsApply {

    /// <summary>Forward Pull. Eager re-Pull on the CompCS element is needed because
    /// MergeInplace mutates state after the streaming toposort already processed it.
    /// See specs_tic/Algebra/CompositeConstraints.md §4.1.1.</summary>
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
        // Strict MergeInplace preserves back-propagation precision; AddAncestor
        // fallback handles unresolved composite shapes (contravariant arg slots)
        // where strict equality would fail but Pull/Push chain reconciles.
        if (descendantNode != ancestorNode && sc.ElementNode != ancestor.ElementNode) {
            if (CanMergeStates(ancestor.ElementNode, sc.ElementNode)) {
                SolvingFunctions.MergeInplace(ancestor.ElementNode, sc.ElementNode);
            } else {
                sc.ElementNode.AddAncestor(ancestor.ElementNode);
                // WORKAROUND (specs_tic/TicTechnicalDebt.md #16): AddAncestor edge is
                // one-way, but streaming Pull may have processed the element before
                // this edge existed — explicit Preferred copy closes the P3 gap.
                PropagatePreferredAcrossFallback(sc.ElementNode, ancestor.ElementNode);
            }
            // Eager re-Pull: toposort already processed this element pre-mutation.
            SolvingFunctions.PullConstraintsForNode(ancestor.ElementNode);
        }
        ancestorNode.State = newState;
        descendantNode.RemoveAncestor(ancestorNode);
        return true;
    }

    private static bool CanMergeStates(TicNode a, TicNode b) {
        var sa = a.GetNonReference().State;
        var sb = b.GetNonReference().State;
        return SolvingFunctions.GetMergedStateOrNull(sa, sb) != null;
    }

    /// <summary>Mirror of the bidirectional Preferred-copy rule from Apply(CS,CS)
    /// at the AddAncestor-fallback boundary. specs_tic/TicTechnicalDebt.md #16.</summary>
    private static void PropagatePreferredAcrossFallback(TicNode source, TicNode target) {
        if (target.GetNonReference().State is ConstraintsState targetCs
            && source.GetNonReference().State is ConstraintsState sourceCs
            && targetCs.Preferred == null && sourceCs.Preferred != null) {
            targetCs.Preferred = sourceCs.Preferred;
        }
    }

    /// <summary>Forward Push: precondition check only.
    /// See specs_tic/Algebra/CompositeConstraints.md §4.1.2.</summary>
    public static bool ForwardPushCompCsSc(
        StateCompositeConstraints ancestor, StateCollection sc,
        TicNode ancestorNode, TicNode descendantNode) {
        var K = sc.Constructor;
        if (ancestor.HasAncestor && !ConstructorLattice.IsSubtypeOf(K, ancestor.Ancestor.Value))
            return false;
        return true;
    }

    /// <summary>Reverse Pull. specs_tic/Algebra/CompositeConstraints.md §4.1.3.</summary>
    public static bool ReversePullScCompCs(
        StateCollection sc, StateCompositeConstraints descendant,
        TicNode ancestorNode, TicNode descendantNode) {
        var K = sc.Constructor;
        if (descendant.HasAncestor && !ConstructorLattice.IsSubtypeOf(K, descendant.Ancestor.Value))
            return false;
        if (descendant.HasDescendant && !ConstructorLattice.IsSubtypeOf(descendant.Descendant.Value, K))
            return false;
        if (!System.Object.ReferenceEquals(descendant.ElementNode, sc.ElementNode))
            SolvingFunctions.MergeInplace(descendant.ElementNode, sc.ElementNode);
        ITicNodeState collapsed = new StateCollection(K, descendant.ElementNode);
        if (descendant.IsOptional) collapsed = StateOptional.Of(collapsed);
        descendantNode.State = collapsed;
        descendantNode.RemoveAncestor(ancestorNode);
        return true;
    }

    /// <summary>Reverse Push. specs_tic/Algebra/CompositeConstraints.md §4.1.4.</summary>
    public static bool ReversePushScCompCs(
        StateCollection sc, StateCompositeConstraints descendant,
        TicNode ancestorNode, TicNode descendantNode) {
        var K = sc.Constructor;
        if (descendant.HasDescendant && !ConstructorLattice.IsSubtypeOf(descendant.Descendant.Value, K))
            return false;
        ConstructorKind newAnc;
        if (descendant.HasAncestor) {
            var narrowed = ConstructorLattice.Gcd(descendant.Ancestor.Value, K);
            if (narrowed == null) return false;
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

    /// <summary>Same-class Apply via UnifyOrNull.
    /// See specs_tic/Algebra/CompositeConstraints.md §3.3, §4.2.</summary>
    public static bool ApplySameClass(
        StateCompositeConstraints ancestor, StateCompositeConstraints descendant,
        TicNode ancestorNode, TicNode descendantNode) {
        var merged = ancestor.UnifyCompCs(descendant);
        if (merged == null) return false;
        ancestorNode.State = merged;
        descendantNode.RemoveAncestor(ancestorNode);
        return true;
    }

    /// <summary>Cross with StateArray, forward.
    /// See specs_tic/Algebra/CompositeConstraints.md §3.10.
    /// AddAncestor fallback loses Preferred precision on contravariant arg slots —
    /// specs_tic/TicTechnicalDebt.md #16.</summary>
    public static bool ForwardCompCsStateArray(
        StateCompositeConstraints ancestor, StateArray sa,
        TicNode ancestorNode, TicNode descendantNode, bool isPull) {
        // Cross-kind narrowing: CompCs cap is stricter than StateArray (≡ FixedArray).
        // IsClearable picks List (unique Clearable kind in Array-branch).
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
                // See PropagatePreferredAcrossFallback. specs_tic/TicTechnicalDebt.md #16.
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
                // See PropagatePreferredAcrossFallback. specs_tic/TicTechnicalDebt.md #16.
                PropagatePreferredAcrossFallback(descendant.ElementNode, sa.ElementNode);
            }
            SolvingFunctions.PullConstraintsForNode(descendant.ElementNode);
        }
        if (isPull)
            descendantNode.RemoveAncestor(ancestorNode);
        return true;
    }
}
