using NFun.Tic.SolvingStates;
using NUnit.Framework;

namespace NFun.Tic.Tests.UnitTests;

using static StatePrimitive;

class FunTest {
    [Test]
    public void ConcreteTypes_SameTypes_EqualsReturnsTrue() {
        var funA = StateFun.Of(Any, I32);
        var funB = StateFun.Of(Any, I32);
        Assert.IsTrue(funA.Equals(funB));
    }

    [Test]
    public void ConcreteTypes_DifferentArgs_EqualsReturnsFalse() {
        var funA = StateFun.Of(Any, I32);
        var funB = StateFun.Of(Any, Real);
        Assert.IsFalse(funA.Equals(funB));
    }

    [Test]
    public void ConcreteTypes_DifferentReturns_EqualsReturnsFalse() {
        var funA = StateFun.Of(Any, I32);
        var funB = StateFun.Of(Real, I32);
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
        var fun = StateFun.Of(Any, I32);
        Assert.IsTrue(fun.IsSolved);
    }

    [Test]
    public void GenericTypes_IsSolvedReturnsFalse() {
        var fun = StateFun.Of(CreateConstrainsNode(), CreateConstrainsNode());
        Assert.IsFalse(fun.IsSolved);
    }

    [Test]
    public void GetLastCommonAncestorOrNull_SameConcreteTypes_ReturnsEqualType() {
        var funA = StateFun.Of(Any, I32);
        var funB = StateFun.Of(Any, I32);
        var ancestor = funA.GetLastCommonAncestorOrNull(funB);
        Assert.AreEqual(funA, ancestor);
        var ancestor2 = funB.GetLastCommonAncestorOrNull(funA);
        Assert.AreEqual(ancestor2, ancestor);
    }

    [Test]
    public void GetLastCommonAncestorOrNull_ConcreteType_ReturnsAncestor() {
        var funA = StateFun.Of(I32, I64);
        var funB = StateFun.Of(U16, U64);
        var expected = StateFun.Of(U16, I96);

        Assert.AreEqual(expected, funA.GetLastCommonAncestorOrNull(funB));
        Assert.AreEqual(expected, funB.GetLastCommonAncestorOrNull(funA));
    }

    [Test]
    public void GetLastCommonAncestorOrNull_NotConcreteTypes_ReturnsNull() {
        var funA = StateFun.Of(CreateConstrainsNode(), TicNode.CreateTypeVariableNode(I32));
        var funB = StateFun.Of(CreateConstrainsNode(), TicNode.CreateTypeVariableNode(I32));

        Assert.IsNull(funA.GetLastCommonAncestorOrNull(funB));
        Assert.IsNull(funB.GetLastCommonAncestorOrNull(funA));
    }

    [Test]
    public void GetLastCommonAncestorOrNull_ConcreteAndNotConcreteType_ReturnsNull() {
        var funA = StateFun.Of(CreateConstrainsNode(), TicNode.CreateTypeVariableNode(I32));
        var funB = StateFun.Of(U16, U64);

        Assert.IsNull(funA.GetLastCommonAncestorOrNull(funB));
        Assert.IsNull(funB.GetLastCommonAncestorOrNull(funA));
    }

    private TicNode CreateConstrainsNode()
        => TicNode.CreateTypeVariableNode("", new ConstrainsState());
}
