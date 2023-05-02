using System;
using NFun.Tic.SolvingStates;
using NUnit.Framework;

namespace NFun.Tic.Tests.Funs;

using static StatePrimitive;

public class TrickyTest {
    [SetUp]
    public void Init() => TraceLog.IsEnabled = true;

    [TearDown]
    public void TearDown() => TraceLog.IsEnabled = false;


    [Test]
    public void MapWithGenericArrayOfArray() {
        //    4   0  3    2     1
        //y = map(a, x->reverse(x))
        var graph = new GraphBuilder();
        graph.SetVar("a", 0);
        graph.SetVar("lx", 1);
        var generic = graph.InitializeVarNode();

        graph.SetCall(new[] { StateArray.Of(generic), StateArray.Of(generic) }, new[] { 1, 2 });
        graph.CreateLambda(2, 3, "lx");
        graph.SetMap(0, 3, 4);
        graph.SetDef("y", 4);

        var result = graph.Solve();

        var t = result.AssertAndGetSingleGeneric(null, null);

        result.AssertNamed(StateArray.Of(t), "lx");
        result.AssertNamed(StateArray.Of(StateArray.Of(t)), "a", "y");

        result.AssertNode(StateFun.Of(StateArray.Of(t), StateArray.Of(t)), 3);
    }

    [Ignore("UB")]
    [Test]
    public void FunDefCallTest_returnIsStrict() {
        //                1 0
        //call(f,x):int = f(x)
        var graph = new GraphBuilder();
        graph.SetVar("x", 0);
        graph.SetCall("f", 0, 1);
        graph.SetVarType("return", I32);
        graph.SetDef("return", 1);
        var result = graph.Solve();

        var t = result.AssertAndGetSingleGeneric(null, null);

        result.AssertAreGenerics(t, "x");
        result.AssertNamed(StateFun.Of(t, TicNode.CreateTypeVariableNode(I32)), "f");
        result.AssertNamed(I32, "return");
    }

    [Test]
    public void FunDefCallTest_argIsStrict() {
        //                1 0
        //call(f,x:int) = f(x)
        var graph = new GraphBuilder();
        graph.SetVarType("x", I32);
        graph.SetVar("x", 0);
        graph.SetCall("f", 0, 1);

        graph.SetDef("return", 1);
        var result = graph.Solve();

        var t = result.AssertAndGetSingleGeneric(null, null);

        result.AssertAreGenerics(t, "return");
        result.AssertNamed(StateFun.Of(I32, new StateRefTo(t)), "f");
    }

    [Test]
    public void FunDefCallTest_strict() {
        //                    1 0
        //call(f,x:int):int = f(x)
        var graph = new GraphBuilder();
        graph.SetVarType("x", I32);
        graph.SetVar("x", 0);
        graph.SetCall("f", 0, 1);
        graph.SetVarType("return", I32);
        graph.SetDef("return", 1);
        var result = graph.Solve();

        result.AssertNoGenerics();

        result.AssertNamed(I32, "x", "return");
        result.AssertNamed(StateFun.Of(I32, I32), "f");
    }

    [Test]
    public void DowncastCallOfFunVar() {
        //                  1  0
        //g: f(any):int; x = g(1.0)
        var graph = new GraphBuilder();
        graph.SetVarType("g", StateFun.Of(Any, I32));
        graph.SetConst(0, Real);
        graph.SetCall("g", 0, 1);
        graph.SetDef("x", 1);
        var result = graph.Solve();

        result.AssertNoGenerics();

        result.AssertNamed(I32, "x");
        result.AssertNamed(StateFun.Of(Any, I32), "g");
    }

    [Test]
    public void DowncastFunctionalArgument_throws() {
        // myFun(f(any):T ):T
        //       4   3         021
        // y = myFun((x:real)->x+1.0)
        var graph = new GraphBuilder();

        graph.SetVarType("lx", Real);
        graph.SetVar("lx", 0);
        graph.SetConst(1, Real);
        graph.SetArith(0, 1, 2);
        graph.CreateLambda(2, 3, "lx");
        var generic = graph.InitializeVarNode();
        TestHelper.AssertThrowsTicError(
            () => {
                // myFun(f(any):T ):T
                graph.SetCall(
                    new ITicNodeState[] { StateFun.Of(Any, generic), generic }, new[] { 3, 4 });
                graph.SetDef("y", 4);
                graph.Solve();
                Assert.Fail("Impossible equation solved");
            });
    }

    [Test]
    public void SequenceCall() {
        //myFun() = i->i
        //    2 0       1
        //x = (myFun())(2)

        var graph = new GraphBuilder();
        var generic = graph.InitializeVarNode();
        graph.SetCall(new ITicNodeState[] { StateFun.Of(generic, generic) }, new[] { 0 });

        graph.SetIntConst(1, U8);

        graph.SetCall(0, new[] { 1, 2 });
        graph.SetDef("x", 2);

        var result = graph.Solve();
        var t = result.AssertAndGetSingleGeneric(U8, Real);
        result.AssertAreGenerics(t, "x");
    }

    [Test]
    public void GenericCallWithFunVar() {
        //fun = i->i
        //    1   0
        //x = rule(2)

        var graph = new GraphBuilder();
        var generic = graph.InitializeVarNode();
        graph.SetIntConst(0, U8);
        graph.SetCall(StateFun.Of(generic, generic), new[] { 0, 1 });
        graph.SetDef("x", 1);

        var result = graph.Solve();
        var t = result.AssertAndGetSingleGeneric(U8, Real);
        result.AssertAreGenerics(t, "x");
    }

    [Test]
    public void GenericCallWithStates() {
        //fun = i->i
        //    1   0
        //x = rule(2)

        var graph = new GraphBuilder();
        var generic = graph.InitializeVarNode();
        graph.SetIntConst(0, U8);
        graph.SetCall(new ITicNodeState[] { generic, generic }, new[] { 0, 1 });
        graph.SetDef("x", 1);

        var result = graph.Solve();
        var t = result.AssertAndGetSingleGeneric(U8, Real);
        result.AssertAreGenerics(t, "x");
    }

    [Test]
    public void SequenceCallWithFunVar() {
        //myFun() = i->i
        //    2 0       1
        //x = (myFun())(2)

        var graph = new GraphBuilder();
        var generic = graph.InitializeVarNode();
        graph.SetCall(StateFun.Of(Array.Empty<ITicNodeState>(), StateFun.Of(generic, generic)), 0);
        graph.SetIntConst(1, U8);
        graph.SetCall(0, new[] { 1, 2 });
        graph.SetDef("x", 2);

        var result = graph.Solve();
        var t = result.AssertAndGetSingleGeneric(U8, Real);
        result.AssertAreGenerics(t, "x");
    }

    [Test]
    public void SequenceCallWithLambda() {
        //myFun() = i->i
        //    31  0  2
        //x = (i->i)(2)

        var graph = new GraphBuilder();
        graph.SetVar("li", 0);
        graph.CreateLambda(0, 1, "li");
        graph.SetIntConst(2, U8);
        graph.SetCall(1, new[] { 2, 3 });
        graph.SetDef("x", 3);

        var result = graph.Solve();
        var t = result.AssertAndGetSingleGeneric(U8, Real);
        result.AssertAreGenerics(t, "x");
    }
}
