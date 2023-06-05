namespace NFun.Tic.Tests.UnitTests;

using NUnit.Framework;
using SolvingStates;

public class ConstraintsTest {
    [Test]
    public void GetOptimized1() {
        var constrains = ConstrainsState.Of(ConstrainsState.Of(isComparable: true), StatePrimitive.Real);
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
}
