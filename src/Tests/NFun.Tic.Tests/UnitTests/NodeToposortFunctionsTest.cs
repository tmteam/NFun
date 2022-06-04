namespace NFun.Tic.Tests.UnitTests; 

class NodeToposortFunctionsTest {
    /*
    [Test]
    public void OneNodeReferencesOther_noCycleFound()
    {
        var nodeA = new SolvingNode("a", new Constrains(), SolvingNodeType.TypeVariable);
        var nodeB = new SolvingNode("b", new Constrains(), SolvingNodeType.TypeVariable);
        nodeA.State = new RefTo(nodeB);

        var graph = NodeToposortFunctions.ConvertToArrayGraph(new[] { nodeA, nodeB });

        var sorted = GraphTools.SortTopology(graph);
        Assert.IsFalse(sorted.HasCycle);
        Assert.AreEqual(2, sorted.NodeNames.Count);
    }
    [Test]
    public void TwoNodesReferencesEachOther_CycleFound()
    {
        var nodeA = new SolvingNode("a", new Constrains(), SolvingNodeType.TypeVariable);
        var nodeB = new SolvingNode("b", new Constrains(), SolvingNodeType.TypeVariable);
        nodeA.State = new RefTo(nodeB);
        nodeB.State = new RefTo(nodeA);

        var graph = NodeToposortFunctions.ConvertToArrayGraph(new []{nodeA,nodeB});

        var sorted = GraphTools.SortTopology(graph);
        Assert.IsTrue(sorted.HasCycle);
        Assert.AreEqual(2,sorted.NodeNames.Count);
    }

    [Test]
    public void ThreeNodesReferencesEachOther_CycleFound()
    {
        var nodeA = new SolvingNode("a", new Constrains(), SolvingNodeType.TypeVariable);
        var nodeB = new SolvingNode("b", new Constrains(), SolvingNodeType.TypeVariable);
        var nodeC = new SolvingNode("c", new Constrains(), SolvingNodeType.TypeVariable);

        nodeA.State = new RefTo(nodeB);
        nodeB.State = new RefTo(nodeC);
        nodeC.State = new RefTo(nodeA);

        var graph = NodeToposortFunctions.ConvertToArrayGraph(new[] { nodeA, nodeB, nodeC });

        var sorted = GraphTools.SortTopology(graph);
        Assert.IsTrue(sorted.HasCycle);
        Assert.AreEqual(3, sorted.NodeNames.Count);
    }*/
}