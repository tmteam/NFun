namespace NFun.UnitTests.TicTests.StateExtensions;

using NFun.Tic;
using NFun.Tic.SolvingStates;
using NUnit.Framework;
using static Tic.SolvingStates.StatePrimitive;

public class MostConcreteStateTest {
    [Test]
    public void Primitive1() => Assert.AreEqual(U24, U24.Concretest());

    [Test]
    public void Constraint1() => Assert.AreEqual(ConstrainsState.Empty, ConstrainsState.Empty.Concretest());

    [Test]
    public void Constraint2() =>
        // if some type is comparable, then it most concrete type has to be comparable
        Assert.AreEqual(
            ConstrainsState.Of(isComparable: true),
            ConstrainsState.Of(isComparable: true).Concretest());

    [Test]
    public void Constraint3() => Assert.AreEqual(U24, ConstrainsState.Of(U24, Real).Concretest());

    [Test]
    public void Constraint4() => Assert.AreEqual(U24, ConstrainsState.Of(U24, Real).Concretest());

    [Test]
    public void Constraint5()
        => Assert.AreEqual(
            StateArray.Of(U24).StateDescription,
            ConstrainsState.Of(StateArray.Of(ConstrainsState.Of(U24, Real)), Any)
                .Concretest().StateDescription);

    [Test]
    public void Array1() {
        var foo = StateArray.Of(U24);
        Assert.AreEqual(foo, foo.Concretest());
    }

    [Test]
    public void Array2() {
        var foo = StateArray.Of(ConstrainsState.Of(U16, Real));
        Assert.AreEqual(StateArray.Of(U16), foo.Concretest());
    }

    [Test]
    public void Array3() {
        var foo = StateArray.Of(ConstrainsState.Empty);
        Assert.AreEqual(StateArray.Of(ConstrainsState.Empty), foo.Concretest());
    }

    [Test]
    public void Fun1() {
        var foo = StateFun.Of(Any, U24);
        Assert.AreEqual(foo, foo.Concretest());
    }

    [Test]
    public void Fun2() {
        var foo = StateFun.Of(
            ConstrainsState.Of(U16, Real),
            ConstrainsState.Of(I16, I64),
            ConstrainsState.Of(U32, I64));

        Assert.AreEqual(
            StateFun.Of(Real, I64, U32),
            foo.Concretest());
    }

    [Test]
    public void Fun3() {
        var foo = StateFun.Of(
            StateFun.Of(ConstrainsState.Of(U16, Real), ConstrainsState.Of(I16, I64)),
            StateArray.Of(ConstrainsState.Of(U16, I64)),
            StateFun.Of(ConstrainsState.Of(U16, Real), ConstrainsState.Of(I16, I64)));

        Assert.AreEqual(
            StateFun.Of(
                StateFun.Of(U16, I64),
                StateArray.Of(I64),
                StateFun.Of(Real, I16)).StateDescription,
            foo.Concretest().StateDescription);
    }

    [Test]
    public void Fun4() =>
        Assert.AreEqual(
            StateFun.Of(Any, ConstrainsState.Empty),
            StateFun.Of(ConstrainsState.Empty, ConstrainsState.Empty).Concretest());

    //actually, it is impossible to say most concrete type of struct, as it contain any possible field name
    [Test]
    public void Struct1() => Assert.AreEqual(StateStruct.Of(), StateStruct.Of().Concretest());

    [Test]
    public void Struct2() =>
        Assert.AreEqual(StateStruct.Of("foo", I64), StateStruct.Of("foo", I64).Concretest());

    [Test]
    public void Struct3() =>
        Assert.AreEqual(
            StateStruct.Of("foo", ConstrainsState.Of(U24, Real)),
            StateStruct.Of("foo", ConstrainsState.Of(U24, Real)).Concretest());
}
