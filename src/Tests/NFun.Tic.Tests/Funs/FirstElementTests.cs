using NFun.Tic.SolvingStates;
using NUnit.Framework;

namespace NFun.Tic.Tests.Funs {

public class AnyElementTests {
    [Test]
    public void Anything_WithStrictArrayArg() {
        //     6  1 0     5  2  4 3
        //y = Any([ 1i ], x->x == 0)
        var graph = new GraphBuilder();
        graph.SetConst(0, StatePrimitive.I32);
        graph.SetStrictArrayInit(1, 0);
        graph.SetVar("lx", 2);
        graph.SetIntConst(3, StatePrimitive.U8);
        graph.SetEquality(2, 3, 4);
        graph.CreateLambda(4, 5, "lx");
        graph.SetIsAny(1, 5, 6);
        graph.SetDef("y", 6);
        var result = graph.Solve();
        result.AssertNoGenerics();
        result.AssertNamed(StatePrimitive.I32, "lx");
        result.AssertNode(StateFun.Of(argType: StatePrimitive.I32, StatePrimitive.Bool), 5);
    }

    [Test]
    public void Anything_WithStrictArrayAndLambdaArg() {
        //     6  1 0         5  2 4 3
        //y = Any([ 1i ], x:int->x== 0)
        var graph = new GraphBuilder();
        graph.SetConst(0, StatePrimitive.I32);
        graph.SetStrictArrayInit(1, 0);
        graph.SetVarType("lx", StatePrimitive.I32);
        graph.SetVar("lx", 2);
        graph.SetIntConst(3, StatePrimitive.U8);
        graph.SetEquality(2, 3, 4);
        graph.CreateLambda(4, 5, "lx");
        graph.SetIsAny(1, 5, 6);
        graph.SetDef("y", 6);
        var result = graph.Solve();
        result.AssertNoGenerics();
        result.AssertNamed(StatePrimitive.I32, "lx");
        result.AssertNode(StateFun.Of(StatePrimitive.I32, StatePrimitive.Bool), 5);
    }

    [Test]
    public void Anything_WithLambdaArgDowncast_Throws() {
        //     6  1 0          5  243
        //y = Any([ 1.0 ], x:int->x==0)
        var graph = new GraphBuilder();
        graph.SetConst(0, StatePrimitive.Real);
        graph.SetStrictArrayInit(1, 0);
        graph.SetVarType("lx", StatePrimitive.I32);
        graph.SetVar("lx", 2);
        graph.SetIntConst(3, StatePrimitive.U8);
        graph.SetEquality(2, 3, 4);
        TestHelper.AssertThrowsTicError(
            () => {
                graph.CreateLambda(4, 5, "lx");
                graph.SetIsAny(1, 5, 6);
                graph.SetDef("y", 6);
                graph.Solve();
            });
    }

    [Test]
    //[Ignore("Upcast for complex types")]
    public void Anything_WithArgUpcastStrictArrayArg() {
        //     6  1 0     5       2 4 3
        //y = Any([ 1i ], x:real->x ==0)
        var graph = new GraphBuilder();
        graph.SetConst(0, StatePrimitive.I32);
        graph.SetStrictArrayInit(1, 0);
        graph.SetVarType("lx", StatePrimitive.Real);
        graph.SetVar("lx", 2);
        graph.SetIntConst(3, StatePrimitive.U8);
        graph.SetEquality(2, 3, 4);
        graph.CreateLambda(4, 5, "lx");
        graph.SetIsAny(1, 5, 6);
        graph.SetDef("y", 6);
        var result = graph.Solve();
        result.AssertNoGenerics();
        result.AssertNamed(StatePrimitive.Real, "lx");
        result.AssertNode(StateFun.Of(StatePrimitive.Real, StatePrimitive.Bool), 5);
    }

    [Test]
    public void Anything_WithBoolArray() {
        //     3  0  2  1
        //y = Any(a, x->x)
        var graph = new GraphBuilder();
        graph.SetVar("a", 0);
        graph.SetVar("2lx", 1);
        graph.CreateLambda(1, 2, "2lx");
        graph.SetIsAny(0, 2, 3);
        graph.SetDef("y", 3);
        var result = graph.Solve();
        result.AssertNoGenerics();
        result.AssertNamed(StateArray.Of(StatePrimitive.Bool), "a");
        result.AssertNamed(StatePrimitive.Bool, "2lx");
        result.AssertNode(StateFun.Of(StatePrimitive.Bool, StatePrimitive.Bool), 2);
    }

    [Test]
    public void Anything_WithConcreteFun() {
        //     2  0   1
        //y = Any(a, isNan)
        var graph = new GraphBuilder();
        graph.SetVar("a", 0);
        graph.SetVarType("isNan", StateFun.Of(StatePrimitive.Real, StatePrimitive.Bool));
        graph.SetVar("isNan", 1);
        graph.SetIsAny(0, 1, 2);
        graph.SetDef("y", 2);
        var result = graph.Solve();
        result.AssertNoGenerics();
        result.AssertNamed(StateArray.Of(StatePrimitive.Real), "a");
    }

    [Test]
    public void Anything_WithConcreteFunAndUpcast() {
        //              2  0   1
        //a:int[]; y = Any(a, isNan)
        var graph = new GraphBuilder();
        graph.SetVarType("a", StateArray.Of(StatePrimitive.I32));
        graph.SetVar("a", 0);
        graph.SetVarType("isNan", StateFun.Of(StatePrimitive.Real, StatePrimitive.Bool));
        graph.SetVar("isNan", 1);
        graph.SetIsAny(0, 1, 2);
        graph.SetDef("y", 2);
        var result = graph.Solve();
        result.AssertNoGenerics();
    }
}

}