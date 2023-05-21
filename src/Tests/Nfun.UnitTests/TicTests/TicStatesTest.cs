using NFun.Tic;
using NFun.Tic.SolvingStates;
using NUnit.Framework;

namespace NFun.UnitTests;

using static SolvingStates;

public class TicStatesTest {
    [TestCase(PrimitiveTypeName.I16, PrimitiveTypeName.Real)]
    [TestCase(null, PrimitiveTypeName.Real)]
    [TestCase(PrimitiveTypeName.Real, null)]
    [TestCase(null, null)]
    public void ConstrainsTestsAreEqual(PrimitiveTypeName? desc, PrimitiveTypeName? anc) {
        var descState = desc == null ? null : new StatePrimitive(desc.Value);
        var ancState = anc == null ? null : new StatePrimitive(anc.Value);
        var state1 = Constrains(descState, ancState);
        var state2 = Constrains(descState, ancState);
        Assert.IsTrue(state1.Equals(state2));
        Assert.AreEqual(state1, state2);
    }

    [TestCase(PrimitiveTypeName.I16, PrimitiveTypeName.Real)]
    [TestCase(null, PrimitiveTypeName.Real)]
    [TestCase(PrimitiveTypeName.Real, null)]
    [TestCase(null, null)]
    public void ConstrainsTestsAreNotEqual(PrimitiveTypeName? desc, PrimitiveTypeName? anc) {
        var descState = desc == null ? null : new StatePrimitive(desc.Value);
        var ancState = anc == null ? null : new StatePrimitive(anc.Value);
        var state1 = Constrains(descState, ancState);
        var state2 = Constrains(null, StatePrimitive.Any);
        Assert.IsFalse(state1.Equals(state2));
        Assert.AreNotEqual(state1, state2);
    }

    [Test]
    public void RefState_RefToSameNode_EqualReturnsTrue() {
        var node = TicNode.CreateNamedNode("a", EmptyConstrains);
        var state1 = Ref(node);
        var state2 = Ref(node);
        Assert.IsTrue(state1.Equals(state2));
        Assert.AreEqual(state1, state2);
    }

    [Test]
    public void RefState_RefToDifferentNodes_EqualReturnsFalse() {
        var state1 = Ref(TicNode.CreateNamedNode("a", EmptyConstrains));
        var state2 = Ref(TicNode.CreateNamedNode("b", EmptyConstrains));
        Assert.IsFalse(state1.Equals(state2));
        Assert.AreNotEqual(state1, state2);
    }
}
