using System;
using NFun.Tic.SolvingStates;
using NFun.TypeInferenceCalculator;
using NFun.TypeInferenceCalculator.Errors;
using NUnit.Framework;
using Array = NFun.Tic.SolvingStates.Array;

namespace NFun.Tic.Tests.Funs
{
    public class TrickyTest
    {
        [SetUp] public void Initiazlize() => TraceLog.IsEnabled = true;
        [TearDown] public void Finalize() => TraceLog.IsEnabled = false;


        [Test]
        public void MapWithGenericArrayOfArray()
        {
            //    4   0  3    2     1     
            //y = map(a, x->reverse(x))
            var graph = new GraphBuilder();
            graph.SetVar("a", 0);
            graph.SetVar("lx", 1);
            var generic = graph.InitializeVarNode();

            graph.SetCall(new []{Array.Of(generic), Array.Of(generic)} , new []{1,2});
            graph.CreateLambda(2,3, "lx");
            graph.SetMap(0, 3,4);
            graph.SetDef("y", 4);

            var result = graph.Solve();

            var t = result.AssertAndGetSingleGeneric(null,null);

            result.AssertNamed(Array.Of(t), "lx");
            result.AssertNamed(Array.Of(Array.Of(t)), "a","y");

            result.AssertNode(Fun.Of(Array.Of(t), Array.Of(t)), 3);
        }
        [Ignore("UB")]
        [Test]
        public void FunDefCallTest_returnIsStrict()
        {
            //                1 0     
            //call(f,x):int = f(x)
            var graph = new GraphBuilder();
            graph.SetVar("x", 0);
            graph.SetCall("f",0,1);
            graph.SetVarType("return", Primitive.I32);
            graph.SetDef("return",1);
            var result = graph.Solve();

            var t = result.AssertAndGetSingleGeneric(null, null);

            result.AssertAreGenerics(t, "x");
            result.AssertNamed(Fun.Of(t,SolvingNode.CreateTypeNode(Primitive.I32)), "f");
            result.AssertNamed(Primitive.I32, "return");
        }

        [Test]
        public void FunDefCallTest_argIsStrict()
        {
            //                1 0     
            //call(f,x:int) = f(x)
            var graph = new GraphBuilder();
            graph.SetVarType("x", Primitive.I32);
            graph.SetVar("x", 0);
            graph.SetCall("f", 0, 1);

            graph.SetDef("return", 1);
            var result = graph.Solve();

            var t = result.AssertAndGetSingleGeneric(null, null);

            result.AssertAreGenerics(t, "return");
            result.AssertNamed(Fun.Of(Primitive.I32, new RefTo(t)), "f");
        }
        [Test]
        public void FunDefCallTest_strict()
        {
            //                    1 0     
            //call(f,x:int):int = f(x)
            var graph = new GraphBuilder();
            graph.SetVarType("x", Primitive.I32);
            graph.SetVar("x", 0);
            graph.SetCall("f", 0, 1);
            graph.SetVarType("return", Primitive.I32);
            graph.SetDef("return", 1);
            var result = graph.Solve();

            result.AssertNoGenerics();

            result.AssertNamed(Primitive.I32, "x","return");
            result.AssertNamed(Fun.Of(Primitive.I32, Primitive.I32), "f");
        }

        [Test]
        public void DowncastCallOfFunVar()
        {
            //                  1  0     
            //g: f(any):int; x = g(1.0)
            var graph = new GraphBuilder();
            graph.SetVarType("g", Fun.Of(Primitive.Any, Primitive.I32));
            graph.SetConst(0, Primitive.Real);
            graph.SetCall("g", 0, 1);
            graph.SetDef("x", 1);
            var result = graph.Solve();

            result.AssertNoGenerics();

            result.AssertNamed(Primitive.I32, "x");
            result.AssertNamed(Fun.Of(Primitive.Any, Primitive.I32), "g");
        }
        [Test]
        public void DowncastFunctionalArgument_throws()
        {
            // myFun(f(any):T ):T
            //       4   3         021     
            // y = myFun((x:real)->x+1.0)
            var graph = new GraphBuilder();
            
            graph.SetVarType("lx", Primitive.Real);
            graph.SetVar("lx", 0);
            graph.SetConst(1, Primitive.Real);
            graph.SetArith(0,1, 2);
            graph.CreateLambda(2, 3, "lx");
            var generic = graph.InitializeVarNode();
            TestHelper.AssertThrowsTicError(() =>
            {
                // myFun(f(any):T ):T
                graph.SetCall(new IState[] {Fun.Of(Primitive.Any, generic), generic}, new[] {3, 4});
                graph.SetDef("y", 4);
                graph.Solve();
                Assert.Fail("Impossible equation solved");
            });
        }

        [Test]
        public void SequenceCall()
        {
            //myFun() = i->i
            //    2 0       1
            //x = (myFun())(2)

            var graph = new GraphBuilder();
            var generic = graph.InitializeVarNode();
            graph.SetCall(new IState[]{Fun.Of(generic,generic)},new []{0});
            
            graph.SetIntConst(1,Primitive.U8);
            
            graph.SetCall(0,new []{1,2});
            graph.SetDef("x",2);

            var result = graph.Solve();
            var t = result.AssertAndGetSingleGeneric(Primitive.U8, Primitive.Real);
            result.AssertAreGenerics(t, "x");
        }
        [Test]
        public void GenericCallWithFunVar()
        {
            //fun = i->i
            //    1   0   
            //x = fun(2)

            var graph = new GraphBuilder();
            var generic = graph.InitializeVarNode();
            graph.SetIntConst(0, Primitive.U8);
            graph.SetCall(Fun.Of(generic, generic), new[] { 0, 1 });
            graph.SetDef("x", 1);

            var result = graph.Solve();
            var t = result.AssertAndGetSingleGeneric(Primitive.U8, Primitive.Real);
            result.AssertAreGenerics(t, "x");
        }

        [Test]
        public void GenericCallWithStates()
        {
            //fun = i->i
            //    1   0   
            //x = fun(2)

            var graph = new GraphBuilder();
            var generic = graph.InitializeVarNode();
            graph.SetIntConst(0, Primitive.U8);
            graph.SetCall(new IState[]{generic, generic},  new[] { 0, 1 });
            graph.SetDef("x", 1);

            var result = graph.Solve();
            var t = result.AssertAndGetSingleGeneric(Primitive.U8, Primitive.Real);
            result.AssertAreGenerics(t, "x");
        }
        [Test]
        public void SequenceCallWithFunVar()
        {
            //myFun() = i->i
            //    2 0       1
            //x = (myFun())(2)

            var graph = new GraphBuilder();
            var generic = graph.InitializeVarNode();
            graph.SetCall(Fun.Of(new IState[0],  Fun.Of(generic, generic)), 0);
            graph.SetIntConst(1, Primitive.U8);
            graph.SetCall(0, new[] { 1, 2 });
            graph.SetDef("x", 2);

            var result = graph.Solve();
            var t = result.AssertAndGetSingleGeneric(Primitive.U8, Primitive.Real);
            result.AssertAreGenerics(t, "x");
        }
        [Test]
        public void SequenceCallWithLambda()
        {
            //myFun() = i->i
            //    31  0  2     
            //x = (i->i)(2)

            var graph = new GraphBuilder();
            graph.SetVar("li",0);
            graph.CreateLambda(0,1,"li");
            graph.SetIntConst(2, Primitive.U8);
            graph.SetCall(1, new[] { 2, 3 });
            graph.SetDef("x", 3);

            var result = graph.Solve();
            var t = result.AssertAndGetSingleGeneric(Primitive.U8, Primitive.Real);
            result.AssertAreGenerics(t, "x");
        }
    }
}
