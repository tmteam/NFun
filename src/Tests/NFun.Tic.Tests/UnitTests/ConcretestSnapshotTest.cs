namespace NFun.Tic.Tests.UnitTests;

using System.Collections.Generic;
using NUnit.Framework;
using NFun.Tic.Algebra;
using SolvingStates;
using static SolvingStates.StatePrimitive;

/// <summary>
/// Contract tests for ↓ₛ (ConcretestSnapshot) — the Destruction-snapshot operator of the
/// RESOLUTION layer, extracted from the pure projection ↓ by debt #19.
///
/// Contract:
///  * on Preferred-free states ↓ₛ ≡ ↓ (delegation law);
///  * ↓ₛ PRESERVES Preferred where ↓ drops it — the two extracted arms:
///    (1) optional CS with fitting hint resolves to opt(Preferred),
///    (2) array elements keep the CS[D, pref] hint carrier;
///  * ↓ₛ is idempotent and produces Destruction-compatible (canonical, Rule B) states.
///
/// Companion pins: the PURE ↓ no longer contains either arm.
/// </summary>
public class ConcretestSnapshotTest {

    private static ConstraintsState Cs(
        ITicNodeState desc = null, StatePrimitive anc = null,
        bool cmp = false, bool opt = false, StatePrimitive preferred = null) {
        var cs = ConstraintsState.Of(desc, anc, cmp, opt);
        cs.Preferred = preferred;
        return cs;
    }

    private static string Print(ITicNodeState s) => s == null ? "<null>" : s.StateDescription;

    // Preferred-free sample of T ∪ C — on this fragment ↓ₛ must coincide with ↓.
    private static IEnumerable<ITicNodeState> HintFreeZoo() {
        yield return U8;
        yield return Real;
        yield return Any;
        yield return None;
        yield return Cs();
        yield return Cs(desc: U8);
        yield return Cs(anc: Real);
        yield return Cs(desc: U8, anc: Real);
        yield return Cs(desc: U8, anc: Real, cmp: true);
        yield return Cs(cmp: true);
        yield return Cs(opt: true);
        yield return Cs(desc: U8, opt: true);
        yield return Cs(desc: Bool, opt: true);
        yield return Cs(desc: Any, opt: true);
        yield return Cs(desc: StateArray.Of(Cs(desc: U8)), opt: true);
        yield return StateArray.Of(I32);
        yield return StateArray.Of(Cs(desc: U8, anc: Real));
        yield return StateArray.Of(StateArray.Of(Cs()));
        yield return StateStruct.Of("a", I32);
        yield return StateStruct.Of("a", Cs(desc: I16, anc: I64));
        yield return StateFun.Of(new ITicNodeState[] { Cs(anc: Real) }, Cs(desc: U8));
        yield return StateOptional.Of(I32);
        yield return StateOptional.Of(Cs(desc: U8, anc: Real));
        yield return new StateRefTo(TicNode.CreateInvisibleNode(Cs(desc: U8, anc: Real)));
    }

    // States exercising the two resolution arms (hints present).
    private static IEnumerable<ITicNodeState> HintedZoo() {
        yield return Cs(desc: U8, opt: true, preferred: I32);
        yield return Cs(desc: U8, anc: Real, opt: true, preferred: I32);
        yield return Cs(desc: Char, opt: true, preferred: I32); // hint does not fit
        yield return Cs(desc: U8, anc: Real, preferred: I32);   // non-optional: hint dropped by both
        yield return StateArray.Of(Cs(desc: U8, anc: Real, preferred: I32));
        yield return StateArray.Of(Cs(desc: U8, preferred: I32, opt: true));
        yield return Cs(desc: StateArray.Of(Cs(desc: U8, preferred: I32)));
        yield return StateOptional.Of(Cs(desc: U8, preferred: I32));
    }

    // ================================================================
    // Delegation law: ↓ₛ ≡ ↓ on Preferred-free states
    // ================================================================

    [Test]
    public void Snapshot_EqualsPureProjection_OnHintFreeStates() {
        foreach (var s in HintFreeZoo())
            Assert.AreEqual(Print(s.Concretest()), Print(s.ConcretestSnapshot()),
                $"↓ₛ must equal ↓ on the hint-free state {Print(s)}");
    }

    // ================================================================
    // Arm 1: optional CS with fitting Preferred → opt(Preferred)
    // ================================================================

    [Test]
    public void Snapshot_OptionalCsWithFittingPreferred_ResolvesToOptPreferred() {
        var cs = Cs(desc: U8, opt: true, preferred: I32); // [U8..]?I32!
        Assert.AreEqual(StateOptional.Of(I32), ((ITicNodeState)cs).ConcretestSnapshot());
    }

    [Test]
    public void Snapshot_OptionalCsWithNonFittingPreferred_FallsBackToLowerBound() {
        var cs = Cs(desc: Char, opt: true, preferred: I32); // hint incompatible — no choice
        Assert.AreEqual(StateOptional.Of(Char), ((ITicNodeState)cs).ConcretestSnapshot());
    }

    [Test]
    public void Concretest_OptionalCsWithPreferred_IgnoresHint_PurePin() {
        // PURE ↓ pin: the projection materializes opt(↓D), never opt(Preferred).
        var cs = Cs(desc: U8, opt: true, preferred: I32);
        Assert.AreEqual(StateOptional.Of(U8), ((ITicNodeState)cs).Concretest());
    }

    // ================================================================
    // Arm 2: array element keeps the CS[D, pref] hint carrier
    // ================================================================

    [Test]
    public void Snapshot_ArrayElementCsWithPreferred_KeepsHintCarrier() {
        var arr = StateArray.Of(Cs(desc: U8, anc: Real, preferred: I32));
        var r = arr.ConcretestSnapshot();
        Assert.IsInstanceOf<StateArray>(r);
        var elem = ((StateArray)r).Element;
        Assert.IsInstanceOf<ConstraintsState>(elem,
            $"element must stay a hint-carrying CS, got {Print(elem)}");
        var cs = (ConstraintsState)elem;
        Assert.AreEqual(U8, cs.Descendant, "lower bound survives the snapshot");
        Assert.IsFalse(cs.HasAncestor, "ancestor is dropped by the snapshot");
        Assert.AreEqual(I32, cs.Preferred, "hint survives the Destruction snapshot");
    }

    [Test]
    public void Concretest_ArrayElementCsWithPreferred_ProjectsToBareElement_PurePin() {
        // PURE ↓ pin: ↓(A[]) = (↓A)[] — no Preferred-carrying CS for array elements.
        var arr = StateArray.Of(Cs(desc: U8, anc: Real, preferred: I32));
        var r = arr.Concretest();
        Assert.IsInstanceOf<StateArray>(r);
        Assert.AreEqual(U8, ((StateArray)r).Element,
            "pure ↓ must collapse the element to its lower bound, hint-free");
    }

    // ================================================================
    // Snapshot laws: idempotence + Destruction compatibility (Rule B kept)
    // ================================================================

    [Test]
    public void Snapshot_Idempotent() {
        foreach (var s in HintFreeZoo())
        {
            var once = s.ConcretestSnapshot();
            Assert.AreEqual(Print(once), Print(once.ConcretestSnapshot()),
                $"↓ₛ↓ₛ ≠ ↓ₛ for {Print(s)}");
        }
        foreach (var s in HintedZoo())
        {
            var once = s.ConcretestSnapshot();
            Assert.AreEqual(Print(once), Print(once.ConcretestSnapshot()),
                $"↓ₛ↓ₛ ≠ ↓ₛ for {Print(s)}");
        }
    }

    [Test]
    public void Snapshot_KeepsUnsolvedOptionalInFlagForm_RuleB() {
        // ↓ₛ obeys the same canonical form as ↓: no dead opt(fresh-CS) islands.
        var r = ((ITicNodeState)Cs(opt: true)).ConcretestSnapshot();
        Assert.IsInstanceOf<ConstraintsState>(r, "opt(⊥) is not canonical — must stay [..]?");
        Assert.IsTrue(((ConstraintsState)r).IsOptional);

        var arrR = StateArray.Of(Cs(opt: true)).ConcretestSnapshot();
        var elem = ((StateArray)arrR).Element;
        Assert.IsInstanceOf<ConstraintsState>(elem, $"element must stay flag form, got {Print(elem)}");
        Assert.IsTrue(((ConstraintsState)elem).IsOptional);
    }

    [Test]
    public void Snapshot_FitsBackIntoSourceCs() {
        // Resolution never leaves the computed set: interval part of ↓ₛCS Fit CS.
        foreach (var s in HintFreeZoo())
        {
            if (s is not ConstraintsState cs) continue;
            var down = s.ConcretestSnapshot();
            Assert.IsTrue(down.FitsInto(cs),
                $"↓ₛCS Fit CS violated: ↓ₛ{Print(s)} = {Print(down)} does not fit back");
        }
        foreach (var s in HintedZoo())
        {
            if (s is not ConstraintsState cs) continue;
            var down = s.ConcretestSnapshot();
            Assert.IsTrue(down.FitsInto(cs),
                $"↓ₛCS Fit CS violated: ↓ₛ{Print(s)} = {Print(down)} does not fit back");
        }
    }
}
