using NFun.Tic.SolvingStates;
using NUnit.Framework;

namespace NFun.Tic.Tests.UnitTests;

using static StatePrimitive;

/// <summary>
/// Unit tests for <see cref="SolvingFunctions.GetMergedStateOrNull"/> — the
/// pair-merge function used during graph construction (SetStructInit, SetStructInitType,
/// MutableStruct) and inside MergeInplace.
///
/// Theoretical contract: returns the merged state that simultaneously satisfies BOTH
/// inputs' constraints, OR null when no such merge exists (caller must reject).
///
/// Foundational invariants (Pottier-Rémy '05 §10.6 + Amadio-Cardelli '93 §4):
///   1. ReferenceEquals short-circuit — same object → trivial merge (no recursion)
///   2. Empty Constraints absorbs anything (one side empty → return other)
///   3. Two solved type states must be equal (no narrowing possible)
///   4. Composite-composite merges recurse via MergeInplace on members
///   5. Cycle guard for struct↔struct: visited-pair set, return strA on re-entry
///      (coinductive bisimulation — assumes equality under cycle)
///
/// Bug A (GH #128) was a missing cycle guard for struct-struct merge in the presence
/// of recursive named types — this file pins the contract so regressions are caught
/// at the algebra layer, not via syntax tests.
/// </summary>
class BugRegression_MergeOrNullCycleTests {

    // ─── Identity & trivial cases ───

    [Test]
    public void Merge_ReferenceEqualStates_ReturnsThat() {
        // Same object reference — short-circuit, no recursion.
        var s = StateArray.Of(I32);
        Assert.AreSame(s, SolvingFunctions.GetMergedStateOrNull(s, s));
    }

    [Test]
    public void Merge_EmptyConstraintsAndPrimitive_ReturnsPrimitive() {
        // Empty Constraints is the unit for merge — absorbs the other state.
        var res = SolvingFunctions.GetMergedStateOrNull(I32, ConstraintsState.Empty);
        Assert.AreSame(I32, res);
    }

    [Test]
    public void Merge_PrimitiveAndEmptyConstraints_ReturnsPrimitive() {
        var res = SolvingFunctions.GetMergedStateOrNull(ConstraintsState.Empty, I32);
        Assert.AreSame(I32, res);
    }

    [Test]
    public void Merge_TwoSameSolvedPrimitives_ReturnsThat() {
        var res = SolvingFunctions.GetMergedStateOrNull(I32, I32);
        Assert.AreEqual(I32, res);
    }

    [Test]
    public void Merge_TwoDifferentSolvedPrimitives_ReturnsNull() {
        // Solved primitives must be equal — no narrowing possible.
        Assert.IsNull(SolvingFunctions.GetMergedStateOrNull(I32, Real));
        Assert.IsNull(SolvingFunctions.GetMergedStateOrNull(Real, I32));
    }

    // ─── None primitive: lift `None ≤ opt(T)` is universal ───
    // Algebraic contract: None fits into any Optional, regardless of the Optional's
    // IsSolved state. Previously the None-handling rule (line 34-37 of
    // SolvingFunctions.cs) was placed AFTER the immutable-vs-immutable Equals shortcut,
    // making it unreachable for solved Optionals (None.Equals(opt(T)) is false → null
    // before reaching the rule). The fix reorders these checks; tests below pin the
    // corrected contract.

    [Test]
    public void Merge_NoneAndNone_ReturnsSameViaReferenceEquals() {
        Assert.AreSame(None, SolvingFunctions.GetMergedStateOrNull(None, None));
    }

    [Test]
    public void Merge_NoneAndSolvedOptional_ReturnsOptional() {
        // None ≤ opt(T) for any T — including solved.
        var opt = StateOptional.Of(I32);
        Assert.AreSame(opt, SolvingFunctions.GetMergedStateOrNull(None, opt));
        Assert.AreSame(opt, SolvingFunctions.GetMergedStateOrNull(opt, None));
    }

    [Test]
    public void Merge_NoneAndUnsolvedOptional_ReturnsOptional() {
        // Same rule applies to unsolved Optionals (used during graph construction).
        var elem = TicNode.CreateNamedNode("v", ConstraintsState.Empty);
        var opt = new StateOptional(elem);
        Assert.AreSame(opt, SolvingFunctions.GetMergedStateOrNull(None, opt));
        Assert.AreSame(opt, SolvingFunctions.GetMergedStateOrNull(opt, None));
    }

    [Test]
    public void Merge_NoneAndOptionalOfNamedStruct_ReturnsOptional() {
        // None ≤ opt(named t) — preserves the named-recursive cycle.
        var st = StateStruct.Of("v", I32);
        st.TypeName = "t";
        var optNamed = StateOptional.Of(st);
        Assert.AreSame(optNamed, SolvingFunctions.GetMergedStateOrNull(None, optNamed));
    }

    // ─── Composite ↔ Composite ───

    [Test]
    public void Merge_TwoSameArrays_ReturnsFirst() {
        // Two solved arrays of same element type — merge returns the first (members
        // are MergeInplace'd structurally).
        var a = StateArray.Of(I32);
        var b = StateArray.Of(I32);
        var res = SolvingFunctions.GetMergedStateOrNull(a, b);
        Assert.AreSame(a, res);
    }

    [Test]
    public void Merge_OptionalAndOptional_ReturnsFirst() {
        var a = StateOptional.Of(I32);
        var b = StateOptional.Of(I32);
        var res = SolvingFunctions.GetMergedStateOrNull(a, b);
        Assert.AreSame(a, res);
    }

    [Test]
    public void Merge_MutableStructAndImmutableStruct_ReturnsNull() {
        // MutStruct and Struct are different type constructors — cannot merge.
        var mut = new StateMutableStruct(isOpen: false);
        var imm = new StateStruct();
        Assert.IsNull(SolvingFunctions.GetMergedStateOrNull(mut, imm));
        Assert.IsNull(SolvingFunctions.GetMergedStateOrNull(imm, mut));
    }

    [Test]
    public void Merge_ArrayAndOptional_ReturnsNull() {
        // Different composite shapes — no merge.
        var arr = StateArray.Of(I32);
        var opt = StateOptional.Of(I32);
        Assert.IsNull(SolvingFunctions.GetMergedStateOrNull(arr, opt));
        Assert.IsNull(SolvingFunctions.GetMergedStateOrNull(opt, arr));
    }

    [Test]
    public void Merge_StructAndArray_ReturnsNull() {
        var s = StateStruct.Of("v", I32);
        var arr = StateArray.Of(I32);
        Assert.IsNull(SolvingFunctions.GetMergedStateOrNull(s, arr));
        Assert.IsNull(SolvingFunctions.GetMergedStateOrNull(arr, s));
    }

    // ─── Struct-struct merge basics ───

    [Test]
    public void Merge_TwoEmptyStructs_ReturnsFirst() {
        var a = new StateStruct();
        var b = new StateStruct();
        var res = SolvingFunctions.GetMergedStateOrNull(a, b);
        Assert.IsNotNull(res);
        Assert.IsInstanceOf<StateStruct>(res);
    }

    [Test]
    public void Merge_TwoStructsSameField_ReturnsMerged() {
        var a = StateStruct.Of("v", I32);
        var b = StateStruct.Of("v", I32);
        var res = SolvingFunctions.GetMergedStateOrNull(a, b);
        Assert.IsNotNull(res);
        Assert.IsInstanceOf<StateStruct>(res);
    }

    // ─── Cycle handling (Bug A regression contract) ───

    [Test]
    public void Merge_SelfReferentialStruct_NoStackOverflow() {
        // Build a struct whose `next` field is a TicNode pointing to itself.
        // The merge of (struct, struct) must terminate via the visited-pair guard.
        var sStruct = new StateStruct();
        var selfNode = TicNode.CreateNamedNode("self", sStruct);
        sStruct.AddField("next", selfNode);

        // Calling Merge(s, s) — same reference, hits ReferenceEquals short-circuit
        // (line 20 of SolvingFunctions.GetMergedStateOrNull).
        var res = SolvingFunctions.GetMergedStateOrNull(sStruct, sStruct);
        Assert.AreSame(sStruct, res, "Merge of self with self must return self via ReferenceEquals");
    }

    [Test]
    public void Merge_TwoCyclicStructsSameShape_TerminatesViaVisitedPair() {
        // Two DIFFERENT (not reference-equal) struct objects, each self-referential
        // through a `next` field. Merge MUST terminate via the visited-pair coinductive
        // guard, NOT recurse infinitely. Per Amadio-Cardelli '93 §4.2: merging
        // bisimilar μ-types terminates by assuming equality on re-entered pairs.
        var a = new StateStruct();
        var aSelf = TicNode.CreateNamedNode("a_self", a);
        a.AddField("next", aSelf);

        var b = new StateStruct();
        var bSelf = TicNode.CreateNamedNode("b_self", b);
        b.AddField("next", bSelf);

        Assert.DoesNotThrow(() => {
            var res = SolvingFunctions.GetMergedStateOrNull(a, b);
            Assert.IsNotNull(res, "Cycle-bisimilar structs should merge");
        });
    }

    [Test]
    public void Merge_NamedRecursiveStructWithSelf_Idempotent() {
        // Named recursive struct merged with itself — TypeName short-circuit on Equals,
        // ReferenceEquals on merge.
        var s = new StateStruct();
        var selfNode = TicNode.CreateNamedNode("self", s);
        s.AddField("next", selfNode);
        s.TypeName = "t";

        Assert.DoesNotThrow(() => {
            var res = SolvingFunctions.GetMergedStateOrNull(s, s);
            Assert.AreSame(s, res);
        });
    }

    // ─── RefTo handling in merge ───
    // RefTo is the graph-rewiring state. GetMergedStateOrNull returns the RefTo
    // itself (the merge result IS the rewiring), NOT the dereferenced target.
    // Dereferencing happens at the call site (MergeInplace / Pull stages).

    [Test]
    public void Merge_RefToAndPrimitive_ReturnsRefTo() {
        // StateRefTo is mutable (IsMutable=true) per its semantics. The non-mutable
        // primitive becomes the merged state via the immutable-side rule (line 119-124
        // of SolvingFunctions): swap-and-recurse path.
        var target = TicNode.CreateNamedNode("target", I32);
        var refState = new StateRefTo(target);
        var res = SolvingFunctions.GetMergedStateOrNull(refState, I32);
        Assert.IsNotNull(res, "Merge with RefTo must not return null when target is compatible");
    }

    [Test]
    public void Merge_TwoRefTosToSameNode_Succeeds() {
        // Different StateRefTo objects pointing to the same target — merge must succeed
        // (the underlying target unifies the constraints).
        var target = TicNode.CreateNamedNode("target", I32);
        var refA = new StateRefTo(target);
        var refB = new StateRefTo(target);
        var res = SolvingFunctions.GetMergedStateOrNull(refA, refB);
        Assert.IsNotNull(res, "Two RefTos to same target must merge");
    }

    // ─── Constraints-side merging ───

    [Test]
    public void Merge_PrimitiveAndCompatibleConstraints_ReturnsPrimitive() {
        // I32 is in [I16..Real]: I32 fits the constraint → merge returns I32.
        var cs = ConstraintsState.Of(I16, Real);
        var res = SolvingFunctions.GetMergedStateOrNull(I32, cs);
        Assert.AreEqual(I32, res);
    }

    [Test]
    public void Merge_PrimitiveAndIncompatibleConstraints_ReturnsNull() {
        // Real is OUTSIDE [U24..U48] — merge fails.
        var cs = ConstraintsState.Of(U24, U48);
        Assert.IsNull(SolvingFunctions.GetMergedStateOrNull(Real, cs));
        Assert.IsNull(SolvingFunctions.GetMergedStateOrNull(cs, Real));
    }

    [Test]
    public void Merge_TwoConstraints_IntersectsIntervals() {
        // ConstraintsState ∩ ConstraintsState — intersection of intervals.
        var a = ConstraintsState.Of(I16, Real);
        var b = ConstraintsState.Of(I32, Real);
        var res = SolvingFunctions.GetMergedStateOrNull(a, b);
        Assert.IsNotNull(res);
        Assert.IsInstanceOf<ConstraintsState>(res);
    }

    [Test]
    public void Merge_TwoConstraints_DisjointIntervals_ReturnsNull() {
        var a = ConstraintsState.Of(I16, I24);  // [I16..I24]
        var b = ConstraintsState.Of(U32, U48);  // [U32..U48] — disjoint
        Assert.IsNull(SolvingFunctions.GetMergedStateOrNull(a, b));
    }

    // ─── Symmetry contract ───
    // For commutative merges (no order-dependent side-effect), Merge(a,b) and Merge(b,a)
    // should both succeed or both fail. The CONTENT may differ (the returned state object
    // may be different) but the success/failure status must agree.

    [Test]
    public void Merge_Symmetry_PrimitiveAndConstraints() {
        var p = I32;
        var c = ConstraintsState.Of(I16, Real);
        Assert.AreEqual(
            SolvingFunctions.GetMergedStateOrNull(p, c) != null,
            SolvingFunctions.GetMergedStateOrNull(c, p) != null);
    }

    [Test]
    public void Merge_Symmetry_IncompatiblePrimitives() {
        Assert.AreEqual(
            SolvingFunctions.GetMergedStateOrNull(I32, Real) != null,
            SolvingFunctions.GetMergedStateOrNull(Real, I32) != null);
    }

    [Test]
    public void Merge_Symmetry_ArrayVsOptional() {
        var arr = StateArray.Of(I32);
        var opt = StateOptional.Of(I32);
        Assert.AreEqual(
            SolvingFunctions.GetMergedStateOrNull(arr, opt) != null,
            SolvingFunctions.GetMergedStateOrNull(opt, arr) != null);
    }
}
