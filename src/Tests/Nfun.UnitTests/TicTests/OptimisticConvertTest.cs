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
        SolvingFunctions.CanBeConvertedOptimistic(new StatePrimitive(from), new StatePrimitive(to))
            .AssertTrue();

    [TestCase(PrimitiveTypeName.I64, PrimitiveTypeName.I32)]
    [TestCase(PrimitiveTypeName.Char, PrimitiveTypeName.Ip)]
    public void PrimitiveToPrimitive_returnsFalse(PrimitiveTypeName from, PrimitiveTypeName to) =>
        SolvingFunctions.CanBeConvertedOptimistic(new StatePrimitive(from), new StatePrimitive(to))
            .AssertFalse();

    [Test]
    public void FromAnyToConstraints_returnsFalse() =>
        SolvingFunctions.CanBeConvertedOptimistic(Any, new ConstrainsState(I64, Real))
            .AssertFalse();

    [Test]
    public void FromAnyToConstraints_returnsTrue() =>
        SolvingFunctions.CanBeConvertedOptimistic(Any, new ConstrainsState(Any))
            .AssertTrue();

    [Test]
    public void FromPrimitiveToConstraints_returnsFalse2() =>
        SolvingFunctions.CanBeConvertedOptimistic(Char, new ConstrainsState(I32, Real))
            .AssertFalse();

    [Test]
    public void FromPrimitiveToConstraints_returnsTrue() =>
        SolvingFunctions.CanBeConvertedOptimistic(I32, new ConstrainsState(Real, Any))
            .AssertTrue();

    [Test]
    public void FromPrimitiveToConstraints_returnsTrue2() =>
        SolvingFunctions.CanBeConvertedOptimistic(I32, new ConstrainsState(U8, Any))
            .AssertTrue();

    [Test]
    public void FromPrimitiveToConstraints_returnsTrue3() =>
        SolvingFunctions.CanBeConvertedOptimistic(I32,
                new ConstrainsState(null, Any))
            .AssertTrue();

    [Test]
    public void FromPrimitiveToConstraints_returnsTrue4() =>
        SolvingFunctions.CanBeConvertedOptimistic(
                I32,
                new ConstrainsState(Char)) // `from` can be of `any` type
            .AssertTrue();

    [Test]
    public void FromPrimitiveToConstraints_returnsTrue5() =>
        SolvingFunctions.CanBeConvertedOptimistic(I32, new ConstrainsState(Char, Any))
            .AssertTrue();


    [Test]
    public void ConstrainsToConstraints_returnsFalse() =>
        SolvingFunctions.CanBeConvertedOptimistic(
                new ConstrainsState(I64, Real),
                new ConstrainsState(U8, I32))
            .AssertFalse();

    [Test]
    public void ConstrainsToConstraints_returnsFalse2() =>
        SolvingFunctions.CanBeConvertedOptimistic(
                new ConstrainsState(U64),
                new ConstrainsState(null, I32))
            .AssertFalse();

    [Test]
    public void ConstrainsToConstraints_returnsFalse3() =>
        SolvingFunctions.CanBeConvertedOptimistic(
                new ConstrainsState(U64),
                new ConstrainsState(null, I32))
            .AssertFalse();

    [Test]
    public void ConstrainsToConstraints_returnsTrue() =>
        SolvingFunctions.CanBeConvertedOptimistic(
                new ConstrainsState(),
                new ConstrainsState(U8, Real))
            .AssertTrue();

    [Test]
    public void ConstrainsToConstraints_returnsTrue2() =>
        SolvingFunctions.CanBeConvertedOptimistic(
                new ConstrainsState(Char),
                new ConstrainsState(U8))
            .AssertTrue();

    [Test]
    public void ConstrainsToConstraints_returnsTrue3() =>
        SolvingFunctions.CanBeConvertedOptimistic(
                new ConstrainsState(Ip),
                new ConstrainsState())
            .AssertTrue();

    [Test]
    public void ConstrainsToConstraints_returnsTrue4() =>
        SolvingFunctions.CanBeConvertedOptimistic(
                new ConstrainsState(I32, I64),
                new ConstrainsState(I64, I96))
            .AssertTrue();

    [Test]
    public void FromConstraintsToPrimitive_returnsFalse() =>
        SolvingFunctions.CanBeConvertedOptimistic(
                new ConstrainsState(I32, Real),
                Char)
            .AssertFalse();

    [Test]
    public void FromConstraintsToPrimitive_returnsFalse2() =>
        SolvingFunctions.CanBeConvertedOptimistic(new ConstrainsState(null, Ip), Char)
            .AssertFalse();

    [Test]
    public void FromConstraintsToPrimitive_returnsFalse3() =>
        SolvingFunctions.CanBeConvertedOptimistic(
                new ConstrainsState(Real, Any), I32)
            .AssertFalse();

    [Test]
    public void FromConstraintsToPrimitive_returnsTrue2() =>
        SolvingFunctions.CanBeConvertedOptimistic(new ConstrainsState(), Ip).AssertTrue();

    [Test]
    public void FromConstraintsToPrimitive_returnsTrue3() =>
        SolvingFunctions.CanBeConvertedOptimistic(new ConstrainsState(U8), Real)
            .AssertTrue();

    [Test]
    public void FromArrayToArray_returnsFalse() =>
        SolvingFunctions.CanBeConvertedOptimistic(
                StateArray.Of(Real), StateArray.Of(U8))
            .AssertFalse();

    [Test]
    public void FromArrayToArray_returnsTrue() =>
        SolvingFunctions.CanBeConvertedOptimistic(
                StateArray.Of(U8), StateArray.Of(Real))
            .AssertTrue();


    [Test]
    public void FromArrayConstraintsToPrimitive_returnsFalse() =>
        SolvingFunctions.CanBeConvertedOptimistic(
                ArrayOfConstrains(I32, Real),
                StateArray.Of(Char))
            .AssertFalse();

    [Test]
    public void FromArrayConstraintsToPrimitive_returnsFalse2() =>
        SolvingFunctions.CanBeConvertedOptimistic(
                ArrayOfConstrains(null, Ip),
                StateArray.Of(Char))
            .AssertFalse();

    [Test]
    public void FromArrayConstraintsToPrimitive_returnsFalse3() =>
        SolvingFunctions.CanBeConvertedOptimistic(
                ArrayOfConstrains(Real, Any),
                StateArray.Of(I32))
            .AssertFalse();

    [Test]
    public void FromArrayConstraintsToPrimitive_returnsTrue2() =>
        SolvingFunctions.CanBeConvertedOptimistic(
                ArrayOfConstrains(),
                StateArray.Of(Ip))
            .AssertTrue();

    [Test]
    public void FromArrayConstraintsToPrimitive_returnsTrue3() =>
        SolvingFunctions.CanBeConvertedOptimistic(
                ArrayOfConstrains(U8),
                StateArray.Of(Real))
            .AssertTrue();

    //----
    [Test]
    public void FromArrayPrimitiveToConstraints_returnsTrue4() =>
        SolvingFunctions.CanBeConvertedOptimistic(
                StateArray.Of(I32),
                ArrayOfConstrains(Char, Any))
            .AssertTrue();

    [Test]
    public void FromArrayPrimitiveToConstraints_returnsFalse2() =>
        SolvingFunctions.CanBeConvertedOptimistic(
                StateArray.Of(Char),
                ArrayOfConstrains(I32, Real))
            .AssertFalse();

    [Test]
    public void FromArrayPrimitiveToConstraints_returnsTrue() =>
        SolvingFunctions.CanBeConvertedOptimistic(
                StateArray.Of(I32),
                ArrayOfConstrains(Real, Any))
            .AssertTrue();

    [Test]
    public void FromArrayPrimitiveToConstraints_returnsTrue2() =>
        SolvingFunctions.CanBeConvertedOptimistic(
                StateArray.Of(I32),
                ArrayOfConstrains(U8, Any))
            .AssertTrue();

    [Test]
    public void FromArrayPrimitiveToConstraints_returnsTrue3() =>
        SolvingFunctions.CanBeConvertedOptimistic(
                StateArray.Of(I32),
                ArrayOfConstrains(null, Any))
            .AssertTrue();

    [Test]
    public void FromArrayPrimitiveToConstraints_returnsTrue5() =>
        SolvingFunctions.CanBeConvertedOptimistic(
                StateArray.Of(I32),
                ArrayOfConstrains(Char)) // `from` can be of `any` type
            .AssertTrue();

    [Test]
    public void ArrayConstrainsToConstraints_returnsFalse() =>
        SolvingFunctions.CanBeConvertedOptimistic(
                ArrayOfConstrains(I64, Real),
                ArrayOfConstrains(U8, I32))
            .AssertFalse();

    [Test]
    public void ArrayConstrainsToConstraints_returnsFalse2() =>
        SolvingFunctions.CanBeConvertedOptimistic(
                ArrayOfConstrains(U64),
                ArrayOfConstrains(null, I32))
            .AssertFalse();

    [Test]
    public void ArrayConstrainsToConstraints_returnsFalse3() =>
        SolvingFunctions.CanBeConvertedOptimistic(
                ArrayOfConstrains(U64),
                ArrayOfConstrains(null, I32))
            .AssertFalse();

    [Test]
    public void ArrayConstrainsToConstraints_returnsTrue() =>
        SolvingFunctions.CanBeConvertedOptimistic(
                ArrayOfConstrains(),
                ArrayOfConstrains(U8, Real))
            .AssertTrue();

    [Test]
    public void ArrayConstrainsToConstraints_returnsTrue2() =>
        SolvingFunctions.CanBeConvertedOptimistic(
                ArrayOfConstrains(Char),
                ArrayOfConstrains(U8))
            .AssertTrue();

    [Test]
    public void ArrayConstrainsToConstraints_returnsTrue3() =>
        SolvingFunctions.CanBeConvertedOptimistic(
                ArrayOfConstrains(Ip),
                ArrayOfConstrains())
            .AssertTrue();

    [Test]
    public void ArrayConstrainsToConstraints_returnsTrue4() =>
        SolvingFunctions.CanBeConvertedOptimistic(
                ArrayOfConstrains(I32, I64),
                ArrayOfConstrains(I64, I96))
            .AssertTrue();

    [Test]
    public void ArrayToPrimitive_returnsTrue() =>
        SolvingFunctions.CanBeConvertedOptimistic(StateArray.Of(I32), Any)
            .AssertTrue();

    [Test]
    public void ArrayToPrimitive_returnsFalse() =>
        SolvingFunctions.CanBeConvertedOptimistic(StateArray.Of(I32), Ip)
            .AssertFalse();

    [Test]
    public void PrimitiveToArray_returnsFalse() =>
        SolvingFunctions.CanBeConvertedOptimistic(Ip, StateArray.Of(I32))
            .AssertFalse();

    [Test]
    public void ArrayToArray_returnsTrue() =>
        SolvingFunctions.CanBeConvertedOptimistic(
                StateArray.Of(StateArray.Of(StateArray.Of(U8))),
                ArrayOfConstrains(StateArray.Of(U8)))
            .AssertTrue();

    private static StateArray ArrayOfConstrains(ITicNodeState desc = null, StatePrimitive anc = null, bool isComparable = false)
        => StateArray.Of(TicNode.CreateNamedNode("foo", new ConstrainsState(desc, anc, isComparable)));
}
