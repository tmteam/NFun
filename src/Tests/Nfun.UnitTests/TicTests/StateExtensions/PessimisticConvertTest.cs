namespace NFun.UnitTests.TicTests.StateExtensions;

using TestTools;
using Tic;
using NFun.Tic.Algebra;
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
        EmptyConstraints.CanBeConvertedPessimisticTo(EmptyConstraints)
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

    #region Optional

    [Test]
    public void None_To_OptI32() => None.CanBeConvertedPessimisticTo(Optional(I32)).AssertTrue();

    [Test]
    public void None_To_OptBool() => None.CanBeConvertedPessimisticTo(Optional(Bool)).AssertTrue();

    [Test]
    public void None_To_I32_Fails() => None.CanBeConvertedPessimisticTo(I32).AssertFalse();

    [Test]
    public void None_To_Bool_Fails() => None.CanBeConvertedPessimisticTo(Bool).AssertFalse();

    [Test]
    public void None_To_None() => None.CanBeConvertedPessimisticTo(None).AssertTrue();

    [Test]public void None_To_Any() => None.CanBeConvertedPessimisticTo(Any).AssertTrue();

    [Test]
    public void OptI32_To_OptReal() => ((ITicNodeState)Optional(I32)).CanBeConvertedPessimisticTo(Optional(Real)).AssertTrue();

    [Test]
    public void OptI32_To_OptI32() => ((ITicNodeState)Optional(I32)).CanBeConvertedPessimisticTo(Optional(I32)).AssertTrue();

    [Test]
    public void OptI32_To_Real_Fails() => ((ITicNodeState)Optional(I32)).CanBeConvertedPessimisticTo(Real).AssertFalse();

    [Test]
    public void OptI32_To_I32_Fails() => ((ITicNodeState)Optional(I32)).CanBeConvertedPessimisticTo(I32).AssertFalse();

    [Test]public void OptI32_To_Any() => ((ITicNodeState)Optional(I32)).CanBeConvertedPessimisticTo(Any).AssertTrue();

    [Test]
    public void I32_To_OptI32() => I32.CanBeConvertedPessimisticTo(Optional(I32)).AssertTrue();

    [Test]
    public void I32_To_OptReal() => I32.CanBeConvertedPessimisticTo(Optional(Real)).AssertTrue();

    [Test]
    public void I32_To_OptBool_Fails() => I32.CanBeConvertedPessimisticTo(Optional(Bool)).AssertFalse();

    [Test]public void OptArrayI32_To_Any() =>
        ((ITicNodeState)Optional(Array(I32))).CanBeConvertedPessimisticTo(Any).AssertTrue();

    [Test]
    public void OptArrayI32_To_ArrayI32_Fails() =>
        ((ITicNodeState)Optional(Array(I32))).CanBeConvertedPessimisticTo(Array(I32)).AssertFalse();

    #endregion
}
