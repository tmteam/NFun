namespace NFun.Tic.Tests.UnitTests;

using NUnit.Framework;
using NFun.Tic.Algebra;
using SolvingStates;
using static SolvingStates.StatePrimitive;

/// <summary>
/// Unit tests for pure algebraic operations on ConstraintsState:
/// IntersectIntervalsOrNull, IntervalIsNonEmpty, SolveCovariant, SolveContravariant, MergeOrNull
/// </summary>
public class ConstraintsAlgebraTest {

    #region IntersectIntervalsOrNull

    [Test]
    public void Intersect_TwoEmpty_ReturnsEmpty() {
        var r = ConstraintsState.Empty.IntersectIntervalsOrNull(ConstraintsState.Empty);
        Assert.IsNotNull(r);
        Assert.IsFalse(r.HasAncestor);
        Assert.IsFalse(r.HasDescendant);
    }

    [Test]
    public void Intersect_DescAndAnc_MergesRange() {
        var a = ConstraintsState.Of(U8);        // desc=U8
        var b = ConstraintsState.Of(anc: Real); // anc=Real
        var r = a.IntersectIntervalsOrNull(b);
        Assert.IsNotNull(r);
        Assert.IsNotNull(r.Descendant);
        Assert.AreEqual(Real, r.Ancestor);
    }

    [Test]
    public void Intersect_CompatibleAncestors_ReturnsGCD() {
        var a = ConstraintsState.Of(anc: Real);
        var b = ConstraintsState.Of(anc: I64);
        var r = a.IntersectIntervalsOrNull(b);
        Assert.IsNotNull(r);
        Assert.AreEqual(I64, r.Ancestor);
    }

    [Test]
    public void Intersect_IncompatibleAncestors_ReturnsNull() {
        var a = ConstraintsState.Of(anc: Bool);
        var b = ConstraintsState.Of(anc: I32);
        Assert.IsNull(a.IntersectIntervalsOrNull(b));
    }

    [Test]
    public void Intersect_ComparableORs() {
        var a = ConstraintsState.Of(isComparable: true);
        var b = ConstraintsState.Of(isComparable: false);
        Assert.IsTrue(a.IntersectIntervalsOrNull(b).IsComparable);
    }

    [Test]
    public void Intersect_OptionalORs() {
        var a = ConstraintsState.Of(isOptional: true);
        var b = ConstraintsState.Empty;
        Assert.IsTrue(a.IntersectIntervalsOrNull(b).IsOptional);
    }

    [Test]
    public void Intersect_BothOptional_IsOptional() {
        var a = ConstraintsState.Of(isOptional: true);
        var b = ConstraintsState.Of(isOptional: true);
        Assert.IsTrue(a.IntersectIntervalsOrNull(b).IsOptional);
    }

    [Test]
    public void Intersect_NeitherOptional_NotOptional() {
        var a = ConstraintsState.Of(U8);
        var b = ConstraintsState.Of(I16);
        Assert.IsFalse(a.IntersectIntervalsOrNull(b).IsOptional);
    }

    #endregion

    #region IntervalIsNonEmpty

    [Test]
    public void NonEmpty_NoConstraints_True() =>
        Assert.IsTrue(ConstraintsState.Empty.IntervalIsNonEmpty());

    [Test]
    public void NonEmpty_OnlyDesc_True() =>
        Assert.IsTrue(ConstraintsState.Of(U8).IntervalIsNonEmpty());

    [Test]
    public void NonEmpty_OnlyAnc_True() =>
        Assert.IsTrue(ConstraintsState.Of(anc: Real).IntervalIsNonEmpty());

    [Test]
    public void NonEmpty_DescFitsAnc_True() =>
        Assert.IsTrue(ConstraintsState.Of(U8, Real).IntervalIsNonEmpty());

    [Test]
    public void NonEmpty_AncEqualsDesc_True() =>
        Assert.IsTrue(ConstraintsState.Of(I32, I32).IntervalIsNonEmpty());

    [Test]
    public void NonEmpty_DescDoesNotFitAnc_False() =>
        Assert.IsFalse(ConstraintsState.Of(Real, I32).IntervalIsNonEmpty());

    [Test]
    public void NonEmpty_Comparable_AncIsComparable_True() =>
        Assert.IsTrue(ConstraintsState.Of(I32, I32, isComparable: true).IntervalIsNonEmpty());

    [Test]
    public void NonEmpty_Comparable_AncNotComparable_False() =>
        Assert.IsFalse(ConstraintsState.Of(Bool, Bool, isComparable: true).IntervalIsNonEmpty());

    #endregion

    #region SolveCovariant

    [Test]
    public void Covariant_Empty_ReturnsAny() =>
        Assert.AreEqual(Any, ConstraintsState.Empty.SolveCovariant());

    [Test]
    public void Covariant_WithAncestor_ReturnsAncestor() =>
        Assert.AreEqual(Real, ConstraintsState.Of(anc: Real).SolveCovariant());

    [Test]
    public void Covariant_WithPreferred_ReturnsPreferred() {
        var cs = ConstraintsState.Of(U8, Real);
        cs.Preferred = I32;
        Assert.AreEqual(I32, cs.SolveCovariant());
    }

    [Test]
    public void Covariant_IgnorePreferred_ReturnsAncestor() {
        var cs = ConstraintsState.Of(U8, Real);
        cs.Preferred = I32;
        Assert.AreEqual(Real, cs.SolveCovariant(ignorePreferred: true));
    }

    [Test]
    public void Covariant_CompositeDescendant_ReturnsDescendant() {
        var cs = ConstraintsState.Of(StateArray.Of(I32));
        Assert.IsInstanceOf<StateArray>(cs.SolveCovariant());
    }

    [Test]
    public void Covariant_IsOptional_WrapsInOptional() {
        var cs = ConstraintsState.Of(anc: I32, isOptional: true);
        Assert.IsInstanceOf<StateOptional>(cs.SolveCovariant());
    }

    [Test]
    public void Covariant_IsOptional_AncAny_ReturnsAny() {
        // opt(Any) collapses to Any
        var cs = ConstraintsState.Of(isOptional: true);
        Assert.AreEqual(Any, cs.SolveCovariant());
    }

    [Test]
    public void Covariant_Comparable_NoComparableAncestor_ReturnsSelf() {
        // ancestor defaults to Any which is not comparable → unresolved
        var cs = ConstraintsState.Of(isComparable: true);
        Assert.IsInstanceOf<ConstraintsState>(cs.SolveCovariant());
    }

    [Test]
    public void Covariant_Comparable_RealAncestor_ReturnsReal() =>
        Assert.AreEqual(Real, ConstraintsState.Of(anc: Real, isComparable: true).SolveCovariant());

    #endregion

    #region SolveContravariant

    [Test]
    public void Contravariant_Empty_ReturnsSelf() =>
        Assert.IsInstanceOf<ConstraintsState>(ConstraintsState.Empty.SolveContravariant());

    [Test]
    public void Contravariant_WithDescendant_ReturnsDescendant() =>
        Assert.AreEqual(U8, ConstraintsState.Of(U8).SolveContravariant());

    [Test]
    public void Contravariant_WithPreferred_ReturnsPreferred() {
        var cs = ConstraintsState.Of(U8, Real);
        cs.Preferred = I32;
        Assert.AreEqual(I32, cs.SolveContravariant());
    }

    [Test]
    public void Contravariant_IsOptional_WrapsInOptional() {
        var cs = ConstraintsState.Of(I32, isOptional: true);
        Assert.IsInstanceOf<StateOptional>(cs.SolveContravariant());
    }

    [Test]
    public void Contravariant_NoDescendant_ReturnsSelf() =>
        Assert.IsInstanceOf<ConstraintsState>(ConstraintsState.Of(anc: Real).SolveContravariant());

    [Test]
    public void Contravariant_NoDescendant_OnlyPreferred_ReturnsPreferred() {
        var cs = ConstraintsState.Of(anc: Real);
        cs.Preferred = I32;
        Assert.AreEqual(I32, cs.SolveContravariant());
    }

    #endregion

    #region MergeOrNull

    [Test]
    public void Merge_TwoEmpty_ReturnsConstraints() =>
        Assert.IsInstanceOf<ConstraintsState>(ConstraintsState.Empty.MergeOrNull(ConstraintsState.Empty));

    [Test]
    public void Merge_SamePoint_CollapsesToPrimitive() =>
        Assert.AreEqual(I32, ConstraintsState.Of(I32, I32).MergeOrNull(ConstraintsState.Of(I32, I32)));

    [Test]
    public void Merge_SamePoint_BothOptional_ReturnsOptional() {
        var a = ConstraintsState.Of(I32, I32, isOptional: true);
        var b = ConstraintsState.Of(I32, I32, isOptional: true);
        Assert.IsInstanceOf<StateOptional>(a.MergeOrNull(b));
    }

    [Test]
    public void Merge_PointAtAny_Optional_CollapsesToAny() {
        // Canonical form law: opt(Any) = Any is a quotient — the point collapse
        // [Any..Any, opt] must not materialize StateOptional(Any).
        var a = ConstraintsState.Of(Any, Any, isOptional: true);
        Assert.AreEqual(Any, a.MergeOrNull(ConstraintsState.Empty));
    }

    [Test]
    public void Merge_IncompatibleAncestors_ReturnsNull() =>
        Assert.IsNull(ConstraintsState.Of(anc: Bool).MergeOrNull(ConstraintsState.Of(anc: I32)));

    [Test]
    public void Merge_PreferredFromFirst_Preserved() {
        var a = ConstraintsState.Of(U8, Real);
        a.Preferred = I32;
        var r = a.MergeOrNull(ConstraintsState.Of(U8, Real)) as ConstraintsState;
        Assert.AreEqual(I32, r?.Preferred);
    }

    [Test]
    public void Merge_PreferredFromSecond_Preserved() {
        var b = ConstraintsState.Of(U8, Real);
        b.Preferred = I32;
        var r = ConstraintsState.Of(U8, Real).MergeOrNull(b) as ConstraintsState;
        Assert.AreEqual(I32, r?.Preferred);
    }

    [Test]
    public void Merge_SolvedStruct_NoAncestor_ReturnsStruct() {
        var str = StateStruct.Of(true, ("x", (ITicNodeState)I32));
        var r = ConstraintsState.Of(str).MergeOrNull(ConstraintsState.Empty);
        Assert.IsInstanceOf<StateStruct>(r);
    }

    #endregion

    #region Invariants

    [TestCase("U8", "Real", false)]
    [TestCase("I32", "I32", false)]
    [TestCase("U8", "I64", true)]
    public void Covariant_ResultFitsOriginal(string descName, string ancName, bool comparable) {
        var cs = ConstraintsState.Of(GetPrimitive(descName), GetPrimitive(ancName), comparable);
        cs.Preferred = I32;
        var result = cs.SolveCovariant();
        if (result is ConstraintsState) return; // unresolved
        Assert.IsTrue(result.FitsInto(cs),
            $"SolveCovariant({cs}) = {result} does not fit into original");
    }

    [TestCase("U8", "Real")]
    [TestCase("I32", "I32")]
    [TestCase("U8", "I64")]
    public void Contravariant_ResultFitsOriginal(string descName, string ancName) {
        var cs = ConstraintsState.Of(GetPrimitive(descName), GetPrimitive(ancName));
        cs.Preferred = I32;
        var result = cs.SolveContravariant();
        if (result is ConstraintsState) return;
        Assert.IsTrue(result.FitsInto(cs),
            $"SolveContravariant({cs}) = {result} does not fit into original");
    }

    [Test]
    public void Intersect_IsCommutative() {
        var a = ConstraintsState.Of(U8, Real);
        var b = ConstraintsState.Of(I16, I64);
        var r1 = a.IntersectIntervalsOrNull(b);
        var r2 = b.IntersectIntervalsOrNull(a);
        Assert.AreEqual(r1?.Ancestor, r2?.Ancestor);
    }

    #endregion

    #region Comparable domain (debt #31)

    // The comparable domain is {numeric primitives, Char, arr(Char)}. For a state with
    // an UNSOLVED part the honest membership question is "can it still become a member"
    // (the ≤/optimistic form) — exact `== Char` is over-strict and rejects satisfiable
    // states. All four inline sites (MergeOrNull, SimplifyOrNull, SolveContravariant,
    // FitsInto cmp-cell) must answer per the single rule `IsComparableDomain`;
    // resolution (SolveContravariant) additionally requires a solved POINT.

    [Test]
    public void FitsInto_ComparableCell_ArrayWithCharBoundedUnsolvedElement_Fits() {
        // arr([..Ch]) can still become arr(Ch) — the only comparable composite.
        // Unsolved targets are accepted conservatively everywhere else in Fit.
        var target = StateArray.Of(ConstraintsState.Of(anc: Char));
        Assert.IsTrue(target.FitsInto(ConstraintsState.Of(isComparable: true)));
    }

    [Test]
    public void FitsInto_ComparableCell_ArrayWithUnconstrainedElement_Fits() {
        var target = StateArray.Of(ConstraintsState.Empty);
        Assert.IsTrue(target.FitsInto(ConstraintsState.Of(isComparable: true)));
    }

    [Test]
    public void FitsInto_ComparableCell_SolvedCharArray_Fits() =>
        Assert.IsTrue(StateArray.Of(Char).FitsInto(ConstraintsState.Of(isComparable: true)));

    [Test]
    public void FitsInto_ComparableCell_SolvedNonCharArray_False() =>
        Assert.IsFalse(StateArray.Of(U8).FitsInto(ConstraintsState.Of(isComparable: true)));

    [Test]
    public void FitsInto_ComparableCell_ArrayWithNonCharBoundedElement_False() {
        // arr([Bool..]) can never become arr(Ch)
        var target = StateArray.Of(ConstraintsState.Of(desc: Bool));
        Assert.IsFalse(target.FitsInto(ConstraintsState.Of(isComparable: true)));
    }

    [Test]
    public void Merge_Cmp_ArrayWithCharBoundedUnsolvedElementDescendant_Satisfiable() {
        // Same shape as the Fit test above must answer the same at the ⊓ cmp-canonicalization
        var cs = ConstraintsState.Of(
            desc: StateArray.Of(ConstraintsState.Of(anc: Char)), isComparable: true);
        Assert.IsNotNull(cs.MergeOrNull(ConstraintsState.Empty));
    }

    [Test]
    public void Merge_Cmp_SolvedNonCharArrayDescendant_ReturnsNull() {
        var cs = ConstraintsState.Of(desc: StateArray.Of(Bool), isComparable: true);
        Assert.IsNull(cs.MergeOrNull(ConstraintsState.Empty));
    }

    [Test]
    public void Simplify_Cmp_ArrayWithCharBoundedUnsolvedElementDescendant_NarrowsToText() {
        var cs = ConstraintsState.Of(
            desc: StateArray.Of(ConstraintsState.Of(anc: Char)), isComparable: true);
        var result = cs.SimplifyOrNull();
        Assert.IsInstanceOf<StateArray>(result);
        Assert.AreEqual(Char, ((StateArray)result).Element);
    }

    [Test]
    public void SolveContravariant_Cmp_ArrayWithUnsolvedElementDescendant_StaysUnresolved() {
        // Resolution requires a comparable POINT (solved member of the domain);
        // arr with an unsolved element is not a point — stay unresolved.
        var cs = ConstraintsState.Of(
            desc: StateArray.Of(ConstraintsState.Of(anc: Char)), isComparable: true);
        Assert.IsInstanceOf<ConstraintsState>(cs.SolveContravariant());
    }

    [Test]
    public void SolveContravariant_Cmp_SolvedCharArrayDescendant_ResolvesToText() {
        var cs = ConstraintsState.Of(desc: StateArray.Of(Char), isComparable: true);
        Assert.IsInstanceOf<StateArray>(cs.SolveContravariant());
    }

    #endregion

    private static StatePrimitive GetPrimitive(string name) => name switch {
        "U8" => U8, "U16" => U16, "U32" => U32, "U64" => U64,
        "I16" => I16, "I32" => I32, "I64" => I64,
        "Real" => Real, "Bool" => Bool, "Any" => Any,
        _ => throw new System.ArgumentException(name)
    };
}
