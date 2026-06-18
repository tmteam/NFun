using NFun.Tic.SolvingStates;
using NUnit.Framework;

namespace NFun.Tic.Tests.Collections;

using static StatePrimitive;

/// <summary>
/// Smoke tests for the Stage 1 data-driven <see cref="StateCollection"/>.
///
/// Pin the basic contract: constructor identity, variance, factory, LCA,
/// equality. Stage 2 must not silently break these.
/// </summary>
[TestFixture]
public class StateCompositeSmokeTest {

    // ─── ConstructorKind discriminator ──────────────────────────────

    [Test]
    public void StateCollection_OfList_ReportsListConstructor() {
        Assert.AreEqual(ConstructorKind.List, StateCollection.OfList(I32).Constructor);
    }

    [Test]
    public void StateCollection_OfFixedArray_ReportsFixedArrayConstructor() {
        Assert.AreEqual(ConstructorKind.FixedArray, StateCollection.OfFixedArray(I32).Constructor);
    }

    [Test]
    public void StateCollection_OfMutableArray_ReportsArrayConstructor() {
        Assert.AreEqual(ConstructorKind.Array, StateCollection.OfMutableArray(I32).Constructor);
    }

    [Test]
    public void StateCollection_OfSet_ReportsSetConstructor() {
        Assert.AreEqual(ConstructorKind.Set, StateCollection.OfSet(I32).Constructor);
    }

    // ─── Single invariant argument ──────────────────────────────────

    [Test]
    public void StateCollection_List_HasSingleInvariantArgument() {
        var s = StateCollection.OfList(I32);
        Assert.AreEqual(1, s.Arguments.Length);
        Assert.AreEqual(Variance.Invariant, s.Arguments[0].Variance);
    }

    [Test]
    public void StateCollection_MutableArray_HasSingleInvariantArgument() {
        var s = StateCollection.OfMutableArray(I32);
        Assert.AreEqual(1, s.Arguments.Length);
        Assert.AreEqual(Variance.Invariant, s.Arguments[0].Variance);
    }

    // ─── Equality ───────────────────────────────────────────────────

    [Test]
    public void StateCollection_Equals_SameKindAndElement_True() {
        Assert.AreEqual(StateCollection.OfList(I32), StateCollection.OfList(I32));
    }

    [Test]
    public void StateCollection_Equals_DifferentElement_False() {
        Assert.AreNotEqual(StateCollection.OfList(I32), StateCollection.OfList(Real));
    }

    [Test]
    public void StateCollection_Equals_DifferentKind_False() {
        // list<I32> != fixedArray<I32> (Constructor is part of identity).
        Assert.AreNotEqual(StateCollection.OfList(I32), StateCollection.OfFixedArray(I32));
    }

    // ─── LCA ────────────────────────────────────────────────────────

    [Test]
    public void StateCollection_List_Lca_SameElement_ReturnsList() {
        var lca = StateCollection.OfList(I32).GetLastCommonAncestorOrNull(StateCollection.OfList(I32));
        Assert.IsInstanceOf<StateCollection>(lca);
        Assert.AreEqual(ConstructorKind.List, ((StateCollection)lca!).Constructor);
    }

    [Test]
    public void StateCollection_List_Lca_DifferentElement_CollapsesToAny() {
        var lca = StateCollection.OfList(I32).GetLastCommonAncestorOrNull(StateCollection.OfList(Real));
        Assert.AreSame(Any, lca);
    }

    [Test]
    public void StateCollection_Lca_DifferentKind_SameElement_WidensPerLattice() {
        // Cross-Constructor (Array-branch) LCA widens to lattice join. Mirrors
        // Stage 2 Liskov decision pinned by
        // `Ambiguity_ListPassedWhereArrayExpected_Accepted`. Bug hunt round 6 #32.
        var lca = StateCollection.OfList(I32).GetLastCommonAncestorOrNull(StateCollection.OfFixedArray(I32));
        Assert.IsInstanceOf<StateCollection>(lca);
        Assert.AreEqual(ConstructorKind.FixedArray, ((StateCollection)lca!).Constructor);
    }

    [Test]
    public void StateCollection_FixedArray_Lca_SameElement_ReturnsFixedArray() {
        var lca = StateCollection.OfFixedArray(I32).GetLastCommonAncestorOrNull(StateCollection.OfFixedArray(I32));
        Assert.IsInstanceOf<StateCollection>(lca);
        Assert.AreEqual(ConstructorKind.FixedArray, ((StateCollection)lca!).Constructor);
    }

    [Test]
    public void StateCollection_MutableArray_Lca_SameElement_ReturnsMutableArray() {
        var lca = StateCollection.OfMutableArray(I32).GetLastCommonAncestorOrNull(StateCollection.OfMutableArray(I32));
        Assert.IsInstanceOf<StateCollection>(lca);
        Assert.AreEqual(ConstructorKind.Array, ((StateCollection)lca!).Constructor);
    }

    // ─── IsSolved / IsMutable ───────────────────────────────────────

    [Test]
    public void StateCollection_OfSolvedPrimitive_IsSolved() {
        Assert.IsTrue(StateCollection.OfList(I32).IsSolved);
    }

    [Test]
    public void StateCollection_OfSolvedPrimitive_IsNotMutable() {
        Assert.IsFalse(StateCollection.OfList(I32).IsMutable);
    }

    [Test]
    public void StateCollection_OfConstraintsState_IsMutable() {
        // Constraints state is mutable; the wrapping composite inherits.
        Assert.IsTrue(StateCollection.OfList(ConstraintsState.Empty).IsMutable);
    }

    // ─── GraphBuilder integration ──────────────────────────────────
    //
    // Acceptance gate for Stage 1: the state class must be addressable through
    // the regular GraphBuilder API. Stage 2 will rely on this when wiring the
    // parser-emitted StateCollection through the TIC solver.

    [Test]
    public void GraphBuilder_AcceptsListVarType_NoCrash() {
        var graph = new GraphBuilder();
        Assert.DoesNotThrow(() => graph.SetVarType("x", StateCollection.OfList(I32)));
    }

    [Test]
    public void GraphBuilder_AcceptsFixedArrayVarType_NoCrash() {
        var graph = new GraphBuilder();
        Assert.DoesNotThrow(() => graph.SetVarType("x", StateCollection.OfFixedArray(I32)));
    }

    [Test]
    public void GraphBuilder_AcceptsMutableArrayVarType_NoCrash() {
        var graph = new GraphBuilder();
        Assert.DoesNotThrow(() => graph.SetVarType("x", StateCollection.OfMutableArray(I32)));
    }

    [Test]
    public void GraphBuilder_StateCollectionNode_ParticipatesInSolve_NoCrash() {
        var graph = new GraphBuilder();
        graph.SetVarType("x", StateCollection.OfList(I32));
        Assert.DoesNotThrow(() => graph.Solve());
    }
}
