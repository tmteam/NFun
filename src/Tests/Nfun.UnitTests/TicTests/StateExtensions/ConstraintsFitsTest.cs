namespace NFun.UnitTests.TicTests.StateExtensions;

using TestTools;
using Tic;
using NUnit.Framework;
using static SolvingStates;
using static Tic.SolvingStates.StatePrimitive;

public class ConstraintsFitsTest {

    [Test]
    public void PrimitiveFits_returnTrue1() =>
        Any.FitsInto(EmptyConstrains).AssertTrue();

    [Test]
    public void PrimitiveFits_returnTrue2() =>
        Ip.FitsInto(EmptyConstrains).AssertTrue();

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
        EmptyConstrains.FitsInto(Constrains(U8, Real, false)).AssertFalse();

    [Test]
    public void PrimitiveFits_returnFalse2() =>
        U32.FitsInto(Constrains(U64, Real, false)).AssertFalse();

    [Test]
    public void PrimitiveFits_returnFalse3() =>
        Bool.FitsInto(Constrains(null, null, true)).AssertFalse();

    [Test]
    public void ConstrainsFits_returnTrue() =>
        Constrains(U8, Real, false).FitsInto(EmptyConstrains).AssertTrue();

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
            .FitsInto(Constrains(Array(EmptyConstrains), null, false))
            .AssertTrue();

    [Test]
    public void ArrayFits_returnTrue3() =>
        Array(U32)
            .FitsInto(Array(EmptyConstrains))
            .AssertTrue();

    [Test]
    public void ArrayFits_returnTrue4() =>
        Array(Constrains(Array(U8), null, false))
            .FitsInto(Array(EmptyConstrains))
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
            .FitsInto(Array(EmptyConstrains))
            .AssertTrue();

    [Test]
    public void ArrayFits_returnTrue8() =>
        Array(Array(EmptyConstrains))
            .FitsInto(Array(EmptyConstrains))
            .AssertTrue();

    [Test]
    public void FunFits_returnsFalse() =>
        Fun(new[] { Any }, Any)
            .FitsInto(Constrains(Array(EmptyConstrains), null, false))
            .AssertFalse();

    [Test]
    public void FunFits_returnsTrue() =>
        Fun(new[] { Real }, U24)
            .FitsInto(Constrains(Fun(new[] { Any }, U16), null, false))
            .AssertTrue();

    [Test]
    public void FunFits_returnsTrue2() {
        var constrains = Constrains(Fun(EmptyConstrains, Constrains(U16, null, false)), null, false);
        var target = Fun(new[] { Any }, U16);
        target.FitsInto(constrains).AssertTrue();
    }

    [Test]
    public void FunFits_returnsTrue3() {
        var constrains = Constrains(Fun(Constrains(U16, Real, false), Constrains(U16, Real, false)), null, false);
        var target = Fun(Constrains(U32, U64, false), Constrains(U32, U64, false));
        target.FitsInto(constrains).AssertTrue();
    }

    [Test]
    public void FunFits_returnsTrue4() {
        var constrains = Constrains(Fun(EmptyConstrains, EmptyConstrains), null, false);
        var target = Fun(new[] { Any }, U16);
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

    [Test]
    public void StructFits_returnsTrue2() {
        var constrains = Constrains(Struct(("a", I32), ("b", I32)));
        var target = Struct("a", I32);
        target.FitsInto(constrains).AssertFalse();
    }

    [Test]
    public void StructFits_returnsTrue3() {
        var constrains = Constrains(Struct("a", I32));
        var target = EmptyStruct();
        target.FitsInto(constrains).AssertFalse();
    }

}
