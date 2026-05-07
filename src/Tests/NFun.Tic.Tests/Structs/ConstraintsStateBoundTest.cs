using NFun.Tic.SolvingStates;
using NUnit.Framework;

namespace NFun.Tic.Tests.Structs;

/// <summary>
/// Push reform Milestone 1 — data-plumbing tests for ConstraintsState.StructBound.
/// Pure unit-level tests on the field itself, no algorithm.
/// See Specs/Tic/PushReform.md §Algebraic shape.
/// </summary>
public class ConstraintsStateBoundTest {

    [Test]
    public void DefaultCS_HasNullStructBound() {
        var cs = ConstraintsState.Empty;
        Assert.IsNull(cs.StructBound);
    }

    [Test]
    public void NoConstrains_TrueOnEmptyAndFalseWithBound() {
        var empty = ConstraintsState.Empty;
        Assert.IsTrue(empty.NoConstrains, "Empty CS has no constraints");

        var bounded = ConstraintsState.Empty;
        bounded.StructBound = StateStruct.Of("v", StatePrimitive.I32);
        Assert.IsFalse(bounded.NoConstrains, "CS with StructBound has a constraint");
    }

    [Test]
    public void GetCopy_PreservesStructBound() {
        var cs = ConstraintsState.Empty;
        var bound = StateStruct.Of("v", StatePrimitive.I32);
        cs.StructBound = bound;

        var copy = cs.GetCopy();
        Assert.AreSame(bound, copy.StructBound,
            "Milestone 1 keeps reference identity (no defensive deep-copy yet — added in Milestone 2 when merges become possible)");
    }

    [Test]
    public void Equals_StructuralOnStructBound() {
        // Push reform M2.B: structural equality on StructBound (Amadio–Cardelli
        // bisimulation, cycle-aware). Two CSs with structurally-identical
        // bounds compare equal even if the bound INSTANCES differ.
        var cs1 = ConstraintsState.Empty;
        var cs2 = ConstraintsState.Empty;
        Assert.IsTrue(cs1.Equals(cs2), "Two empty CSs are equal");

        var bound = StateStruct.Of("v", StatePrimitive.I32);
        cs1.StructBound = bound;
        Assert.IsFalse(cs1.Equals(cs2), "CS with bound differs from CS without bound");

        cs2.StructBound = bound;
        Assert.IsTrue(cs1.Equals(cs2), "Same bound reference: equal");

        // M2.B change: two structurally-identical bounds compare equal.
        cs2.StructBound = StateStruct.Of("v", StatePrimitive.I32);
        Assert.IsTrue(cs1.Equals(cs2),
            "Two distinct bound instances with same field set + types are STRUCTURALLY equal (M2.B coinductive bisimulation).");

        // Different field set: not equal.
        cs2.StructBound = StateStruct.Of("w", StatePrimitive.I32);
        Assert.IsFalse(cs1.Equals(cs2), "Different field name → not equal");

        cs2.StructBound = StateStruct.Of(("v", StatePrimitive.I32), ("w", StatePrimitive.Bool));
        Assert.IsFalse(cs1.Equals(cs2), "Different arity → not equal");
    }

    [Test]
    public void SolveCovariant_BoundOnly_MaterializesStructBound() {
        // Push reform M2.B: when StructBound is the only constraint
        // (no Ancestor, no Descendant, no Preferred, not Comparable),
        // SolveCovariant returns the bound itself — F-bound materialization
        // (Pierce TAPL §20.2 fold[μX.S]).
        var cs = ConstraintsState.Empty;
        cs.StructBound = StateStruct.Of("v", StatePrimitive.I32);
        var resolved = cs.SolveCovariant();
        Assert.AreSame(cs.StructBound, resolved,
            "Bound-only CS resolves to the bound itself, not Any");
    }

    [Test]
    public void SolveContravariant_BoundOnly_MaterializesStructBound() {
        var cs = ConstraintsState.Empty;
        cs.StructBound = StateStruct.Of("v", StatePrimitive.I32);
        var resolved = cs.SolveContravariant();
        Assert.AreSame(cs.StructBound, resolved);
    }

    [Test]
    public void GcdBound_DistinctFields_FieldUnion() {
        // Push reform M2.C: meet of two F-bounds = field union (narrower
        // upper-bound predicate). {left:T?} ∩ {right:T?} = {left:T?, right:T?}.
        var s1 = StateStruct.Of("left", StatePrimitive.I32);
        var s2 = StateStruct.Of("right", StatePrimitive.I32);
        var owner1 = TicNode.CreateInvisibleNode(StatePrimitive.Any);
        var owner2 = TicNode.CreateInvisibleNode(StatePrimitive.Any);
        var merged = SolvingFunctions.GcdBound(s1, s2, owner1, owner2);
        Assert.IsNotNull(merged);
        Assert.AreEqual(2, merged.FieldsCount);
        Assert.IsNotNull(merged.GetFieldOrNull("left"));
        Assert.IsNotNull(merged.GetFieldOrNull("right"));
    }

    [Test]
    public void GcdBound_SharedField_PrimitiveGcd() {
        // Shared field with different primitive types: result has the GCD
        // of those types (narrower).
        var s1 = StateStruct.Of("v", StatePrimitive.I32);
        var s2 = StateStruct.Of("v", StatePrimitive.Real);
        var owner1 = TicNode.CreateInvisibleNode(StatePrimitive.Any);
        var owner2 = TicNode.CreateInvisibleNode(StatePrimitive.Any);
        var merged = SolvingFunctions.GcdBound(s1, s2, owner1, owner2);
        Assert.IsNotNull(merged);
        var vField = merged.GetFieldOrNull("v");
        Assert.IsNotNull(vField);
        // I32 ∩ Real = I32 (narrower of the two)
        Assert.AreEqual(StatePrimitive.I32, vField.GetNonReference().State);
    }

    [Test]
    public void GcdBound_Idempotent() {
        // Push reform M2.C confluence: GcdBound(S, S) ≡ S structurally.
        var s = StateStruct.Of(("v", StatePrimitive.I32), ("w", StatePrimitive.Bool));
        var owner1 = TicNode.CreateInvisibleNode(StatePrimitive.Any);
        var owner2 = TicNode.CreateInvisibleNode(StatePrimitive.Any);
        var merged = SolvingFunctions.GcdBound(s, s, owner1, owner2);
        // Reference-equal (idempotent fast path).
        Assert.AreSame(s, merged);
    }

    [Test]
    public void SimplifyOrNull_StructBound_RejectsPrimitiveDescendant() {
        // Push reform M2.C three-way SimplifyOrNull: D=primitive vs S=struct
        // is empty (no struct ≤ primitive).
        var cs = ConstraintsState.Of(desc: StatePrimitive.I32);
        cs.StructBound = StateStruct.Of("v", StatePrimitive.I32);
        Assert.IsNull(cs.SimplifyOrNull());
    }

    [Test]
    public void SimplifyOrNull_StructBound_RejectsNonAnyPrimitiveAncestor() {
        // Push reform M2.C three-way SimplifyOrNull: A=non-Any primitive vs
        // S=struct is empty (struct ≰ primitive).
        var cs = ConstraintsState.Of(anc: StatePrimitive.Real);
        cs.StructBound = StateStruct.Of("v", StatePrimitive.I32);
        Assert.IsNull(cs.SimplifyOrNull());
    }

    [Test]
    public void SimplifyOrNull_StructBound_RejectsComparable() {
        // Push reform M2.C: structs aren't Comparable; bound + comparable = empty.
        var cs = ConstraintsState.Of(isComparable: true);
        cs.StructBound = StateStruct.Of("v", StatePrimitive.I32);
        Assert.IsNull(cs.SimplifyOrNull());
    }

    [Test]
    public void GetHashCode_DependsOnStructBoundFieldNames() {
        // Push reform M2.B: hash field names + arity (no recursion into types
        // — cycle-unsafe). Equality test still arbitrates collisions.
        var csA = ConstraintsState.Empty;
        csA.StructBound = StateStruct.Of("v", StatePrimitive.I32);
        var csB = ConstraintsState.Empty;
        csB.StructBound = StateStruct.Of("v", StatePrimitive.I32);
        Assert.AreEqual(csA.GetHashCode(), csB.GetHashCode(),
            "Same field set ⇒ same hash (Equals = true)");

        var csC = ConstraintsState.Empty;
        csC.StructBound = StateStruct.Of("w", StatePrimitive.I32);
        Assert.AreNotEqual(csA.GetHashCode(), csC.GetHashCode(),
            "Different field name ⇒ different hash bucket");
    }

    [Test]
    public void StructBound_IsIndependentOfDescAndAnc() {
        // Theorem PT-F: StructBound is the THIRD independent dimension on CS,
        // peer to IsComparable/IsOptional. It does not interact with [D..A]
        // through Concretest/Abstractest. This test asserts the field can be
        // set together with primitive bounds without either affecting the other.
        var cs = ConstraintsState.Of(desc: StatePrimitive.I32, anc: StatePrimitive.Real);
        cs.StructBound = StateStruct.Of("v", StatePrimitive.I32);

        Assert.AreEqual(StatePrimitive.I32, cs.Descendant);
        Assert.AreEqual(StatePrimitive.Real, cs.Ancestor);
        Assert.IsNotNull(cs.StructBound);
        Assert.AreEqual(1, cs.StructBound.FieldsCount);
    }

    // ============================================================
    // Fit(T, CS{S}) — structural width-subtype check (M1.2)
    // Algebra_Fit.md clause 4.
    // ============================================================

    [Test]
    public void Fit_StructWithRequiredField_Accepts() {
        // S = {v: int}; T = {v: int}. Exact match — fits.
        var cs = ConstraintsState.Empty;
        cs.StructBound = StateStruct.Of("v", StatePrimitive.I32);
        var t = StateStruct.Of("v", StatePrimitive.I32);

        Assert.IsTrue(cs.CanBeConvertedTo(t));
    }

    [Test]
    public void Fit_StructWithExtraFields_Accepts() {
        // S = {v: int}; T = {v: int, label: text}. Width subtyping: T's fields ⊇ S's fields.
        var cs = ConstraintsState.Empty;
        cs.StructBound = StateStruct.Of("v", StatePrimitive.I32);
        var t = StateStruct.Of(
            ("v", StatePrimitive.I32),
            ("label", StatePrimitive.Char));

        Assert.IsTrue(cs.CanBeConvertedTo(t),
            "T has all fields of S plus extras — width subtyping accepts");
    }

    [Test]
    public void Fit_StructMissingField_Rejects() {
        // S = {v: int, next: ...}; T = {v: int}. T missing `next` — rejects.
        var cs = ConstraintsState.Empty;
        cs.StructBound = StateStruct.Of(
            ("v", StatePrimitive.I32),
            ("next", StatePrimitive.None));
        var t = StateStruct.Of("v", StatePrimitive.I32);

        Assert.IsFalse(cs.CanBeConvertedTo(t),
            "T missing required field — rejects");
    }

    [Test]
    public void Fit_NotAStruct_Rejects() {
        // S = {v: int}; T = primitive. T is not a struct — rejects.
        var cs = ConstraintsState.Empty;
        cs.StructBound = StateStruct.Of("v", StatePrimitive.I32);

        Assert.IsFalse(cs.CanBeConvertedTo(StatePrimitive.I32),
            "Primitive cannot satisfy struct F-bound");
    }

    [Test]
    public void Fit_NoBound_FallsBackToExistingLogic() {
        // No StructBound — CanBeConvertedTo behaves as before (regression check).
        var cs = ConstraintsState.Empty;
        Assert.IsTrue(cs.CanBeConvertedTo(StatePrimitive.I32),
            "Empty CS accepts anything (no bound, no constraints)");
    }
}
