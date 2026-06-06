using NFun.Tic.SolvingStates;
using NUnit.Framework;

namespace NFun.Tic.Tests.Collections;

using static StatePrimitive;

/// <summary>
/// Stage 2.1a acceptance tests for <see cref="SolvingFunctions.GetMergedStateOrNull"/>
/// over the unified <see cref="StateCollection"/> (data-driven via ConstructorKind).
///
/// Rules pinned here (from Specs/Collections.md and Specs/Tic/ConstructorLattice.md):
///
///  • SAME-KIND, same element  ⇒ returns the composite (invariant merge succeeds).
///  • SAME-KIND, different element ⇒ null (uniform invariance, no widening).
///  • CROSS-KIND (any pair drawn from {List, FixedArray, Array, Set})
///      ⇒ null. Even though the constructor lattice has a non-trivial LCA
///        (e.g. Lca(List, Array) = Array), MERGE requires equality at the
///        element position under invariance. Mixed kinds have no shared
///        instance, so merge falls null.
///  • StateCollection vs legacy ee <see cref="StateArray"/> ⇒ null (different
///        state families; the new composites are NOT in the StateArray lattice).
///  • StateCollection vs <see cref="ConstraintsState"/>:
///        — empty constraints      ⇒ returns the composite (constraint accepts all).
///        — non-empty constraints  ⇒ null when constraint's descendant/ancestor is not
///                                   convertible to the composite.
///  • Recursive composites (cycle through inner ElementNode) ⇒ no stack overflow;
///    coinductive merge to self.
///
/// All tests below run as-is. Stage 2.1a delivered cycle-guarded recursion in
/// <see cref="StateComposite.IsMutable"/> and <see cref="StateComposite.IsSolved"/>;
/// non-cycle behaviour was already correct on Stage 1's surface (handled by
/// the existing ConstraintsState.NoConstrains short-circuit + default null
/// fallback in GetMergedStateOrNull).
/// </summary>
[TestFixture]
public class MergeOrNullTest {

    // ─── Section 1: SAME-CLASS same-element invariant merge (success) ────

    [Test]
    public void SameClass_List_SameElement_ReturnsList() {
        var merged = SolvingFunctions.GetMergedStateOrNull(StateCollection.OfList(I32), StateCollection.OfList(I32));
        Assert.IsNotNull(merged);
        Assert.IsInstanceOf<StateCollection>(merged);
    }

    [Test]
    public void SameClass_FixedArray_SameElement_ReturnsFixedArray() {
        var merged = SolvingFunctions.GetMergedStateOrNull(StateCollection.OfFixedArray(Real), StateCollection.OfFixedArray(Real));
        Assert.IsNotNull(merged);
        Assert.IsInstanceOf<StateCollection>(merged);
    }

    [Test]
    public void SameClass_MutableArray_SameElement_ReturnsMutableArray() {
        var merged = SolvingFunctions.GetMergedStateOrNull(StateCollection.OfMutableArray(Bool), StateCollection.OfMutableArray(Bool));
        Assert.IsNotNull(merged);
        Assert.IsInstanceOf<StateCollection>(merged);
    }

    [Test]
    public void SameClass_List_NestedList_PreservesStructure() {
        var outerA = StateCollection.OfList(StateCollection.OfList(I32));
        var outerB = StateCollection.OfList(StateCollection.OfList(I32));
        var merged = SolvingFunctions.GetMergedStateOrNull(outerA, outerB);
        Assert.IsNotNull(merged);
        Assert.IsInstanceOf<StateCollection>(merged);
    }

    [Test]
    public void SameClass_FixedArray_NestedFixedArray_PreservesStructure() {
        var outerA = StateCollection.OfFixedArray(StateCollection.OfFixedArray(I32));
        var outerB = StateCollection.OfFixedArray(StateCollection.OfFixedArray(I32));
        var merged = SolvingFunctions.GetMergedStateOrNull(outerA, outerB);
        Assert.IsNotNull(merged);
        Assert.IsInstanceOf<StateCollection>(merged);
    }

    [Test]
    public void SameClass_MutableArray_NestedMutableArray_PreservesStructure() {
        var outerA = StateCollection.OfMutableArray(StateCollection.OfMutableArray(Bool));
        var outerB = StateCollection.OfMutableArray(StateCollection.OfMutableArray(Bool));
        var merged = SolvingFunctions.GetMergedStateOrNull(outerA, outerB);
        Assert.IsNotNull(merged);
        Assert.IsInstanceOf<StateCollection>(merged);
    }

    // ─── Section 2: SAME-CLASS different-element invariant merge (fail) ───

    [Test]
    public void SameClass_List_DifferentElement_ReturnsNull() {
        var merged = SolvingFunctions.GetMergedStateOrNull(StateCollection.OfList(I32), StateCollection.OfList(Real));
        Assert.IsNull(merged);
    }

    [Test]
    public void SameClass_FixedArray_DifferentElement_ReturnsNull() {
        var merged = SolvingFunctions.GetMergedStateOrNull(StateCollection.OfFixedArray(I32), StateCollection.OfFixedArray(Bool));
        Assert.IsNull(merged);
    }

    [Test]
    public void SameClass_MutableArray_DifferentElement_ReturnsNull() {
        var merged = SolvingFunctions.GetMergedStateOrNull(StateCollection.OfMutableArray(I32), StateCollection.OfMutableArray(Real));
        Assert.IsNull(merged);
    }

    [Test]
    public void SameClass_List_NestedDifferentElement_ReturnsNull() {
        var a = StateCollection.OfList(StateCollection.OfList(I32));
        var b = StateCollection.OfList(StateCollection.OfList(Real));
        Assert.IsNull(SolvingFunctions.GetMergedStateOrNull(a, b));
    }

    // ─── Section 3: CROSS-CLASS merge fails (uniform invariance + lattice) ─

    [Test]
    public void CrossClass_List_vs_FixedArray_ReturnsNull() {
        Assert.IsNull(SolvingFunctions.GetMergedStateOrNull(StateCollection.OfList(I32), StateCollection.OfFixedArray(I32)));
    }

    [Test]
    public void CrossClass_List_vs_MutableArray_ReturnsNull() {
        Assert.IsNull(SolvingFunctions.GetMergedStateOrNull(StateCollection.OfList(I32), StateCollection.OfMutableArray(I32)));
    }

    [Test]
    public void CrossClass_FixedArray_vs_MutableArray_ReturnsNull() {
        Assert.IsNull(SolvingFunctions.GetMergedStateOrNull(StateCollection.OfFixedArray(I32), StateCollection.OfMutableArray(I32)));
    }

    [Test]
    public void CrossClass_MutableArray_vs_List_ReturnsNull_AsymmetricCall() {
        Assert.IsNull(SolvingFunctions.GetMergedStateOrNull(StateCollection.OfMutableArray(I32), StateCollection.OfList(I32)));
    }

    [Test]
    public void CrossClass_FixedArray_vs_List_ReturnsNull_AsymmetricCall() {
        Assert.IsNull(SolvingFunctions.GetMergedStateOrNull(StateCollection.OfFixedArray(I32), StateCollection.OfList(I32)));
    }

    // ─── Section 4: Composite vs legacy StateArray (cross-system) ──────────

    [Test]
    public void NewComposite_List_vs_LegacyStateArray_ReturnsNull() {
        Assert.IsNull(SolvingFunctions.GetMergedStateOrNull(StateCollection.OfList(I32), StateArray.Of(I32)));
    }

    [Test]
    public void NewComposite_MutableArray_vs_LegacyStateArray_ReturnsNull() {
        Assert.IsNull(SolvingFunctions.GetMergedStateOrNull(StateCollection.OfMutableArray(I32), StateArray.Of(I32)));
    }

    [Test]
    public void NewComposite_FixedArray_vs_LegacyStateArray_ReturnsNull() {
        Assert.IsNull(SolvingFunctions.GetMergedStateOrNull(StateCollection.OfFixedArray(I32), StateArray.Of(I32)));
    }

    [Test]
    public void RegressionShield_LegacyStateArray_DifferentElement_ReturnsNull() {
        // REGRESSION SHIELD: legacy StateArray's merge is invariant in element
        // (covariance applies only via GetLastCommonAncestorOrNull, not via merge —
        // merge attempts MergeInplace on the element nodes and fails for unequal
        // concrete primitives).
        // Stage 2.1a must NOT touch this behaviour.
        var merged = SolvingFunctions.GetMergedStateOrNull(StateArray.Of(I32), StateArray.Of(Real));
        Assert.IsNull(merged);
    }

    [Test]
    public void RegressionShield_LegacyStateArray_LcaDifferentElement_Widens() {
        // REGRESSION SHIELD: LCA on legacy StateArray IS covariant — widens to
        // Real per the existing ee-mode semantics. This is the channel through
        // which `if(c) [1] else [1.0]` produces real[]. Stage 2.1a must preserve.
        var a = StateArray.Of(I32);
        var b = StateArray.Of(Real);
        var lca = a.GetLastCommonAncestorOrNull(b);
        Assert.IsInstanceOf<StateArray>(lca);
        Assert.AreSame(Real, ((StateArray)lca!).Element);
    }

    // ─── Section 5: Composite vs ConstraintsState ──────────────────────────

    [Test]
    public void List_vs_EmptyConstraints_ReturnsList() {
        // Empty constraint accepts everything (NoConstrains short-circuit).
        var merged = SolvingFunctions.GetMergedStateOrNull(StateCollection.OfList(I32), ConstraintsState.Empty);
        Assert.IsNotNull(merged);
        Assert.IsInstanceOf<StateCollection>(merged);
    }

    [Test]
    public void EmptyConstraints_vs_List_ReturnsList_SymmetricOrder() {
        var merged = SolvingFunctions.GetMergedStateOrNull(ConstraintsState.Empty, StateCollection.OfList(I32));
        Assert.IsNotNull(merged);
        Assert.IsInstanceOf<StateCollection>(merged);
    }

    [Test]
    public void FixedArray_vs_EmptyConstraints_ReturnsFixedArray() {
        var merged = SolvingFunctions.GetMergedStateOrNull(StateCollection.OfFixedArray(I32), ConstraintsState.Empty);
        Assert.IsInstanceOf<StateCollection>(merged);
    }

    [Test]
    public void MutableArray_vs_EmptyConstraints_ReturnsMutableArray() {
        var merged = SolvingFunctions.GetMergedStateOrNull(StateCollection.OfMutableArray(I32), ConstraintsState.Empty);
        Assert.IsInstanceOf<StateCollection>(merged);
    }

    [Test]
    public void List_vs_ConstraintsWithDescendantLegacyArray_ReturnsNull() {
        // Constraint with descendant in legacy-Array family cannot be satisfied by
        // a new-composite StateList — different state families.
        var cs = ConstraintsState.Of(desc: StateArray.Of(I32));
        Assert.IsNull(SolvingFunctions.GetMergedStateOrNull(StateCollection.OfList(I32), cs));
    }

    [Test]
    public void List_vs_ConstraintsWithPrimitiveAncestor_ReturnsNull() {
        // A constraint demanding a primitive ancestor cannot be satisfied by a composite.
        var cs = ConstraintsState.Of(anc: I32);
        Assert.IsNull(SolvingFunctions.GetMergedStateOrNull(StateCollection.OfList(I32), cs));
    }

    // ─── Section 6: LCA via GetLastCommonAncestorOrNull (regression-shield) ─

    [Test]
    public void Lca_List_SameElement_ReturnsList() {
        var lca = StateCollection.OfList(I32).GetLastCommonAncestorOrNull(StateCollection.OfList(I32));
        Assert.IsInstanceOf<StateCollection>(lca);
    }

    [Test]
    public void Lca_List_DifferentElement_ReturnsAny() {
        var lca = StateCollection.OfList(I32).GetLastCommonAncestorOrNull(StateCollection.OfList(Real));
        Assert.AreSame(Any, lca);
    }

    [Test]
    public void Lca_List_vs_MutableArray_ReturnsAny() {
        var lca = StateCollection.OfList(I32).GetLastCommonAncestorOrNull(StateCollection.OfMutableArray(I32));
        Assert.AreSame(Any, lca);
    }

    [Test]
    public void Lca_FixedArray_vs_MutableArray_ReturnsAny() {
        var lca = StateCollection.OfFixedArray(I32).GetLastCommonAncestorOrNull(StateCollection.OfMutableArray(I32));
        Assert.AreSame(Any, lca);
    }

    // ─── Section 7: Cycle guard (Stage 2.1a delivered) ─────────────────────
    //
    // Recursive composites stack-overflow in StateComposite.IsMutable /
    // IsSolved without explicit cycle guards. Stage 2.1a added per-property
    // VisitMark-based coinductive guards (IsMutableCycleMark = -58600,
    // IsSolvedCycleMark = -58700) following the Amadio-Cardelli '93
    // bisimulation pattern.

    [Test]
    public void CycleGuard_SelfReferentialList_MergeDoesNotStackOverflow() {
        var nodeA = TicNode.CreateInvisibleNode(ConstraintsState.Empty);
        nodeA.State = StateCollection.OfList(nodeA);

        var nodeB = TicNode.CreateInvisibleNode(ConstraintsState.Empty);
        nodeB.State = StateCollection.OfList(nodeB);

        Assert.DoesNotThrow(() => SolvingFunctions.GetMergedStateOrNull(nodeA.State, nodeB.State));
    }

    [Test]
    public void CycleGuard_SelfReferentialList_MergeReturnsNonNull() {
        var nodeA = TicNode.CreateInvisibleNode(ConstraintsState.Empty);
        nodeA.State = StateCollection.OfList(nodeA);

        var nodeB = TicNode.CreateInvisibleNode(ConstraintsState.Empty);
        nodeB.State = StateCollection.OfList(nodeB);

        var merged = SolvingFunctions.GetMergedStateOrNull(nodeA.State, nodeB.State);
        Assert.IsNotNull(merged);
        Assert.IsInstanceOf<StateCollection>(merged);
    }

    [Test]
    public void CycleGuard_SelfReferentialFixedArray_MergeDoesNotStackOverflow() {
        var nodeA = TicNode.CreateInvisibleNode(ConstraintsState.Empty);
        nodeA.State = StateCollection.OfFixedArray(nodeA);

        var nodeB = TicNode.CreateInvisibleNode(ConstraintsState.Empty);
        nodeB.State = StateCollection.OfFixedArray(nodeB);

        Assert.DoesNotThrow(() => SolvingFunctions.GetMergedStateOrNull(nodeA.State, nodeB.State));
    }

    [Test]
    public void CycleGuard_SelfReferentialMutableArray_MergeDoesNotStackOverflow() {
        var nodeA = TicNode.CreateInvisibleNode(ConstraintsState.Empty);
        nodeA.State = StateCollection.OfMutableArray(nodeA);

        var nodeB = TicNode.CreateInvisibleNode(ConstraintsState.Empty);
        nodeB.State = StateCollection.OfMutableArray(nodeB);

        Assert.DoesNotThrow(() => SolvingFunctions.GetMergedStateOrNull(nodeA.State, nodeB.State));
    }

    [Test]
    public void CycleGuard_MixedRecursion_ListContainingListContainingSelf() {
        var outer = TicNode.CreateInvisibleNode(ConstraintsState.Empty);
        var inner = TicNode.CreateInvisibleNode(StateCollection.OfList(outer));
        outer.State = StateCollection.OfList(inner);

        var outer2 = TicNode.CreateInvisibleNode(ConstraintsState.Empty);
        var inner2 = TicNode.CreateInvisibleNode(StateCollection.OfList(outer2));
        outer2.State = StateCollection.OfList(inner2);

        Assert.DoesNotThrow(() => SolvingFunctions.GetMergedStateOrNull(outer.State, outer2.State));
    }

    // ─── Section 8: Solver integration (GraphBuilder) ──────────────────────

    [Test]
    public void Solver_TwoVarsWithSameListType_SolvesWithoutError() {
        var graph = new GraphBuilder();
        graph.SetVarType("a", StateCollection.OfList(I32));
        graph.SetVarType("b", StateCollection.OfList(I32));
        Assert.DoesNotThrow(() => graph.Solve());
    }

    [Test]
    public void Solver_TwoVarsWithSameFixedArrayType_SolvesWithoutError() {
        var graph = new GraphBuilder();
        graph.SetVarType("a", StateCollection.OfFixedArray(Real));
        graph.SetVarType("b", StateCollection.OfFixedArray(Real));
        Assert.DoesNotThrow(() => graph.Solve());
    }

    [Test]
    public void Solver_TwoVarsWithSameMutableArrayType_SolvesWithoutError() {
        var graph = new GraphBuilder();
        graph.SetVarType("a", StateCollection.OfMutableArray(Bool));
        graph.SetVarType("b", StateCollection.OfMutableArray(Bool));
        Assert.DoesNotThrow(() => graph.Solve());
    }

    [Test]
    public void Solver_VarTypedAsList_FinalTypeIsList() {
        var graph = new GraphBuilder();
        graph.SetVarType("x", StateCollection.OfList(I32));
        var result = graph.Solve();
        result.AssertNamed(StateCollection.OfList(I32), "x");
    }

    [Test]
    public void Solver_VarTypedAsFixedArray_FinalTypeIsFixedArray() {
        var graph = new GraphBuilder();
        graph.SetVarType("x", StateCollection.OfFixedArray(I32));
        var result = graph.Solve();
        result.AssertNamed(StateCollection.OfFixedArray(I32), "x");
    }

    [Test]
    public void Solver_VarTypedAsMutableArray_FinalTypeIsMutableArray() {
        var graph = new GraphBuilder();
        graph.SetVarType("x", StateCollection.OfMutableArray(I32));
        var result = graph.Solve();
        result.AssertNamed(StateCollection.OfMutableArray(I32), "x");
    }

    [Test]
    public void Solver_NestedListAnnotation_FinalTypeIsNestedList() {
        var graph = new GraphBuilder();
        graph.SetVarType("x", StateCollection.OfList(StateCollection.OfList(I32)));
        var result = graph.Solve();
        result.AssertNamed(StateCollection.OfList(StateCollection.OfList(I32)), "x");
    }
}
