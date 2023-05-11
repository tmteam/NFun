namespace NFun.UnitTests.TicTests;

using NUnit.Framework;
using TestTools;
using Tic;
using Tic.SolvingStates;
using static Tic.SolvingStates.StatePrimitive;

public class PessimisticConvertTest {

    [TestCase(PrimitiveTypeName.I64, PrimitiveTypeName.Real)]
    [TestCase(PrimitiveTypeName.Char, PrimitiveTypeName.Any)]
    public void PrimitiveToPrimitive_returnsTrue(PrimitiveTypeName from, PrimitiveTypeName to) =>
        new StatePrimitive(from).CanBeConvertedPessimisticTo(new StatePrimitive(to))
            .AssertTrue();

    [TestCase(PrimitiveTypeName.I64, PrimitiveTypeName.I32)]
    [TestCase(PrimitiveTypeName.Char, PrimitiveTypeName.Ip)]
    public void PrimitiveToPrimitive_returnsFalse(PrimitiveTypeName from, PrimitiveTypeName to) =>
        new StatePrimitive(from).CanBeConvertedPessimisticTo(new StatePrimitive(to))
            .AssertFalse();

    [Test]
    public void FromAnyToConstraints_returnsFalse() =>
        Any.CanBeConvertedPessimisticTo(ConstrainsState.Of(I64, Real))
            .AssertFalse();

    [Test]
    public void FromAnyToConstraints_returnsTrue() =>
        Any.CanBeConvertedPessimisticTo(ConstrainsState.Of(Any))
            .AssertTrue();
}
