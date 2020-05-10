using System;
using NFun.Tic.SolvingStates;
using NFun.TypeInferenceCalculator.Errors;
using NUnit.Framework;
using Array = NFun.Tic.SolvingStates.Array;

namespace NFun.Tic.Tests.Funs
{
    public class MultiArgTests
    {
        [Test]
        public void GenericReduce_GetSum()
        {
            //        5  0  4      132
            //y = reduce(x, f(a,b)=a+b)
            var graph = new GraphBuilder();
            
            graph.SetVar("x", 0);
            graph.SetVar("la", 1);
            graph.SetVar("lb", 2);
            graph.SetArith(1,2,3);
            graph.CreateLambda(3, 4, "la","lb");
            graph.SetReduceCall(0, 4, 5);
            graph.SetDef("y", 5);

            var result = graph.Solve();

            var t = result.AssertAndGetSingleArithGeneric();

            result.AssertAreGenerics(t, "y","la","lb");
            result.AssertNamed(Array.Of(t), "x");
            result.AssertNode(Fun.Of(new []{t,t}, t), 4);
        }

        [Test]
        public void Reduce_ConcreteLambdaReturn_GetSum()
        {
            //        5  0  4          132
            //y = reduce(x, f(a,b):i64=a+b)
            var graph = new GraphBuilder();

            graph.SetVar("x", 0);
            graph.SetVar("la", 1);
            graph.SetVar("lb", 2);
            graph.SetArith(1, 2, 3);
            graph.CreateLambda(3, 4, Primitive.I64, "la", "lb");
            graph.SetReduceCall(0, 4, 5);
            graph.SetDef("y", 5);

            var result = graph.Solve();

            result.AssertNoGenerics();


            result.AssertNamed(Primitive.I64, "y", "la", "lb");
            result.AssertNamed(Array.Of(Primitive.I64), "x");
            result.AssertNode(Fun.Of(new[] { Primitive.I64, Primitive.I64 }, Primitive.I64), 4);
        }



        [Test]
        public void ReduceConcreteOut_GetSum()
        {
            //            5  0  4      132
            //y:u32 = reduce(x, f(a,b)=a+b)
            var graph = new GraphBuilder();

            graph.SetVar("x", 0);
            graph.SetVar("la", 1);
            graph.SetVar("lb", 2);
            graph.SetArith(1, 2, 3);
            graph.CreateLambda(3, 4, "la", "lb");
            graph.SetReduceCall(0, 4, 5);
            graph.SetVarType("y", Primitive.U32);
            graph.SetDef("y", 5);

            var result = graph.Solve();

            result.AssertNoGenerics();

            result.AssertNamed(Primitive.U32, "y", "la", "lb");
            result.AssertNamed(Array.Of(Primitive.U32), "x");
            result.AssertNode(Fun.Of(new[] { Primitive.U32, Primitive.U32 }, Primitive.U32), 4);
        }

        [Test]
        public void  ReduceConcreteArg_GetSum()
        {
            //                 5  0  4      132
            //x:u32[]; y = reduce(x, f(a,b)=a+b)
            var graph = new GraphBuilder();

            graph.SetVarType("y", Primitive.U32);
            graph.SetVar("x", 0);
            graph.SetVar("la", 1);
            graph.SetVar("lb", 2);
            graph.SetArith(1, 2, 3);
            graph.CreateLambda(3, 4, "la", "lb");
            graph.SetReduceCall(0, 4, 5);
            graph.SetDef("y", 5);

            var result = graph.Solve();

            result.AssertNoGenerics();

            result.AssertNamed(Primitive.U32, "y", "la", "lb");
            result.AssertNamed(Array.Of(Primitive.U32), "x");
            result.AssertNode(Fun.Of(new[] { Primitive.U32, Primitive.U32 }, Primitive.U32), 4);
        }


        [Test]
        //[Ignore("Input variable generic")]
        public void GenericFold_AllIsNan()
        {
            //      6  0  5      1  4    3   2
            //y = fold(x, f(a,b)=a and isNan(b))
            var graph = new GraphBuilder();

            graph.SetVar("x", 0);
            graph.SetVar("la", 1);
            graph.SetVar("lb", 2);
            graph.SetCall(new []{Primitive.Real, Primitive.Bool},new []{2,3});
            graph.SetBoolCall(1,3,4);
            graph.CreateLambda(4, 5, "la", "lb");
            graph.SetFoldCall(0, 5, 6);
            graph.SetDef("y",6);
            var result = graph.Solve();

            result.AssertNoGenerics();
    
            result.AssertNamed(Array.Of(Primitive.Real), "x");
            result.AssertNamed(Primitive.Real, "lb");
            result.AssertNamed(Primitive.Bool, "la","y");
        }
        [Test]
        public void Fold_ConcreteLambda_GetSum()
        {
            //         5  0  4              132
            //y = fold(x, f(a,b:i32):i64=a+b)
            var graph = new GraphBuilder();

            graph.SetVar("x", 0);
            graph.SetVar("la", 1);
            graph.SetVarType("lb", Primitive.I32);
            graph.SetVar("lb", 2);
            graph.SetArith(1, 2, 3);
            graph.CreateLambda(3, 4, Primitive.I64, "la", "lb");
            graph.SetFoldCall( 0, 4, 5);
            graph.SetDef("y", 5);

            var result = graph.Solve();

            result.AssertNoGenerics();

            result.AssertNamed(Primitive.I64, "y", "la");
            result.AssertNamed(Primitive.I32, "lb");
            result.AssertNamed(Array.Of(Primitive.I32), "x");
            result.AssertNode(Fun.Of(new[] { Primitive.I64, Primitive.I32}, Primitive.I64), 4);
        }

        [Test]
        public void Reduce_GetSumWithImpossibleTypes_throws()
        {
            //        5  0  4              132
            //y = reduce(x, f(a,b:i32):i64=a+b)
            var graph = new GraphBuilder();

            graph.SetVar("x", 0);
            graph.SetVar("la", 1);
            graph.SetVarType("lb", Primitive.I32);
            graph.SetVar("lb", 2);
            graph.SetArith(1, 2, 3);
            TestHelper.AssertThrowsTicError(() =>
            {
                graph.CreateLambda(3, 4, Primitive.I64, "la", "lb");
                graph.SetReduceCall(0, 4, 5);
                graph.SetDef("y", 5);
                graph.Solve();
            });
        }

    }
}
