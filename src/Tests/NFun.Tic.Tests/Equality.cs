using NFun.Tic.SolvingStates;
using NUnit.Framework;

namespace NFun.Tic.Tests;

using static StatePrimitive;

class Equality {
    [Test]
    [Ignore("Not defined behaviour")]
    public void TwoVariableEquality() {
        //     0  2 1
        // y = a == b
        var graph = new GraphBuilder();
        graph.SetVar("a", 0);
        graph.SetVar("b", 1);
        graph.SetEquality(0, 1, 2);
        graph.SetDef("y", 2);

        var result = graph.Solve();

        result.AssertNamed(Bool, "y");
        var generic = result.AssertAndGetSingleGeneric(null, null);
        result.AssertAreGenerics(generic, "a", "b");
    }

    [Test]
    public void VariableAndConstEquality() {
        //     0  2 1
        // y = a == 1i
        var graph = new GraphBuilder();
        graph.SetVar("a", 0);
        graph.SetConst(1, I32);
        graph.SetEquality(0, 1, 2);
        graph.SetDef("y", 2);

        var result = graph.Solve();

        result.AssertNoGenerics();
        result.AssertNamed(Bool, "y");
        result.AssertNamed(I32, "a");
    }

    [Test]
    public void ConstEquality() {
        //     0  2 1
        // y = 1i == 1i
        var graph = new GraphBuilder();
        graph.SetConst(0, I32);
        graph.SetConst(1, I32);
        graph.SetEquality(0, 1, 2);
        graph.SetDef("y", 2);

        var result = graph.Solve();

        result.AssertNoGenerics();
        result.AssertNamed(Bool, "y");
    }

    [Test]
    public void DifferentTypesEquality() {
        //     0   2 1
        // y = 1i == 1.0
        var graph = new GraphBuilder();
        graph.SetConst(0, I32);
        graph.SetConst(1, Real);
        graph.SetEquality(0, 1, 2);
        graph.SetDef("y", 2);

        var result = graph.Solve();

        result.AssertNoGenerics();
        result.AssertNamed(Bool, "y");
    }
}
