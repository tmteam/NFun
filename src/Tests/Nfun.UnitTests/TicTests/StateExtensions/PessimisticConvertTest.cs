namespace NFun.UnitTests.TicTests.StateExtensions;

using TestTools;
using Tic;
using NFun.Tic.SolvingStates;
using NUnit.Framework;
using static SolvingStates;
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
        Any.CanBeConvertedPessimisticTo(Constrains(I64, Real))
            .AssertFalse();

    [Test]
    public void FromAnyToConstraints_returnsTrue() =>
        Any.CanBeConvertedPessimisticTo(Constrains(Any))
            .AssertTrue();

    [Test]
    public void Сonstrains1() =>
        Constrains(U8,I64).CanBeConvertedPessimisticTo(Constrains(I64, Real))
            .AssertTrue();

    [Test]
    public void Сonstrains2() =>
        Constrains(U8,I64).CanBeConvertedPessimisticTo(Constrains(I32, Real))
            .AssertFalse();

    [Test]
    public void Сonstrains3() =>
        EmptyConstrains.CanBeConvertedPessimisticTo(EmptyConstrains)
            .AssertFalse();

    [Test]
    public void Array1() =>
        Array(U8).CanBeConvertedPessimisticTo(Array(Constrains(I32, Real)))
            .AssertTrue();

    [Test]
    public void Array2() =>
        Array(Constrains(I32, Real)).CanBeConvertedPessimisticTo(Array(I64))
            .AssertFalse();

    [Test]
    public void Array3() =>
        Array(Constrains(I32, Real)).CanBeConvertedPessimisticTo(Any)
            .AssertTrue();

    [Test]
    public void Fun1() =>
        Fun(Constrains(I32, Real), Real)
            .CanBeConvertedPessimisticTo(
                Fun(I64, I32))
            .AssertFalse();

    [Test]
    public void Fun2() =>
        Fun(Constrains(I32, Real), Real)
            .CanBeConvertedPessimisticTo(
                Fun(Constrains(I32, Real), Constrains(I32)))
            .AssertFalse();

    [Test]
    public void Fun3() =>
        Fun(Constrains(I32, Real), Real)
            .CanBeConvertedPessimisticTo(
                Fun(Constrains(U8, I32), Any))
            .AssertTrue();

    [Test]
    public void Struct1() =>
        Struct(("a", I32), ("b", I32))
            .CanBeConvertedPessimisticTo(
                Struct("a", I32)).AssertTrue();

    [Test]
    public void Struct2() =>
        Struct(
                ("a",Constrains(I32, Real)),
                ("b", I32))
            .CanBeConvertedPessimisticTo(
                Struct("a", I32)).AssertFalse();

    [Test]
    public void Struct3() =>
        EmptyStruct().CanBeConvertedPessimisticTo(
                Struct("a", I32)).AssertFalse();

    [Test]
    public void Struct4() =>
        EmptyStruct().CanBeConvertedPessimisticTo(StateStruct.Empty()).AssertTrue();

    [Test]
    public void Struct5() =>
        Struct(("a", I32))
            .CanBeConvertedPessimisticTo(
                Constrains(Struct("a", I32))).AssertTrue();


}
