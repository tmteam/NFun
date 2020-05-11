using System;
using NFun.Tic.SolvingStates;
using NFun.TypeInferenceCalculator.Errors;
using NUnit.Framework;
using Array = NFun.Tic.SolvingStates.Array;

namespace NFun.Tic.Tests.Funs
{
    public class MapTests
    {
        [Test]
        public void StrictArrayArg()
        {
            //     6  1 0     5  2  4 3
            //y = map([ 1i ], x->x == 0)
            var graph = new GraphBuilder();
            graph.SetConst(0, Primitive.I32);
            graph.SetStrictArrayInit(1, 0);
            graph.SetVar("lx", 2);
            graph.SetIntConst(3, Primitive.U8);
            graph.SetEquality(2, 3, 4);
            graph.CreateLambda(4, 5, "lx");
            graph.SetMap(1, 5, 6);
            graph.SetDef("y", 6);
            
            var result = graph.Solve();
            
            result.AssertNoGenerics();
            result.AssertNamed(Array.Of(Primitive.Bool), "y");
            result.AssertNamed(Primitive.I32, "lx");
            result.AssertNode(Fun.Of(Primitive.I32, Primitive.Bool), 5);
        }
        [Test]
        public void StrictArrayAndLambdaArg()
        {
            //     6  1 0     5      2 4 3
            //y = map([ 1i ], x:int->x * 2)
            var graph = new GraphBuilder();
            graph.SetConst(0, Primitive.I32);
            graph.SetStrictArrayInit(1, 0);
            graph.SetVarType("lx", Primitive.I32);
            graph.SetVar("lx", 2);
            graph.SetIntConst(3, Primitive.U8);
            graph.SetArith(2, 3, 4);
            graph.CreateLambda(4, 5, "lx");
            graph.SetMap(1, 5, 6);
            graph.SetDef("y", 6);
            
            var result = graph.Solve();
            
            result.AssertNoGenerics();
            result.AssertNamed(Array.Of(Primitive.I32), "y");
            result.AssertNamed(Primitive.I32, "lx");
            result.AssertNode(Fun.Of(Primitive.I32, Primitive.I32), 5);
        }

        [Test]
        public void LambdaArgDowncast_Throws()
        {
            //       6  1 0          5  2 43
            //y = map([ 1.0 ], x:int->x==0)
            var graph = new GraphBuilder();
            graph.SetConst(0, Primitive.Real);
            graph.SetStrictArrayInit(1, 0);
            graph.SetVarType("lx", Primitive.I32);
            graph.SetVar("lx", 2);
            graph.SetIntConst(3, Primitive.U8);
            graph.SetEquality(2, 3, 4);
            TestHelper.AssertThrowsTicError(() =>
            {
                graph.CreateLambda(4, 5, "lx");
                graph.SetMap(1, 5, 6);
                graph.SetDef("y", 6);
                graph.Solve();
                Assert.Fail("Impossible equation solved");
            });
        }

        [Test]
        //[Ignore("Upcast for complex types")]
        public void ArgUpcastStrictArrayArg()
        {
            //     6  1 0     5       2 4 3
            //y = Map([ 1i ], x:real->x*2)
            var graph = new GraphBuilder();
            graph.SetConst(0, Primitive.I32);
            graph.SetStrictArrayInit(1, 0);
            graph.SetVarType("lx", Primitive.Real);
            graph.SetVar("lx", 2);
            graph.SetIntConst(3, Primitive.U8);
            graph.SetArith(2, 3, 4);
            graph.CreateLambda(4, 5, "lx");
            graph.SetMap(1, 5, 6);
            graph.SetDef("y", 6);
            
            var result = graph.Solve();
            
            result.AssertNoGenerics();
            result.AssertNamed(Primitive.Real, "lx");
            result.AssertNamed(Array.Of(Primitive.Real), "y");
            result.AssertNode(Fun.Of(Primitive.Real, Primitive.Real), 5);
        }
        [Test]
        public void ConcreteLambdaReturn()
        {
            //     6  1 0     5       2 4 3
            //y = Map([ 1i ], (x):real->x*2)
            var graph = new GraphBuilder();
            graph.SetConst(0, Primitive.I32);
            graph.SetStrictArrayInit(1, 0);
            graph.SetVar("lx", 2);
            graph.SetIntConst(3, Primitive.U8);
            graph.SetArith(2, 3, 4);
            graph.CreateLambda(4, 5,Primitive.Real, "lx");
            graph.SetMap(1, 5, 6);
            graph.SetDef("y", 6);

            var result = graph.Solve();

            result.AssertNoGenerics();
            result.AssertNamed(Primitive.I32, "lx");
            result.AssertNamed(Array.Of(Primitive.Real), "y");
            result.AssertNode(Fun.Of(Primitive.I32, Primitive.Real), 5);
        }

        [Test]
        public void Generic()
        {
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

            result.AssertNamed(Array.Of(t), "a","y");
            result.AssertNode(Fun.Of(t, t));
        }

        [Test]
        public void ConcreteOutput()
        {
            //     3  0  2 1
            //y:u16[] = Map(a, x->x)
            var graph = new GraphBuilder();
            graph.SetVar("a", 0);

            graph.SetVar("2lx", 1);
            graph.CreateLambda(1, 2, "2lx");
            graph.SetMap(0, 2, 3);
            graph.SetVarType("y", Array.Of(Primitive.U16));
            graph.SetDef("y", 3);

            var result = graph.Solve();

            result.AssertNoGenerics();

            result.AssertNamed(Array.Of(Primitive.U16), "a", "y");
            result.AssertNode(Fun.Of(Primitive.U16, Primitive.U16));
        }

        [Test]
        public void ConcreteFun()
        {
            //     2  0   1
            //y = Map(a, SQRT)
            var graph = new GraphBuilder();
            graph.SetVar("a", 0);
            graph.SetVarType("SQRT", Fun.Of(Primitive.Real,Primitive.Real));
            graph.SetVar("SQRT", 1);
            graph.SetMap(0, 1, 2);
            graph.SetDef("y", 2);
            
            var result = graph.Solve();
            
            result.AssertNoGenerics();
            result.AssertNamed(Array.Of(Primitive.Real), "a","y");
        }

        [Test]
        public void ConcreteFunAndUpcast()
        {
            //                2  0   1
            //a:int[]; y = Map(a, SQRT)
            var graph = new GraphBuilder();
            graph.SetVarType("a", Array.Of(Primitive.I32));
            graph.SetVar("a", 0);
            graph.SetVarType("SQRT", Fun.Of(Primitive.Real,Primitive.Real));
            graph.SetVar("SQRT", 1);
            graph.SetMap(0, 1, 2);
            graph.SetDef("y", 2);
            
            var result = graph.Solve();
            
            result.AssertNoGenerics();
            result.AssertNamed(Array.Of(Primitive.Real), "y");
        }
    }
}
