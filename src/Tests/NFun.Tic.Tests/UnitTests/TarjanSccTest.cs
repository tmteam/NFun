using System.Collections.Generic;
using System.Linq;
using NFun.Tic.SolvingStates;
using NUnit.Framework;

namespace NFun.Tic.Tests.UnitTests;

/// <summary>
/// Unit tests for Tarjan SCC utility (Push reform M2.A Phase A).
/// Tests classify graph topologies by their SCC structure and contractivity.
/// </summary>
public class TarjanSccTest {

    [Test]
    public void SingleNode_NoEdges_OneSingletonScc() {
        var a = TicNode.CreateTypeVariableNode("a", ConstraintsState.Empty);
        var sccs = TarjanScc.ComputeSccs(new[] { a });
        Assert.AreEqual(1, sccs.Count);
        Assert.AreEqual(1, sccs[0].Count);
        Assert.AreSame(a, sccs[0][0]);
        Assert.IsFalse(TarjanScc.IsCyclicScc(sccs[0]),
            "Singleton with no self-loop is acyclic");
    }

    [Test]
    public void TwoNodes_NoEdges_TwoSingletonSccs() {
        var a = TicNode.CreateTypeVariableNode("a", ConstraintsState.Empty);
        var b = TicNode.CreateTypeVariableNode("b", ConstraintsState.Empty);
        var sccs = TarjanScc.ComputeSccs(new[] { a, b });
        Assert.AreEqual(2, sccs.Count);
        Assert.IsTrue(sccs.All(s => s.Count == 1));
    }

    [Test]
    public void RefToCycle_OneScc() {
        // a → b → a via RefTo
        var a = TicNode.CreateTypeVariableNode("a", ConstraintsState.Empty);
        var b = TicNode.CreateTypeVariableNode("b", ConstraintsState.Empty);
        a.State = new StateRefTo(b);
        b.State = new StateRefTo(a);
        var sccs = TarjanScc.ComputeSccs(new[] { a, b });
        Assert.AreEqual(1, sccs.Count);
        Assert.AreEqual(2, sccs[0].Count);
        Assert.IsTrue(TarjanScc.IsCyclicScc(sccs[0]));
        Assert.IsFalse(TarjanScc.IsContractive(sccs[0]),
            "RefTo-only cycle has no constructor crossing — non-contractive");
    }

    [Test]
    public void AncestorCycle_OneScc_NonContractive() {
        // a ≤ b ≤ a — ancestor edges only
        var a = TicNode.CreateTypeVariableNode("a", ConstraintsState.Empty);
        var b = TicNode.CreateTypeVariableNode("b", ConstraintsState.Empty);
        a.AddAncestor(b);
        b.AddAncestor(a);
        var sccs = TarjanScc.ComputeSccs(new[] { a, b });
        Assert.AreEqual(1, sccs.Count);
        Assert.AreEqual(2, sccs[0].Count);
        Assert.IsTrue(TarjanScc.IsCyclicScc(sccs[0]));
        Assert.IsFalse(TarjanScc.IsContractive(sccs[0]),
            "Pure ancestor cycle has no constructor crossing — non-contractive");
    }

    [Test]
    public void OptionalCycle_Contractive() {
        // elem ≤ outer where outer.State = opt(elem) — composite-member edge
        var elem = TicNode.CreateTypeVariableNode("elem", ConstraintsState.Empty);
        var outer = TicNode.CreateTypeVariableNode("outer", new StateOptional(elem));
        elem.AddAncestor(outer);
        var sccs = TarjanScc.ComputeSccs(new[] { outer, elem });
        Assert.AreEqual(1, sccs.Count, "outer and elem form one SCC");
        Assert.AreEqual(2, sccs[0].Count);
        Assert.IsTrue(TarjanScc.IsCyclicScc(sccs[0]));
        Assert.IsTrue(TarjanScc.IsContractive(sccs[0]),
            "Cycle through StateOptional element IS constructor-guarded — contractive");
    }

    [Test]
    public void StructFieldCycle_Contractive() {
        // outer.State = struct{f: outer} — self-cycle through struct field
        var fields = new Dictionary<string, TicNode>();
        var outerPlaceholder = TicNode.CreateTypeVariableNode("outer", ConstraintsState.Empty);
        fields["f"] = outerPlaceholder;
        var s = new StateStruct(fields, isFrozen: false);
        outerPlaceholder.State = s;
        var sccs = TarjanScc.ComputeSccs(new[] { outerPlaceholder });
        Assert.AreEqual(1, sccs.Count);
        Assert.IsTrue(TarjanScc.IsCyclicScc(sccs[0]));
        Assert.IsTrue(TarjanScc.IsContractive(sccs[0]),
            "Cycle through struct field IS constructor-guarded — contractive");
    }

    [Test]
    public void DAG_NoSccCollapse() {
        // a → b → c (linear, no cycle). Three singleton SCCs.
        var a = TicNode.CreateTypeVariableNode("a", ConstraintsState.Empty);
        var b = TicNode.CreateTypeVariableNode("b", ConstraintsState.Empty);
        var c = TicNode.CreateTypeVariableNode("c", ConstraintsState.Empty);
        a.AddAncestor(b);
        b.AddAncestor(c);
        var sccs = TarjanScc.ComputeSccs(new[] { a });
        Assert.AreEqual(3, sccs.Count);
        Assert.IsTrue(sccs.All(s => s.Count == 1));
        Assert.IsTrue(sccs.All(s => !TarjanScc.IsCyclicScc(s)));
    }

    [Test]
    public void TopologicalOrder_LeavesFirst() {
        // a → b → c. SCCs returned in reverse-topo order (c, b, a).
        var a = TicNode.CreateTypeVariableNode("a", ConstraintsState.Empty);
        var b = TicNode.CreateTypeVariableNode("b", ConstraintsState.Empty);
        var c = TicNode.CreateTypeVariableNode("c", ConstraintsState.Empty);
        a.AddAncestor(b);
        b.AddAncestor(c);
        var sccs = TarjanScc.ComputeSccs(new[] { a });
        Assert.AreSame(c, sccs[0][0]);
        Assert.AreSame(b, sccs[1][0]);
        Assert.AreSame(a, sccs[2][0]);
    }

    [Test]
    public void GetLastTopology_OneCyclicContractiveScc() {
        // Models the SetCoalesce/getLast cycle:
        //   9 (CS) → V8 (opt(V7)) → V7 (CS, ancestor 9)
        // V8.State = opt(V7); 9.Ancestors = [V8]; V7.Ancestors = [9].
        var nine = TicNode.CreateTypeVariableNode("9", ConstraintsState.Empty);
        var v7 = TicNode.CreateTypeVariableNode("V7", ConstraintsState.Empty);
        var v8 = TicNode.CreateTypeVariableNode("V8", new StateOptional(v7));
        nine.AddAncestor(v8);
        v7.AddAncestor(nine);

        var sccs = TarjanScc.ComputeSccs(new[] { nine, v7, v8 });
        // All three nodes are in one SCC (mutually reachable):
        // nine → V8 (ancestor) → V7 (composite member) → nine (ancestor).
        var cyclicSccs = sccs.Where(TarjanScc.IsCyclicScc).ToList();
        Assert.AreEqual(1, cyclicSccs.Count);
        Assert.IsTrue(cyclicSccs[0].Contains(nine));
        Assert.IsTrue(cyclicSccs[0].Contains(v7));
        Assert.IsTrue(cyclicSccs[0].Contains(v8));
        Assert.IsTrue(TarjanScc.IsContractive(cyclicSccs[0]),
            "getLast cycle goes through StateOptional element — contractive");
    }
}
