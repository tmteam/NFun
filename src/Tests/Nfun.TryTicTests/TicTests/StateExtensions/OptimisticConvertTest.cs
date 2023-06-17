namespace NFun.UnitTests.TicTests.StateExtensions;

using TestTools;
using Tic;
using NFun.Tic.SolvingStates;
using NUnit.Framework;
using static SolvingStates;
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
        Any.CanBeConvertedOptimisticTo(Constrains(I64, Real))
            .AssertFalse();

    [Test]
    public void FromAnyToConstraints_returnsTrue() =>
        Any.CanBeConvertedOptimisticTo(Constrains(Any))
            .AssertTrue();

    [Test]
    public void FromPrimitiveToConstraints_returnsFalse2() =>
        Char.CanBeConvertedOptimisticTo(Constrains(I32, Real))
            .AssertFalse();

    [Test]
    public void FromPrimitiveToConstraints_returnsTrue() =>
        I32.CanBeConvertedOptimisticTo(Constrains(Real, Any))
            .AssertTrue();

    [Test]
    public void FromPrimitiveToConstraints_returnsTrue2() =>
        I32.CanBeConvertedOptimisticTo(Constrains(U8, Any))
            .AssertTrue();

    [Test]
    public void FromPrimitiveToConstraints_returnsTrue3() =>
        I32.CanBeConvertedOptimisticTo(Constrains(null, Any))
            .AssertTrue();

    [Test]
    public void FromPrimitiveToConstrains_returnsTrue4() =>
        U8.CanBeConvertedOptimisticTo(Constrains(null, I96))
            .AssertTrue();

    [Test]
    public void FromPrimitiveToConstraints_returnsTrue4() =>
        I32.CanBeConvertedOptimisticTo(Constrains(Char)) // `from` can be of `any` type
            .AssertTrue();

    [Test]
    public void FromPrimitiveToConstraints_returnsTrue5() =>
        I32.CanBeConvertedOptimisticTo(Constrains(Char, Any))
            .AssertTrue();


    [Test]
    public void ConstrainsToConstraints_returnsFalse() =>
        Constrains(I64, Real)
            .CanBeConvertedOptimisticTo(Constrains(U8, I32))
            .AssertFalse();

    [Test]
    public void ConstrainsToConstraints_returnsFalse2() =>
        Constrains(U64)
            .CanBeConvertedOptimisticTo(Constrains(null, I32))
            .AssertFalse();

    [Test]
    public void ConstrainsToConstraints_returnsFalse3() =>
        Constrains(U64)
            .CanBeConvertedOptimisticTo(Constrains(null, I32))
            .AssertFalse();

    [Test]
    public void ConstrainsToConstraints_returnsTrue() =>
        EmptyConstrains
            .CanBeConvertedOptimisticTo(Constrains(U8, Real))
            .AssertTrue();

    [Test]
    public void ConstrainsToConstraints_returnsTrue2() =>
        Constrains(Char)
            .CanBeConvertedOptimisticTo(Constrains(U8))
            .AssertTrue();

    [Test]
    public void ConstrainsToConstraints_returnsTrue3() =>
        Constrains(Ip)
            .CanBeConvertedOptimisticTo(EmptyConstrains)
            .AssertTrue();

    [Test]
    public void ConstrainsToConstraints_returnsTrue4() =>
        Constrains(I32, I64)
            .CanBeConvertedOptimisticTo(Constrains(I64, I96))
            .AssertTrue();

    [Test]
    public void ConstrainsToPrimitive_returnsTrue() =>
        Constrains().CanBeConvertedOptimisticTo(I32)
            .AssertTrue();

    [Test]
    public void ConstrainsToPrimitive_returnsTrue2() =>
        Constrains(Constrains(isComparable:true)).CanBeConvertedOptimisticTo(I32)
            .AssertTrue();

    [Test]
    public void ConstrainsToPrimitive_returnsTrue3() =>
        Constrains(Constrains()).CanBeConvertedOptimisticTo(I32)
            .AssertTrue();

    [Test]
    public void ConstrainsToPrimitive_returnsTrue4() =>
        Constrains(Constrains()).CanBeConvertedOptimisticTo(Char)
            .AssertTrue();

    [Test]
    public void ConstrainsToPrimitive_returnsTrue5() =>
        Constrains(null, I96).CanBeConvertedOptimisticTo(U8)
            .AssertTrue();

    [Test]
    public void ConstrainsToPrimitive_returnsTrue6() =>
        Constrains(U8, I96).CanBeConvertedOptimisticTo(U8)
            .AssertTrue();

    [Test]
    public void ConstrainsToPrimitive_returnsTrue7() =>
        Constrains(U8, I96).CanBeConvertedOptimisticTo(I32)
            .AssertTrue();

    [Test]
    public void ConstrainsToPrimitive_returnsTrue8() =>
        Constrains(U8, I96).CanBeConvertedOptimisticTo(I96)
            .AssertTrue();

    [Test]
    public void ConstrainsToPrimitive_returnsTrue9() =>
        Constrains(U8, I96).CanBeConvertedOptimisticTo(Real)
            .AssertTrue();

    [Test]
    public void ConstrainsToPrimitive_returnsTrue10() =>
        Constrains(isComparable:true).CanBeConvertedOptimisticTo(I32)
            .AssertTrue();


    [Test]
    public void ConstrainsToPrimitive_returnsTrue11() =>
        Constrains(isComparable:true).CanBeConvertedOptimisticTo(Real)
            .AssertTrue();

    [Test]
    public void ConstrainsToPrimitive_returnsTrue12() =>
        Constrains(isComparable:true).CanBeConvertedOptimisticTo(Char)
            .AssertTrue();

    [Test]
    public void ConstrainsToPrimitive_returnsFalse1() =>
        Constrains(U16, I96).CanBeConvertedOptimisticTo(U8)
            .AssertFalse();

    [Test]
    public void ConstrainsToPrimitive_returnsFalse2() =>
        Constrains(U16, U64).CanBeConvertedOptimisticTo(I16)
            .AssertFalse();

    [Test]
    public void FromConstraintsToPrimitive_returnsFalse() =>
        Constrains(I32, Real)
            .CanBeConvertedOptimisticTo(Char)
            .AssertFalse();

    [Test]
    public void FromConstraintsToPrimitive_returnsFalse2() =>
        Constrains(null, Ip)
            .CanBeConvertedOptimisticTo(Char)
            .AssertFalse();

    [Test]
    public void FromConstraintsToPrimitive_returnsFalse3() =>
        Constrains(Real, Any)
            .CanBeConvertedOptimisticTo(I32)
            .AssertFalse();

    [Test]
    public void ConstrainsToPrimitive_returnsFalse4() =>
        Constrains(isComparable:true).CanBeConvertedOptimisticTo(Bool)
            .AssertFalse();

    [Test]
    public void FromConstraintsToPrimitive_returnsTrue2() =>
        EmptyConstrains
            .CanBeConvertedOptimisticTo(Ip)
            .AssertTrue();

    [Test]
    public void FromConstraintsToPrimitive_returnsTrue3() =>
        Constrains(U8)
            .CanBeConvertedOptimisticTo(Real)
            .AssertTrue();

    [Test]
    public void FromArrayToArray_returnsFalse() =>
        Array(Real)
            .CanBeConvertedOptimisticTo(Array(U8))
            .AssertFalse();

    [Test]
    public void FromArrayToArray_returnsTrue() =>
        Array(U8)
            .CanBeConvertedOptimisticTo(Array(Real))
            .AssertTrue();


    [Test]
    public void FromArrayConstraintsToPrimitive_returnsFalse() =>
        Array(Constrains(I32, Real, false))
            .CanBeConvertedOptimisticTo(Array(Char))
            .AssertFalse();

    [Test]
    public void FromArrayConstraintsToPrimitive_returnsFalse2() =>
        Array(Constrains(null, Ip, false))
            .CanBeConvertedOptimisticTo(Array(Char))
            .AssertFalse();

    [Test]
    public void FromArrayConstraintsToPrimitive_returnsFalse3() =>
        Array(Constrains(Real, Any, false))
            .CanBeConvertedOptimisticTo(Array(I32))
            .AssertFalse();

    [Test]
    public void FromArrayConstraintsToPrimitive_returnsTrue2() =>
        Array(Constrains(null, null, false))
            .CanBeConvertedOptimisticTo(Array(Ip))
            .AssertTrue();

    [Test]
    public void FromArrayConstraintsToPrimitive_returnsTrue3() =>
        Array(Constrains(U8, null, false))
            .CanBeConvertedOptimisticTo(Array(Real))
            .AssertTrue();

    //----
    [Test]
    public void FromArrayPrimitiveToConstraints_returnsTrue4() =>
        Array(I32)
            .CanBeConvertedOptimisticTo(Array(Constrains(Char, Any, false)))
            .AssertTrue();

    [Test]
    public void FromArrayPrimitiveToConstraints_returnsFalse2() =>
        Array(Char)
            .CanBeConvertedOptimisticTo(Array(Constrains(I32, Real, false)))
            .AssertFalse();

    [Test]
    public void FromArrayPrimitiveToConstraints_returnsTrue() =>
        Array(I32)
            .CanBeConvertedOptimisticTo(Array(Constrains(Real, Any, false)))
            .AssertTrue();

    [Test]
    public void FromArrayPrimitiveToConstraints_returnsTrue2() =>
        Array(I32)
            .CanBeConvertedOptimisticTo(Array(Constrains(U8, Any, false)))
            .AssertTrue();

    [Test]
    public void FromArrayPrimitiveToConstraints_returnsTrue3() =>
        Array(I32)
            .CanBeConvertedOptimisticTo(Array(Constrains(null, Any, false)))
            .AssertTrue();

    [Test]
    public void FromArrayPrimitiveToConstraints_returnsTrue5() =>
        Array(I32)
            .CanBeConvertedOptimisticTo(Array(Constrains(Char, null, false)))
            // `from` can be of `any` type
            .AssertTrue();

    [Test]
    public void ArrayConstrainsToConstraints_returnsFalse() =>
        Array(Constrains(I64, Real, false))
            .CanBeConvertedOptimisticTo(Array(Constrains(U8, I32, false)))
            .AssertFalse();

    [Test]
    public void ArrayConstrainsToConstraints_returnsFalse2() =>
        Array(Constrains(U64, null, false))
            .CanBeConvertedOptimisticTo(Array(Constrains(null, I32, false)))
            .AssertFalse();

    [Test]
    public void ArrayConstrainsToConstraints_returnsFalse3() =>
        Array(Constrains(U64, null, false))
            .CanBeConvertedOptimisticTo(Array(Constrains(null, I32, false)))
            .AssertFalse();

    [Test]
    public void ArrayConstrainsToConstraints_returnsTrue() =>
        Array(Constrains(null, null, false))
            .CanBeConvertedOptimisticTo(Array(Constrains(U8, Real, false)))
            .AssertTrue();

    [Test]
    public void ArrayConstrainsToConstraints_returnsTrue2() =>
        Array(Constrains(Char, null, false))
            .CanBeConvertedOptimisticTo(Array(Constrains(U8, null, false)))
            .AssertTrue();

    [Test]
    public void ArrayConstrainsToConstraints_returnsTrue3() =>
        Array(Constrains(Ip, null, false))
            .CanBeConvertedOptimisticTo(Array(Constrains(null, null, false)))
            .AssertTrue();

    [Test]
    public void ArrayConstrainsToConstraints_returnsTrue4() =>
        Array(Constrains(I32, I64, false))
            .CanBeConvertedOptimisticTo(Array(Constrains(I64, I96, false)))
            .AssertTrue();

    [Test]
    public void ArrayToPrimitive_returnsTrue() =>
        Array(I32)
            .CanBeConvertedOptimisticTo(Any)
            .AssertTrue();

    [Test]
    public void ArrayToPrimitive_returnsFalse() =>
        Array(I32)
            .CanBeConvertedOptimisticTo(Ip)
            .AssertFalse();

    [Test]
    public void PrimitiveToArray_returnsFalse() =>
        Ip.CanBeConvertedOptimisticTo(Array(I32))
            .AssertFalse();

    [Test]
    public void ArrayToArray_returnsTrue() =>
        Array(Array(Array(U8)))
            .CanBeConvertedOptimisticTo(Array(Constrains(Array(U8), null, false)))
            .AssertTrue();

    [Test]
    public void Fun1() =>
        Fun(Constrains(I32, Real), I32)
            .CanBeConvertedOptimisticTo(Fun(I64, Real))
            .AssertTrue();

    [Test]
    public void Fun2() =>
        Fun(Constrains(I32, Real), Real)
            .CanBeConvertedOptimisticTo(
                Fun(Constrains(I32, Real), Constrains(I32)))
            .AssertTrue();

    [Test]
    public void Fun3() =>
        Fun(Any, Real)
            .CanBeConvertedOptimisticTo(Fun(Constrains(I32, Real), Constrains(I32)))
            .AssertTrue();

    [Test]
    public void Fun4() =>
        Fun(Constrains(I32, Real), Real)
            .CanBeConvertedOptimisticTo(Any)
            .AssertTrue();

    [Test]
    public void Struct1() =>
        Struct(("a", I32), ("b", I32))
            .CanBeConvertedOptimisticTo(Struct("a", I32))
            .AssertTrue();

    [Test]
    public void Struct2() =>
        Struct(
                ("a",Constrains(I32, Real)),
                ("b", I32))
            .CanBeConvertedOptimisticTo(
                Struct("a", I32)).AssertTrue();

    [Test]
    public void Struct3() =>
        StateStruct.Empty()
            .CanBeConvertedOptimisticTo(Struct("a", I32))
            .AssertFalse();

    [Test]
    public void Struct4() =>
        StateStruct.Empty()
            .CanBeConvertedOptimisticTo(StateStruct.Empty())
            .AssertTrue();

    [Test]
    public void Struct5() =>
        Struct("a",Constrains(I32, Real))
            .CanBeConvertedOptimisticTo(Struct("a", I64))
            .AssertTrue();

    [Test]
    public void Struct6() =>
        Struct("a",EmptyConstrains)
            .CanBeConvertedOptimisticTo(Struct("a", EmptyConstrains))
            .AssertTrue();

    [Test]
    public void Struct7() =>
        Struct("a",EmptyConstrains)
            .CanBeConvertedOptimisticTo(Any)
            .AssertTrue();
}
