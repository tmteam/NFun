using NFun.Tic.Algebra;
using NFun.Tic.SolvingStates;
using NUnit.Framework;

namespace NFun.Tic.Tests;

using static StatePrimitive;

/// <summary>
/// Bug #46 — TIC-level isolation (FIXED).
///
/// Syntax repro (previously rejected):
///     y = [1,2]
///     out = if(true) {x=y} else {x=[3]}
///   Was: Parse error FU761. Now: out:{x:list&lt;Int32&gt;} = { x = [1,2] }
///
/// Root cause: <see cref="SolvingFunctions.GetMergedStateOrNull"/> had an
/// explicit (<see cref="StateStruct"/>, CS{Desc=<see cref="StateStruct"/>}) cell
/// but no analogue for <see cref="StateCollection"/>. After the
/// `case ConstraintsState:` swap, the recursion landed on
/// (StateCollection, CS) with no matching case → null → MergeInplace threw
/// CannotMerge at <c>PushConstraintsFunctions.cs:175-176</c>.
///
/// Fix mirrors the existing StateStruct + CS precedent for StateCollection.
/// LcaStructFields' UnifyOrNull(CS, SC) short-circuit is left intact — the
/// identity merge happens downstream at Push via GetMergedStateOrNull instead.
/// StateFun + CS analogue overreaches and breaks `list(rule …, rule …)`
/// literal-of-lambdas shape — left for separate work (see
/// Bug46_StructFieldLcaTests.Probe5 in the syntax suite).
/// </summary>
public class Bug46_StructFieldLca_TicTests {

    // ------------------------------------------------------------------
    // T1 — Algebraic root (FIXED): GetMergedStateOrNull(CS{Desc=list(V0)}, list(V1))
    //   returns a non-null merged StateCollection via the new
    //   (StateCollection, ConstraintsState) cell in the switch.
    // ------------------------------------------------------------------
    [Test]
    public void T1_GetMergedStateOrNull_CsDescIsList_AndStateCollection_WithUnresolvedElements_MergesCleanly() {
        // Shape that the failing Push cell used to receive:
        //     descField.State = CS{Desc=list(V0)}, V0 unresolved
        //     ancField.Value.State = list(V1),     V1 unresolved
        // Pre-fix: returned null → MergeInplace threw CannotMerge.
        // Post-fix: the new (StateCollection, CS) cell recurses into the
        // existing (StateCollection, StateCollection) case, returning the
        // merged StateCollection (and merging V0/V1 via MergeCollectionsWithCycleGuard).
        var v0 = TicNode.CreateInvisibleNode(ConstraintsState.Empty);
        var v1 = TicNode.CreateInvisibleNode(ConstraintsState.Empty);
        var lhsCs = ConstraintsState.Of(desc: StateCollection.OfList(v0));
        var rhs = StateCollection.OfList(v1);

        var merged = SolvingFunctions.GetMergedStateOrNull(lhsCs, rhs);

        Assert.IsNotNull(merged,
            "Bug #46 fix: GetMergedStateOrNull(CS{Desc=list(V0)}, list(V1)) must "
            + "produce a merged list, not return null.");
        Assert.IsInstanceOf<StateCollection>(merged,
            "Merged result should be a StateCollection.");
    }

    // ------------------------------------------------------------------
    // T2 — UnifyOrNull short-circuit is INTENTIONALLY preserved.
    //   The fix lives at Push-time (GetMergedStateOrNull, T1), not Pull-time.
    //   This test pins the LCA-time behavior so a future "tighten Unify"
    //   refactor doesn't silently start over-merging.
    // ------------------------------------------------------------------
    [Test]
    public void T2_UnifyOrNull_CsDescIsList_ReturnsRhs_WithoutFusingElementIdentities() {
        var v0 = TicNode.CreateInvisibleNode(ConstraintsState.Empty);
        var v1 = TicNode.CreateInvisibleNode(ConstraintsState.Empty);

        var lhsCs = ConstraintsState.Of(desc: StateCollection.OfList(v0));
        var rhs = StateCollection.OfList(v1);

        var result = lhsCs.UnifyOrNull(rhs);

        Assert.IsNotNull(result, "Unify returns rhs (FitsInto short-circuit).");
        // rhs is returned verbatim because `rhs.FitsInto(lhsCs)` is true
        // (StateExtensions.Unify.cs:30-38). Element nodes V0 and V1 stay
        // identity-distinct at LCA time — the merge happens downstream at
        // Push via GetMergedStateOrNull (covered by T1, T4).
        Assert.IsFalse(
            object.ReferenceEquals(v0.GetNonReference(), v1.GetNonReference()),
            "Bug #46 design: UnifyOrNull does NOT merge element identities. "
            + "The merge is deferred to Push-time GetMergedStateOrNull.");
    }

    // ------------------------------------------------------------------
    // T3 — LcaStructFields between two mutable structs.
    //   Element identities stay distinct at LCA time (Pull-time merge would be
    //   the alternative Layer-0 fix, deliberately not chosen). The deferred
    //   merge runs at Push via T1's GetMergedStateOrNull cell.
    // ------------------------------------------------------------------
    [Test]
    public void T3_LcaStructFields_MutStructWithCsListField_AndMutStructWithListField_DefersElementMergeToPush() {
        var v0 = TicNode.CreateInvisibleNode(ConstraintsState.Empty);
        var v1 = TicNode.CreateInvisibleNode(ConstraintsState.Empty);

        var leftFieldNode = TicNode.CreateInvisibleNode(
            ConstraintsState.Of(desc: StateCollection.OfList(v0)));
        var rightFieldNode = TicNode.CreateInvisibleNode(StateCollection.OfList(v1));

        var leftFields = new System.Collections.Generic.Dictionary<string, TicNode>(
            System.StringComparer.OrdinalIgnoreCase) { { "x", leftFieldNode } };
        var rightFields = new System.Collections.Generic.Dictionary<string, TicNode>(
            System.StringComparer.OrdinalIgnoreCase) { { "x", rightFieldNode } };
        var left = new StateMutableStruct(leftFields, isFrozen: false);
        var right = new StateMutableStruct(rightFields, isFrozen: false);

        ITicNodeState lca = left.Lca(right);

        Assert.IsNotNull(lca);
        Assert.IsInstanceOf<StateStruct>(lca);

        var lcaStruct = (StateStruct)lca;
        var lcaXField = lcaStruct.GetFieldOrNull("x");
        Assert.IsNotNull(lcaXField);

        Assert.IsFalse(
            object.ReferenceEquals(v0.GetNonReference(), v1.GetNonReference()),
            "By design: LcaStructFields leaves element identities distinct. "
            + "Element merge is deferred to Push-time GetMergedStateOrNull "
            + "(see T1, T4).");
    }

    // ------------------------------------------------------------------
    // T4 — FULL GRAPH (FIXED): the algebraic shape of bug #46.
    //   y = [1,2]
    //   out = if(true) mut{x=y} else mut{x=[3]}
    // ------------------------------------------------------------------
    [Test]
    public void T4_IfElseOfTwoMutableStructsWithListField_SolvesCleanly() {
        var graph = new GraphBuilder();

        // y = [1, 2]
        graph.SetIntConst(0, U8);
        graph.SetIntConst(1, U8);
        graph.SetSoftListInit(2, 0, 1);
        graph.SetDef("y", 2);

        // leftStruct = mut{ x = y }
        graph.SetVar("y", 10);
        graph.SetMutableStructInit(new[] { "x" }, new[] { 10 }, 11);

        // [3]
        graph.SetIntConst(20, U8);
        graph.SetSoftListInit(21, 20);

        // rightStruct = mut{ x = [3] }
        graph.SetMutableStructInit(new[] { "x" }, new[] { 21 }, 22);

        // out = if(cond) leftStruct else rightStruct
        graph.SetConst(30, Bool);
        graph.SetIfElse(new[] { 30 }, new[] { 11, 22 }, 31);
        graph.SetDef("out", 31);

        // Pre-fix: TicException (CannotMerge inside PushConstraintsFunctions.cs:175-176).
        // Post-fix: Solve succeeds; 'out' resolves to a struct with a list field.
        Assert.DoesNotThrow(() => graph.Solve());
    }

    // ------------------------------------------------------------------
    // T5 — CONTROL: same shape WITHOUT the struct wrap. Already works because
    //   the Bug-hunt round 6 #32 fix added LcaOrShareIdentity / MergeInplace
    //   on StateCollection. This makes T5 the discriminator: only the
    //   mutable-struct-field aggregation path is broken; bare list LCA is fine.
    // ------------------------------------------------------------------
    [Test]
    public void T5_CONTROL_IfElseOfTwoListsDirectly_SolvesCleanly() {
        var graph = new GraphBuilder();

        // y = [1, 2]
        graph.SetIntConst(0, U8);
        graph.SetIntConst(1, U8);
        graph.SetSoftListInit(2, 0, 1);
        graph.SetDef("y", 2);

        // [3]  (right branch literal)
        graph.SetIntConst(10, U8);
        graph.SetSoftListInit(11, 10);

        // out = if(cond) y else [3]   — no struct wrap
        graph.SetVar("y", 12);
        graph.SetConst(20, Bool);
        graph.SetIfElse(new[] { 20 }, new[] { 12, 11 }, 21);
        graph.SetDef("out", 21);

        // CONTROL: this already passes today via StateCollection.LcaOrShareIdentity.
        var result = graph.Solve();
        var outState = result.GetVariableNode("out").GetNonReference().State;
        Assert.IsInstanceOf<StateCollection>(outState,
            "Bare LCA(list, list) works today — only the struct-field LCA path "
            + "(see T3, T4) is broken.");
    }
}
