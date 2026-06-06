namespace NFun.Tic.Tests.UnitTests;

using NFun.Interpretation.Functions;
using NUnit.Framework;
using SolvingStates;

/// <summary>
/// Stage C.4a — verify GenericConstrains carries CompositeAncestor and that
/// GraphBuilder.InitializeCompositeVarNode emits a StateCompositeConstraints
/// node carrying the cap.
/// </summary>
public class GenericConstrainsCompositeTest {

    [Test]
    public void Enumerable_FactoryHasEnumerableAncestor() {
        var c = GenericConstrains.Enumerable;
        Assert.IsTrue(c.HasCompositeAncestor);
        Assert.AreEqual(ConstructorKind.Enumerable, c.CompositeAncestor);
    }

    [Test]
    public void IndexedRead_FactoryHasFixedArrayAncestor() {
        var c = GenericConstrains.IndexedRead;
        Assert.IsTrue(c.HasCompositeAncestor);
        Assert.AreEqual(ConstructorKind.FixedArray, c.CompositeAncestor);
    }

    [Test]
    public void IndexedMutable_FactoryHasArrayAncestor() {
        var c = GenericConstrains.IndexedMutable;
        Assert.IsTrue(c.HasCompositeAncestor);
        Assert.AreEqual(ConstructorKind.Array, c.CompositeAncestor);
    }

    [Test]
    public void NonCompositeFactories_HaveNoCompositeAncestor() {
        Assert.IsFalse(GenericConstrains.Any.HasCompositeAncestor);
        Assert.IsFalse(GenericConstrains.Comparable.HasCompositeAncestor);
        Assert.IsFalse(GenericConstrains.Arithmetical.HasCompositeAncestor);
        Assert.IsFalse(GenericConstrains.Numbers.HasCompositeAncestor);
    }

    [Test]
    public void ToString_CompositeAncestor_FormattedAsCap() {
        Assert.AreEqual("⊆Enumerable", GenericConstrains.Enumerable.ToString());
        Assert.AreEqual("⊆FixedArray", GenericConstrains.IndexedRead.ToString());
        Assert.AreEqual("⊆Array", GenericConstrains.IndexedMutable.ToString());
    }

    [Test]
    public void GraphBuilder_InitializeCompositeVarNode_EmitsCompCS() {
        var graph = new GraphBuilder();
        var refTo = graph.InitializeCompositeVarNode(ConstructorKind.Enumerable);
        var state = refTo.Node.State;
        Assert.IsInstanceOf<StateCompositeConstraints>(state);
        var cc = (StateCompositeConstraints)state;
        Assert.AreEqual(ConstructorKind.Enumerable, cc.Ancestor);
        Assert.IsFalse(cc.HasDescendant);
        Assert.IsFalse(cc.IsOptional);
    }

    [Test]
    public void GraphBuilder_InitializeCompositeVarNode_NullAncestor_NoCap() {
        var graph = new GraphBuilder();
        var refTo = graph.InitializeCompositeVarNode(null);
        var state = refTo.Node.State;
        Assert.IsInstanceOf<StateCompositeConstraints>(state);
        var cc = (StateCompositeConstraints)state;
        Assert.IsFalse(cc.HasAncestor);
        Assert.IsFalse(cc.HasDescendant);
    }

    [Test]
    public void GraphBuilder_InitializeCompositeVarNode_HasOwnElementNode() {
        var graph = new GraphBuilder();
        var a = graph.InitializeCompositeVarNode(ConstructorKind.Enumerable);
        var b = graph.InitializeCompositeVarNode(ConstructorKind.Enumerable);
        var ccA = (StateCompositeConstraints)a.Node.State;
        var ccB = (StateCompositeConstraints)b.Node.State;
        // Distinct CompCS nodes must have distinct element nodes (no global sharing).
        Assert.AreNotSame(ccA.ElementNode, ccB.ElementNode);
    }
}
