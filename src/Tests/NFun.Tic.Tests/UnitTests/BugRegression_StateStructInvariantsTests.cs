using System.Collections.Generic;
using NFun.Tic.SolvingStates;
using NUnit.Framework;

namespace NFun.Tic.Tests.UnitTests;

using static StatePrimitive;

/// <summary>
/// Unit tests for <see cref="StateStruct"/> structural invariants:
///   - IsSolved: "all components have a concrete type assigned"
///   - IsMutable: "TypeName == null" — named types are immutable solved
///   - IsOpen: row-polymorphism marker (caller may add more fields)
///   - IsFrozen: width-subtyping marker (LCA computed a frozen result)
///   - AddField / ReplaceField — graph-construction operations
///   - GetFieldOrNull lookup semantics (case-insensitive)
///
/// These tests are foundational for many GH #128 bug fixes:
///   - Bug C/D/G relied on TypeName preservation through merge/LCA paths
///   - Bug E2 used ReplaceField to insert Optional-wrappers during graph construction
///   - Bug K cycle detection rests on IsSolved acting as a contractive boundary
///   - GH #126 followup's pinned-identity predicate uses (IsSolved && state is StatePrimitive)
///
/// Each test models a SINGLE algebraic invariant in isolation — no GraphBuilder,
/// no Solve, no Pull/Push. Pure StateStruct API.
/// </summary>
class BugRegression_StateStructInvariantsTests {

    // ─── Construction + basic field access ───

    [Test]
    public void EmptyStruct_HasZeroFields() {
        var s = new StateStruct();
        Assert.AreEqual(0, s.FieldsCount);
    }

    [Test]
    public void EmptyOpenStruct_IsOpen() {
        var s = new StateStruct(isOpen: true);
        Assert.IsTrue(s.IsOpen);
    }

    [Test]
    public void EmptyClosedStruct_IsNotOpen() {
        var s = new StateStruct(isOpen: false);
        Assert.IsFalse(s.IsOpen);
    }

    [Test]
    public void Of_SingleField_FieldsCountOne() {
        var s = StateStruct.Of("v", I32);
        Assert.AreEqual(1, s.FieldsCount);
        Assert.IsNotNull(s.GetFieldOrNull("v"));
    }

    [Test]
    public void Of_FieldLookup_CaseInsensitive() {
        // NFun is case-insensitive across the language; the field map must follow suit.
        var s = StateStruct.Of("Name", I32);
        Assert.IsNotNull(s.GetFieldOrNull("name"));
        Assert.IsNotNull(s.GetFieldOrNull("NAME"));
        Assert.IsNotNull(s.GetFieldOrNull("Name"));
    }

    [Test]
    public void Of_FieldLookup_MissingField_ReturnsNull() {
        var s = StateStruct.Of("v", I32);
        Assert.IsNull(s.GetFieldOrNull("missing"));
    }

    [Test]
    public void AddField_GrowsFieldsCount() {
        var s = new StateStruct();
        s.AddField("a", TicNode.CreateTypeVariableNode(I32));
        s.AddField("b", TicNode.CreateTypeVariableNode(Real));
        Assert.AreEqual(2, s.FieldsCount);
    }

    [Test]
    public void ReplaceField_DoesNotGrowFieldsCount() {
        // Used by Bug E2 fix: SetStructInitType inserts Optional-wrapper nodes by
        // REPLACING the literal's field-value node, not adding a new field.
        var s = new StateStruct();
        var nodeA = TicNode.CreateTypeVariableNode(I32);
        var nodeB = TicNode.CreateTypeVariableNode(Real);
        s.AddField("v", nodeA);
        s.ReplaceField("v", nodeB);
        Assert.AreEqual(1, s.FieldsCount, "ReplaceField must NOT grow field count");
        Assert.AreSame(nodeB, s.GetFieldOrNull("v"), "Replaced node must be visible");
    }

    [Test]
    public void ReplaceField_CaseInsensitive() {
        var s = new StateStruct();
        var nodeA = TicNode.CreateTypeVariableNode(I32);
        var nodeB = TicNode.CreateTypeVariableNode(Real);
        s.AddField("Foo", nodeA);
        s.ReplaceField("FOO", nodeB);
        Assert.AreSame(nodeB, s.GetFieldOrNull("foo"));
    }

    // ─── IsSolved invariants ───
    // Contract: IsSolved=true iff TypeName != null OR all field types are solved.
    // This drives the GH #126 followup pinned-identity check and many cycle-rescue paths.

    [Test]
    public void IsSolved_EmptyAnonymous_True() {
        // An empty anonymous struct has no unsolved members → vacuously solved.
        var s = new StateStruct();
        Assert.IsTrue(s.IsSolved);
    }

    [Test]
    public void IsSolved_NamedStructWithSolvedField_True() {
        var s = StateStruct.Of("v", I32);
        s.TypeName = "t";
        Assert.IsTrue(s.IsSolved);
    }

    [Test]
    public void IsSolved_NamedStructEmpty_AlwaysTrue() {
        // Per spec: "Named types are always solved" (TypeName != null short-circuit).
        var s = new StateStruct();
        s.TypeName = "t";
        Assert.IsTrue(s.IsSolved);
    }

    [Test]
    public void IsSolved_AnonymousWithSolvedPrimitives_True() {
        var s = StateStruct.Of(("a", I32), ("b", Real));
        Assert.IsTrue(s.IsSolved);
    }

    [Test]
    public void IsSolved_AnonymousWithUnsolvedField_False() {
        var s = new StateStruct();
        var unsolved = TicNode.CreateNamedNode("x", ConstraintsState.Empty);
        s.AddField("x", unsolved);
        Assert.IsFalse(s.IsSolved);
    }

    [Test]
    public void IsSolved_NamedStructWithUnsolvedField_StillTrue() {
        // Critical for the GH #126 followup pinned-identity rule:
        // "TypeName != null" short-circuits IsSolved regardless of inner field states.
        // This is by design — named types are externally declared, the field constraints
        // are obligations the user code must satisfy, but the type itself is "solved".
        var s = new StateStruct();
        s.AddField("x", TicNode.CreateNamedNode("x", ConstraintsState.Empty));
        s.TypeName = "t";
        Assert.IsTrue(s.IsSolved);
    }

    // ─── IsMutable invariants ───
    // Contract: IsMutable = (TypeName == null). Named types are not mutable —
    // setting TypeName commits to a declared identity.

    [Test]
    public void IsMutable_Anonymous_True() {
        Assert.IsTrue(new StateStruct().IsMutable);
    }

    [Test]
    public void IsMutable_Named_False() {
        var s = new StateStruct();
        s.TypeName = "t";
        Assert.IsFalse(s.IsMutable);
    }

    [Test]
    public void IsMutable_NamedThenCleared_TrueAgain() {
        // TypeName is a settable property — if cleared, mutability returns.
        // (Not used by current code paths, but the contract must hold.)
        var s = new StateStruct();
        s.TypeName = "t";
        Assert.IsFalse(s.IsMutable);
        s.TypeName = null;
        Assert.IsTrue(s.IsMutable);
    }

    // ─── IsOpen vs IsFrozen ───
    // IsOpen: caller (e.g. SetFieldAccess) may add more fields → width-polymorphism.
    // IsFrozen: LCA-computed result — fields are fixed at the snapshot.
    // The two are orthogonal: a closed-but-not-frozen struct is the literal default.

    [Test]
    public void DefaultStruct_IsNotOpen() =>
        Assert.IsFalse(new StateStruct().IsOpen);

    [Test]
    public void OpenStruct_StaysOpenAfterAddField() {
        var s = new StateStruct(isOpen: true);
        s.AddField("v", TicNode.CreateTypeVariableNode(I32));
        Assert.IsTrue(s.IsOpen);
    }

    [Test]
    public void DefaultStruct_NotFrozen() =>
        Assert.IsFalse(new StateStruct().IsFrozen);

    // ─── TypeName preservation across structural identity ───
    // Foundation for Bug D/G fixes — TypeName must SURVIVE struct copies/snapshots.

    [Test]
    public void TypeName_SettableOnceConstructed() {
        var s = StateStruct.Of("v", I32);
        Assert.IsNull(s.TypeName);
        s.TypeName = "t";
        Assert.AreEqual("t", s.TypeName);
    }

    [Test]
    public void TypeName_PreservedAcrossGetNonReferenced() {
        // GetNonReferenced creates a COPY where fields are dereferenced (RefTo chains
        // collapsed). The COPY must preserve TypeName — without this, recursive μ-types
        // would lose identity at every walk.
        var s = StateStruct.Of("v", I32);
        s.TypeName = "t";
        var copy = s.GetNonReferenced();
        Assert.IsInstanceOf<StateStruct>(copy);
        Assert.AreEqual("t", ((StateStruct)copy).TypeName);
    }

    // ─── IsOptionalSourced propagation ───

    [Test]
    public void IsOptionalSourced_DefaultsFalse() =>
        Assert.IsFalse(new StateStruct().IsOptionalSourced);

    [Test]
    public void IsOptionalSourced_PreservedAcrossGetNonReferenced() {
        var s = StateStruct.Of("v", I32);
        s.IsOptionalSourced = true;
        var copy = s.GetNonReferenced();
        Assert.IsInstanceOf<StateStruct>(copy);
        Assert.IsTrue(((StateStruct)copy).IsOptionalSourced);
    }

    // ─── ICompositeState contract ───

    [Test]
    public void MemberCount_EqualsFieldsCount() {
        var s = StateStruct.Of(("a", I32), ("b", Real));
        Assert.AreEqual(s.FieldsCount, s.MemberCount);
    }

    [Test]
    public void GetMember_ReturnsNonReferencedNode() {
        // GetMember is the iteration interface used by Pull/Push/Destruction recursion.
        // It MUST return GetNonReference() of the underlying field node — to avoid
        // recursive walks through RefTo chains that the caller already collapses.
        var target = TicNode.CreateTypeVariableNode(I32);
        var refToTarget = TicNode.CreateNamedNode("v_ref", new StateRefTo(target));
        var s = new StateStruct();
        s.AddField("v", refToTarget);
        var member = s.GetMember(0);
        Assert.AreSame(target, member, "GetMember must return GetNonReference of stored node");
    }

    // ─── Equality / cycle scenarios ───

    [Test]
    public void StructsWithSameFields_AreStructurallyEqual() {
        // Cycle-aware Equals (line 253 onwards) — two named structs with same TypeName
        // are equal regardless of field instantiation details, two anonymous structs
        // are equal iff fields agree.
        var a = StateStruct.Of("v", I32);
        var b = StateStruct.Of("v", I32);
        Assert.IsTrue(a.Equals(b), "Anonymous structs with same single field must equal");
    }

    [Test]
    public void NamedStructs_SameName_Equal_RegardlessOfFields() {
        var a = StateStruct.Of("v", I32);
        a.TypeName = "t";
        var b = new StateStruct(); // empty fields but same name
        b.TypeName = "t";
        // Per Equals comment: "Two named structs with the same declared TypeName
        // are the same type by definition." This is the iso-recursive identity rule.
        Assert.IsTrue(a.Equals(b));
    }

    [Test]
    public void NamedStructs_DifferentNames_NotEqual() {
        // Nominal-typing rule: `type t1 = {v:int}` and `type t2 = {v:int}` are DISTINCT
        // types — Equals must reject regardless of shape match (Pierce TAPL §19).
        // Previously this fell through to structural comparison and returned true; the
        // bug-regression investigation traced production silent-merge of `y:t2 = x where
        // x:t1` to this Equals path being too lenient. Fix tightened both this Equals
        // and the parallel struct/CS Pull check in PullConstraintsFunctions.
        var a = StateStruct.Of("v", I32);
        a.TypeName = "t";
        var b = StateStruct.Of("v", I32);
        b.TypeName = "s";
        Assert.IsFalse(a.Equals(b),
            "Two distinct named structs with matching shape must NOT be Equals (nominal typing)");
    }

    [Test]
    public void NamedStructs_SameName_DifferentShape_Equal_ByNameShortCircuit() {
        // Two structs declared as the same `type t` are equal by name — even if their
        // current fields differ (cycle-rescued recursive types reach Equals with field
        // bookkeeping mid-resolution). The TypeName short-circuit is critical.
        var a = StateStruct.Of("v", I32);
        a.TypeName = "t";
        var b = new StateStruct();
        b.TypeName = "t"; // No fields yet — different field-count, same name
        Assert.IsTrue(a.Equals(b),
            "Same-name structs are Equals via short-circuit (named-type identity)");
    }

    [Test]
    public void NamedVsAnonymous_StructurallyCompared() {
        // When at least one side lacks a TypeName, fall-through to structural comparison
        // is preserved — this is the row-polymorphism / open-struct path used by
        // SetFieldAccess generic-function flow. Names identity rule applies only when
        // BOTH sides have a name.
        var named = StateStruct.Of("v", I32);
        named.TypeName = "t";
        var anon = StateStruct.Of("v", I32);
        Assert.IsTrue(named.Equals(anon),
            "Named-vs-anonymous compared structurally — preserved fall-through");
    }
}
