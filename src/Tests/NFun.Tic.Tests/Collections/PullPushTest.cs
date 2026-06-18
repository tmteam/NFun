using NFun.Tic.SolvingStates;
using NUnit.Framework;

namespace NFun.Tic.Tests.Collections;

using static StatePrimitive;

/// <summary>
/// Stage 2.1b acceptance tests for TIC constraint stages (Pull / Push /
/// Destruction / Finalize) over the unified <see cref="StateCollection"/>.
///
/// Each test drives a graph that puts a StateCollection in a position where
/// constraint propagation must traverse it. Acceptance gate: solve completes
/// without crash, with the expected output type at the destination node.
///
/// Sections:
///   1. Same-kind same-element propagation (success cases).
///   2. Same-kind different-element propagation (failure — TIC error).
///   3. Cross-kind propagation (failure — TIC error).
///   4. Composite vs legacy StateArray (must NOT cross-propagate).
///   5. Nested composites.
///   6. If-else LCA through composites.
/// </summary>
[TestFixture]
public class PullPushTest {

    // ─── Section 1: Same-kind same-element propagation ──────────────

    [Test(Description = "x:list<int>; y = x")]
    public void Pull_ListPropagatesThroughEquationDef() {
        var graph = new GraphBuilder();
        graph.SetVarType("x", StateCollection.OfList(I32));
        graph.SetVar("x", 0);
        graph.SetDef("y", 0);
        var result = graph.Solve();
        result.AssertNamed(StateCollection.OfList(I32), "x");
        result.AssertNamed(StateCollection.OfList(I32), "y");
    }

    [Test(Description = "x:fixedArray<real>; y = x")]
    public void Pull_FixedArrayPropagatesThroughEquationDef() {
        var graph = new GraphBuilder();
        graph.SetVarType("x", StateCollection.OfFixedArray(Real));
        graph.SetVar("x", 0);
        graph.SetDef("y", 0);
        var result = graph.Solve();
        result.AssertNamed(StateCollection.OfFixedArray(Real), "x");
        result.AssertNamed(StateCollection.OfFixedArray(Real), "y");
    }

    [Test(Description = "x:array<bool>; y = x")]
    public void Pull_MutableArrayPropagatesThroughEquationDef() {
        var graph = new GraphBuilder();
        graph.SetVarType("x", StateCollection.OfMutableArray(Bool));
        graph.SetVar("x", 0);
        graph.SetDef("y", 0);
        var result = graph.Solve();
        result.AssertNamed(StateCollection.OfMutableArray(Bool), "x");
        result.AssertNamed(StateCollection.OfMutableArray(Bool), "y");
    }

    [Test(Description = "x:list<int>; y:list<int> = x — both sides match")]
    public void Push_ListIntoTypedDef() {
        var graph = new GraphBuilder();
        graph.SetVarType("x", StateCollection.OfList(I32));
        graph.SetVarType("y", StateCollection.OfList(I32));
        graph.SetVar("x", 0);
        graph.SetDef("y", 0);
        var result = graph.Solve();
        result.AssertNamed(StateCollection.OfList(I32), "x");
        result.AssertNamed(StateCollection.OfList(I32), "y");
    }

    // ─── Section 2: Same-kind different-element — TIC error ────────

    [Test(Description = "x:list<int>; y:list<real> = x — invariant element mismatch")]
    public void Push_ListIntIntoListReal_Rejected() {
        var graph = new GraphBuilder();
        graph.SetVarType("x", StateCollection.OfList(I32));
        graph.SetVarType("y", StateCollection.OfList(Real));
        graph.SetVar("x", 0);
        TestHelper.AssertThrowsTicError(() => {
            graph.SetDef("y", 0);
            graph.Solve();
        });
    }

    [Test(Description = "x:fixedArray<int>; y:fixedArray<bool> = x")]
    public void Push_FixedArrayDifferentElement_Rejected() {
        var graph = new GraphBuilder();
        graph.SetVarType("x", StateCollection.OfFixedArray(I32));
        graph.SetVarType("y", StateCollection.OfFixedArray(Bool));
        graph.SetVar("x", 0);
        TestHelper.AssertThrowsTicError(() => {
            graph.SetDef("y", 0);
            graph.Solve();
        });
    }

    // ─── Section 3: Cross-kind — Stage C unified lattice ──────────────
    // Per the lattice (list ⊆ array ⊆ fixedArray) and the unified ee/lang
    // model (any collection is fixedArray semantically), cross-kind Push from
    // subtype into supertype is now ACCEPTED (was rejected before Stage C).

    [Test(Description = "x:list<int>; y:array<int> = x — list ⊆ array per lattice, accepted")]
    public void Push_ListIntoMutableArray_AcceptedViaSubtype() {
        var graph = new GraphBuilder();
        graph.SetVarType("x", StateCollection.OfList(I32));
        graph.SetVarType("y", StateCollection.OfMutableArray(I32));
        graph.SetVar("x", 0);
        graph.SetDef("y", 0);
        var result = graph.Solve();
        result.AssertNamed(StateCollection.OfList(I32), "x");
        result.AssertNamed(StateCollection.OfMutableArray(I32), "y");
    }

    [Test(Description = "x:list<int>; y:fixedArray<int> = x — list ⊆ fixedArray per lattice")]
    public void Push_ListIntoFixedArray_AcceptedViaSubtype() {
        var graph = new GraphBuilder();
        graph.SetVarType("x", StateCollection.OfList(I32));
        graph.SetVarType("y", StateCollection.OfFixedArray(I32));
        graph.SetVar("x", 0);
        graph.SetDef("y", 0);
        var result = graph.Solve();
        result.AssertNamed(StateCollection.OfList(I32), "x");
        result.AssertNamed(StateCollection.OfFixedArray(I32), "y");
    }

    [Test(Description = "x:fixedArray<int>; y:array<int> = x — fixedArray > array, rejected")]
    public void Push_FixedArrayIntoMutableArray_Rejected() {
        // fixedArray is supertype of array; assigning fixedArray to array slot
        // is downcast — still rejected (Push invariance on supertype → subtype).
        var graph = new GraphBuilder();
        graph.SetVarType("x", StateCollection.OfFixedArray(I32));
        graph.SetVarType("y", StateCollection.OfMutableArray(I32));
        graph.SetVar("x", 0);
        TestHelper.AssertThrowsTicError(() => {
            graph.SetDef("y", 0);
            graph.Solve();
        });
    }

    // ─── Section 4: Composite vs legacy StateArray ─────────────────

    [Test(Description = "x:list<int>; y:int[] = x — list flows into array per `List ≤ Array` hierarchy")]
    public void Push_ListIntoLegacyArray_Accepted() {
        // Stage 2.5 / LINQ migration: cross-family subtyping `list<T> ≤ T[]`
        // makes lang-mode list literals usable in ee-mode T[] argument slots
        // (existing LINQ generic functions key on T[]). Result variable resolves
        // to the array shape because that's its declared annotation.
        var graph = new GraphBuilder();
        graph.SetVarType("x", StateCollection.OfList(I32));
        graph.SetVarType("y", StateArray.Of(I32));
        graph.SetVar("x", 0);
        graph.SetDef("y", 0);
        var result = graph.Solve();
        result.AssertNamed(StateCollection.OfList(I32), "x");
        result.AssertNamedEqualToArrayOf(I32, "y");
    }

    [Test(Description = "x:int[]; y:list<int> = x — legacy ee T[] flows into lang list slot")]
    public void Push_LegacyArrayIntoList_AcceptedViaTransform() {
        // Stage C — under the unified model, ee-mode T[] IS fixedArray-shape; the
        // TransformToCollectionOrNull accepts StateArray descendants for any
        // Array-branch lang-mode kind (list/array/fixedArray). The result variable
        // resolves to the declared slot kind (list here).
        var graph = new GraphBuilder();
        graph.SetVarType("x", StateArray.Of(I32));
        graph.SetVarType("y", StateCollection.OfList(I32));
        graph.SetVar("x", 0);
        graph.SetDef("y", 0);
        var result = graph.Solve();
        result.AssertNamed(StateCollection.OfList(I32), "y");
    }

    [Test(Description = "REGRESSION SHIELD: x:int[]; y:int[] = x — legacy path unchanged")]
    public void Push_LegacyArrayIntoLegacyArray_StillWorks() {
        var graph = new GraphBuilder();
        graph.SetVarType("x", StateArray.Of(I32));
        graph.SetVarType("y", StateArray.Of(I32));
        graph.SetVar("x", 0);
        graph.SetDef("y", 0);
        var result = graph.Solve();
        result.AssertNamed(StateArray.Of(I32), "x");
        result.AssertNamed(StateArray.Of(I32), "y");
    }

    // ─── Section 5: Nested composites ──────────────────────────────

    [Test(Description = "x:list<list<int>>; y = x")]
    public void Pull_NestedList_Preserved() {
        var graph = new GraphBuilder();
        graph.SetVarType("x", StateCollection.OfList(StateCollection.OfList(I32)));
        graph.SetVar("x", 0);
        graph.SetDef("y", 0);
        var result = graph.Solve();
        result.AssertNamed(StateCollection.OfList(StateCollection.OfList(I32)), "y");
    }

    [Test(Description = "x:list<list<int>>; y:list<list<int>> = x")]
    public void Push_NestedList_Matches() {
        var graph = new GraphBuilder();
        graph.SetVarType("x", StateCollection.OfList(StateCollection.OfList(I32)));
        graph.SetVarType("y", StateCollection.OfList(StateCollection.OfList(I32)));
        graph.SetVar("x", 0);
        graph.SetDef("y", 0);
        var result = graph.Solve();
        result.AssertNamed(StateCollection.OfList(StateCollection.OfList(I32)), "y");
    }

    [Test(Description = "x:list<list<int>>; y:list<list<real>> = x — inner mismatch")]
    public void Push_NestedListInnerMismatch_Rejected() {
        var graph = new GraphBuilder();
        graph.SetVarType("x", StateCollection.OfList(StateCollection.OfList(I32)));
        graph.SetVarType("y", StateCollection.OfList(StateCollection.OfList(Real)));
        graph.SetVar("x", 0);
        TestHelper.AssertThrowsTicError(() => {
            graph.SetDef("y", 0);
            graph.Solve();
        });
    }

    // ─── Section 6: If-else LCA through composites ─────────────────

    [Test(Description = "a:list<int>; b:list<int>; y = if(c) a else b — preserves list<int>")]
    public void IfElse_SameLists_PreservesList() {
        var graph = new GraphBuilder();
        graph.SetVarType("a", StateCollection.OfList(I32));
        graph.SetVarType("b", StateCollection.OfList(I32));
        graph.SetVar("a", 0);
        graph.SetVar("b", 1);
        graph.SetIfElse(new[] { 2 }, new[] { 0, 1 }, 3);
        graph.SetDef("y", 3);
        var result = graph.Solve();
        result.AssertNamed(StateCollection.OfList(I32), "y");
    }

    [Test(Description = "a:list<int>; b:list<real>; y = if(c) a else b — invariant LCA collapses to Any")]
    public void IfElse_DifferentLists_CollapsesToAny() {
        var graph = new GraphBuilder();
        graph.SetVarType("a", StateCollection.OfList(I32));
        graph.SetVarType("b", StateCollection.OfList(Real));
        graph.SetVar("a", 0);
        graph.SetVar("b", 1);
        graph.SetIfElse(new[] { 2 }, new[] { 0, 1 }, 3);
        graph.SetDef("y", 3);
        var result = graph.Solve();
        result.AssertNamed(Any, "y");
    }

    [Test(Description = "a:list<int>; b:array<int>; y = if(c) a else b — cross-kind LCA widens per lattice (Stage 2 Liskov)")]
    public void IfElse_ListAndMutableArray_WidensToArray() {
        // Bug hunt round 6 #32: cross-Constructor LCA within Array-branch widens
        // per ConstructorLattice. Mirrors Stage 2 Liskov decision pinned by
        // `Ambiguity_ListPassedWhereArrayExpected_Accepted`.
        var graph = new GraphBuilder();
        graph.SetVarType("a", StateCollection.OfList(I32));
        graph.SetVarType("b", StateCollection.OfMutableArray(I32));
        graph.SetVar("a", 0);
        graph.SetVar("b", 1);
        graph.SetIfElse(new[] { 2 }, new[] { 0, 1 }, 3);
        graph.SetDef("y", 3);
        var result = graph.Solve();
        result.AssertNamed(StateCollection.OfMutableArray(I32), "y");
    }

    [Test(Description = "REGRESSION SHIELD: legacy array covariance still widens")]
    public void IfElse_LegacyArrayCovariance_StillWidens() {
        var graph = new GraphBuilder();
        graph.SetVarType("a", StateArray.Of(I32));
        graph.SetVarType("b", StateArray.Of(Real));
        graph.SetVar("a", 0);
        graph.SetVar("b", 1);
        graph.SetIfElse(new[] { 2 }, new[] { 0, 1 }, 3);
        graph.SetDef("y", 3);
        var result = graph.Solve();
        result.AssertNamedEqualToArrayOf(Real, "y");
    }
}
