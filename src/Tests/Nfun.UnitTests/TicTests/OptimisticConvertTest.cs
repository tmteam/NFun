namespace NFun.UnitTests.TicTests;

using NUnit.Framework;
using TestTools;
using Tic;
using Tic.SolvingStates;

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
        SolvingFunctions.CanBeConvertedOptimistic(StatePrimitive.Any,
                new ConstrainsState(StatePrimitive.I64, StatePrimitive.Real))
            .AssertFalse();

    [Test]
    public void FromAnyToConstraints_returnsTrue() =>
        SolvingFunctions.CanBeConvertedOptimistic(StatePrimitive.Any, new ConstrainsState(StatePrimitive.Any))
            .AssertTrue();

    [Test]
    public void FromPrimitiveToConstraints_returnsFalse2() =>
        SolvingFunctions.CanBeConvertedOptimistic(StatePrimitive.Char,
                new ConstrainsState(StatePrimitive.I32, StatePrimitive.Real))
            .AssertFalse();

    [Test]
    public void FromPrimitiveToConstraints_returnsTrue() =>
        SolvingFunctions.CanBeConvertedOptimistic(StatePrimitive.I32,
                new ConstrainsState(StatePrimitive.Real, StatePrimitive.Any))
            .AssertTrue();

    [Test]
    public void FromPrimitiveToConstraints_returnsTrue2() =>
        SolvingFunctions.CanBeConvertedOptimistic(StatePrimitive.I32,
                new ConstrainsState(StatePrimitive.U8, StatePrimitive.Any))
            .AssertTrue();

    [Test]
    public void FromPrimitiveToConstraints_returnsTrue3() =>
        SolvingFunctions.CanBeConvertedOptimistic(StatePrimitive.I32,
                new ConstrainsState(null, StatePrimitive.Any))
            .AssertTrue();

    [Test]
    public void FromPrimitiveToConstraints_returnsTrue4() =>
        SolvingFunctions.CanBeConvertedOptimistic(
                StatePrimitive.I32,
                new ConstrainsState(StatePrimitive.Char)) // `from` can be of `any` type
            .AssertTrue();

    [Test]
    public void FromPrimitiveToConstraints_returnsTrue5() =>
        SolvingFunctions.CanBeConvertedOptimistic(StatePrimitive.I32,
                new ConstrainsState(StatePrimitive.Char, StatePrimitive.Any))
            .AssertTrue();


    [Test]
    public void ConstrainsToConstraints_returnsFalse() =>
        SolvingFunctions.CanBeConvertedOptimistic(
                new ConstrainsState(StatePrimitive.I64, StatePrimitive.Real),
                new ConstrainsState(StatePrimitive.U8, StatePrimitive.I32))
            .AssertFalse();

    [Test]
    public void ConstrainsToConstraints_returnsFalse2() =>
        SolvingFunctions.CanBeConvertedOptimistic(
                new ConstrainsState(StatePrimitive.U64),
                new ConstrainsState(null, StatePrimitive.I32))
            .AssertFalse();

    [Test]
    public void ConstrainsToConstraints_returnsFalse3() =>
        SolvingFunctions.CanBeConvertedOptimistic(
                new ConstrainsState(StatePrimitive.U64),
                new ConstrainsState(null, StatePrimitive.I32))
            .AssertFalse();

    [Test]
    public void ConstrainsToConstraints_returnsTrue() =>
        SolvingFunctions.CanBeConvertedOptimistic(
                new ConstrainsState(),
                new ConstrainsState(StatePrimitive.U8, StatePrimitive.Real))
            .AssertTrue();

    [Test]
    public void ConstrainsToConstraints_returnsTrue2() =>
        SolvingFunctions.CanBeConvertedOptimistic(
                new ConstrainsState(StatePrimitive.Char),
                new ConstrainsState(StatePrimitive.U8))
            .AssertTrue();

    [Test]
    public void ConstrainsToConstraints_returnsTrue3() =>
        SolvingFunctions.CanBeConvertedOptimistic(
                new ConstrainsState(StatePrimitive.Ip),
                new ConstrainsState())
            .AssertTrue();

    [Test]
    public void ConstrainsToConstraints_returnsTrue4() =>
        SolvingFunctions.CanBeConvertedOptimistic(
                new ConstrainsState(StatePrimitive.I32, StatePrimitive.I64),
                new ConstrainsState(StatePrimitive.I64, StatePrimitive.I96))
            .AssertTrue();

    [Test]
    public void FromConstraintsToPrimitive_returnsFalse() =>
        SolvingFunctions.CanBeConvertedOptimistic(
                new ConstrainsState(StatePrimitive.I32, StatePrimitive.Real),
                StatePrimitive.Char)
            .AssertFalse();

    [Test]
    public void FromConstraintsToPrimitive_returnsFalse2() =>
        SolvingFunctions.CanBeConvertedOptimistic(new ConstrainsState(null, StatePrimitive.Ip), StatePrimitive.Char)
            .AssertFalse();

    [Test]
    public void FromConstraintsToPrimitive_returnsFalse3() =>
        SolvingFunctions.CanBeConvertedOptimistic(
                new ConstrainsState(StatePrimitive.Real, StatePrimitive.Any), StatePrimitive.I32)
            .AssertFalse();

    [Test]
    public void FromConstraintsToPrimitive_returnsTrue2() =>
        SolvingFunctions.CanBeConvertedOptimistic(new ConstrainsState(), StatePrimitive.Ip).AssertTrue();

    [Test]
    public void FromConstraintsToPrimitive_returnsTrue3() =>
        SolvingFunctions.CanBeConvertedOptimistic(new ConstrainsState(StatePrimitive.U8), StatePrimitive.Real)
            .AssertTrue();

    [Test]
    public void FromArrayToArray_returnsFalse() =>
        SolvingFunctions.CanBeConvertedOptimistic(
                StateArray.Of(StatePrimitive.Real), StateArray.Of(StatePrimitive.U8))
            .AssertFalse();

    [Test]
    public void FromArrayToArray_returnsTrue() =>
        SolvingFunctions.CanBeConvertedOptimistic(
                StateArray.Of(StatePrimitive.U8), StateArray.Of(StatePrimitive.Real))
            .AssertTrue();


    [Test]
    public void FromArrayConstraintsToPrimitive_returnsFalse() =>
        SolvingFunctions.CanBeConvertedOptimistic(
                ArrayOfConstrains(StatePrimitive.I32, StatePrimitive.Real),
                StateArray.Of(StatePrimitive.Char))
            .AssertFalse();

    [Test]
    public void FromArrayConstraintsToPrimitive_returnsFalse2() =>
        SolvingFunctions.CanBeConvertedOptimistic(
                ArrayOfConstrains(null, StatePrimitive.Ip),
                StateArray.Of(StatePrimitive.Char))
            .AssertFalse();

    [Test]
    public void FromArrayConstraintsToPrimitive_returnsFalse3() =>
        SolvingFunctions.CanBeConvertedOptimistic(
                ArrayOfConstrains(StatePrimitive.Real, StatePrimitive.Any),
                StateArray.Of(StatePrimitive.I32))
            .AssertFalse();

    [Test]
    public void FromArrayConstraintsToPrimitive_returnsTrue2() =>
        SolvingFunctions.CanBeConvertedOptimistic(
                ArrayOfConstrains(),
                StateArray.Of(StatePrimitive.Ip))
            .AssertTrue();

    [Test]
    public void FromArrayConstraintsToPrimitive_returnsTrue3() =>
        SolvingFunctions.CanBeConvertedOptimistic(
                ArrayOfConstrains(StatePrimitive.U8),
                StateArray.Of(StatePrimitive.Real))
            .AssertTrue();

    //----
    [Test]
    public void FromArrayPrimitiveToConstraints_returnsTrue4() =>
        SolvingFunctions.CanBeConvertedOptimistic(
                StateArray.Of(StatePrimitive.I32),
                ArrayOfConstrains(StatePrimitive.Char, StatePrimitive.Any))
            .AssertTrue();

    [Test]
    public void FromArrayPrimitiveToConstraints_returnsFalse2() =>
        SolvingFunctions.CanBeConvertedOptimistic(
                StateArray.Of(StatePrimitive.Char),
                ArrayOfConstrains(StatePrimitive.I32, StatePrimitive.Real))
            .AssertFalse();

    [Test]
    public void FromArrayPrimitiveToConstraints_returnsTrue() =>
        SolvingFunctions.CanBeConvertedOptimistic(
                StateArray.Of(StatePrimitive.I32),
                ArrayOfConstrains(StatePrimitive.Real, StatePrimitive.Any))
            .AssertTrue();

    [Test]
    public void FromArrayPrimitiveToConstraints_returnsTrue2() =>
        SolvingFunctions.CanBeConvertedOptimistic(
                StateArray.Of(StatePrimitive.I32),
                ArrayOfConstrains(StatePrimitive.U8, StatePrimitive.Any))
            .AssertTrue();

    [Test]
    public void FromArrayPrimitiveToConstraints_returnsTrue3() =>
        SolvingFunctions.CanBeConvertedOptimistic(
                StateArray.Of(StatePrimitive.I32),
                ArrayOfConstrains(null, StatePrimitive.Any))
            .AssertTrue();

    [Test]
    public void FromArrayPrimitiveToConstraints_returnsTrue5() =>
        SolvingFunctions.CanBeConvertedOptimistic(
                StateArray.Of(StatePrimitive.I32),
                ArrayOfConstrains(StatePrimitive.Char)) // `from` can be of `any` type
            .AssertTrue();

    [Test]
    public void ArrayConstrainsToConstraints_returnsFalse() =>
        SolvingFunctions.CanBeConvertedOptimistic(
                ArrayOfConstrains(StatePrimitive.I64, StatePrimitive.Real),
                ArrayOfConstrains(StatePrimitive.U8, StatePrimitive.I32))
            .AssertFalse();

    [Test]
    public void ArrayConstrainsToConstraints_returnsFalse2() =>
        SolvingFunctions.CanBeConvertedOptimistic(
                ArrayOfConstrains(StatePrimitive.U64),
                ArrayOfConstrains(null, StatePrimitive.I32))
            .AssertFalse();

    [Test]
    public void ArrayConstrainsToConstraints_returnsFalse3() =>
        SolvingFunctions.CanBeConvertedOptimistic(
                ArrayOfConstrains(StatePrimitive.U64),
                ArrayOfConstrains(null, StatePrimitive.I32))
            .AssertFalse();

    [Test]
    public void ArrayConstrainsToConstraints_returnsTrue() =>
        SolvingFunctions.CanBeConvertedOptimistic(
                ArrayOfConstrains(),
                ArrayOfConstrains(StatePrimitive.U8, StatePrimitive.Real))
            .AssertTrue();

    [Test]
    public void ArrayConstrainsToConstraints_returnsTrue2() =>
        SolvingFunctions.CanBeConvertedOptimistic(
                ArrayOfConstrains(StatePrimitive.Char),
                ArrayOfConstrains(StatePrimitive.U8))
            .AssertTrue();

    [Test]
    public void ArrayConstrainsToConstraints_returnsTrue3() =>
        SolvingFunctions.CanBeConvertedOptimistic(
                ArrayOfConstrains(StatePrimitive.Ip),
                ArrayOfConstrains())
            .AssertTrue();

    [Test]
    public void ArrayConstrainsToConstraints_returnsTrue4() =>
        SolvingFunctions.CanBeConvertedOptimistic(
                ArrayOfConstrains(StatePrimitive.I32, StatePrimitive.I64),
                ArrayOfConstrains(StatePrimitive.I64, StatePrimitive.I96))
            .AssertTrue();

    [Test]
    public void ArrayToPrimitive_returnsTrue() =>
        SolvingFunctions.CanBeConvertedOptimistic(
                StateArray.Of(StatePrimitive.I32),
                StatePrimitive.Any)
            .AssertTrue();

    [Test]
    public void ArrayToPrimitive_returnsFalse() =>
        SolvingFunctions.CanBeConvertedOptimistic(
                StateArray.Of(StatePrimitive.I32),
                StatePrimitive.Ip)
            .AssertFalse();

    [Test]
    public void PrimitiveToArray_returnsFalse() =>
        SolvingFunctions.CanBeConvertedOptimistic(
                StatePrimitive.Ip,
                StateArray.Of(StatePrimitive.I32))
            .AssertFalse();

    [Test]
    public void ArrayToArray_returnsTrue() =>
        SolvingFunctions.CanBeConvertedOptimistic(
                StateArray.Of(StateArray.Of(StateArray.Of(StatePrimitive.U8))),
                ArrayOfConstrains(StateArray.Of(StatePrimitive.U8)))
            .AssertTrue();

    private static StateArray ArrayOfConstrains(ITicNodeState desc = null, StatePrimitive anc = null, bool isComparable = false)
        => StateArray.Of(TicNode.CreateNamedNode("foo", new ConstrainsState(desc, anc, isComparable)));
}
