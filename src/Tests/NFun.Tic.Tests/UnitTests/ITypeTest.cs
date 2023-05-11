using NFun.Tic.SolvingStates;
using NUnit.Framework;

namespace NFun.Tic.Tests.UnitTests;

using static StatePrimitive;

public class ITypeTest {
    [Test]
    public void GetLastCommonAncestorOrNull_ConcreteFunTypesAndPrimitive_ReturnsAny() {
        var fun = StateFun.Of(I32, I64);

        Assert.AreEqual(Any, fun.GetLastCommonAncestorOrNull(I32));
        Assert.AreEqual(Any, I32.GetLastCommonAncestorOrNull(fun));
    }

    [Test]
    public void GetLastCommonAncestorOrNull_ConcreteFunTypeAndConcreteArray_ReturnsAny() {
        var fun = StateFun.Of(I32, I64);
        var array = StateArray.Of(I64);
        Assert.AreEqual(Any, fun.GetLastCommonAncestorOrNull(array));
        Assert.AreEqual(Any, array.GetLastCommonAncestorOrNull(fun));
    }

    [Test]
    public void GetLastCommonAncestorOrNull_ConcreteFunTypeAndConstrainsArray_ReturnsAny() {
        var fun = StateFun.Of(I32, I64);
        var array = StateArray.Of(CreateConstrainsNode());
        Assert.AreEqual(Any, fun.GetLastCommonAncestorOrNull(array));
        Assert.AreEqual(Any, array.GetLastCommonAncestorOrNull(fun));
    }


    private TicNode CreateConstrainsNode()
        => TicNode.CreateTypeVariableNode("", ConstrainsState.Empty);
}
