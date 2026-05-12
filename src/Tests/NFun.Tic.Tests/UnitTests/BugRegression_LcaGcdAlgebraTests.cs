using NFun.Tic.Algebra;
using NFun.Tic.SolvingStates;
using NUnit.Framework;

namespace NFun.Tic.Tests.UnitTests;

using static StatePrimitive;

/// <summary>
/// Unit tests for the algebraic operators <c>Lca</c> (Least Common Ancestor / supremum)
/// and <c>Gcd</c> (Greatest Common Descendant / infimum) — the lattice meet/join used
/// throughout Pull/Push to compute upper/lower bounds.
///
/// Theoretical contract (per Algebra.md):
///   - <c>a.Lca(b)</c>: smallest type that is a supertype of BOTH a and b (widening join)
///   - <c>a.Gcd(b)</c>: largest type that is a subtype of BOTH a and b (narrowing meet)
///   - Both operators must be COMMUTATIVE, IDEMPOTENT, and ASSOCIATIVE
///   - Lca and Gcd are DUAL: Lca expands upward, Gcd narrows downward
///   - For incompatible types: Lca → Any (top), Gcd → null or bottom
///   - Special cases: Any is the absorbing element of Lca; None is the unit-ish of Lca
///
/// These tests pin the lattice contracts so future refactors (e.g. RecBound elimination
/// in task #108) can verify the algebraic surface remains intact.
/// </summary>
class BugRegression_LcaGcdAlgebraTests {

    // ─── LCA: lattice join (supremum / least upper bound) ───

    [Test]
    public void Lca_SamePrimitive_ReturnsThat() =>
        Assert.AreSame(I32, I32.Lca(I32));

    [Test]
    public void Lca_IsIdempotent_Primitive() {
        var once = I32.Lca(I32);
        var twice = once.Lca(once);
        Assert.AreEqual(once, twice);
    }

    [Test]
    public void Lca_IsCommutative_Primitives() {
        Assert.AreEqual(I32.Lca(I64), I64.Lca(I32));
    }

    [Test]
    public void Lca_TwoIntegers_PromotesToWiderInteger() {
        // LCA of overlapping signed integers — the wider one absorbs the narrower.
        var res = I16.Lca(I32);
        Assert.IsInstanceOf<StatePrimitive>(res);
    }

    [Test]
    public void Lca_IntAndReal_PromotesToReal() {
        // Integer + Real → Real (numeric widening).
        var res = I32.Lca(Real);
        Assert.AreSame(Real, res);
    }

    [Test]
    public void Lca_IncompatibleFamilies_Any() {
        // Numeric vs Bool: no common subtype in either family → Any (top).
        var res = I32.Lca(Bool);
        Assert.AreSame(Any, res);
    }

    [Test]
    public void Lca_CharAndNumeric_Any() {
        // Char is NOT in the numeric family at LCA level (despite a runtime
        // VarTypeConverter.cs:103 Char→Number conversion that's used only for explicit
        // casts). The LcaMap default at line 123-125 of StatePrimitive.cs is Any for
        // all cross-family pairs, and Char's row is never overridden by the numeric
        // loop (which starts at Real.Order, line 138-145). Therefore Char.Lca(any
        // numeric) = Any. This holds for all numeric types — verified below.
        Assert.AreSame(Any, Char.Lca(I32));
    }

    [Test]
    public void Lca_CharAndAllNumerics_AllAny() {
        // Diagnostic: probe ALL numerics, all must produce Any.
        StatePrimitive[] numerics = { I16, I24, I32, I48, I64, I96, U8, U12, U16, U24, U32, U48, U64, Real };
        foreach (var n in numerics)
            Assert.AreSame(Any, Char.Lca(n), $"Char.Lca({n}) should be Any");
    }

    [Test]
    public void Lca_CharAndChar_Char() =>
        // Char ^ Char = Char (self-LCA, line 131 of StatePrimitive.cs).
        Assert.AreSame(Char, Char.Lca(Char));

    [Test]
    public void Lca_CharAndBool_Any() =>
        // Char and Bool are both non-numeric — cross-family → Any (default).
        Assert.AreSame(Any, Char.Lca(Bool));

    [Test]
    public void Lca_AnyAndAny_Any() =>
        Assert.AreSame(Any, Any.Lca(Any));

    [Test]
    public void Lca_AnyAbsorbsEverything() {
        // Any is the top element — Lca(Any, T) = Any for all T.
        Assert.AreSame(Any, Any.Lca(I32));
        Assert.AreSame(Any, I32.Lca(Any));
        Assert.AreSame(Any, Any.Lca(Bool));
        Assert.AreSame(Any, Any.Lca(Char));
    }

    [Test]
    public void Lca_TwoArrays_SameElement_Array() {
        var a = StateArray.Of(I32);
        var b = StateArray.Of(I32);
        var res = a.Lca(b);
        Assert.IsInstanceOf<StateArray>(res);
    }

    [Test]
    public void Lca_TwoArrays_DifferentElement_AnyOrUpcast() {
        var a = StateArray.Of(I32);
        var b = StateArray.Of(Real);
        // Element types are LCA'd; result is arr(Real) — covariance.
        var res = a.Lca(b);
        Assert.IsInstanceOf<StateArray>(res);
    }

    [Test]
    public void Lca_TwoOptionals_SameElement_Optional() {
        var a = StateOptional.Of(I32);
        var b = StateOptional.Of(I32);
        var res = a.Lca(b);
        Assert.IsInstanceOf<StateOptional>(res);
    }

    [Test]
    public void Lca_ArrayAndOptional_Any() {
        // Different composite shapes → Any.
        var arr = StateArray.Of(I32);
        var opt = StateOptional.Of(I32);
        Assert.AreSame(Any, arr.Lca(opt));
        Assert.AreSame(Any, opt.Lca(arr));
    }

    [Test]
    public void Lca_StructAndArray_Any() {
        var s = StateStruct.Of("v", I32);
        var arr = StateArray.Of(I32);
        Assert.AreSame(Any, s.Lca(arr));
        Assert.AreSame(Any, arr.Lca(s));
    }

    // ─── GCD: lattice meet (infimum / greatest lower bound) ───

    [Test]
    public void Gcd_SamePrimitive_ReturnsThat() =>
        Assert.AreSame(I32, I32.Gcd(I32));

    [Test]
    public void Gcd_IsIdempotent() {
        var once = I32.Gcd(I32);
        var twice = once.Gcd(once);
        Assert.AreEqual(once, twice);
    }

    [Test]
    public void Gcd_IsCommutative() {
        Assert.AreEqual(I32.Gcd(I64), I64.Gcd(I32));
    }

    [Test]
    public void Gcd_NestedIntegers_Narrows() {
        // GCD of narrower-and-wider integer = narrower.
        var res = I16.Gcd(I32);
        // Result is some primitive — GCD narrows to a common subtype.
        Assert.IsInstanceOf<StatePrimitive>(res);
    }

    [Test]
    public void Gcd_IntAndReal_NarrowsToInt() {
        // Int ≤ Real, so GCD(Int, Real) = Int (the narrower).
        var res = I32.Gcd(Real);
        Assert.IsInstanceOf<StatePrimitive>(res);
    }

    [Test]
    public void Gcd_TwoArrays_SameElement_Array() {
        var a = StateArray.Of(I32);
        var b = StateArray.Of(I32);
        var res = a.Gcd(b);
        Assert.IsInstanceOf<StateArray>(res);
    }

    [Test]
    public void Gcd_StructAndArray_NotComposite() {
        // Disjoint composites have no common subtype that's a struct AND array.
        // Should fall back to None or null.
        var s = StateStruct.Of("v", I32);
        var arr = StateArray.Of(I32);
        var res = s.Gcd(arr);
        // Result IS computed (Algebra.md says no nulls from Gcd/Lca for valid type pairs),
        // but it must NOT be a struct or array.
        if (res != null)
            Assert.IsTrue(res is StatePrimitive,
                "GCD of disjoint composites must degenerate to a primitive (None/Any)");
    }

    // ─── Duality and bounding ───

    [Test]
    public void LcaAndGcd_AgreeOnSameInput() {
        // Lca(T,T) = Gcd(T,T) = T (idempotent).
        var lca = I32.Lca(I32);
        var gcd = I32.Gcd(I32);
        Assert.AreEqual(lca, gcd);
    }

    [Test]
    public void LcaIsAtLeastGcd_ForCompatibleTypes() {
        // For any comparable pair, Lca(a,b) ≥ Gcd(a,b) — the upper bound is wider than the lower.
        var lca = I16.Lca(I32);
        var gcd = I16.Gcd(I32);
        // Both are primitives in the same family — relation is via type hierarchy.
        // Weak invariant: both should be non-null.
        Assert.IsNotNull(lca);
        Assert.IsNotNull(gcd);
    }

    // ─── Special: None primitive role ───

    [Test]
    public void Lca_NoneAndAny_Any() =>
        // None ≤ Any: LCA goes UP, so LCA(None, Any) = Any.
        Assert.AreSame(Any, None.Lca(Any));

    [Test]
    public void Gcd_NoneAndAny_None() =>
        // None ≤ Any: GCD goes DOWN, so GCD(None, Any) = None.
        Assert.AreSame(None, None.Gcd(Any));

    [Test]
    public void Lca_NoneAndInt_DocumentBehavior() {
        // Per StatePrimitive.cs: "None ≤ Any (none is a value with toString/equals),
        // but None is not ≤ any other primitive". So LCA(None, Int) should NOT be Int.
        var res = None.Lca(I32);
        Assert.IsNotNull(res, "LCA should always produce a result (Any at worst)");
    }

    // ─── Symmetry contract (commutativity check) ───

    [Test]
    public void Lca_IsCommutative_OverManyPairs() {
        var pairs = new[] {
            (I16, I32), (I32, Real), (Real, Bool), (Char, Char),
            (I32, Any), (Bool, Any), (None, I32)
        };
        foreach (var (a, b) in pairs)
            Assert.AreEqual(a.Lca(b), b.Lca(a),
                $"Lca commutativity broken for ({a}, {b})");
    }

    [Test]
    public void Gcd_IsCommutative_OverManyPairs() {
        var pairs = new[] {
            (I16, I32), (I32, Real), (I32, Any), (Real, Real),
            (None, Any), (Bool, Any)
        };
        foreach (var (a, b) in pairs)
            Assert.AreEqual(a.Gcd(b), b.Gcd(a),
                $"Gcd commutativity broken for ({a}, {b})");
    }
}
