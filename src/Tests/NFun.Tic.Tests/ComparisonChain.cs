using NFun.Tic.SolvingStates;
using NUnit.Framework;

namespace NFun.Tic.Tests;

using static StatePrimitive;

class ComparisonChain {

    [Test]
    public void GenericChain3() {
        //         3
        //     0   1   2
        // y = a < b < c
        var graph = new GraphBuilder();
        graph.SetVar("a", 0);
        graph.SetVar("b", 1);
        graph.SetVar("c", 2);

        var g1 = graph.InitializeVarNode(null, null, true);
        var g2 = graph.InitializeVarNode(null, null, true);

        graph.SetCompareChain(3, new[] { g1, g2 }, new[] { 0, 1, 2 });
        graph.SetDef("y", 3);
        var result = graph.Solve();
        result.AssertNamed(Bool, "y");
        var generic = result.AssertAndGetSingleGeneric(null, null, true);
        result.AssertAreGenerics(generic, "a", "b", "c");
    }

    [Test]
    public void ConcreteChain3() {
        //         3
        //     0   1   2
        // y = a < 1i < c
        var graph = new GraphBuilder();
        graph.SetVar("a", 0);
        graph.SetConst(1, I32);
        graph.SetVar("c", 2);

        var g1 = graph.InitializeVarNode(null, null, true);
        var g2 = graph.InitializeVarNode(null, null, true);

        graph.SetCompareChain(3, new[] { g1, g2 }, new[] { 0, 1, 2 });
        graph.SetDef("y", 3);
        var result = graph.Solve();
        result.AssertNoGenerics();
        result.AssertNamed(Bool, "y");
        result.AssertNamed(I32, "a");
        result.AssertNamed(I32, "c");
    }
}
