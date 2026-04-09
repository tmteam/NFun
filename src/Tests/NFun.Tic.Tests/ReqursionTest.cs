using System.Linq;
using NFun.Tic.SolvingStates;
using NUnit.Framework;

namespace NFun.Tic.Tests;

using static StatePrimitive;

class ReqursionTest {
    [SetUp]
    public void Initiazlize() => TraceLog.IsEnabled = true;

    [TearDown]
    public void Finalize() => TraceLog.IsEnabled = false;

    [Test]
    public void SimpleReqursive() {
        //node |       1 0  3 2
        //expr |y(x) = y(x) + 1i
        var graph = new GraphBuilder();
        var fun = graph.SetFunDef("y", 3, null, "x");
        graph.SetVar("x", 0);
        graph.SetCall(fun, 0, 1);
        graph.SetConst(2, I32);
        graph.SetArith(1, 2, 3);
        var result = graph.Solve();
        result.AssertNoGenerics();
        var nonReferencedFun = (StateFun)fun.GetNonReferenced();
        Assert.AreEqual(I32, nonReferencedFun.ReturnType);
        Assert.AreEqual(Any, nonReferencedFun.Args.First());
    }

    [Test]
    public void ReqursiveWithoutGenerics() {
        //node |       4 3  5 1 2  0
        //expr |y(x) = y(x) + x + 1.0
        var graph = new GraphBuilder();

        var fun = graph.SetFunDef("y", 5, null, "x");
        graph.SetConst(0, Real);
        graph.SetVar("x", 1);
        graph.SetArith(0, 1, 2);

        graph.SetVar("x", 3);
        graph.SetCall(fun, 3, 4);

        graph.SetArith(2, 4, 5);
        var result = graph.Solve();
        result.AssertNoGenerics();
        Assert.AreEqual(fun.ReturnType, Real);
        Assert.AreEqual(fun.Args.First(), Real);
    }

    [Test]
    public void ReqursiveWithGeneric() {
        //node |       4    0   2 1    3
        //expr |y(x) = if true: y(x) | x
        var graph = new GraphBuilder();
        var fun = graph.SetFunDef("y", 4, null, "x");
        graph.SetConst(0, Bool);
        graph.SetVar("x", 1);
        graph.SetCall(fun, 1, 2);
        graph.SetVar("x", 3);
        graph.SetIfElse(new[] { 0 }, new[] { 2, 3 }, 4);
        var result = graph.Solve();
        var generic = result.AssertAndGetSingleGeneric(null, null, false);
        Assert.AreEqual(fun.ReturnType, generic.GetNonReference().State);
        Assert.AreEqual(fun.ArgNodes.First().GetNonReference().State, generic.GetNonReference().State);
    }

    [Test]
    public void RecursiveWithLambdaArg() {
        // applyN(x, f, n:int) = if(n > 0) applyN(f(x), f, n-1) else x
        //
        // Node layout:
        //   Named: x, f, n (n:int)
        //   0: n ref in condition
        //   1: 0 literal
        //   2: n > 0 condition result
        //   3: x ref in f(x)
        //   4: f(x) result  (higher-order call: f is variable)
        //   5: f ref in recursive call args
        //   6: n ref in n-1
        //   7: 1 literal (for n-1)
        //   8: n-1 result
        //   9: recursive call result: applyN(f(x), f, n-1)
        //   10: x ref in else branch
        //   11: if-else result = function body

        var graph = new GraphBuilder();

        // Define function: applyN(x, f, n:int) with body at node 11
        graph.SetVarType("n", I32);
        var fun = graph.SetFunDef("applyN'3", 11, null, "x", "f", "n");

        // Condition: n > 0
        graph.SetVar("n", 0);
        graph.SetIntConst(1, StatePrimitive.U8);
        graph.SetComparable(0, 1, 2);

        // f(x) — higher-order call using variable f
        graph.SetVar("x", 3);
        graph.SetCall("f", new[] { 3, 4 });

        // Recursive call: applyN(f(x), f, n-1)
        graph.SetVar("f", 5);
        graph.SetVar("n", 6);
        graph.SetIntConst(7, StatePrimitive.U8);
        graph.SetArith(6, 7, 8);  // n - 1
        graph.SetCall(fun, new[] { 4, 5, 8, 9 });

        // Else branch: x
        graph.SetVar("x", 10);

        // if(n > 0) [recursive_call_result] else [x]
        graph.SetIfElse(new[] { 2 }, new[] { 9, 10 }, 11);

        var result = graph.Solve();
        // Expected: x is generic T, f is (T)->T, result is T
    }
}
