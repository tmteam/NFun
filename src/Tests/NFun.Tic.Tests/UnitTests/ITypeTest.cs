using NFun.Tic.SolvingStates;
using NUnit.Framework;

namespace NFun.Tic.Tests.UnitTests; 

public class ITypeTest {
    [Test]
    public void GetLastCommonAncestorOrNull_ConcreteFunTypesAndPrimitive_ReturnsAny() {
        var fun = StateFun.Of(StatePrimitive.I32, StatePrimitive.I64);

        Assert.AreEqual(StatePrimitive.Any, fun.GetLastCommonAncestorOrNull(StatePrimitive.I32));
        Assert.AreEqual(StatePrimitive.Any, StatePrimitive.I32.GetLastCommonAncestorOrNull(fun));
    }

    [Test]
    public void GetLastCommonAncestorOrNull_ConcreteFunTypeAndConcreteArray_ReturnsAny() {
        var fun = StateFun.Of(StatePrimitive.I32, StatePrimitive.I64);
        var array = StateArray.Of(StatePrimitive.I64);
        Assert.AreEqual(StatePrimitive.Any, fun.GetLastCommonAncestorOrNull(array));
        Assert.AreEqual(StatePrimitive.Any, array.GetLastCommonAncestorOrNull(fun));
    }

    [Test]
    public void GetLastCommonAncestorOrNull_ConcreteFunTypeAndConstrainsArray_ReturnsAny() {
        var fun = StateFun.Of(StatePrimitive.I32, StatePrimitive.I64);
        var array = StateArray.Of(CreateConstrainsNode());
        Assert.AreEqual(StatePrimitive.Any, fun.GetLastCommonAncestorOrNull(array));
        Assert.AreEqual(StatePrimitive.Any, array.GetLastCommonAncestorOrNull(fun));
    }


    private TicNode CreateConstrainsNode()
        => TicNode.CreateTypeVariableNode("", new ConstrainsState());
}