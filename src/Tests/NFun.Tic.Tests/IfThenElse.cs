using NFun.Tic.SolvingStates;
using NUnit.Framework;

namespace NFun.Tic.Tests;

using static StatePrimitive;

class IfThenElse {
    [Test(Description = "y = if a: 1 else 0")]
    public void SolvingSimpleCaseWithIfs() {
        //node |    3  0  1      2
        //expr |y = if a: 1 else 0;

        var graph = new GraphBuilder();

        graph.SetVar("a", 0);
        graph.SetIntConst(1, U8);
        graph.SetIntConst(2, U8);
        graph.SetIfElse(new[] { 0 }, new[] { 1, 2 }, 3);
        graph.SetDef("y", 3);
        var result = graph.Solve();

        var generic = result.AssertAndGetSingleGeneric(U8, Real);
        result.AssertAreGenerics(generic, "y");
        result.AssertNamed(Bool, "a");
    }

    [Test]
    public void If_withOneIntAndOneVar_equalReal() {
        //     3     0   1      2
        //y  = if (true) 1 else x

        var graph = new GraphBuilder();

        graph.SetConst(0, Bool);
        graph.SetIntConst(1, U8);
        graph.SetVar("x", 2);
        graph.SetIfElse(new[] { 0 }, new[] { 1, 2 }, 3);
        graph.SetDef("y", 3);

        var result = graph.Solve();

        var generic = result.AssertAndGetSingleGeneric(U8, Real, false);
        result.AssertAreGenerics(generic, "y", "x");
    }

    [Test(Description = "y=if a:x; else z+1;")]
    public void SolvingCaseWithIfs() {
        //node |   5    0  1        2 4 3
        //expr |y = if (a) x; else (z + 1);

        var graph = new GraphBuilder();

        graph.SetVar("a", 0);
        graph.SetVar("x", 1);
        graph.SetVar("z", 2);
        graph.SetIntConst(3, U8);
        graph.SetArith(2, 3, 4);

        graph.SetIfElse(new[] { 0 }, new[] { 1, 4 }, 5);
        graph.SetDef("y", 5);

        var result = graph.Solve();

        var generic = result.AssertAndGetSingleArithGeneric();
        result.AssertAreGenerics(generic, "y", "x", "z");
        result.AssertNamed(Bool, "a");
    }

    [Test(Description = "y = if (a) x else z ")]
    public void CleanGenericOnIfs() {
        //node |     3    0   1      2
        //expr |y = if (true) x else z

        var graph = new GraphBuilder();

        graph.SetConst(0, Bool);
        graph.SetVar("x", 1);
        graph.SetVar("z", 2);
        graph.SetIfElse(new[] { 0 }, new[] { 1, 2 }, 3);
        graph.SetDef("y", 3);

        var result = graph.Solve();

        var generic = result.AssertAndGetSingleGeneric(null, null, false);
        result.AssertAreGenerics(generic, "y", "x", "z");
    }

    [Test(Description = "y = if (a) x else x ")]
    public void DummyGenericOnIfs() {
        //node |     3    0   1      2
        //expr |y = if (true) x else x

        var graph = new GraphBuilder();

        graph.SetConst(0, Bool);
        graph.SetVar("x", 1);
        graph.SetVar("x", 2);
        graph.SetIfElse(new[] { 0 }, new[] { 1, 2 }, 3);
        graph.SetDef("y", 3);

        var result = graph.Solve();

        var generic = result.AssertAndGetSingleGeneric(null, null, false);
        result.AssertAreGenerics(generic, "y", "x");
    }

    [Test(Description = "y = if (x) x else x ")]
    public void IfXxx() {
        //node |     3  0  1      2
        //expr |y = if (x) x else x

        var graph = new GraphBuilder();

        graph.SetVar("x", 0);
        graph.SetVar("x", 1);
        graph.SetVar("x", 2);
        graph.SetIfElse(new[] { 0 }, new[] { 1, 2 }, 3);
        graph.SetDef("y", 3);

        var result = graph.Solve();

        result.AssertNoGenerics();
        result.AssertNamed(Bool, "x", "y");
    }

    [Test(Description = "y = if (true) 127.0.0.1 else 12")]
    public void IfOfIpAndNumeric() {
        //node |    3   0       1            2
        //expr |y = if (true) 127.0.0.1 else 12;

        var graph = new GraphBuilder();

        graph.SetConst(0, Bool);
        graph.SetConst(1, Ip);
        graph.SetIntConst(2, U8);
        graph.SetIfElse(new[] { 0 }, new[] { 1, 2 }, 3);
        graph.SetDef("y", 3);
        var result = graph.Solve();

        result.AssertNoGenerics();
        result.AssertNamed(Any, "y");
    }

    [Test(Description = "y = if (true) 127.0.0.1 else 12")]
    public void IfOfBoolAndNumeric() {
        //node |    3   0       1        2
        //expr |y = if (true) true else 12;

        var graph = new GraphBuilder();

        graph.SetConst(0, Bool);
        graph.SetConst(1, Bool);
        graph.SetIntConst(2, U8);
        graph.SetIfElse(new[] { 0 }, new[] { 1, 2 }, 3);
        graph.SetDef("y", 3);
        var result = graph.Solve();

        result.AssertNoGenerics();
        result.AssertNamed(Any, "y");
    }

    [Test]
    public void If_withMultipleAncestorRules_EquationSolved() {
        //      3     0   1      2       4        5 7 6
        //y1  = if (true) 1 else x; y2 = y1; y3 = y1 * 2

        var graph = new GraphBuilder();

        graph.SetConst(0, Bool);
        graph.SetIntConst(1, U8);
        graph.SetVar("x", 2);
        graph.SetIfElse(new[] { 0 }, new[] { 1, 2 }, 3);
        graph.SetDef("y1", 3);

        graph.SetVar("y1", 4);
        graph.SetDef("y2", 4);

        graph.SetVar("y1", 5);
        graph.SetIntConst(6, U8);
        graph.SetArith(5, 6, 7);
        graph.SetDef("y3", 7);

        var result = graph.Solve();
        var generic = result.AssertAndGetSingleArithGeneric();
        result.AssertAreGenerics(generic, "y1", "y2", "y3");
    }
}
