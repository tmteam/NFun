namespace NFun.UnitTests.TicTests.StateExtensions;

using NFun.Tic.Algebra;
using NFun.Tic.SolvingStates;
using NUnit.Framework;
using static LcaTestTools;
using static SolvingStates;
using static Tic.SolvingStates.StatePrimitive;

/// <summary>
/// Unit-level algebra tests for None interactions.
/// Tests LCA, FitsInto, and CanBePessimisticConvertedTo with None.
/// These are direct algebra tests — no solver involved.
/// </summary>
public class NoneAlgebraTest {

    #region CanBePessimisticConvertedTo: None → primitive

    [Test]
    public void None_CanConvertTo_None() =>
        Assert.IsTrue(None.CanBePessimisticConvertedTo(None));

    [Test]
    public void None_CanConvertTo_Any() =>
        Assert.IsTrue(None.CanBePessimisticConvertedTo(Any));

    [Test]
    public void None_CannotConvertTo_I32() =>
        Assert.IsFalse(None.CanBePessimisticConvertedTo(I32),
            "Bug A algebra: None should NOT be pessimistic-convertible to I32");

    [Test]
    public void None_CannotConvertTo_Bool() =>
        Assert.IsFalse(None.CanBePessimisticConvertedTo(Bool),
            "None should NOT be pessimistic-convertible to Bool");

    [Test]
    public void None_CannotConvertTo_Char() =>
        Assert.IsFalse(None.CanBePessimisticConvertedTo(Char));

    [Test]
    public void None_CannotConvertTo_Real() =>
        Assert.IsFalse(None.CanBePessimisticConvertedTo(Real));

    [Test]
    public void None_CannotConvertTo_U8() =>
        Assert.IsFalse(None.CanBePessimisticConvertedTo(U8));

    [Test]
    public void I32_CannotConvertTo_None() =>
        Assert.IsFalse(I32.CanBePessimisticConvertedTo(None));

    [Test]
    public void Bool_CannotConvertTo_None() =>
        Assert.IsFalse(Bool.CanBePessimisticConvertedTo(None));

    #endregion

    #region FitsInto: None → type

    [Test]
    public void None_FitsInto_OptI32() =>
        Assert.IsTrue(None.FitsInto(Optional(I32)),
            "None should fit into opt(I32)");

    [Test]
    public void None_FitsInto_OptBool() =>
        Assert.IsTrue(None.FitsInto(Optional(Bool)),
            "None should fit into opt(Bool)");

    [Test]
    public void None_FitsInto_OptReal() =>
        Assert.IsTrue(None.FitsInto(Optional(Real)),
            "None should fit into opt(Real)");

    [Test]
    public void None_FitsInto_None() =>
        Assert.IsTrue(None.FitsInto(None),
            "None should fit into None");

    [Test]
    public void None_FitsInto_Any() =>
        Assert.IsTrue(None.FitsInto(Any),
            "None should fit into Any");

    [Test]
    public void None_DoesNotFitInto_I32() =>
        Assert.IsFalse(None.FitsInto(I32),
            "Bug A algebra: None should NOT fit into I32");

    [Test]
    public void None_DoesNotFitInto_Bool() =>
        Assert.IsFalse(None.FitsInto(Bool),
            "None should NOT fit into Bool");

    [Test]
    public void None_DoesNotFitInto_Char() =>
        Assert.IsFalse(None.FitsInto(Char),
            "None should NOT fit into Char");

    [Test]
    public void None_DoesNotFitInto_Real() =>
        Assert.IsFalse(None.FitsInto(Real),
            "None should NOT fit into Real");

    [Test]
    public void None_DoesNotFitInto_ArrayOfI32() =>
        Assert.IsFalse(None.FitsInto(Array(I32)),
            "None should NOT fit into I32[]");

    [Test]
    public void None_FitsInto_OptArrayOfI32() =>
        Assert.IsTrue(None.FitsInto(Optional(Array(I32))),
            "None should fit into opt(I32[])");

    #endregion

    #region FitsInto: None → ConstraintsState

    [Test]
    public void None_FitsInto_EmptyConstraints() {
        // Empty constraints = unconstrained generic T — None should fit (T could be None)
        Assert.IsTrue(None.FitsInto(EmptyConstraints),
            "None should fit into empty constraints (unconstrained T)");
    }

    [Test]
    public void None_DoesNotFitInto_ConstraintWithI32Descendant() {
        // Constraint [I32..Real] — None is not a numeric type, should not fit
        var c = Constrains(I32, Real, false);
        Assert.IsFalse(None.FitsInto(c),
            "Bug A: None should NOT fit into [I32..Real] constraint");
    }

    [Test]
    public void None_DoesNotFitInto_ConstraintWithU8Descendant() {
        var c = Constrains(U8, Real, false);
        Assert.IsFalse(None.FitsInto(c),
            "None should NOT fit into [U8..Real] constraint");
    }

    [Test]
    public void None_DoesNotFitInto_ComparableConstraint() {
        var c = Constrains(null, null, true);
        Assert.IsFalse(None.FitsInto(c),
            "None should NOT fit into comparable constraint (None is not comparable)");
    }

    [Test]
    public void None_FitsInto_ConstraintWithAnyAncestor() {
        // [..Any] — None ≤ Any, so None fits
        var c = Constrains(null, Any, false);
        Assert.IsTrue(None.FitsInto(c),
            "None should fit into [..Any] constraint");
    }

    #endregion

    #region LCA with None (already tested in LcaOptionalTest, but key ones for bug context)

    [Test]
    public void LCA_None_I32_IsOptI32() =>
        AssertLca(None, I32, Optional(I32));

    [Test]
    public void LCA_None_OptI32_IsOptI32() =>
        // None ^ opt(I32) = opt(I32) — no double wrap
        AssertLca(None, Optional(I32), Optional(I32));

    [Test]
    public void LCA_None_None_IsNone() =>
        AssertLca(None, None, None);

    [Test]
    public void LCA_OptI32_OptReal_IsOptReal() =>
        // Covariant: opt(I32) ^ opt(Real) = opt(I32 ^ Real) = opt(Real)
        AssertLca(Optional(I32), Optional(Real), Optional(Real));

    [Test]
    public void LCA_None_ConstraintsU8toReal() {
        // LCA(None, [U8..Real]) — what does this produce?
        // Should be opt([U8..Real]) or opt(something) — NOT a bare constraints state
        var c = Constrains(U8, Real, false);
        var result = None.Lca(c);
        // The result should be Optional-wrapped
        Assert.IsInstanceOf<StateOptional>(result,
            $"LCA(None, [U8..Real]) should be Optional but got {result} ({result.GetType().Name})");
    }

    [Test]
    public void LCA_None_EmptyConstraints() {
        // LCA(None, empty_CS) = None. Empty CS = bottom type, LCA(None, bottom) = None.
        var c = EmptyConstraints;
        var result = None.Lca(c);
        Assert.AreEqual(None, result,
            $"LCA(None, empty_constraints) should be None but got {result}");
    }

    #endregion

    #region OptI32 FitsInto checks (Bug C related)

    [Test]
    public void OptI32_FitsInto_OptReal() =>
        Assert.IsTrue(Optional(I32).FitsInto(Optional(Real)),
            "opt(I32) should fit into opt(Real) — covariant widening");

    [Test]
    public void OptI32_DoesNotFitInto_I32() =>
        Assert.IsFalse(Optional(I32).FitsInto(I32),
            "opt(I32) should NOT fit into I32 — implicit unwrap is invalid");

    [Test]
    public void OptI32_FitsInto_Any() =>
        Assert.IsTrue(Optional(I32).FitsInto(Any),
            "opt(I32) should fit into Any");

    #endregion
}
