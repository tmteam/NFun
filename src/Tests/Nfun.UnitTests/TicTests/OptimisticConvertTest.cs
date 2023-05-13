namespace NFun.UnitTests.TicTests;

using NUnit.Framework;
using TestTools;
using Tic;
using Tic.SolvingStates;
using static Tic.SolvingStates.StatePrimitive;

public class OptimisticConvertTest {
    [TestCase(PrimitiveTypeName.I64, PrimitiveTypeName.Real)]
    [TestCase(PrimitiveTypeName.Char, PrimitiveTypeName.Any)]
    public void PrimitiveToPrimitive_returnsTrue(PrimitiveTypeName from, PrimitiveTypeName to) =>
        new StatePrimitive(from).CanBeConvertedOptimisticTo(new StatePrimitive(to))
            .AssertTrue();

    [TestCase(PrimitiveTypeName.I64, PrimitiveTypeName.I32)]
    [TestCase(PrimitiveTypeName.Char, PrimitiveTypeName.Ip)]
    public void PrimitiveToPrimitive_returnsFalse(PrimitiveTypeName from, PrimitiveTypeName to) =>
        new StatePrimitive(from).CanBeConvertedOptimisticTo(new StatePrimitive(to))
            .AssertFalse();

    [Test]
    public void FromAnyToConstraints_returnsFalse() =>
        Any.CanBeConvertedOptimisticTo(ConstrainsState.Of(I64, Real))
            .AssertFalse();

    [Test]
    public void FromAnyToConstraints_returnsTrue() =>
        Any.CanBeConvertedOptimisticTo(ConstrainsState.Of(Any))
            .AssertTrue();

    [Test]
    public void FromPrimitiveToConstraints_returnsFalse2() =>
        Char.CanBeConvertedOptimisticTo(ConstrainsState.Of(I32, Real))
            .AssertFalse();

    [Test]
    public void FromPrimitiveToConstraints_returnsTrue() =>
        I32.CanBeConvertedOptimisticTo(ConstrainsState.Of(Real, Any))
            .AssertTrue();

    [Test]
    public void FromPrimitiveToConstraints_returnsTrue2() =>
        I32.CanBeConvertedOptimisticTo(ConstrainsState.Of(U8, Any))
            .AssertTrue();

    [Test]
    public void FromPrimitiveToConstraints_returnsTrue3() =>
        I32.CanBeConvertedOptimisticTo(ConstrainsState.Of(null, Any))
            .AssertTrue();

    [Test]
    public void FromPrimitiveToConstraints_returnsTrue4() =>
        I32.CanBeConvertedOptimisticTo(ConstrainsState.Of(Char)) // `from` can be of `any` type
            .AssertTrue();

    [Test]
    public void FromPrimitiveToConstraints_returnsTrue5() =>
        I32.CanBeConvertedOptimisticTo(ConstrainsState.Of(Char, Any))
            .AssertTrue();


    [Test]
    public void ConstrainsToConstraints_returnsFalse() =>
        ConstrainsState.Of(I64, Real)
            .CanBeConvertedOptimisticTo(ConstrainsState.Of(U8, I32))
            .AssertFalse();

    [Test]
    public void ConstrainsToConstraints_returnsFalse2() =>
        ConstrainsState.Of(U64)
            .CanBeConvertedOptimisticTo(ConstrainsState.Of(null, I32))
            .AssertFalse();

    [Test]
    public void ConstrainsToConstraints_returnsFalse3() =>
        ConstrainsState.Of(U64)
            .CanBeConvertedOptimisticTo(ConstrainsState.Of(null, I32))
            .AssertFalse();

    [Test]
    public void ConstrainsToConstraints_returnsTrue() =>
        ConstrainsState.Empty
            .CanBeConvertedOptimisticTo(ConstrainsState.Of(U8, Real))
            .AssertTrue();

    [Test]
    public void ConstrainsToConstraints_returnsTrue2() =>
        ConstrainsState.Of(Char)
            .CanBeConvertedOptimisticTo(ConstrainsState.Of(U8))
            .AssertTrue();

    [Test]
    public void ConstrainsToConstraints_returnsTrue3() =>
        ConstrainsState.Of(Ip)
            .CanBeConvertedOptimisticTo(ConstrainsState.Empty)
            .AssertTrue();

    [Test]
    public void ConstrainsToConstraints_returnsTrue4() =>
        ConstrainsState.Of(I32, I64)
            .CanBeConvertedOptimisticTo(ConstrainsState.Of(I64, I96))
            .AssertTrue();

    [Test]
    public void FromConstraintsToPrimitive_returnsFalse() =>
        ConstrainsState.Of(I32, Real)
            .CanBeConvertedOptimisticTo(Char)
            .AssertFalse();

    [Test]
    public void FromConstraintsToPrimitive_returnsFalse2() =>
        ConstrainsState.Of(null, Ip)
            .CanBeConvertedOptimisticTo(Char)
            .AssertFalse();

    [Test]
    public void FromConstraintsToPrimitive_returnsFalse3() =>
        ConstrainsState.Of(Real, Any)
            .CanBeConvertedOptimisticTo(I32)
            .AssertFalse();

    [Test]
    public void FromConstraintsToPrimitive_returnsTrue2() =>
        ConstrainsState.Empty
            .CanBeConvertedOptimisticTo(Ip)
            .AssertTrue();

    [Test]
    public void FromConstraintsToPrimitive_returnsTrue3() =>
        ConstrainsState.Of(U8)
            .CanBeConvertedOptimisticTo(Real)
            .AssertTrue();

    [Test]
    public void FromArrayToArray_returnsFalse() =>
        StateArray.Of(Real)
            .CanBeConvertedOptimisticTo(StateArray.Of(U8))
            .AssertFalse();

    [Test]
    public void FromArrayToArray_returnsTrue() =>
        StateArray.Of(U8)
            .CanBeConvertedOptimisticTo(StateArray.Of(Real))
            .AssertTrue();


    [Test]
    public void FromArrayConstraintsToPrimitive_returnsFalse() =>
        StateArray.Of(ConstrainsState.Of(I32, Real, false))
            .CanBeConvertedOptimisticTo(StateArray.Of(Char))
            .AssertFalse();

    [Test]
    public void FromArrayConstraintsToPrimitive_returnsFalse2() =>
        StateArray.Of(ConstrainsState.Of(null, Ip, false))
            .CanBeConvertedOptimisticTo(StateArray.Of(Char))
            .AssertFalse();

    [Test]
    public void FromArrayConstraintsToPrimitive_returnsFalse3() =>
        StateArray.Of(ConstrainsState.Of(Real, Any, false))
            .CanBeConvertedOptimisticTo(StateArray.Of(I32))
            .AssertFalse();

    [Test]
    public void FromArrayConstraintsToPrimitive_returnsTrue2() =>
        StateArray.Of(ConstrainsState.Of(null, null, false))
            .CanBeConvertedOptimisticTo(StateArray.Of(Ip))
            .AssertTrue();

    [Test]
    public void FromArrayConstraintsToPrimitive_returnsTrue3() =>
        StateArray.Of(ConstrainsState.Of(U8, null, false))
            .CanBeConvertedOptimisticTo(StateArray.Of(Real))
            .AssertTrue();

    //----
    [Test]
    public void FromArrayPrimitiveToConstraints_returnsTrue4() =>
        StateArray.Of(I32)
            .CanBeConvertedOptimisticTo(StateArray.Of(ConstrainsState.Of(Char, Any, false)))
            .AssertTrue();

    [Test]
    public void FromArrayPrimitiveToConstraints_returnsFalse2() =>
        StateArray.Of(Char)
            .CanBeConvertedOptimisticTo(StateArray.Of(ConstrainsState.Of(I32, Real, false)))
            .AssertFalse();

    [Test]
    public void FromArrayPrimitiveToConstraints_returnsTrue() =>
        StateArray.Of(I32)
            .CanBeConvertedOptimisticTo(StateArray.Of(ConstrainsState.Of(Real, Any, false)))
            .AssertTrue();

    [Test]
    public void FromArrayPrimitiveToConstraints_returnsTrue2() =>
        StateArray.Of(I32)
            .CanBeConvertedOptimisticTo(StateArray.Of(ConstrainsState.Of(U8, Any, false)))
            .AssertTrue();

    [Test]
    public void FromArrayPrimitiveToConstraints_returnsTrue3() =>
        StateArray.Of(I32)
            .CanBeConvertedOptimisticTo(StateArray.Of(ConstrainsState.Of(null, Any, false)))
            .AssertTrue();

    [Test]
    public void FromArrayPrimitiveToConstraints_returnsTrue5() =>
        StateArray.Of(I32)
            .CanBeConvertedOptimisticTo(StateArray.Of(ConstrainsState.Of(Char, null, false)))
            // `from` can be of `any` type
            .AssertTrue();

    [Test]
    public void ArrayConstrainsToConstraints_returnsFalse() =>
        StateArray.Of(ConstrainsState.Of(I64, Real, false))
            .CanBeConvertedOptimisticTo(StateArray.Of(ConstrainsState.Of(U8, I32, false)))
            .AssertFalse();

    [Test]
    public void ArrayConstrainsToConstraints_returnsFalse2() =>
        StateArray.Of(ConstrainsState.Of(U64, null, false))
            .CanBeConvertedOptimisticTo(StateArray.Of(ConstrainsState.Of(null, I32, false)))
            .AssertFalse();

    [Test]
    public void ArrayConstrainsToConstraints_returnsFalse3() =>
        StateArray.Of(ConstrainsState.Of(U64, null, false))
            .CanBeConvertedOptimisticTo(StateArray.Of(ConstrainsState.Of(null, I32, false)))
            .AssertFalse();

    [Test]
    public void ArrayConstrainsToConstraints_returnsTrue() =>
        StateArray.Of(ConstrainsState.Of(null, null, false))
            .CanBeConvertedOptimisticTo(StateArray.Of(ConstrainsState.Of(U8, Real, false)))
            .AssertTrue();

    [Test]
    public void ArrayConstrainsToConstraints_returnsTrue2() =>
        StateArray.Of(ConstrainsState.Of(Char, null, false))
            .CanBeConvertedOptimisticTo(StateArray.Of(ConstrainsState.Of(U8, null, false)))
            .AssertTrue();

    [Test]
    public void ArrayConstrainsToConstraints_returnsTrue3() =>
        StateArray.Of(ConstrainsState.Of(Ip, null, false))
            .CanBeConvertedOptimisticTo(StateArray.Of(ConstrainsState.Of(null, null, false)))
            .AssertTrue();

    [Test]
    public void ArrayConstrainsToConstraints_returnsTrue4() =>
        StateArray.Of(ConstrainsState.Of(I32, I64, false))
            .CanBeConvertedOptimisticTo(StateArray.Of(ConstrainsState.Of(I64, I96, false)))
            .AssertTrue();

    [Test]
    public void ArrayToPrimitive_returnsTrue() =>
        StateArray.Of(I32)
            .CanBeConvertedOptimisticTo(Any)
            .AssertTrue();

    [Test]
    public void ArrayToPrimitive_returnsFalse() =>
        StateArray.Of(I32)
            .CanBeConvertedOptimisticTo(Ip)
            .AssertFalse();

    [Test]
    public void PrimitiveToArray_returnsFalse() =>
        Ip.CanBeConvertedOptimisticTo(StateArray.Of(I32))
            .AssertFalse();

    [Test]
    public void ArrayToArray_returnsTrue() =>
        StateArray.Of(StateArray.Of(StateArray.Of(U8)))
            .CanBeConvertedOptimisticTo(StateArray.Of(ConstrainsState.Of(StateArray.Of(U8), null, false)))
            .AssertTrue();

    [Test]
    public void Fun1() =>
        StateFun.Of(ConstrainsState.Of(I32, Real), I32)
            .CanBeConvertedOptimisticTo(StateFun.Of(I64, Real))
            .AssertTrue();

    [Test]
    public void Fun2() =>
        StateFun.Of(ConstrainsState.Of(I32, Real), Real)
            .CanBeConvertedOptimisticTo(
                StateFun.Of(ConstrainsState.Of(I32, Real), ConstrainsState.Of(I32)))
            .AssertTrue();

    [Test]
    public void Fun3() =>
        StateFun.Of(Any, Real)
            .CanBeConvertedOptimisticTo(StateFun.Of(ConstrainsState.Of(I32, Real), ConstrainsState.Of(I32)))
            .AssertTrue();

    [Test]
    public void Fun4() =>
        StateFun.Of(ConstrainsState.Of(I32, Real), Real)
            .CanBeConvertedOptimisticTo(Any)
            .AssertTrue();

    [Test]
    public void Struct1() =>
        StateStruct.Of(("a", I32), ("b", I32))
            .CanBeConvertedOptimisticTo(StateStruct.Of("a", I32))
            .AssertTrue();

    [Test]
    public void Struct2() =>
        StateStruct.Of(
                ("a",ConstrainsState.Of(I32, Real)),
                ("b", I32))
            .CanBeConvertedOptimisticTo(
                StateStruct.Of("a", I32)).AssertTrue();

    [Test]
    public void Struct3() =>
        StateStruct.Empty()
            .CanBeConvertedOptimisticTo(StateStruct.Of("a", I32))
            .AssertFalse();

    [Test]
    public void Struct4() =>
        StateStruct.Empty()
            .CanBeConvertedOptimisticTo(StateStruct.Empty())
            .AssertTrue();

    [Test]
    public void Struct5() =>
        StateStruct.Of("a",ConstrainsState.Of(I32, Real))
            .CanBeConvertedOptimisticTo(StateStruct.Of("a", I64))
            .AssertTrue();

    [Test]
    public void Struct6() =>
        StateStruct.Of("a",ConstrainsState.Empty)
            .CanBeConvertedOptimisticTo(StateStruct.Of("a", ConstrainsState.Empty))
            .AssertTrue();

    [Test]
    public void Struct7() =>
        StateStruct.Of("a",ConstrainsState.Empty)
            .CanBeConvertedOptimisticTo(Any)
            .AssertTrue();
}
