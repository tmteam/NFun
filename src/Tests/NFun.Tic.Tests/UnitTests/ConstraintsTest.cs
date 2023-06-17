namespace NFun.Tic.Tests.UnitTests;

using NUnit.Framework;
using SolvingStates;

public class ConstraintsTest {
    [Test]
    public void GetOptimized1() {
        var constrains = ConstrainsState.Of(
            ConstrainsState.Of(isComparable: true),
            StatePrimitive.Real);

        var optimized = constrains.GetOptimizedOrNull() as ConstrainsState;
        Assert.IsNotNull(optimized);
        Assert.AreEqual(null, optimized.Descendant); //todo: byte can be there
        Assert.AreEqual(StatePrimitive.Real, optimized.Ancestor);
        //todo: no matter if it is comparable or not Assert.AreEqual(true, optimized.IsComparable);
    }

    [Test]
    public void GetOptimized2() {
        var constrains = ConstrainsState.Of(ConstrainsState.Of());
        var optimized = constrains.GetOptimizedOrNull() as ConstrainsState;
        Assert.IsNotNull(optimized);
        Assert.AreEqual(null, optimized.Descendant);
        Assert.AreEqual(null, optimized.Ancestor);
        Assert.AreEqual(false, optimized.IsComparable);
    }

    [Test]
    public void GetOptimized3() {
        var constrains = ConstrainsState.Of(ConstrainsState.Of(isComparable:true), isComparable:true);
        var optimized = constrains.GetOptimizedOrNull() as ConstrainsState;
        Assert.IsNotNull(optimized);
        Assert.AreEqual(null, optimized.Descendant);
        Assert.AreEqual(null, optimized.Ancestor);
        Assert.AreEqual(true, optimized.IsComparable);
    }

    [Test]
    public void GetOptimized4() {
        //the only one array type, that is comparable - is Text
        var constrains = ConstrainsState.Of(StateArray.Of(ConstrainsState.Empty), isComparable: true);
        var optimized = constrains.GetOptimizedOrNull();
        Assert.AreEqual(optimized, StateArray.Of(StatePrimitive.Char));
    }

    [Test]
    public void GetOptimized5() {
        //the only one array type, that is comparable - is Text
        var constrains = ConstrainsState.Of(StateArray.Of(StatePrimitive.Char), isComparable: true);
        var optimized = constrains.GetOptimizedOrNull();
        Assert.AreEqual(optimized, StateArray.Of(StatePrimitive.Char));
    }

    [Test]
    public void GetOptimized_returnsNull() {
        //the only one array type, that is comparable - is Text
        var constrains = ConstrainsState.Of(StateArray.Of(StatePrimitive.Bool), isComparable: true);
        var optimized = constrains.GetOptimizedOrNull();
        Assert.IsNull(optimized);
    }

    [Test]
    public void GetOptimized_returnsNull2() {
        //the only one array type, that is comparable - is Text
        var constrains = ConstrainsState.Of(StateArray.Of(ConstrainsState.Of(null, StatePrimitive.I32)), isComparable: true);
        var optimized = constrains.GetOptimizedOrNull();
        Assert.IsNull(optimized);
    }

    [Test]
    public void GetOptimized_returnsNull3() {
        //the only one array type, that is comparable - is Text
        var constrains = ConstrainsState.Of(StateArray.Of(StateArray.Of(ConstrainsState.Empty)), isComparable: true);
        var optimized = constrains.GetOptimizedOrNull();
        Assert.IsNull(optimized);
    }
}
