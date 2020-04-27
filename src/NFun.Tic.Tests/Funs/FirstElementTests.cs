using System;
using NFun.Tic.SolvingStates;
using NUnit.Framework;
using Array = NFun.Tic.SolvingStates.Array;

namespace NFun.Tic.Tests.Funs
{
    public class AnyElementTests
    {
        [Test]
        public void Anything_WithStrictArrayArg()
        {
            //     6  1 0    5  243
            //y = Any([ 1i ], x->x==0)
            var graph = new GraphBuilder();
            graph.SetConst(0, Primitive.I32);
            graph.SetArrayInit(1, 0);
            graph.SetVar("lx",2);
            graph.SetIntConst(3, Primitive.U8);
            graph.SetEquality(2,3,4);
            graph.CreateLambda(4,5,"lx");
            graph.SetIsAny(1,5,6);
            graph.SetDef("y", 6);
            var result = graph.Solve();
            result.AssertNoGenerics();
            result.AssertNamed(Primitive.I32, "lx");
            result.AssertNode(Fun.Of(argType: Primitive.I32, Primitive.Bool), 5);
        }
        [Test]
        public void Anything_WithStrictArrayAndLambdaArg()
        {
            //     6  1 0         5  2 4 3
            //y = Any([ 1i ], x:int->x== 0)
            var graph = new GraphBuilder();
            graph.SetConst(0, Primitive.I32);
            graph.SetArrayInit(1, 0);
            graph.SetVarType("lx", Primitive.I32);
            graph.SetVar("lx", 2);
            graph.SetIntConst(3, Primitive.U8);
            graph.SetEquality(2, 3, 4);
            graph.CreateLambda(4, 5, "lx");
            graph.SetIsAny(1, 5, 6);
            graph.SetDef("y", 6);
            var result = graph.Solve();
            result.AssertNoGenerics();
            result.AssertNamed(Primitive.I32, "lx");
            result.AssertNode(Fun.Of(Primitive.I32, Primitive.Bool), 5);
        }

        [Test]
        public void Anything_WithLambdaArgDowncast_Throws()
        {
            //     6  1 0          5  243
            //y = Any([ 1.0 ], x:int->x==0)
            var graph = new GraphBuilder();
            graph.SetConst(0, Primitive.Real);
            graph.SetArrayInit(1, 0);
            graph.SetVarType("lx", Primitive.I32);
            graph.SetVar("lx", 2);
            graph.SetIntConst(3, Primitive.U8);
            graph.SetEquality(2, 3, 4);
            try
            {
                graph.CreateLambda(4, 5, "lx");
                graph.SetIsAny(1, 5, 6);
                graph.SetDef("y", 6);
                graph.Solve();
                Assert.Fail("Impossible equation solved");
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        [Test]
        //[Ignore("Upcast for complex types")]
        public void Anything_WithArgUpcastStrictArrayArg()
        {
            //     6  1 0     5       2 4 3
            //y = Any([ 1i ], x:real->x ==0)
            var graph = new GraphBuilder();
            graph.SetConst(0, Primitive.I32);
            graph.SetArrayInit(1, 0);
            graph.SetVarType("lx", Primitive.Real);
            graph.SetVar("lx", 2);
            graph.SetIntConst(3, Primitive.U8);
            graph.SetEquality(2, 3, 4);
            graph.CreateLambda(4, 5, "lx");
            graph.SetIsAny(1, 5, 6);
            graph.SetDef("y", 6);
            var result = graph.Solve();
            result.AssertNoGenerics();
            result.AssertNamed(Primitive.Real, "lx");
            result.AssertNode(Fun.Of(Primitive.Real, Primitive.Bool), 5);
        }
        [Test]
        public void Anything_WithBoolArray()
        {
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
            result.AssertNamed(Array.Of(Primitive.Bool), "a");
            result.AssertNamed(Primitive.Bool, "2lx");
            result.AssertNode(Fun.Of(Primitive.Bool,Primitive.Bool), 2);

        }

        [Test]
        public void Anything_WithConcreteFun()
        {
            //     2  0   1
            //y = Any(a, isNan)
            var graph = new GraphBuilder();
            graph.SetVar("a", 0);
            graph.SetVarType("isNan", Fun.Of(Primitive.Real,Primitive.Bool));
            graph.SetVar("isNan", 1);
            graph.SetIsAny(0, 1, 2);
            graph.SetDef("y", 2);
            var result = graph.Solve();
            result.AssertNoGenerics();
            result.AssertNamed(Array.Of(Primitive.Real), "a");
        }

        [Test]
        public void Anything_WithConcreteFunAndUpcast()
        {
            //              2  0   1
            //a:int[]; y = Any(a, isNan)
            var graph = new GraphBuilder();
            graph.SetVarType("a", Array.Of(Primitive.I32));
            graph.SetVar("a", 0);
            graph.SetVarType("isNan", Fun.Of(Primitive.Real,Primitive.Bool));
            graph.SetVar("isNan", 1);
            graph.SetIsAny(0, 1, 2);
            graph.SetDef("y", 2);
            var result = graph.Solve();
            result.AssertNoGenerics();
        }
    }

    
}
