using NFun.Tic.SolvingStates;
using NUnit.Framework;

namespace NFun.Tic.Tests.Funs;

using static StatePrimitive;

public class GetFirstTests {
    [Test]
    public void StrictArrayArg() {
        //     6  1 0    5  2  4 3
        //y = First([ 1i ], x->x==0)
        var graph = new GraphBuilder();
        graph.SetConst(0, I32);
        graph.SetStrictArrayInit(1, 0);
        graph.SetVar("lx", 2);
        graph.SetIntConst(3, U8);
        graph.SetEquality(2, 3, 4);
        graph.CreateLambda(4, 5, "lx");
        graph.SetGetFirst(1, 5, 6);
        graph.SetDef("y", 6);
        var result = graph.Solve();
        result.AssertNoGenerics();
        result.AssertNamed(I32, "y");
        result.AssertNamed(I32, "lx");
        result.AssertNode(StateFun.Of(I32, Bool), 5);
    }

    [Test]
    public void StrictArrayArgAndLambdaReturn() {
        //     6  1 0    5  2  4 3
        //y = First([ 1i ], (x):bool->x==0)
        var graph = new GraphBuilder();
        graph.SetConst(0, I32);
        graph.SetStrictArrayInit(1, 0);
        graph.SetVar("lx", 2);
        graph.SetIntConst(3, U8);
        graph.SetEquality(2, 3, 4);
        graph.CreateLambda(4, 5, Bool, "lx");
        graph.SetGetFirst(1, 5, 6);
        graph.SetDef("y", 6);
        var result = graph.Solve();
        result.AssertNoGenerics();
        result.AssertNamed(I32, "y");
        result.AssertNamed(I32, "lx");
        result.AssertNode(StateFun.Of(I32, Bool), 5);
    }

    [Test]
    public void InvalidLambdaReturn_Throws() {
        //       3  0  2        1
        //y = First(a, f(x):any=x)
        var graph = new GraphBuilder();
        graph.SetVar("a", 0);
        graph.SetVar("2lx", 1);
        graph.CreateLambda(1, 2, Any, "2lx");
        TestHelper.AssertThrowsTicError(
            () => {
                graph.SetGetFirst(0, 2, 3);
                graph.SetDef("y", 3);
                graph.Solve();
            });
    }

    [Test]
    public void StrictArrayAndLambdaArg() {
        //       6  1 0         5   2 4 3
        //y = first([ 1i ], x:int-> x == 0)
        var graph = new GraphBuilder();
        graph.SetConst(0, I32);
        graph.SetStrictArrayInit(1, 0);
        graph.SetVarType("lx", I32);
        graph.SetVar("lx", 2);
        graph.SetIntConst(3, U8);
        graph.SetEquality(2, 3, 4);
        graph.CreateLambda(4, 5, "lx");
        graph.SetGetFirst(1, 5, 6);
        graph.SetDef("y", 6);
        var result = graph.Solve();
        result.AssertNoGenerics();
        result.AssertNamed(I32, "y");
        result.AssertNamed(I32, "lx");
        result.AssertNode(StateFun.Of(I32, Bool), 5);
    }

    [Test]
    public void LambdaArgDowncast_Throws() {
        //       6  1 0          5  2 43
        //y = First([ 1.0 ], x:int->x==0)
        var graph = new GraphBuilder();
        graph.SetConst(0, Real);
        graph.SetStrictArrayInit(1, 0);
        graph.SetVarType("lx", I32);
        graph.SetVar("lx", 2);
        graph.SetIntConst(3, U8);
        graph.SetEquality(2, 3, 4);
        TestHelper.AssertThrowsTicError(
            () => {
                graph.CreateLambda(4, 5, "lx");
                graph.SetGetFirst(1, 5, 6);
                graph.SetDef("y", 6);
                graph.Solve();
                Assert.Fail("Impossible equation solved");
            });
    }

    [Test]
    //[Ignore("Upcast for complex types")]
    public void ArgUpcastStrictArrayArg() {
        //       6  1 0     5       2 4 3
        //y = First([ 1i ], x:real->x ==0)
        var graph = new GraphBuilder();
        graph.SetConst(0, I32);
        graph.SetStrictArrayInit(1, 0);
        graph.SetVarType("lx", Real);
        graph.SetVar("lx", 2);
        graph.SetIntConst(3, U8);
        graph.SetEquality(2, 3, 4);
        graph.CreateLambda(4, 5, "lx");
        graph.SetGetFirst(1, 5, 6);
        graph.SetDef("y", 6);
        var result = graph.Solve();
        result.AssertNoGenerics();
        result.AssertNamed(Real, "lx");
        result.AssertNode(StateFun.Of(Real, Bool), 5);
    }

    [Test]
    public void BoolArrayArg() {
        //       3  0  2  1
        //y = First(a, x->x)
        var graph = new GraphBuilder();
        graph.SetVar("a", 0);
        graph.SetVar("2lx", 1);
        graph.CreateLambda(1, 2, "2lx");
        graph.SetGetFirst(0, 2, 3);
        graph.SetDef("y", 3);
        var result = graph.Solve();
        result.AssertNoGenerics();
        result.AssertNamed(StateArray.Of(Bool), "a");
        result.AssertNamed(Bool, "2lx");
        result.AssertNamed(Bool, "y");
        result.AssertNode(StateFun.Of(Bool, Bool), 2);
    }

    [Test]
    public void ConcreteFun() {
        //       2  0   1
        //y = First(a, isNan)
        var graph = new GraphBuilder();
        graph.SetVar("a", 0);
        graph.SetVarType("isNan", StateFun.Of(Real, Bool));
        graph.SetVar("isNan", 1);
        graph.SetGetFirst(0, 1, 2);
        graph.SetDef("y", 2);
        var result = graph.Solve();
        result.AssertNoGenerics();
        result.AssertNamed(StateArray.Of(Real), "a");
    }

    [Test]
    public void ConcreteFunAndUpcast() {
        //                2  0   1
        //a:int[]; y = First(a, isNan)
        var graph = new GraphBuilder();
        graph.SetVarType("a", StateArray.Of(I32));
        graph.SetVar("a", 0);
        graph.SetVarType("isNan", StateFun.Of(Real, Bool));
        graph.SetVar("isNan", 1);
        graph.SetGetFirst(0, 1, 2);
        graph.SetDef("y", 2);
        var result = graph.Solve();
        result.AssertNoGenerics();
    }
}
