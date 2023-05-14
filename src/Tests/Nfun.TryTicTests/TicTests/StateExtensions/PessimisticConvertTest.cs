namespace NFun.UnitTests.TicTests.StateExtensions;

using NFun.TestTools;
using NFun.Tic;
using NFun.Tic.SolvingStates;
using NUnit.Framework;
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

    [Test]
    public void Сonstrains1() =>
        ConstrainsState.Of(U8,I64).CanBeConvertedPessimisticTo(ConstrainsState.Of(I64, Real))
            .AssertTrue();

    [Test]
    public void Сonstrains2() =>
        ConstrainsState.Of(U8,I64).CanBeConvertedPessimisticTo(ConstrainsState.Of(I32, Real))
            .AssertFalse();

    [Test]
    public void Сonstrains3() =>
        ConstrainsState.Empty.CanBeConvertedPessimisticTo(ConstrainsState.Empty)
            .AssertFalse();

    [Test]
    public void Array1() =>
        StateArray.Of(U8).CanBeConvertedPessimisticTo(StateArray.Of(ConstrainsState.Of(I32, Real)))
            .AssertTrue();

    [Test]
    public void Array2() =>
        StateArray.Of(ConstrainsState.Of(I32, Real)).CanBeConvertedPessimisticTo(StateArray.Of(I64))
            .AssertFalse();

    [Test]
    public void Array3() =>
        StateArray.Of(ConstrainsState.Of(I32, Real)).CanBeConvertedPessimisticTo(Any)
            .AssertTrue();

    [Test]
    public void Fun1() =>
        StateFun.Of(ConstrainsState.Of(I32, Real), Real)
            .CanBeConvertedPessimisticTo(
                StateFun.Of(I64, I32))
            .AssertFalse();

    [Test]
    public void Fun2() =>
        StateFun.Of(ConstrainsState.Of(I32, Real), Real)
            .CanBeConvertedPessimisticTo(
                StateFun.Of(ConstrainsState.Of(I32, Real), ConstrainsState.Of(I32)))
            .AssertFalse();

    [Test]
    public void Fun3() =>
        StateFun.Of(ConstrainsState.Of(I32, Real), Real)
            .CanBeConvertedPessimisticTo(
                StateFun.Of(ConstrainsState.Of(U8, I32), Any))
            .AssertTrue();

    [Test]
    public void Struct1() =>
        StateStruct.Of(("a", I32), ("b", I32))
            .CanBeConvertedPessimisticTo(
                StateStruct.Of("a", I32)).AssertTrue();

    [Test]
    public void Struct2() =>
        StateStruct.Of(
                ("a",ConstrainsState.Of(I32, Real)),
                ("b", I32))
            .CanBeConvertedPessimisticTo(
                StateStruct.Of("a", I32)).AssertFalse();

    [Test]
    public void Struct3() =>
        StateStruct.Empty().CanBeConvertedPessimisticTo(
                StateStruct.Of("a", I32)).AssertFalse();

    [Test]
    public void Struct4() =>
        StateStruct.Empty().CanBeConvertedPessimisticTo(StateStruct.Empty()).AssertTrue();

    [Test]
    public void Struct5() =>
        StateStruct.Of(("a", I32))
            .CanBeConvertedPessimisticTo(
                ConstrainsState.Of(StateStruct.Of("a", I32))).AssertTrue();


}
