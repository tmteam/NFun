﻿using NFun.Tic.SolvingStates;
using NUnit.Framework;

namespace NFun.Tic.Tests;

using static StatePrimitive;

class Comparation {
    [Test]
    public void CompareTwoVariables() {
        //      0 2 1
        // y =  a > b

        var graph = new GraphBuilder();
        graph.SetVar("a", 0);
        graph.SetVar("b", 1);
        graph.SetComparable(0, 1, 2);
        graph.SetDef("y", 2);

        var result = graph.Solve();

        result.AssertNamed(Bool, "y");
        var generic = result.AssertAndGetSingleGeneric(null, null, true);
        result.AssertAreGenerics(generic, "a", "b");
    }

    [Test]
    public void CompareVariableAndConstant() {
        //      0 2 1
        // y =  a > 1i

        var graph = new GraphBuilder();
        graph.SetVar("a", 0);
        graph.SetConst(1, I32);
        graph.SetComparable(0, 1, 2);
        graph.SetDef("y", 2);

        var result = graph.Solve();

        result.AssertNamed(Bool, "y");
        result.AssertNoGenerics();
        result.AssertNamed(I32, "a");
    }

    [Test]
    public void CompareConstants() {
        //      0  2 1
        // y =  2i > 1i

        var graph = new GraphBuilder();
        graph.SetConst(0, I32);
        graph.SetConst(1, I32);
        graph.SetComparable(0, 1, 2);
        graph.SetDef("y", 2);

        var result = graph.Solve();

        result.AssertNamed(Bool, "y");
        result.AssertNoGenerics();
    }

    [Test]
    public void CompareTwoDifferentConstants() {
        //      0   2 1
        // y =  2.0 > 1i

        var graph = new GraphBuilder();
        graph.SetConst(0, Real);
        graph.SetConst(1, I32);
        graph.SetComparable(0, 1, 2);
        graph.SetDef("y", 2);

        var result = graph.Solve();

        result.AssertNamed(Bool, "y");
        result.AssertNoGenerics();
    }

    [Test]
    public void CompareTwoDifferentUncomparableConstants() {
        //      0   2  1
        // y =  2.0 > 'v'

        var graph = new GraphBuilder();
        graph.SetConst(0, Real);
        graph.SetConst(1, Char);
        TestHelper.AssertThrowsTicError(
            () => {
                graph.SetComparable(0, 1, 2);
                graph.SetDef("y", 2);

                graph.Solve();
            });
    }

    [Test]
    public void CompareTwoDifferentUncomparableConstants_Reversed() {
        //      0   2  1
        // y = 'v'  >  2

        var graph = new GraphBuilder();
        graph.SetConst(0, Char);
        graph.SetConst(1, Real);

        TestHelper.AssertThrowsTicError(
            () => {
                graph.SetComparable(0, 1, 2);
                graph.SetDef("y", 2);

                graph.Solve();
            });
    }
}
