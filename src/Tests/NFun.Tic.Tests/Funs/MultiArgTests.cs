using NFun.Tic.SolvingStates;
using NUnit.Framework;

namespace NFun.Tic.Tests.Funs;

using static StatePrimitive;

public class MultiArgTests {
    [Test]
    public void Genericfold_GetSum() {
        //        5  0  4      132
        //y = fold(x, f(a,b)=a+b)
        var graph = new GraphBuilder();

        graph.SetVar("x", 0);
        graph.SetVar("la", 1);
        graph.SetVar("lb", 2);
        graph.SetArith(1, 2, 3);
        graph.CreateLambda(3, 4, "la", "lb");
        graph.SetfoldCall(0, 4, 5);
        graph.SetDef("y", 5);

        var result = graph.Solve();

        var t = result.AssertAndGetSingleArithGeneric();

        result.AssertAreGenerics(t, "y", "la", "lb");
        result.AssertNamed(StateArray.Of(t), "x");
        result.AssertNode(StateFun.Of(new[] { t, t }, t), 4);
    }

    [Test]
    public void fold_ConcreteLambdaReturn_GetSum() {
        //        5  0  4          132
        //y = fold(x, f(a,b):i64=a+b)
        var graph = new GraphBuilder();

        graph.SetVar("x", 0);
        graph.SetVar("la", 1);
        graph.SetVar("lb", 2);
        graph.SetArith(1, 2, 3);
        graph.CreateLambda(3, 4, I64, "la", "lb");
        graph.SetfoldCall(0, 4, 5);
        graph.SetDef("y", 5);

        var result = graph.Solve();

        result.AssertNoGenerics();


        result.AssertNamed(I64, "y", "la", "lb");
        result.AssertNamed(StateArray.Of(I64), "x");
        result.AssertNode(StateFun.Of(new[] { I64, I64 }, I64), 4);
    }


    [Test]
    public void foldConcreteOut_GetSum() {
        //            5  0  4      132
        //y:u32 = fold(x, f(a,b)=a+b)
        var graph = new GraphBuilder();

        graph.SetVar("x", 0);
        graph.SetVar("la", 1);
        graph.SetVar("lb", 2);
        graph.SetArith(1, 2, 3);
        graph.CreateLambda(3, 4, "la", "lb");
        graph.SetfoldCall(0, 4, 5);
        graph.SetVarType("y", U32);
        graph.SetDef("y", 5);

        var result = graph.Solve();

        result.AssertNoGenerics();

        result.AssertNamed(U32, "y", "la", "lb");
        result.AssertNamed(StateArray.Of(U32), "x");
        result.AssertNode(StateFun.Of(new[] { U32, U32 }, U32), 4);
    }

    [Test]
    public void foldConcreteArg_GetSum() {
        //                 5  0  4      132
        //x:u32[]; y = fold(x, f(a,b)=a+b)
        var graph = new GraphBuilder();

        graph.SetVarType("y", U32);
        graph.SetVar("x", 0);
        graph.SetVar("la", 1);
        graph.SetVar("lb", 2);
        graph.SetArith(1, 2, 3);
        graph.CreateLambda(3, 4, "la", "lb");
        graph.SetfoldCall(0, 4, 5);
        graph.SetDef("y", 5);

        var result = graph.Solve();

        result.AssertNoGenerics();

        result.AssertNamed(U32, "y", "la", "lb");
        result.AssertNamed(StateArray.Of(U32), "x");
        result.AssertNode(StateFun.Of(new[] { U32, U32 }, U32), 4);
    }


    [Test]
    //[Ignore("Input variable generic")]
    public void GenericFold_AllIsNan() {
        //      6  0  5      1  4    3   2
        //y = fold(x, f(a,b)=a and isNan(b))
        var graph = new GraphBuilder();

        graph.SetVar("x", 0);
        graph.SetVar("la", 1);
        graph.SetVar("lb", 2);
        graph.SetCall(new[] { Real, Bool }, new[] { 2, 3 });
        graph.SetBoolCall(1, 3, 4);
        graph.CreateLambda(4, 5, "la", "lb");
        graph.SetFoldCall(0, 5, 6);
        graph.SetDef("y", 6);
        var result = graph.Solve();

        result.AssertNoGenerics();

        result.AssertNamed(StateArray.Of(Real), "x");
        result.AssertNamed(Real, "lb");
        result.AssertNamed(Bool, "la", "y");
    }

    [Test]
    public void Fold_ConcreteLambda_GetSum() {
        //         5  0  4              132
        //y = fold(x, f(a,b:i32):i64=a+b)
        var graph = new GraphBuilder();

        graph.SetVar("x", 0);
        graph.SetVar("la", 1);
        graph.SetVarType("lb", I32);
        graph.SetVar("lb", 2);
        graph.SetArith(1, 2, 3);
        graph.CreateLambda(3, 4, I64, "la", "lb");
        graph.SetFoldCall(0, 4, 5);
        graph.SetDef("y", 5);

        var result = graph.Solve();

        result.AssertNoGenerics();

        result.AssertNamed(I64, "y", "la");
        result.AssertNamed(I32, "lb");
        result.AssertNamed(StateArray.Of(I32), "x");
        result.AssertNode(StateFun.Of(new[] { I64, I32 }, I64), 4);
    }

    [Test]
    public void fold_GetSumWithImpossibleTypes_throws() {
        //        5  0  4              132
        //y = fold(x, f(a,b:i32):i64=a+b)
        var graph = new GraphBuilder();

        graph.SetVar("x", 0);
        graph.SetVar("la", 1);
        graph.SetVarType("lb", I32);
        graph.SetVar("lb", 2);
        graph.SetArith(1, 2, 3);
        TestHelper.AssertThrowsTicError(
            () => {
                graph.CreateLambda(3, 4, I64, "la", "lb");
                graph.SetfoldCall(0, 4, 5);
                graph.SetDef("y", 5);
                graph.Solve();
            });
    }
}
