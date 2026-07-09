namespace NFun.Tic.Tests.UnitTests;

using NUnit.Framework;
using NFun.Tic.Algebra;
using SolvingStates;
using static SolvingStates.StatePrimitive;

/// <summary>
/// Rule B — canonical Optional form:
///   In any stored state, opt(τ) implies τ is solved; the Optional lift of an
///   unsolved constraint [D..A] is the flag form [D..A]?, and every operation
///   (join, snapshot, transfer) preserves this canonical form.
///
/// Violation history: Concretest(CS[..]?) fabricated opt(fresh-empty-CS) — a dead
/// invisible island no edge can refine — which split the Optional axis into two
/// representations mid-solve (flag vs constructor) and killed nested none-joins
/// (`if(true) [[1]] else [[none]]` → FU710, StaleSnapshotSyntaxTests).
/// </summary>
public class CanonicalOptionalFormTest {

    #region Concretest keeps unsolved Optional in flag form

    [Test]
    public void Concretest_EmptyOptionalCs_StaysFlagForm() {
        var cs = ConstraintsState.Of(isOptional: true); // [..]?
        var r = cs.Concretest();
        Assert.IsInstanceOf<ConstraintsState>(r, "opt(⊥) is not canonical — must stay [..]?");
        Assert.IsTrue(((ConstraintsState)r).IsOptional);
    }

    [Test]
    public void Concretest_OptionalCsWithPreferred_MaterializesLowerBoundAndDropsHint() {
        // Pure ↓ (debt #19 closed): the projection never CHOOSES by Preferred.
        // ↓[U8..]?I32! = opt(↓U8) = opt(U8); the hint dies with the solved result
        // (Preferred lives only on CS — same rule as LCA's collapse-quenches-hint).
        // The resolution-flavored opt(Preferred) arm lives in ConcretestSnapshot —
        // see ConcretestSnapshotTest.Snapshot_OptionalCsWithFittingPreferred.
        var cs = ConstraintsState.Of(U8, isOptional: true); // [U8..]? I32!
        cs.Preferred = I32;
        var r = cs.Concretest();
        Assert.AreEqual(StateOptional.Of(U8), r);
    }

    [Test]
    public void Concretest_SolvedOptionalCs_MaterializesOpt() {
        // Ground case unchanged: [Bool..]? with solved non-numeric bottom → opt(Bool).
        var cs = ConstraintsState.Of(Bool, isOptional: true);
        var r = cs.Concretest();
        Assert.AreEqual(StateOptional.Of(Bool), r);
    }

    [Test]
    public void Concretest_OptionalAny_CollapsesToAny() {
        // opt(Any) = Any law is untouched.
        var cs = ConstraintsState.Of(Any, isOptional: true);
        Assert.AreEqual(Any, cs.Concretest());
    }

    [Test]
    public void Concretest_ArrayOfEmptyOptionalCs_NoDeadOptIsland() {
        // arr([..]?) — the exact shape of `[none]` inside `[[none]]` at snapshot time.
        var elem = ConstraintsState.Of(isOptional: true);
        var arr = StateArray.Of(elem);
        var r = arr.Concretest();
        Assert.IsInstanceOf<StateArray>(r);
        var relem = ((StateArray)r).Element;
        Assert.IsInstanceOf<ConstraintsState>(relem,
            $"element must stay in flag form, got {relem}");
        Assert.IsTrue(((ConstraintsState)relem).IsOptional);
    }

    #endregion

    #region Joins stay in the interval arm at any depth

    [Test]
    public void Lca_ArrayIntPref_vs_ArrayEmptyOptional_KeepsIntervalAndHint() {
        // Lca(arr([U8..]I32!), arr([..]?)) = arr([U8..]?I32!)
        var a = ConstraintsState.Of(U8);
        a.Preferred = I32;
        var b = ConstraintsState.Of(isOptional: true);
        var r = StateArray.Of(a).Lca(StateArray.Of(b));
        Assert.IsInstanceOf<StateArray>(r, $"got {r}");
        var elem = ((StateArray)r).ElementNode.GetNonReference().State;
        Assert.IsInstanceOf<ConstraintsState>(elem, $"element must be interval form, got {elem}");
        var cs = (ConstraintsState)elem;
        Assert.IsTrue(cs.IsOptional, "Optional axis joins as the flag");
        Assert.AreEqual(U8, cs.Descendant, "interval lower bound survives the join");
        Assert.AreEqual(I32, cs.Preferred, "hint survives the join");
    }

    [Test]
    public void Lca_NestedArrays_ReducesToFlatByInduction() {
        // Lca(arr(arr([U8..]I32!)), arr(arr([..]?))) = arr(arr([U8..]?I32!))
        var a = ConstraintsState.Of(U8);
        a.Preferred = I32;
        var b = ConstraintsState.Of(isOptional: true);
        var r = StateArray.Of(StateArray.Of(a)).Lca(StateArray.Of(StateArray.Of(b)));
        Assert.IsInstanceOf<StateArray>(r, $"got {r}");
        var mid = ((StateArray)r).ElementNode.GetNonReference().State;
        Assert.IsInstanceOf<StateArray>(mid, $"middle layer must stay array, got {mid}");
        var elem = ((StateArray)mid).ElementNode.GetNonReference().State;
        Assert.IsInstanceOf<ConstraintsState>(elem, $"inner element must be interval form, got {elem}");
        var cs = (ConstraintsState)elem;
        Assert.IsTrue(cs.IsOptional);
        Assert.AreEqual(U8, cs.Descendant);
        Assert.AreEqual(I32, cs.Preferred);
    }

    #endregion
}
