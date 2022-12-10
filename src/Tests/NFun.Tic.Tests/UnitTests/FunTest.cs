using NFun.Tic.SolvingStates;
using NUnit.Framework;

namespace NFun.Tic.Tests.UnitTests;

class FunTest {
    [Test]
    public void ConcreteTypes_SameTypes_EqualsReturnsTrue() {
        var funA = StateFun.Of(StatePrimitive.Any, StatePrimitive.I32);
        var funB = StateFun.Of(StatePrimitive.Any, StatePrimitive.I32);
        Assert.IsTrue(funA.Equals(funB));
    }

    [Test]
    public void ConcreteTypes_DifferentArgs_EqualsReturnsFalse() {
        var funA = StateFun.Of(StatePrimitive.Any, StatePrimitive.I32);
        var funB = StateFun.Of(StatePrimitive.Any, StatePrimitive.Real);
        Assert.IsFalse(funA.Equals(funB));
    }

    [Test]
    public void ConcreteTypes_DifferentReturns_EqualsReturnsFalse() {
        var funA = StateFun.Of(StatePrimitive.Any, StatePrimitive.I32);
        var funB = StateFun.Of(StatePrimitive.Real, StatePrimitive.I32);
        Assert.IsFalse(funA.Equals(funB));
    }

    [Test]
    public void NonConcreteTypes_SameNodes_EqualsReturnsTrue() {
        var retNode = CreateConstrainsNode();
        var argNode = CreateConstrainsNode();

        var funA = StateFun.Of(retNode, argNode);
        var funB = StateFun.Of(retNode, argNode);
        Assert.IsTrue(funA.Equals(funB));
    }

    [Test]
    public void NonConcreteTypes_DifferentNodes_EqualsReturnsFalse() {
        var retNodeA = CreateConstrainsNode();
        var retNodeB = CreateConstrainsNode();

        var argNode = CreateConstrainsNode();

        var funA = StateFun.Of(retNodeA, argNode);
        var funB = StateFun.Of(retNodeB, argNode);
        Assert.IsFalse(funA.Equals(funB));
    }

    [Test]
    public void ConcreteTypes_IsSolvedReturnsTrue() {
        var fun = StateFun.Of(StatePrimitive.Any, StatePrimitive.I32);
        Assert.IsTrue(fun.IsSolved);
    }

    [Test]
    public void GenericTypes_IsSolvedReturnsFalse() {
        var fun = StateFun.Of(CreateConstrainsNode(), CreateConstrainsNode());
        Assert.IsFalse(fun.IsSolved);
    }

    [Test]
    public void GetLastCommonAncestorOrNull_SameConcreteTypes_ReturnsEqualType() {
        var funA = StateFun.Of(StatePrimitive.Any, StatePrimitive.I32);
        var funB = StateFun.Of(StatePrimitive.Any, StatePrimitive.I32);
        var ancestor = funA.GetLastCommonAncestorOrNull(funB);
        Assert.AreEqual(funA, ancestor);
        var ancestor2 = funB.GetLastCommonAncestorOrNull(funA);
        Assert.AreEqual(ancestor2, ancestor);
    }

    [Test]
    public void GetLastCommonAncestorOrNull_ConcreteType_ReturnsAncestor() {
        var funA = StateFun.Of(StatePrimitive.I32, StatePrimitive.I64);
        var funB = StateFun.Of(StatePrimitive.U16, StatePrimitive.U64);
        var expected = StateFun.Of(StatePrimitive.U16, StatePrimitive.I96);

        Assert.AreEqual(expected, funA.GetLastCommonAncestorOrNull(funB));
        Assert.AreEqual(expected, funB.GetLastCommonAncestorOrNull(funA));
    }

    [Test]
    public void GetLastCommonAncestorOrNull_NotConcreteTypes_ReturnsNull() {
        var funA = StateFun.Of(CreateConstrainsNode(), TicNode.CreateTypeVariableNode(StatePrimitive.I32));
        var funB = StateFun.Of(CreateConstrainsNode(), TicNode.CreateTypeVariableNode(StatePrimitive.I32));

        Assert.IsNull(funA.GetLastCommonAncestorOrNull(funB));
        Assert.IsNull(funB.GetLastCommonAncestorOrNull(funA));
    }

    [Test]
    public void GetLastCommonAncestorOrNull_ConcreteAndNotConcreteType_ReturnsNull() {
        var funA = StateFun.Of(CreateConstrainsNode(), TicNode.CreateTypeVariableNode(StatePrimitive.I32));
        var funB = StateFun.Of(StatePrimitive.U16, StatePrimitive.U64);

        Assert.IsNull(funA.GetLastCommonAncestorOrNull(funB));
        Assert.IsNull(funB.GetLastCommonAncestorOrNull(funA));
    }

    private TicNode CreateConstrainsNode()
        => TicNode.CreateTypeVariableNode("", new ConstrainsState());
}
