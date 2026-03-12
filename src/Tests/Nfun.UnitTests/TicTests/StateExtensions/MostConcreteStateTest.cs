namespace NFun.UnitTests.TicTests.StateExtensions;

using Tic;
using NFun.Tic.Algebra;
using NFun.Tic.SolvingStates;
using NUnit.Framework;
using static SolvingStates;
using static Tic.SolvingStates.StatePrimitive;

public class MostConcreteStateTest {
    [Test]
    public void Primitive1() => Assert.AreEqual(U24, U24.Concretest());

    [Test]
    public void Constraint1() => Assert.AreEqual(EmptyConstraints, EmptyConstraints.Concretest());

    [Test]
    public void Constraint2() =>
        // if some type is comparable, then it most concrete type has to be comparable
        Assert.AreEqual(
            Constrains(isComparable: true),
            Constrains(isComparable: true).Concretest());

    [Test]
    public void Constraint3() => Assert.AreEqual(U24, Constrains(U24, Real).Concretest());

    [Test]
    public void Constraint4() => Assert.AreEqual(U24, Constrains(U24, Real).Concretest());

    [Test]
    public void Constraint5()
        => Assert.AreEqual(
            Array(U24).StateDescription,
            Constrains(Array(Constrains(U24, Real)), Any)
                .Concretest().StateDescription);

    [Test]
    public void Array1() {
        var foo = Array(U24);
        Assert.AreEqual(foo, foo.Concretest());
    }

    [Test]
    public void Array2() {
        var foo = Array(Constrains(U16, Real));
        Assert.AreEqual(Array(U16), foo.Concretest());
    }

    [Test]
    public void Array3() {
        var foo = Array(EmptyConstraints);
        Assert.AreEqual(Array(EmptyConstraints), foo.Concretest());
    }

    [Test]
    public void Fun1() {
        var foo = Fun(Any, U24);
        Assert.AreEqual(foo, foo.Concretest());
    }

    [Test]
    public void Fun2() {
        var foo = Fun(
            Constrains(U16, Real),
            Constrains(I16, I64),
            Constrains(U32, I64));

        Assert.AreEqual(
            Fun(Real, I64, U32),
            foo.Concretest());
    }

    [Test]
    public void Fun3() {
        var foo = Fun(
            Fun(Constrains(U16, Real), Constrains(I16, I64)),
            Array(Constrains(U16, I64)),
            Fun(Constrains(U16, Real), Constrains(I16, I64)));

        Assert.AreEqual(
            Fun(
                Fun(U16, I64),
                Array(I64),
                Fun(Real, I16)).StateDescription,
            foo.Concretest().StateDescription);
    }

    [Test]
    public void Fun4() =>
        Assert.AreEqual(
            Fun(Any, EmptyConstraints),
            Fun(EmptyConstraints, EmptyConstraints).Concretest());

    //actually, it is impossible to say most concrete type of struct, as it contain any possible field name
    [Test]
    public void Struct1() => Assert.AreEqual(EmptyStruct(), EmptyStruct().Concretest());

    [Test]
    public void Struct2() =>
        Assert.AreEqual(Struct("foo", I64), Struct("foo", I64).Concretest());

    [Test]
    public void Struct3() =>
        Assert.AreEqual(
            Struct("foo", Constrains(U24, Real)),
            Struct("foo", Constrains(U24, Real)).Concretest());

    // ═══════════════════════════════════════════════════════════════
    // Optional Concretest
    //
    // Invariant: Concretest(opt(T)) = opt(Concretest(T))
    // Exception: Concretest(opt(Any)) = Any (opt(Any) = Any)
    // ═══════════════════════════════════════════════════════════════

    [Test]
    public void Opt_Primitive() =>
        Assert.AreEqual(Optional(I32), Optional(I32).Concretest());

    [Test]
    public void Opt_Any_CollapseToAny() =>
        // opt(any) = any is a design choice
        Assert.AreEqual(Any, Optional(Any).Concretest());

    [Test]
    public void Opt_ConstraintsWithDesc() =>
        // opt(C[U8..Real]) → opt(U8) — inner Concretest resolves to descendant
        Assert.AreEqual(Optional(U8), Optional(Constrains(U8, Real)).Concretest());

    [Test]
    public void Opt_EmptyConstraints() =>
        // opt(C[]) → opt(C[]) — no descendant to resolve
        Assert.AreEqual(Optional(EmptyConstraints), Optional(EmptyConstraints).Concretest());

    [Test]
    public void Opt_Array() =>
        Assert.AreEqual(Optional(Array(I32)), Optional(Array(I32)).Concretest());

    [Test]
    public void Opt_ArrayWithConstraints() =>
        Assert.AreEqual(
            Optional(Array(U16)).StateDescription,
            Optional(Array(Constrains(U16, Real))).Concretest().StateDescription);

    [Test]
    public void Opt_Struct() =>
        Assert.AreEqual(
            Optional(Struct("a", I32)),
            Optional(Struct("a", I32)).Concretest());

    [Test]
    public void Opt_Bool() =>
        Assert.AreEqual(Optional(Bool), Optional(Bool).Concretest());

    [Test]
    public void Opt_None() =>
        // opt(None) stays opt(None) — Concretest of None is None
        Assert.AreEqual(Optional(None), Optional(None).Concretest());
}
