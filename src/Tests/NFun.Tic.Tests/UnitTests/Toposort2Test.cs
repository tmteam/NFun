using System;
using System.Collections.Generic;
using System.Linq;
using NFun.Tic;
using NFun.Tic.SolvingStates;
using NUnit.Framework;

namespace NFun.ModuleTests {

public class Toposort2Test {
    /* Test is ok, but cannot be launched in debug environment
    // Reason: Self referencing is denied
    [Test]
    public void SelfReferenceCycle()
    {
        var a1 = CreateNode("a1");
        a1.State = new StateRefTo(a1);
        var algorithm = new NodeToposort2(1);
        algorithm.AddToTopology(a1);
        algorithm.OptimizeTopology();
        Assert.AreEqual(1,algorithm.NonReferenceOrdered.Length);
        Assert.AreEqual(a1,algorithm.NonReferenceOrdered[0]);
        Assert.IsInstanceOf<ConstrainsState>(a1.State);
    }*/
    [Test]
    public void AddNullToTopology_DoesNotThrow() {
        var algorithm = new NodeToposort(3);
        Assert.DoesNotThrow(() => algorithm.AddToTopology(null));
    }

    [Test]
    public void TwoNodesReferencesEachOther_OneNodeInResult() {
        var a1 = CreateNode("a1");
        var a2 = CreateNode("a2");
        a1.State = new StateRefTo(a2);
        a2.State = new StateRefTo(a1);
        var algorithm = new NodeToposort(3);
        algorithm.AddMany(a1, a2);
        algorithm.OptimizeTopology();
        Assert.AreEqual(1, algorithm.NonReferenceOrdered.Length);
    }


    [Test]
    public void AncestorMultiCycle() {
        //V2 -> n  -> 13 -> V2 
        //   -> 16 -> V3 -> n	 

        //cycle 1:
        //1) v2->n->13->v2  merges as 'X'

        //cycle 2:
        //2)  X -> 16 -> v3 -> X

        var v2 = CreateNode("v2", new ConstrainsState(StatePrimitive.I32, StatePrimitive.Real));
        var v3 = CreateNode("v3", new ConstrainsState(StatePrimitive.I24, StatePrimitive.Real));

        var i13 = CreateNode("13");
        var i16 = CreateNode("16");
        var n = CreateNode("n");

        v2.AddAncestor(n);
        n.AddAncestor(i13);
        i13.AddAncestor(v2);
        v2.AddAncestor(i16);
        i16.AddAncestor(v3);
        v3.AddAncestor(n);

        var algorithm = new NodeToposort(6);
        algorithm.AddMany(v2, i13, i16, v3, n);
        algorithm.OptimizeTopology();

        AssertHaveNoAncestorCycles(v2, v3, n, i13, i16);

        Assert.AreEqual(1, algorithm.NonReferenceOrdered.Length);
        var theNode = algorithm.NonReferenceOrdered[0];
        Assert.IsInstanceOf<ConstrainsState>(theNode.State);
        Assert.AreEqual(new ConstrainsState(StatePrimitive.I32, StatePrimitive.Real), theNode.State);
    }

    [Test]
    public void AncestorAndRefMultiCycle() {
        //V2   -> n  ==> 13 -> V2 
        //  |<== 16   -> V3 -> n	 


        var v2 = CreateNode("v2", new ConstrainsState(StatePrimitive.I32, StatePrimitive.Real));
        var v3 = CreateNode("v3", new ConstrainsState(StatePrimitive.I24, StatePrimitive.Real));

        var i13 = CreateNode("13");
        var i16 = CreateNode("16");
        var n = CreateNode("n");

        v2.AddAncestor(n);
        n.State = new StateRefTo(i13);
        i13.AddAncestor(v2);
        v2.AddAncestor(i16);
        i16.AddAncestor(v3);
        i16.State = new StateRefTo(v2);
        v3.AddAncestor(n);

        var algorithm = new NodeToposort(6);
        algorithm.AddMany(v2, i13, i16, v3, n);
        algorithm.OptimizeTopology();

        AssertHaveNoAncestorCycles(v2, v3, n, i13, i16);

        Assert.AreEqual(1, algorithm.NonReferenceOrdered.Length);
        var theNode = algorithm.NonReferenceOrdered[0];
        Assert.IsInstanceOf<ConstrainsState>(theNode.State);
        Assert.AreEqual(new ConstrainsState(StatePrimitive.I32, StatePrimitive.Real), theNode.State);
    }

    [Test]
    public void AncestorAndRefMultiCycle_allAncestorsMoveToMerged() {
        //V2   -> n  ==> 13 -> V2 
        //  |<== 16   -> V3 -> n	 

        // Each of nodes has its own ancestor
        // all these 'side' -ancestors has to move to main node

        var v2 = CreateNode("v2", new ConstrainsState(StatePrimitive.I32, StatePrimitive.Real));
        var v3 = CreateNode("v3", new ConstrainsState(StatePrimitive.I24, StatePrimitive.Real));

        var i13 = CreateNode("13");
        var i16 = CreateNode("16");
        var n = CreateNode("n");

        v2.AddAncestor(n);
        n.State = new StateRefTo(i13);
        i13.AddAncestor(v2);
        v2.AddAncestor(i16);
        i16.AddAncestor(v3);
        i16.State = new StateRefTo(v2);
        v3.AddAncestor(n);

        var ancestors = new[]
            { CreateNode("anc1"), CreateNode("anc2"), CreateNode("anc3"), CreateNode("anc4"), CreateNode("anc5") };
        v2.AddAncestor(ancestors[0]);
        n.AddAncestor(ancestors[1]);
        i13.AddAncestor(ancestors[2]);
        v2.AddAncestor(ancestors[3]);
        i16.AddAncestor(ancestors[4]);

        var algorithm = new NodeToposort(6);
        algorithm.AddMany(v2, i13, i16, v3, n);
        algorithm.AddMany(ancestors);
        algorithm.OptimizeTopology();

        AssertHaveNoAncestorCycles(v2, v3, n, i13, i16);
        AssertHaveNoAncestorCycles(ancestors);

        Assert.AreEqual(1 + ancestors.Length, algorithm.NonReferenceOrdered.Length);
        var theNode = algorithm.NonReferenceOrdered[0];
        Assert.IsInstanceOf<ConstrainsState>(theNode.State);
        Assert.AreEqual(new ConstrainsState(StatePrimitive.I32, StatePrimitive.Real), theNode.State);
        CollectionAssert.AreEquivalent(ancestors, theNode.Ancestors);
    }


    [Test]
    public void AncestorAndRefMultiCycle2() {
        //V2   -> n  ==> 13 <== V2 
        //         |-> 16  ==> V3 -> n	 

        // After adding - all references should dissapear
        // So the graph has to look like:
        // v2->13->v2
        //  |->v3->13

        var v2 = CreateNode("v2");
        var v3 = CreateNode("v3");

        var i13 = CreateNode("13");
        var i16 = CreateNode("16");
        var n = CreateNode("n");

        v2.AddAncestor(n);
        n.State = new StateRefTo(i13);
        v2.State = new StateRefTo(i13);
        n.AddAncestor(i16);
        i16.State = new StateRefTo(v3);
        v3.AddAncestor(n);

        var algorithm = new NodeToposort(6);
        algorithm.AddMany(v2, i13, i16, v3, n);
        algorithm.OptimizeTopology();

        AssertHaveNoAncestorCycles(v2, v3, n, i13, i16);

        Assert.AreEqual(1, algorithm.NonReferenceOrdered.Length);
        var theNode = algorithm.NonReferenceOrdered[0];
        Assert.IsInstanceOf<ConstrainsState>(theNode.State);
    }

    [Test]
    public void AncestorMultiCycleSimpliest() {
        //V2 13 V3 N

        //V2[a b] => n       => V2 
        //        => 16 =>  n	 
        var v2 = CreateNode("v2");

        var i16 = CreateNode("16");
        var n = CreateNode("n");

        v2.State = new ConstrainsState(StatePrimitive.I32, StatePrimitive.Real);

        v2.AddAncestor(n);
        n.AddAncestor(v2);
        v2.AddAncestor(i16);
        i16.AddAncestor(n);

        var algorithm = new NodeToposort(3);
        algorithm.AddMany(v2, i16, n);
        algorithm.OptimizeTopology();

        AssertHaveNoAncestorCycles(v2);
        AssertHaveNoAncestorCycles(n);
        AssertHaveNoAncestorCycles(i16);

        Assert.AreEqual(1, algorithm.NonReferenceOrdered.Length);
        var theNode = algorithm.NonReferenceOrdered[0];
        Assert.IsInstanceOf<ConstrainsState>(theNode.State);
        Assert.AreEqual(new ConstrainsState(StatePrimitive.I32, StatePrimitive.Real), theNode.State);
    }

    [Test]
    public void ConcreteAncestorCycle_OneNodeInResult() {
        var a1 = CreateNode("a1");
        var a2 = CreateNode("a2");
        a1.State = new ConstrainsState(StatePrimitive.I32, StatePrimitive.I96);
        a1.AddAncestor(a2);
        a2.AddAncestor(a1);
        var algorithm = new NodeToposort(3);
        algorithm.AddMany(a1, a2);
        algorithm.OptimizeTopology();
        Assert.AreEqual(1, algorithm.NonReferenceOrdered.Length);
        Assert.AreEqual(a2, algorithm.NonReferenceOrdered[0]);
    }

    [Test]
    public void NodeReferencesOtherNode_OneOfTheNodesHasAncestorsOfBoth() {
        var a1 = CreateNode("a1");
        var a2 = CreateNode("a2");
        var c1 = CreateNode("c1");
        var c2 = CreateNode("c2");
        c1.AddAncestor(a1);
        c2.AddAncestor(a2);
        c1.State = new StateRefTo(c2);

        var algorithm = new NodeToposort(3);
        algorithm.AddMany(a1, a2, c1, c2);
        algorithm.OptimizeTopology();

        Assert.AreEqual(3, algorithm.NonReferenceOrdered.Length);
        TicNode central = null;
        if (algorithm.NonReferenceOrdered.Contains(c1))
            central = c1;
        if (algorithm.NonReferenceOrdered.Contains(c2))
        {
            if (central != null)
                Assert.Fail("Both referenced still remains in algorithm");
            central = c2;
        }

        Assert.IsNotNull(central);
        Assert.AreEqual(2, central.Ancestors.Count);
        Assert.True(central.Ancestors.Contains(a1));
        Assert.True(central.Ancestors.Contains(a2));
    }

    [Test]
    public void ThreeNodesAncestorLine() {
        var n1 = CreateNode("1");
        var n2 = CreateNode("2");
        var n3 = CreateNode("3");

        n1.AddAncestor(n3);
        n2.AddAncestor(n1);

        var algorithm = new NodeToposort(3);
        algorithm.AddToTopology(n1);
        algorithm.AddToTopology(n2);
        algorithm.AddToTopology(n3);
        algorithm.OptimizeTopology();
        CollectionAssert.AreEqual(new[] { n2, n1, n3 }, algorithm.NonReferenceOrdered);
    }

    [Test]
    public void ThreeRefCycle() {
        var n1 = CreateNode("1");
        var n2 = CreateNode("2");
        var n3 = CreateNode("3");


        n1.State = new StateRefTo(n2);
        n2.State = new StateRefTo(n3);
        n3.State = new StateRefTo(n1);
        var algorithm = new NodeToposort(3);

        algorithm.AddToTopology(n2);
        algorithm.AddToTopology(n1);
        algorithm.AddToTopology(n3);

        algorithm.OptimizeTopology();

        Assert.AreEqual(1, algorithm.NonReferenceOrdered.Length);
        Assert.IsEmpty(algorithm.NonReferenceOrdered[0].Ancestors);
        Assert.IsEmpty(n1.Ancestors);
        Assert.IsEmpty(n2.Ancestors);
        Assert.IsEmpty(n3.Ancestors);
    }

    [Test]
    public void NotObviousThreeRefCycle() {
        var n1 = CreateNode("1");
        var n2 = CreateNode("2");
        var n3 = CreateNode("3");

        n1.State = new StateRefTo(n2);
        n3.State = new StateRefTo(n1);
        var algorithm = new NodeToposort(3);

        algorithm.AddToTopology(n2);
        algorithm.AddToTopology(n1);
        algorithm.AddToTopology(n3);

        algorithm.OptimizeTopology();

        Assert.AreEqual(1, algorithm.NonReferenceOrdered.Length);
        Assert.IsEmpty(algorithm.NonReferenceOrdered[0].Ancestors);
        Assert.IsEmpty(n1.Ancestors);
        Assert.IsEmpty(n2.Ancestors);
        Assert.IsEmpty(n3.Ancestors);
    }

    [Test]
    public void NotObviousFourRefCycle() {
        var n1 = CreateNode("1");
        var n2 = CreateNode("2");
        var n3 = CreateNode("3");
        var n4 = CreateNode("4");

        n1.State = new StateRefTo(n2);
        n3.State = new StateRefTo(n1);
        n2.State = new StateRefTo(n4);
        var algorithm = new NodeToposort(3);

        algorithm.AddToTopology(n3);
        algorithm.AddToTopology(n2);
        algorithm.AddToTopology(n1);
        algorithm.AddToTopology(n4);

        algorithm.OptimizeTopology();

        Assert.AreEqual(1, algorithm.NonReferenceOrdered.Length);
        Assert.AreEqual(n4, algorithm.NonReferenceOrdered[0]);
        Assert.IsEmpty(algorithm.NonReferenceOrdered[0].Ancestors);
        Assert.IsEmpty(n1.Ancestors);
        Assert.IsEmpty(n2.Ancestors);
        Assert.IsEmpty(n3.Ancestors);
    }

    [Test]
    public void AllRefsToSingle_SingleItemAppears() {
        var n1 = CreateNode("1");
        var n2 = CreateNode("2");
        var n3 = CreateNode("3");
        var central = CreateNode("C");

        n1.State = new StateRefTo(central);
        n2.State = new StateRefTo(central);
        n3.State = new StateRefTo(central);
        var algorithm = new NodeToposort(3);

        algorithm.AddToTopology(n2);
        algorithm.AddToTopology(n1);
        algorithm.AddToTopology(central);
        algorithm.AddToTopology(n3);

        algorithm.OptimizeTopology();

        Assert.AreEqual(1, algorithm.NonReferenceOrdered.Length);
        Assert.AreEqual(central, algorithm.NonReferenceOrdered[0]);
        Assert.IsEmpty(algorithm.NonReferenceOrdered[0].Ancestors);
        Assert.IsEmpty(n1.Ancestors);
        Assert.IsEmpty(n2.Ancestors);
        Assert.IsEmpty(n3.Ancestors);
    }

    [Test]
    public void NotObviousRefAndAncestorCycle() {
        var n1 = CreateNode("1");
        var n2 = CreateNode("2");
        var n3 = CreateNode("3");

        n1.State = new StateRefTo(n2);
        n1.AddAncestor(n3);
        n3.AddAncestor(n2);

        var algorithm = new NodeToposort(3);

        algorithm.AddToTopology(n2);
        algorithm.AddToTopology(n1);
        algorithm.AddToTopology(n3);

        algorithm.OptimizeTopology();

        Assert.AreEqual(1, algorithm.NonReferenceOrdered.Length);
        Assert.IsEmpty(algorithm.NonReferenceOrdered[0].Ancestors);
        Assert.IsEmpty(n1.Ancestors);
        Assert.IsEmpty(n2.Ancestors);
        Assert.IsEmpty(n3.Ancestors);
    }

    [Test]
    public void ThreeAncestorsCycle_HasSingleNodeWithNoAncestors() {
        var n1 = CreateNode("1");
        var n2 = CreateNode("2");
        var n3 = CreateNode("3");

        n1.AddAncestor(n3);
        n2.AddAncestor(n1);
        n3.AddAncestor(n2);

        var algorithm = new NodeToposort(3);
        algorithm.AddToTopology(n1);
        algorithm.AddToTopology(n2);
        algorithm.AddToTopology(n3);
        algorithm.OptimizeTopology();

        Assert.AreEqual(1, algorithm.NonReferenceOrdered.Length);
        Assert.IsEmpty(algorithm.NonReferenceOrdered[0].Ancestors);
        Assert.IsEmpty(n1.Ancestors);
        Assert.IsEmpty(n2.Ancestors);
        Assert.IsEmpty(n3.Ancestors);
    }

    private static void AssertHaveNoAncestorCycles(params TicNode[] targetNodes) {
        foreach (var targetNode in targetNodes)
        {
            AssertHasNoAncestorCycleReq(new HashSet<TicNode>(), targetNode);
        }

        static void AssertHasNoAncestorCycleReq(HashSet<TicNode> route, TicNode targetNode) {
            if (!route.Add(targetNode))
                Assert.Fail($"{targetNode} is in Cycle: {string.Join(",", route)}");
            foreach (var ancestor in targetNode.Ancestors)
            {
                AssertHasNoAncestorCycleReq(route, ancestor);
            }
        }
    }

    private static void AssertOrder(IReadOnlyList<TicNode> of, TicNode target, TicNode isBefore) {
        var collection = of.ToArray();
        var less = Array.IndexOf(collection, target);
        var more = Array.IndexOf(collection, isBefore);
        Assert.Less(less, more);
    }

    private static TicNode CreateNode(string name, ITicNodeState state = null)
        => TicNode.CreateNamedNode(name, state ?? new ConstrainsState());
}

}