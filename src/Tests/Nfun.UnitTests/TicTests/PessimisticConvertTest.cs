namespace NFun.UnitTests.TicTests;

using NUnit.Framework;
using TestTools;
using Tic;
using Tic.SolvingStates;

public class PessimisticConvertTest {

    [TestCase(PrimitiveTypeName.I64, PrimitiveTypeName.Real)]
    [TestCase(PrimitiveTypeName.Char, PrimitiveTypeName.Any)]
    public void PrimitiveToPrimitive_returnsTrue(PrimitiveTypeName from, PrimitiveTypeName to) =>
        SolvingFunctions.CanBeConvertedPessimistic(new StatePrimitive(from), new StatePrimitive(to))
            .AssertTrue();

    [TestCase(PrimitiveTypeName.I64, PrimitiveTypeName.I32)]
    [TestCase(PrimitiveTypeName.Char, PrimitiveTypeName.Ip)]
    public void PrimitiveToPrimitive_returnsFalse(PrimitiveTypeName from, PrimitiveTypeName to) =>
        SolvingFunctions.CanBeConvertedPessimistic(new StatePrimitive(from), new StatePrimitive(to))
            .AssertFalse();

    [Test]
    public void FromAnyToConstraints_returnsFalse() =>
        SolvingFunctions.CanBeConvertedPessimistic(StatePrimitive.Any,
                new ConstrainsState(StatePrimitive.I64, StatePrimitive.Real))
            .AssertFalse();

    [Test]
    public void FromAnyToConstraints_returnsTrue() =>
        SolvingFunctions.CanBeConvertedPessimistic(StatePrimitive.Any, new ConstrainsState(StatePrimitive.Any))
            .AssertTrue();
}
