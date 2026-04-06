namespace NFun.UnitTests.TicTests.StateExtensions;

using Tic;
using NFun.Tic.Algebra;
using NFun.Tic.SolvingStates;
using NUnit.Framework;
using static SolvingStates;
using static Tic.SolvingStates.StatePrimitive;

public class MostAbstractStateTest {
    [Test]
    public void Primitive1() => Assert.AreEqual(U24, U24.Abstractest());

    [Test]
    public void Constraint1() => Assert.AreEqual(Any, EmptyConstraints.Abstractest());

    [Test]
    public void Constraint2() =>
        // if some type is comparable, then it most abstract type is comparable
        Assert.AreEqual(
            Constrains(isComparable: true),
            Constrains(isComparable: true).Abstractest());

    [Test]
    public void Constraint3() => Assert.AreEqual(Real, Constrains(U24, Real).Abstractest());

    [Test]
    public void Constraint4() => Assert.AreEqual(Real, Constrains(U24, Real).Abstractest());

    [Test]
    public void Constraint5()
        => Assert.AreEqual(
            Any,
            Constrains(Array(Constrains(U24, Real)), Any).Abstractest());

    [Test]
    public void Array1() {
        var foo = Array(U24);
        Assert.AreEqual(foo, foo.Abstractest());
    }

    [Test]
    public void Array2() =>
        Assert.AreEqual(Array(Real), Array(Constrains(U16, Real)).Abstractest());

    [Test]
    public void Array3() =>
        Assert.AreEqual(Array(Any), Array(EmptyConstraints).Abstractest());

    [Test]
    public void Fun1() {
        var foo = Fun(I64, U24);
        Assert.AreEqual(foo, foo.Abstractest());
    }

    [Test]
    public void Fun2() {
        var foo = Fun(
            Constrains(U16, Real),
            Constrains(I16, I64),
            Constrains(U32, I64));

        Assert.AreEqual(
            Fun(U16, I16, I64),
            foo.Abstractest());
    }

    [Test]
    public void Fun3() {
        var foo = Fun(
            Fun(Constrains(U16, Real), Constrains(I16, I64)),
            Array(Constrains(U16, I64)),
            Fun(Constrains(U16, Real), Constrains(I16, I64)));

        Assert.AreEqual(
            Fun(
                Fun(Real, I16),
                Array(U16),
                Fun(U16, I64)).StateDescription,
            foo.Abstractest().StateDescription);
    }

    [Test]
    public void Fun4() =>
        Assert.AreEqual(
            Fun(EmptyConstraints, Any).StateDescription,
            Fun(EmptyConstraints, EmptyConstraints).Abstractest().StateDescription);

    [Test]
    public void Struct1() => Assert.AreEqual(EmptyStruct(), EmptyStruct().Abstractest());

    [Test]
    public void Struct2() =>
        Assert.AreEqual(Struct("foo", I64), Struct("foo", I64).Abstractest());

    [Test]
    public void Struct3() =>
        Assert.AreEqual(
            Struct("foo", Constrains(U24, Real)),
            Struct("foo", Constrains(U24, Real)).Abstractest());

    [Test]
    public void Struct4() =>
        Assert.AreEqual(
            Struct("foo", EmptyConstraints),
            Struct("foo", EmptyConstraints).Abstractest());

    #region Optional

    [Test]
    public void OptI32() => Assert.AreEqual(Optional(I32), Optional(I32).Abstractest());

    [Test]
    public void OptReal() => Assert.AreEqual(Optional(Real), Optional(Real).Abstractest());

    [Test]
    public void OptBool() => Assert.AreEqual(Optional(Bool), Optional(Bool).Abstractest());

    [Test]
    public void OptAny_CollapsesToAny() => Assert.AreEqual(Any, Optional(Any).Abstractest());

    [Test]
    public void NoneStaysNone() => Assert.AreEqual(StatePrimitive.None, StatePrimitive.None.Abstractest());

    [Test]
    public void OptArrayI32() =>
        Assert.AreEqual(Optional(Array(I32)), Optional(Array(I32)).Abstractest());

    [Test]
    public void OptArrayAny_StaysOptArrayAny() =>
        Assert.AreEqual(Optional(Array(Any)), Optional(Array(Any)).Abstractest());

    [Test]
    public void CS_IsOptional_NoAnc() =>
        Assert.AreEqual(Any, ((ITicNodeState)ConstraintsState.Of(isOptional: true)).Abstractest());

    [Test]
    public void CS_IsOptional_WithAnc() {
        var cs = ConstraintsState.Of(null, Real, false);
        cs.AddDescendant(StatePrimitive.None);
        Assert.AreEqual(Real, ((ITicNodeState)cs).Abstractest());
    }

    #endregion
}
