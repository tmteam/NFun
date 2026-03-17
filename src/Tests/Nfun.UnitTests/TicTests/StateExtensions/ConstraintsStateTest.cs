namespace NFun.UnitTests.TicTests.StateExtensions;

using NUnit.Framework;
using Tic.SolvingStates;
using static SolvingStates;
using static Tic.SolvingStates.StatePrimitive;

public class ConstraintsStateTest {

    private static ConstraintsState C(ITicNodeState desc = null, StatePrimitive anc = null,
        bool isComparable = false, StatePrimitive preferred = null) {
        var c = ConstraintsState.Of(desc, anc, isComparable);
        c.Preferred = preferred;
        return c;
    }

    [Test]
    public void IntersectIntervalsOrNull_BothEmpty() =>
        Assert.AreEqual(C(), C().IntersectIntervalsOrNull(C()));

    [Test]
    public void IntersectIntervalsOrNull_LeftDescOnly() =>
        Assert.AreEqual(C(desc: U8), C(desc: U8).IntersectIntervalsOrNull(C()));

    [Test]
    public void IntersectIntervalsOrNull_RightDescOnly() =>
        Assert.AreEqual(C(desc: I32), C().IntersectIntervalsOrNull(C(desc: I32)));

    [Test]
    public void IntersectIntervalsOrNull_LeftAncOnly() =>
        Assert.AreEqual(C(anc: Real), C(anc: Real).IntersectIntervalsOrNull(C()));

    [Test]
    public void IntersectIntervalsOrNull_RightAncOnly() =>
        Assert.AreEqual(C(anc: I64), C().IntersectIntervalsOrNull(C(anc: I64)));

    [Test]
    public void IntersectIntervalsOrNull_MergeDesc_Lca() =>
        Assert.AreEqual(C(desc: I16), C(desc: U8).IntersectIntervalsOrNull(C(desc: I16)));

    [Test]
    public void IntersectIntervalsOrNull_MergeAnc_Gcd() =>
        Assert.AreEqual(C(anc: I64), C(anc: Real).IntersectIntervalsOrNull(C(anc: I64)));

    [Test]
    public void IntersectIntervalsOrNull_FullIntervals() =>
        Assert.AreEqual(
            C(desc: I16, anc: I64),
            C(desc: U8, anc: Real).IntersectIntervalsOrNull(C(desc: I16, anc: I64)));

    [Test]
    public void IntersectIntervalsOrNull_IncompatibleAnc_ReturnsNull() =>
        Assert.IsNull(C(anc: Bool).IntersectIntervalsOrNull(C(anc: I32)));

    [Test]
    public void IntersectIntervalsOrNull_ComparableJoin() =>
        Assert.AreEqual(
            C(isComparable: true),
            C(isComparable: true).IntersectIntervalsOrNull(C()));

    [Test]
    public void IntersectIntervalsOrNull_ComparableBoth() =>
        Assert.AreEqual(
            C(desc: U8, anc: Real, isComparable: true),
            C(isComparable: true).IntersectIntervalsOrNull(C(desc: U8, anc: Real, isComparable: true)));

    [Test]
    public void IntersectIntervalsOrNull_DescIsComposite() =>
        Assert.AreEqual(
            C(desc: Array(Real)),
            C(desc: Array(I32)).IntersectIntervalsOrNull(C(desc: Array(Real))));

    [Test]
    public void IntersectIntervalsOrNull_DescIsOptional() =>
        Assert.AreEqual(
            C(desc: Optional(Real)),
            C(desc: Optional(I32)).IntersectIntervalsOrNull(C(desc: Optional(Real))));

    [Test]
    public void IntersectIntervalsOrNull_DescPrimAndComposite() =>
        Assert.AreEqual(
            C(desc: Any),
            C(desc: I32).IntersectIntervalsOrNull(C(desc: Array(I32))));

    [Test]
    public void IntervalIsNonEmpty_EmptyConstraints() =>
        Assert.IsTrue(C().IntervalIsNonEmpty());

    [Test]
    public void IntervalIsNonEmpty_DescOnly() =>
        Assert.IsTrue(C(desc: U8).IntervalIsNonEmpty());

    [Test]
    public void IntervalIsNonEmpty_AncOnly() =>
        Assert.IsTrue(C(anc: Real).IntervalIsNonEmpty());

    [Test]
    public void IntervalIsNonEmpty_ValidInterval() =>
        Assert.IsTrue(C(desc: U8, anc: Real).IntervalIsNonEmpty());

    [Test]
    public void IntervalIsNonEmpty_PointInterval_Same() =>
        Assert.IsTrue(C(desc: I32, anc: I32).IntervalIsNonEmpty());

    [Test]
    public void IntervalIsNonEmpty_PointInterval_Comparable_Numeric() =>
        Assert.IsTrue(C(desc: I32, anc: I32, isComparable: true).IntervalIsNonEmpty());

    [Test]
    public void IntervalIsNonEmpty_PointInterval_Comparable_Bool() =>
        Assert.IsFalse(C(desc: Bool, anc: Bool, isComparable: true).IntervalIsNonEmpty());

    [Test]
    public void IntervalIsNonEmpty_EmptyInterval_DescAboveAnc() =>
        Assert.IsFalse(C(desc: Real, anc: U8).IntervalIsNonEmpty());

    [Test]
    public void IntervalIsNonEmpty_Disjoint_BoolAndNumeric() =>
        Assert.IsFalse(C(desc: Bool, anc: I32).IntervalIsNonEmpty());

    [Test]
    public void IntervalIsNonEmpty_CompositeDesc() =>
        Assert.IsFalse(C(desc: Array(I32), anc: Real).IntervalIsNonEmpty());

    [Test]
    public void CanBeConvertedTo_Empty_AnyPrimitive() =>
        Assert.IsTrue(C().CanBeConvertedTo(I32));

    [Test]
    public void CanBeConvertedTo_Empty_AnyArray() =>
        Assert.IsTrue(C().CanBeConvertedTo(Array(I32)));

    [Test]
    public void CanBeConvertedTo_DescOnly_FitsTarget() =>
        Assert.IsTrue(C(desc: U8).CanBeConvertedTo(I32));

    [Test]
    public void CanBeConvertedTo_DescOnly_AboveTarget() =>
        Assert.IsFalse(C(desc: I64).CanBeConvertedTo(I32));

    [Test]
    public void CanBeConvertedTo_AncOnly_TargetFitsAnc() =>
        Assert.IsTrue(C(anc: Real).CanBeConvertedTo(I32));

    [Test]
    public void CanBeConvertedTo_AncOnly_TargetAboveAnc() =>
        Assert.IsFalse(C(anc: I32).CanBeConvertedTo(Real));

    [Test]
    public void CanBeConvertedTo_FullInterval() =>
        Assert.IsTrue(C(desc: U8, anc: Real).CanBeConvertedTo(I32));

    [Test]
    public void CanBeConvertedTo_FullInterval_OutBelow() =>
        Assert.IsFalse(C(desc: I32, anc: Real).CanBeConvertedTo(U8));

    [Test]
    public void CanBeConvertedTo_FullInterval_OutAbove() =>
        Assert.IsFalse(C(desc: U8, anc: I32).CanBeConvertedTo(Real));

    [Test]
    public void CanBeConvertedTo_Comparable_NumericOk() =>
        Assert.IsTrue(C(isComparable: true).CanBeConvertedTo(I32));

    [Test]
    public void CanBeConvertedTo_Comparable_BoolFails() =>
        Assert.IsFalse(C(isComparable: true).CanBeConvertedTo(Bool));

    [Test]
    public void CanBeConvertedTo_Comparable_TextOk() =>
        Assert.IsTrue(C(isComparable: true).CanBeConvertedTo(Array(Char)));

    [Test]
    public void CanBeConvertedTo_Comparable_NonTextArrayFails() =>
        Assert.IsFalse(C(isComparable: true).CanBeConvertedTo(Array(I32)));

    [Test]
    public void CanBeConvertedTo_CompositeTarget_SameType() =>
        Assert.IsTrue(C(desc: Array(I32)).CanBeConvertedTo(Array(Real)));

    [Test]
    public void SolveCovariant_Empty() =>
        Assert.AreEqual(Any, C().SolveCovariant());

    [Test]
    public void SolveCovariant_AncOnly() =>
        Assert.AreEqual(Real, C(anc: Real).SolveCovariant());

    [Test]
    public void SolveCovariant_DescOnly_Primitive() =>
        Assert.AreEqual(Any, C(desc: I32).SolveCovariant());

    [Test]
    public void SolveCovariant_FullInterval() =>
        Assert.AreEqual(Real, C(desc: U8, anc: Real).SolveCovariant());

    [Test]
    public void SolveCovariant_WithPreferred_Fits() =>
        Assert.AreEqual(I32, C(desc: U8, anc: Real, preferred: I32).SolveCovariant());

    [Test]
    public void SolveCovariant_WithPreferred_DoesntFit() =>
        Assert.AreEqual(I32, C(desc: U8, anc: I32, preferred: Real).SolveCovariant());

    [Test]
    public void SolveCovariant_Comparable_AncIsComparable() =>
        Assert.AreEqual(Real, C(anc: Real, isComparable: true).SolveCovariant());

    [Test]
    public void SolveCovariant_Comparable_AncNotComparable() {
        var c = C(anc: Any, isComparable: true);
        Assert.AreSame(c, c.SolveCovariant());
    }

    [Test]
    public void SolveCovariant_CompositeDesc_Array() =>
        Assert.AreEqual(Array(I32), C(desc: Array(I32)).SolveCovariant());

    [Test]
    public void SolveCovariant_CompositeDesc_Struct() {
        var s = Struct("a", I32);
        Assert.AreEqual(s, C(desc: s).SolveCovariant());
    }

    [Test]
    public void SolveCovariant_CompositeDesc_Optional() =>
        Assert.AreEqual(Optional(I32), C(desc: Optional(I32)).SolveCovariant());

    [Test]
    public void SolveCovariant_IgnorePreferred() =>
        Assert.AreEqual(Real, C(desc: U8, anc: Real, preferred: I32).SolveCovariant(ignorePreferred: true));

    [Test]
    public void SolveContravariant_Empty() {
        var c = C();
        Assert.AreSame(c, c.SolveContravariant());
    }

    [Test]
    public void SolveContravariant_AncOnly() {
        var c = C(anc: Real);
        Assert.AreSame(c, c.SolveContravariant());
    }

    [Test]
    public void SolveContravariant_DescOnly_Primitive() =>
        Assert.AreEqual(I32, C(desc: I32).SolveContravariant());

    [Test]
    public void SolveContravariant_FullInterval() =>
        Assert.AreEqual(U8, C(desc: U8, anc: Real).SolveContravariant());

    [Test]
    public void SolveContravariant_WithPreferred_Fits() =>
        Assert.AreEqual(I32, C(desc: U8, anc: Real, preferred: I32).SolveContravariant());

    [Test]
    public void SolveContravariant_WithPreferred_DoesntFit() =>
        Assert.AreEqual(U8, C(desc: U8, anc: I32, preferred: Real).SolveContravariant());

    [Test]
    public void SolveContravariant_Comparable_DescComparable() =>
        Assert.AreEqual(I32, C(desc: I32, isComparable: true).SolveContravariant());

    [Test]
    public void SolveContravariant_Comparable_DescNotComparable() {
        var c = C(desc: Bool, isComparable: true);
        Assert.AreSame(c, c.SolveContravariant());
    }

    [Test]
    public void SolveContravariant_Comparable_TextDesc() =>
        Assert.AreEqual(Array(Char), C(desc: Array(Char), isComparable: true).SolveContravariant());

    [Test]
    public void SolveContravariant_CompositeDesc() =>
        Assert.AreEqual(Array(I32), C(desc: Array(I32)).SolveContravariant());

    [Test]
    public void AddDescendant_None_OnPrimitiveBound_WrapsInOptional() {
        var c = C(desc: U8);
        c.AddDescendant(None);
        Assert.AreEqual(Optional(U8), c.Descendant);
    }

    [Test]
    public void IntervalIsNonEmpty_OptionalDescPrimitiveAnc_ReturnsFalse() {
        // Step 2: the intersected interval [opt(U8)..Real] is empty
        // because opt(U8) ≰ Real.
        var c = C(desc: Optional(U8), anc: Real);
        Assert.IsFalse(c.IntervalIsNonEmpty());
    }

    [Test]
    public void MergeOrNull_AfterPullWithNone_FailsAgainstIntConst() {
        // Step 3: Destruction tries [opt(U8)..].MergeOrNull([U8..Real]Real!)
        // and gets null — the intervals are incompatible.
        var afterPull = C(desc: Optional(U8));
        var intConst = C(desc: U8, anc: Real, preferred: Real);
        var result = afterPull.MergeOrNull(intConst);
        Assert.IsNull(result);
    }

    [Test]
    public void MergeOrNull_SolvedOptionalDesc_CollapsesToOptional() {
        // Shows how MergeOrNull collapses [opt(T)..] to opt(T) when T is solved
        // and there is no ancestor or comparable constraint.
        // This collapse is correct algebra, but in the pipeline it happens
        // before the intConst's upper bound has been integrated.
        var c1 = C(desc: Optional(U8));
        var c2 = C(desc: Optional(U8));
        var result = c1.MergeOrNull(c2);
        Assert.IsInstanceOf<StateOptional>(result);
        Assert.AreEqual(Optional(U8), result);
    }
}
