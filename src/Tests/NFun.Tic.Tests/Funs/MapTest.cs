using NFun.Tic.SolvingStates;
using NUnit.Framework;

namespace NFun.Tic.Tests.Funs;

using static StatePrimitive;

public class MapTests {
    [Test]
    public void StrictArrayArg() {
        //     6  1 0     5  2  4 3
        //y = map([ 1i ], x->x == 0)
        var graph = new GraphBuilder();
        graph.SetConst(0, I32);
        graph.SetStrictArrayInit(1, 0);
        graph.SetVar("lx", 2);
        graph.SetIntConst(3, U8);
        graph.SetEquality(2, 3, 4);
        graph.CreateLambda(4, 5, "lx");
        graph.SetMap(1, 5, 6);
        graph.SetDef("y", 6);

        var result = graph.Solve();

        result.AssertNoGenerics();
        result.AssertNamed(StateArray.Of(Bool), "y");
        result.AssertNamed(I32, "lx");
        result.AssertNode(StateFun.Of(I32, Bool), 5);
    }

    [Test]
    public void StrictArrayAndLambdaArg() {
        //     6  1 0     5      2 4 3
        //y = map([ 1i ], x:int->x * 2)
        var graph = new GraphBuilder();
        graph.SetConst(0, I32);
        graph.SetStrictArrayInit(1, 0);
        graph.SetVarType("lx", I32);
        graph.SetVar("lx", 2);
        graph.SetIntConst(3, U8);
        graph.SetArith(2, 3, 4);
        graph.CreateLambda(4, 5, "lx");
        graph.SetMap(1, 5, 6);
        graph.SetDef("y", 6);

        var result = graph.Solve();

        result.AssertNoGenerics();
        result.AssertNamed(StateArray.Of(I32), "y");
        result.AssertNamed(I32, "lx");
        result.AssertNode(StateFun.Of(I32, I32), 5);
    }

    [Test]
    public void LambdaArgDowncast_Throws() {
        //       6  1 0          5  2 43
        //y = map([ 1.0 ], x:int->x==0)
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
                graph.SetMap(1, 5, 6);
                graph.SetDef("y", 6);
                graph.Solve();
                Assert.Fail("Impossible equation solved");
            });
    }

    [Test]
    //[Ignore("Upcast for complex types")]
    public void ArgUpcastStrictArrayArg() {
        //     6  1 0     5       2 4 3
        //y = Map([ 1i ], x:real->x*2)
        var graph = new GraphBuilder();
        graph.SetConst(0, I32);
        graph.SetStrictArrayInit(1, 0);
        graph.SetVarType("lx", Real);
        graph.SetVar("lx", 2);
        graph.SetIntConst(3, U8);
        graph.SetArith(2, 3, 4);
        graph.CreateLambda(4, 5, "lx");
        graph.SetMap(1, 5, 6);
        graph.SetDef("y", 6);

        var result = graph.Solve();

        result.AssertNoGenerics();
        result.AssertNamed(Real, "lx");
        result.AssertNamed(StateArray.Of(Real), "y");
        result.AssertNode(StateFun.Of(Real, Real), 5);
    }

    [Test]
    public void ConcreteLambdaReturn() {
        //     6  1 0     5       2 4 3
        //y = Map([ 1i ], (x):real->x*2)
        var graph = new GraphBuilder();
        graph.SetConst(0, I32);
        graph.SetStrictArrayInit(1, 0);
        graph.SetVar("lx", 2);
        graph.SetIntConst(3, U8);
        graph.SetArith(2, 3, 4);
        graph.CreateLambda(4, 5, Real, "lx");
        graph.SetMap(1, 5, 6);
        graph.SetDef("y", 6);

        var result = graph.Solve();

        result.AssertNoGenerics();
        result.AssertNamed(I32, "lx");
        result.AssertNamed(StateArray.Of(Real), "y");
        result.AssertNode(StateFun.Of(I32, Real), 5);
    }

    [Test]
    public void Generic() {
        //     3  0  2 1
        //y = Map(a, x->x)
        var graph = new GraphBuilder();
        graph.SetVar("a", 0);

        graph.SetVar("2lx", 1);
        graph.CreateLambda(1, 2, "2lx");
        graph.SetMap(0, 2, 3);
        graph.SetDef("y", 3);

        var result = graph.Solve();

        var t = result.AssertAndGetSingleGeneric(null, null);

        result.AssertNamed(StateArray.Of(t), "a", "y");
        result.AssertNode(StateFun.Of(t, t));
    }

    [Test]
    public void ConcreteOutput() {
        //     3  0  2 1
        //y:u16[] = Map(a, x->x)
        var graph = new GraphBuilder();
        graph.SetVar("a", 0);

        graph.SetVar("2lx", 1);
        graph.CreateLambda(1, 2, "2lx");
        graph.SetMap(0, 2, 3);
        graph.SetVarType("y", StateArray.Of(U16));
        graph.SetDef("y", 3);

        var result = graph.Solve();

        result.AssertNoGenerics();

        result.AssertNamed(StateArray.Of(U16), "a", "y");
        result.AssertNode(StateFun.Of(U16, U16));
    }

    [Test]
    public void ConcreteFun() {
        //     2  0   1
        //y = Map(a, SQRT)
        var graph = new GraphBuilder();
        graph.SetVar("a", 0);
        graph.SetVarType("SQRT", StateFun.Of(Real, Real));
        graph.SetVar("SQRT", 1);
        graph.SetMap(0, 1, 2);
        graph.SetDef("y", 2);

        var result = graph.Solve();

        result.AssertNoGenerics();
        result.AssertNamed(StateArray.Of(Real), "a", "y");
    }

    [Test]
    public void ConcreteFunAndUpcast() {
        //                2  0   1
        //a:int[]; y = Map(a, SQRT)
        var graph = new GraphBuilder();
        graph.SetVarType("a", StateArray.Of(I32));
        graph.SetVar("a", 0);
        graph.SetVarType("SQRT", StateFun.Of(Real, Real));
        graph.SetVar("SQRT", 1);
        graph.SetMap(0, 1, 2);
        graph.SetDef("y", 2);

        var result = graph.Solve();

        result.AssertNoGenerics();
        result.AssertNamed(StateArray.Of(Real), "y");
    }
}
