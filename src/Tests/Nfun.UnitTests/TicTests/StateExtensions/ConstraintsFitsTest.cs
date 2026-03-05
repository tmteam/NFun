namespace NFun.UnitTests.TicTests.StateExtensions;

using TestTools;
using Tic;
using NUnit.Framework;
using static SolvingStates;
using static Tic.SolvingStates.StatePrimitive;

public class ConstraintsFitsTest {

    [Test]
    public void PrimitiveFits_returnTrue1() =>
        Any.FitsInto(EmptyConstraints).AssertTrue();

    [Test]
    public void PrimitiveFits_returnTrue2() =>
        Ip.FitsInto(EmptyConstraints).AssertTrue();

    [Test]
    public void PrimitiveFits_returnTrue3() =>
        U32.FitsInto(Constrains(U32, Real, false)).AssertTrue();

    [Test]
    public void PrimitiveFits_returnTrue4() =>
        U64.FitsInto(Constrains(U32, Real, false)).AssertTrue();

    [Test]
    public void PrimitiveFits_returnTrue5() =>
        U32.FitsInto(Constrains(U32, Real, true)).AssertTrue();

    [Test]
    public void PrimitiveFits_returnTrue6() =>
        U32.FitsInto(Constrains(null, null, true)).AssertTrue();

    [Test]
    public void PrimitiveFits_returnTrue7() =>
        U32.FitsInto(Constrains(U32, null, true)).AssertTrue();

    [Test]
    public void PrimitiveFits_returnTrue8() =>
        U32.FitsInto(Constrains(null, Real, true)).AssertTrue();

    [Test]
    public void PrimitiveFits_returnFalse() =>
        EmptyConstraints.FitsInto(Constrains(U8, Real, false)).AssertFalse();

    [Test]
    public void PrimitiveFits_returnFalse2() =>
        U32.FitsInto(Constrains(U64, Real, false)).AssertFalse();

    [Test]
    public void PrimitiveFits_returnFalse3() =>
        Bool.FitsInto(Constrains(null, null, true)).AssertFalse();

    [Test]
    public void ConstrainsFits_returnTrue() =>
        Constrains(U8, Real, false).FitsInto(EmptyConstraints).AssertTrue();

    [Test]
    public void ConstrainsFits_returnFalse() =>
        Constrains(U64, Real, false).FitsInto(U32).AssertFalse();

    [Test]
    public void ArrayFits_returnFalse() =>
        Array(Constrains(U8, null, false))
            .FitsInto(Constrains(Array(Any), null, false))
            .AssertFalse();

    [Test]
    public void ArrayFits_returnTrue() =>
        Array(Constrains(U32, null, false))
            .FitsInto(Constrains(Array(U8), null, false))
            .AssertTrue();

    [Test]
    public void ArrayFits_returnTrue2() =>
        Array(Constrains(U8, Real, false))
            .FitsInto(Constrains(Array(EmptyConstraints), null, false))
            .AssertTrue();

    [Test]
    public void ArrayFits_returnTrue3() =>
        Array(U32)
            .FitsInto(Array(EmptyConstraints))
            .AssertTrue();

    [Test]
    public void ArrayFits_returnTrue4() =>
        Array(Constrains(Array(U8), null, false))
            .FitsInto(Array(EmptyConstraints))
            .AssertTrue();

    [Test]
    public void ArrayFits_returnTrue5() =>
        Array(U32).FitsInto(Constrains(Array(U8), null, false)).AssertTrue();

    [Test]
    public void ArrayFits_returnTrue6() =>
        Array(U32)
            .FitsInto(Array(Constrains(U32, Real, true)))
            .AssertTrue();

    [Test]
    public void ArrayFits_returnTrue7() =>
        Array(Constrains(U32, Real, true))
            .FitsInto(Array(EmptyConstraints))
            .AssertTrue();

    [Test]
    public void ArrayFits_returnTrue8() =>
        Array(Array(EmptyConstraints))
            .FitsInto(Array(EmptyConstraints))
            .AssertTrue();

    [Test]
    public void FunFits_returnsFalse() =>
        Fun(new[] { Any }, Any)
            .FitsInto(Constrains(Array(EmptyConstraints), null, false))
            .AssertFalse();

    [Test]
    public void FunFits_returnsTrue() =>
        Fun(new[] { Real }, U24)
            .FitsInto(Constrains(Fun(new[] { Any }, U16), null, false))
            .AssertTrue();

    // CanBeFitConverted semantics: ∀t∈to, ∃d∈desc: d ≤ t
    // These tests had wrong expectations — they passed only due to a copy-paste bug
    // that skipped arg checking entirely. With correct arg checks, they are false.

    // Fun(Any→U16).Fits(C[desc=Fun(EC→C[U16])])
    // Args (contra): CanBeFitConverted(Any, EmptyConstraints) = false
    // because ∀t∈EmptyConstraints: Any ≤ t is false (Any ≰ Bool)
    [Test]
    public void FunFits_EmptyConstraintArg_returnsFalse() {
        var constrains = Constrains(Fun(EmptyConstraints, Constrains(U16, null, false)), null, false);
        var target = Fun(new[] { Any }, U16);
        target.FitsInto(constrains).AssertFalse();
    }

    // Fun(C[U32..U64]→C[U32..U64]).Fits(C[desc=Fun(C[U16..Real]→C[U16..Real])])
    // Args (contra): CanBeFitConverted(U32, C[U16..Real]) = false
    // because ∀t∈[U16..Real]: U32 ≤ t is false (U32 ≰ U16)
    [Test]
    public void FunFits_IncompatibleConstraintArgs_returnsFalse() {
        var constrains = Constrains(Fun(Constrains(U16, Real, false), Constrains(U16, Real, false)), null, false);
        var target = Fun(Constrains(U32, U64, false), Constrains(U32, U64, false));
        target.FitsInto(constrains).AssertFalse();
    }

    // Fun(Any→U16).Fits(C[desc=Fun(EC→EC)])
    // Same as above — EmptyConstraints arg makes it false
    [Test]
    public void FunFits_BothEmptyConstraintArgs_returnsFalse() {
        var constrains = Constrains(Fun(EmptyConstraints, EmptyConstraints), null, false);
        var target = Fun(new[] { Any }, U16);
        target.FitsInto(constrains).AssertFalse();
    }

    // Valid true case: Fun(U32→U16).Fits(C[desc=Fun(Real→U8)])
    // Return: CanBeFitConverted(U8, U16) = true (U8 ≤ U16)
    // Args (contra): CanBeFitConverted(U32, Real) = true (U32 ≤ Real)
    [Test]
    public void FunFits_CompatibleConcreteArgs_returnsTrue() {
        var constrains = Constrains(Fun(new[] { Real }, U8), null, false);
        var target = Fun(new[] { U32 }, U16);
        target.FitsInto(constrains).AssertTrue();
    }

    [Test]
    public void FunFits_returnsTrue5() {
        var constrains = Constrains(Fun( Any,
            Fun(Any,
                Fun(Any,
                    U24))), null, false);
        var target =
            Fun(Constrains(U24, Real, false) ,
                Fun(Constrains(U24, Real, false),
                    Fun(Constrains(U24, Real, false),
                        Constrains(U24, Real, false))));
        target.FitsInto(constrains).AssertTrue();
    }

    // Regression: CanBeFitConverted(StateFun,StateFun) had a copy-paste bug
    // where both arg variables read from desc instead of desc vs to.
    // Fun(Bool→I32) should NOT fit C[desc=Fun(Real→I32)] because
    // contravariant check requires desc.arg ≤ target.arg, i.e. Real ≤ Bool — false.
    [Test]
    public void FunFits_IncompatibleArgs_returnsFalse() =>
        Fun(new[] { Bool }, I32)
            .FitsInto(Constrains(Fun(new[] { Real }, I32), null, false))
            .AssertFalse();

    [Test]
    public void TextFits_into_Comparable_returnsTrue() {
        var constrains = Constrains(isComparable: true);
        var target = Array(Char);
        target.FitsInto(constrains).AssertTrue();
    }


    [Test]
    public void StructFits_returnsFalse1() {
        var constrains = Constrains(EmptyStruct());
        var target = Struct("a", I32);
        target.FitsInto(constrains).AssertFalse();
    }

    [Test]
    public void StructFits_returnsFalse2() {
        var constrains = Constrains(Struct("a", I32));
        var target = Struct(("a", I32), ("b", I32));
        target.FitsInto(constrains).AssertFalse();
    }

    [Test]
    public void StructFits_returnsTrue1() {
        var constrains = Constrains(Struct("a", I32));
        var target = Struct("a", I32);
        target.FitsInto(constrains).AssertTrue();
    }

    // Width subtyping: desc={a,b} has more fields = subtype. Target={a} is supertype.
    // desc ≤ target, so target satisfies C[desc={a,b}].
    [Test]
    public void StructFits_returnsTrue2() {
        var constrains = Constrains(Struct(("a", I32), ("b", I32)));
        var target = Struct("a", I32);
        target.FitsInto(constrains).AssertTrue();
    }

    // Width subtyping: desc={a} ≤ {} (any struct fits into empty struct).
    [Test]
    public void StructFits_returnsTrue3() {
        var constrains = Constrains(Struct("a", I32));
        var target = EmptyStruct();
        target.FitsInto(constrains).AssertTrue();
    }

}
