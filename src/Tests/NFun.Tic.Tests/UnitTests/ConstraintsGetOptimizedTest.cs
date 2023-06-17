namespace NFun.Tic.Tests.UnitTests;

using NUnit.Framework;
using SolvingStates;
using static SolvingStates.StatePrimitive;

public class ConstraintsGetOptimizedTest {
    [Test]
    public void Primitive1() {
        var constrains = ConstrainsState.Of(
            ConstrainsState.Of(isComparable: true),
            Real);

        var optimized = constrains.GetOptimizedOrNull() as ConstrainsState;
        Assert.IsNotNull(optimized);
        Assert.AreEqual(null, optimized.Descendant); //todo: byte can be there
        Assert.AreEqual(Real, optimized.Ancestor);
        //todo: no matter if it is comparable or not Assert.AreEqual(true, optimized.IsComparable);
    }

    [Test]
    public void NoConstrains1() {
        var constrains = ConstrainsState.Of(ConstrainsState.Of());
        var optimized = constrains.GetOptimizedOrNull() as ConstrainsState;
        Assert.IsNotNull(optimized);
        Assert.AreEqual(null, optimized.Descendant);
        Assert.AreEqual(null, optimized.Ancestor);
        Assert.AreEqual(false, optimized.IsComparable);
    }

    [Test]
    public void Comparable1() {
        var constrains = ConstrainsState.Of(ConstrainsState.Of(isComparable:true), isComparable:true);
        var optimized = constrains.GetOptimizedOrNull() as ConstrainsState;
        Assert.IsNotNull(optimized);
        Assert.AreEqual(null, optimized.Descendant);
        Assert.AreEqual(null, optimized.Ancestor);
        Assert.AreEqual(true, optimized.IsComparable);
    }

    [Test]
    public void Comparable2() {
        //the only one array type, that is comparable - is Text
        var constrains = ConstrainsState.Of(Bool, isComparable: true);
        var optimized = constrains.GetOptimizedOrNull();
        Assert.IsNull(optimized);
    }

    [Test]
    public void Array1() {
        //the only one array type, that is comparable - is Text
        var constrains = ConstrainsState.Of(StateArray.Of(ConstrainsState.Empty), isComparable: true);
        var optimized = constrains.GetOptimizedOrNull();
        Assert.AreEqual(optimized, StateArray.Of(Char));
    }

    [Test]
    public void ComparableText1() {
        //the only one array type, that is comparable - is Text
        var constrains = ConstrainsState.Of(StateArray.Of(Char), isComparable: true);
        var optimized = constrains.GetOptimizedOrNull();
        Assert.AreEqual(optimized, StateArray.Of(Char));
    }

    [Test]
    public void ComparableText2() {
        //the only one array type, that is comparable - is Text
        var constrains = ConstrainsState.Of(StateArray.Of(Bool), isComparable: true);
        var optimized = constrains.GetOptimizedOrNull();
        Assert.IsNull(optimized);
    }

    [Test]
    public void ComparableTex3() {
        //the only one array type, that is comparable - is Text
        var constrains = ConstrainsState.Of(StateArray.Of(ConstrainsState.Of(null, I32)), isComparable: true);
        var optimized = constrains.GetOptimizedOrNull();
        Assert.IsNull(optimized);
    }

    [Test]
    public void ComparableText4() {
        //the only one array type, that is comparable - is Text
        var constrains = ConstrainsState.Of(StateArray.Of(StateArray.Of(ConstrainsState.Empty)), isComparable: true);
        var optimized = constrains.GetOptimizedOrNull();
        Assert.IsNull(optimized);
    }

    [Test]
    public void Struct1() {
        var constrains = ConstrainsState.Of(StateStruct.Of("a", I32));
        var optimized = constrains.GetOptimizedOrNull();
        Assert.AreEqual(optimized, constrains);
    }

    [Test]
    public void Struct2() {
        var constrains = ConstrainsState.Of();
        constrains.AddDescendant(StateStruct.Of("a", I32));
        var optimized = constrains.GetOptimizedOrNull();
        Assert.AreEqual(optimized, ConstrainsState.Of(StateStruct.Of("a", I32)));
    }

    [Test]
    public void Struct3() {
        var constrains = ConstrainsState.Of();
        constrains.AddDescendant(StateStruct.Of(("a", I32), ("b", I32)));
        constrains.AddDescendant(StateStruct.Of("a", I32));
        var optimized = constrains.GetOptimizedOrNull();
        Assert.AreEqual(optimized, ConstrainsState.Of(StateStruct.Of("a", I32)));
    }

    [Test]
    public void Struct4() {
        var constrains = ConstrainsState.Of();
        constrains.AddDescendant(StateStruct.Of(("a", I32), ("b", I32)));
        constrains.AddDescendant(StateStruct.Of("a", Bool));
        var optimized = constrains.GetOptimizedOrNull();
        Assert.AreEqual(optimized, ConstrainsState.Of(StateStruct.Empty()));
    }

    [Test]
    public void Struct5() {
        var constrains = ConstrainsState.Of();
        constrains.AddDescendant(StateStruct.Of("a", I32));
        constrains.AddDescendant(StateStruct.Of("a", ConstrainsState.Of(U8, Real)));
        var optimized = constrains.GetOptimizedOrNull();
        Assert.AreEqual(optimized, ConstrainsState.Of(StateStruct.Of("a", I32)));
    }

    [Test]
    public void Struct6() {
        var constrains = ConstrainsState.Of();
        constrains.AddDescendant(StateStruct.Of("a", ConstrainsState.Of(U8, Real)));
        constrains.AddDescendant(StateStruct.Of("a", I32));
        var optimized = constrains.GetOptimizedOrNull();
        Assert.AreEqual(optimized, ConstrainsState.Of(StateStruct.Of("a", I32)));
    }

    [Test]
    public void Struct7() {
        var constrains = ConstrainsState.Of().GetCopy();
        constrains.AddDescendant(StateStruct.Of("a", I32));
        var opt1 = constrains.GetOptimizedOrNull() as ConstrainsState;
        var opt1Copy = opt1.GetCopy();
        opt1Copy.AddDescendant(StateStruct.Of("a", ConstrainsState.Of(U8, Real)));
        var optimized = opt1Copy.GetOptimizedOrNull();
        Assert.AreEqual(optimized, ConstrainsState.Of(StateStruct.Of("a", I32)));
    }
}

