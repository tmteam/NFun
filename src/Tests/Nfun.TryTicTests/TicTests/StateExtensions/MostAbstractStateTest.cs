namespace NFun.UnitTests.TicTests.StateExtensions;

using NFun.Tic;
using NFun.Tic.SolvingStates;
using NUnit.Framework;
using static Tic.SolvingStates.StatePrimitive;

public class MostAbstractStateTest {
    [Test]
    public void Primitive1() => Assert.AreEqual(U24, U24.Abstractest());

    [Test]
    public void Constraint1() => Assert.AreEqual(Any, ConstrainsState.Empty.Abstractest());

    [Test]
    public void Constraint2() =>
        // if some type is comparable, then it most abstract type is comparable
        Assert.AreEqual(
            ConstrainsState.Of(isComparable: true),
            ConstrainsState.Of(isComparable: true).Abstractest());

    [Test]
    public void Constraint3() => Assert.AreEqual(Real, ConstrainsState.Of(U24, Real).Abstractest());

    [Test]
    public void Constraint4() => Assert.AreEqual(Real, ConstrainsState.Of(U24, Real).Abstractest());

    [Test]
    public void Constraint5()
        => Assert.AreEqual(
            Any,
            ConstrainsState.Of(StateArray.Of(ConstrainsState.Of(U24, Real)), Any).Abstractest());

    [Test]
    public void Array1() {
        var foo = StateArray.Of(U24);
        Assert.AreEqual(foo, foo.Abstractest());
    }

    [Test]
    public void Array2() =>
        Assert.AreEqual(StateArray.Of(Real), StateArray.Of(ConstrainsState.Of(U16, Real)).Abstractest());

    [Test]
    public void Array3() =>
        Assert.AreEqual(StateArray.Of(Any), StateArray.Of(ConstrainsState.Empty).Abstractest());

    [Test]
    public void Fun1() {
        var foo = StateFun.Of(I64, U24);
        Assert.AreEqual(foo, foo.Abstractest());
    }

    [Test]
    public void Fun2() {
        var foo = StateFun.Of(
            ConstrainsState.Of(U16, Real),
            ConstrainsState.Of(I16, I64),
            ConstrainsState.Of(U32, I64));

        Assert.AreEqual(
            StateFun.Of(U16, I16, I64),
            foo.Abstractest());
    }

    [Test]
    public void Fun3() {
        var foo = StateFun.Of(
            StateFun.Of(ConstrainsState.Of(U16, Real), ConstrainsState.Of(I16, I64)),
            StateArray.Of(ConstrainsState.Of(U16, I64)),
            StateFun.Of(ConstrainsState.Of(U16, Real), ConstrainsState.Of(I16, I64)));

        Assert.AreEqual(
            StateFun.Of(
                StateFun.Of(Real, I16),
                StateArray.Of(U16),
                StateFun.Of(U16, I64)).StateDescription,
            foo.Abstractest().StateDescription);
    }

    [Test]
    public void Fun4() =>
        Assert.AreEqual(
            StateFun.Of(ConstrainsState.Empty, Any).StateDescription,
            StateFun.Of(ConstrainsState.Empty, ConstrainsState.Empty).Abstractest().StateDescription);

    [Test]
    public void Struct1() => Assert.AreEqual(StateStruct.Of(), StateStruct.Of().Abstractest());

    [Test]
    public void Struct2() =>
        Assert.AreEqual(StateStruct.Of("foo", I64), StateStruct.Of("foo", I64).Abstractest());

    [Test]
    public void Struct3() =>
        Assert.AreEqual(
            StateStruct.Of("foo", ConstrainsState.Of(U24, Real)),
            StateStruct.Of("foo", ConstrainsState.Of(U24, Real)).Abstractest());

    [Test]
    public void Struct4() =>
        Assert.AreEqual(
            StateStruct.Of("foo", ConstrainsState.Empty),
            StateStruct.Of("foo", ConstrainsState.Empty).Abstractest());

}
