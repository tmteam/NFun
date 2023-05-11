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
        ConstrainsState.Of(I64, Real).CanBeConvertedOptimisticTo(ConstrainsState.Of(U8, I32))
            .AssertFalse();

    [Test]
    public void ConstrainsToConstraints_returnsFalse2() =>
        ConstrainsState.Of(U64).CanBeConvertedOptimisticTo(ConstrainsState.Of(null, I32))
            .AssertFalse();

    [Test]
    public void ConstrainsToConstraints_returnsFalse3() =>
        ConstrainsState.Of(U64).CanBeConvertedOptimisticTo(ConstrainsState.Of(null, I32))
            .AssertFalse();

    [Test]
    public void ConstrainsToConstraints_returnsTrue() =>
        ConstrainsState.Empty.CanBeConvertedOptimisticTo(ConstrainsState.Of(U8, Real))
            .AssertTrue();

    [Test]
    public void ConstrainsToConstraints_returnsTrue2() =>
        ConstrainsState.Of(Char).CanBeConvertedOptimisticTo(ConstrainsState.Of(U8))
            .AssertTrue();

    [Test]
    public void ConstrainsToConstraints_returnsTrue3() =>
        ConstrainsState.Of(Ip).CanBeConvertedOptimisticTo(ConstrainsState.Empty)
            .AssertTrue();

    [Test]
    public void ConstrainsToConstraints_returnsTrue4() =>
        ConstrainsState.Of(I32, I64).CanBeConvertedOptimisticTo(ConstrainsState.Of(I64, I96))
            .AssertTrue();

    [Test]
    public void FromConstraintsToPrimitive_returnsFalse() =>
        ConstrainsState.Of(I32, Real).CanBeConvertedOptimisticTo(Char)
            .AssertFalse();

    [Test]
    public void FromConstraintsToPrimitive_returnsFalse2() =>
        ConstrainsState.Of(null, Ip).CanBeConvertedOptimisticTo(Char)
            .AssertFalse();

    [Test]
    public void FromConstraintsToPrimitive_returnsFalse3() =>
        ConstrainsState.Of(Real, Any).CanBeConvertedOptimisticTo(I32)
            .AssertFalse();

    [Test]
    public void FromConstraintsToPrimitive_returnsTrue2() =>
        ConstrainsState.Empty.CanBeConvertedOptimisticTo(Ip).AssertTrue();

    [Test]
    public void FromConstraintsToPrimitive_returnsTrue3() =>
        ConstrainsState.Of(U8).CanBeConvertedOptimisticTo(Real)
            .AssertTrue();

    [Test]
    public void FromArrayToArray_returnsFalse() =>
        StateArray.Of(Real).CanBeConvertedOptimisticTo(StateArray.Of(U8))
            .AssertFalse();

    [Test]
    public void FromArrayToArray_returnsTrue() =>
        StateArray.Of(U8).CanBeConvertedOptimisticTo(StateArray.Of(Real))
            .AssertTrue();


    [Test]
    public void FromArrayConstraintsToPrimitive_returnsFalse() =>
        ArrayOfConstrains(I32, Real).CanBeConvertedOptimisticTo(StateArray.Of(Char))
            .AssertFalse();

    [Test]
    public void FromArrayConstraintsToPrimitive_returnsFalse2() =>
        ArrayOfConstrains(null, Ip).CanBeConvertedOptimisticTo(StateArray.Of(Char))
            .AssertFalse();

    [Test]
    public void FromArrayConstraintsToPrimitive_returnsFalse3() =>
        ArrayOfConstrains(Real, Any).CanBeConvertedOptimisticTo(StateArray.Of(I32))
            .AssertFalse();

    [Test]
    public void FromArrayConstraintsToPrimitive_returnsTrue2() =>
        ArrayOfConstrains().CanBeConvertedOptimisticTo(StateArray.Of(Ip))
            .AssertTrue();

    [Test]
    public void FromArrayConstraintsToPrimitive_returnsTrue3() =>
        ArrayOfConstrains(U8).CanBeConvertedOptimisticTo(StateArray.Of(Real))
            .AssertTrue();

    //----
    [Test]
    public void FromArrayPrimitiveToConstraints_returnsTrue4() =>
        StateArray.Of(I32).CanBeConvertedOptimisticTo(ArrayOfConstrains(Char, Any))
            .AssertTrue();

    [Test]
    public void FromArrayPrimitiveToConstraints_returnsFalse2() =>
        StateArray.Of(Char).CanBeConvertedOptimisticTo(ArrayOfConstrains(I32, Real))
            .AssertFalse();

    [Test]
    public void FromArrayPrimitiveToConstraints_returnsTrue() =>
        StateArray.Of(I32).CanBeConvertedOptimisticTo(ArrayOfConstrains(Real, Any))
            .AssertTrue();

    [Test]
    public void FromArrayPrimitiveToConstraints_returnsTrue2() =>
        StateArray.Of(I32).CanBeConvertedOptimisticTo(ArrayOfConstrains(U8, Any))
            .AssertTrue();

    [Test]
    public void FromArrayPrimitiveToConstraints_returnsTrue3() =>
        StateArray.Of(I32).CanBeConvertedOptimisticTo(ArrayOfConstrains(null, Any))
            .AssertTrue();

    [Test]
    public void FromArrayPrimitiveToConstraints_returnsTrue5() =>
        StateArray.Of(I32).CanBeConvertedOptimisticTo(ArrayOfConstrains(Char)) // `from` can be of `any` type
            .AssertTrue();

    [Test]
    public void ArrayConstrainsToConstraints_returnsFalse() =>
        ArrayOfConstrains(I64, Real).CanBeConvertedOptimisticTo(ArrayOfConstrains(U8, I32))
            .AssertFalse();

    [Test]
    public void ArrayConstrainsToConstraints_returnsFalse2() =>
        ArrayOfConstrains(U64).CanBeConvertedOptimisticTo(ArrayOfConstrains(null, I32))
            .AssertFalse();

    [Test]
    public void ArrayConstrainsToConstraints_returnsFalse3() =>
        ArrayOfConstrains(U64).CanBeConvertedOptimisticTo(ArrayOfConstrains(null, I32))
            .AssertFalse();

    [Test]
    public void ArrayConstrainsToConstraints_returnsTrue() =>
        ArrayOfConstrains().CanBeConvertedOptimisticTo(ArrayOfConstrains(U8, Real))
            .AssertTrue();

    [Test]
    public void ArrayConstrainsToConstraints_returnsTrue2() =>
        ArrayOfConstrains(Char).CanBeConvertedOptimisticTo(ArrayOfConstrains(U8))
            .AssertTrue();

    [Test]
    public void ArrayConstrainsToConstraints_returnsTrue3() =>
        ArrayOfConstrains(Ip).CanBeConvertedOptimisticTo(ArrayOfConstrains())
            .AssertTrue();

    [Test]
    public void ArrayConstrainsToConstraints_returnsTrue4() =>
        ArrayOfConstrains(I32, I64).CanBeConvertedOptimisticTo(ArrayOfConstrains(I64, I96))
            .AssertTrue();

    [Test]
    public void ArrayToPrimitive_returnsTrue() =>
        StateArray.Of(I32).CanBeConvertedOptimisticTo(Any)
            .AssertTrue();

    [Test]
    public void ArrayToPrimitive_returnsFalse() =>
        StateArray.Of(I32).CanBeConvertedOptimisticTo(Ip)
            .AssertFalse();

    [Test]
    public void PrimitiveToArray_returnsFalse() =>
        Ip.CanBeConvertedOptimisticTo(StateArray.Of(I32))
            .AssertFalse();

    [Test]
    public void ArrayToArray_returnsTrue() =>
        StateArray.Of(StateArray.Of(StateArray.Of(U8))).CanBeConvertedOptimisticTo(ArrayOfConstrains(StateArray.Of(U8)))
            .AssertTrue();

    private static StateArray ArrayOfConstrains(ITicNodeState desc = null, StatePrimitive anc = null, bool isComparable = false)
        => StateArray.Of(TicNode.CreateNamedNode("foo", ConstrainsState.Of(desc, anc, isComparable)));
}
