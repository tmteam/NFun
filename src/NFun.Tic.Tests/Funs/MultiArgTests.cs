using System;
using NFun.Tic.SolvingStates;
using NFun.TypeInferenceCalculator.Errors;
using NUnit.Framework;

namespace NFun.Tic.Tests.Funs
{
    public class MultiArgTests
    {
        [Test]
        public void Genericfold_GetSum()
        {
            //        5  0  4      132
            //y = fold(x, f(a,b)=a+b)
            var graph = new GraphBuilder();
            
            graph.SetVar("x", 0);
            graph.SetVar("la", 1);
            graph.SetVar("lb", 2);
            graph.SetArith(1,2,3);
            graph.CreateLambda(3, 4, "la","lb");
            graph.SetfoldCall(0, 4, 5);
            graph.SetDef("y", 5);

            var result = graph.Solve();

            var t = result.AssertAndGetSingleArithGeneric();

            result.AssertAreGenerics(t, "y","la","lb");
            result.AssertNamed(StateArray.Of(t), "x");
            result.AssertNode(StateFun.Of(new []{t,t}, t), 4);
        }

        [Test]
        public void fold_ConcreteLambdaReturn_GetSum()
        {
            //        5  0  4          132
            //y = fold(x, f(a,b):i64=a+b)
            var graph = new GraphBuilder();

            graph.SetVar("x", 0);
            graph.SetVar("la", 1);
            graph.SetVar("lb", 2);
            graph.SetArith(1, 2, 3);
            graph.CreateLambda(3, 4, StatePrimitive.I64, "la", "lb");
            graph.SetfoldCall(0, 4, 5);
            graph.SetDef("y", 5);

            var result = graph.Solve();

            result.AssertNoGenerics();


            result.AssertNamed(StatePrimitive.I64, "y", "la", "lb");
            result.AssertNamed(StateArray.Of(StatePrimitive.I64), "x");
            result.AssertNode(StateFun.Of(new[] { StatePrimitive.I64, StatePrimitive.I64 }, StatePrimitive.I64), 4);
        }



        [Test]
        public void foldConcreteOut_GetSum()
        {
            //            5  0  4      132
            //y:u32 = fold(x, f(a,b)=a+b)
            var graph = new GraphBuilder();

            graph.SetVar("x", 0);
            graph.SetVar("la", 1);
            graph.SetVar("lb", 2);
            graph.SetArith(1, 2, 3);
            graph.CreateLambda(3, 4, "la", "lb");
            graph.SetfoldCall(0, 4, 5);
            graph.SetVarType("y", StatePrimitive.U32);
            graph.SetDef("y", 5);

            var result = graph.Solve();

            result.AssertNoGenerics();

            result.AssertNamed(StatePrimitive.U32, "y", "la", "lb");
            result.AssertNamed(StateArray.Of(StatePrimitive.U32), "x");
            result.AssertNode(StateFun.Of(new[] { StatePrimitive.U32, StatePrimitive.U32 }, StatePrimitive.U32), 4);
        }

        [Test]
        public void  foldConcreteArg_GetSum()
        {
            //                 5  0  4      132
            //x:u32[]; y = fold(x, f(a,b)=a+b)
            var graph = new GraphBuilder();

            graph.SetVarType("y", StatePrimitive.U32);
            graph.SetVar("x", 0);
            graph.SetVar("la", 1);
            graph.SetVar("lb", 2);
            graph.SetArith(1, 2, 3);
            graph.CreateLambda(3, 4, "la", "lb");
            graph.SetfoldCall(0, 4, 5);
            graph.SetDef("y", 5);

            var result = graph.Solve();

            result.AssertNoGenerics();

            result.AssertNamed(StatePrimitive.U32, "y", "la", "lb");
            result.AssertNamed(StateArray.Of(StatePrimitive.U32), "x");
            result.AssertNode(StateFun.Of(new[] { StatePrimitive.U32, StatePrimitive.U32 }, StatePrimitive.U32), 4);
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
            graph.SetCall(new []{StatePrimitive.Real, StatePrimitive.Bool},new []{2,3});
            graph.SetBoolCall(1,3,4);
            graph.CreateLambda(4, 5, "la", "lb");
            graph.SetFoldCall(0, 5, 6);
            graph.SetDef("y",6);
            var result = graph.Solve();

            result.AssertNoGenerics();
    
            result.AssertNamed(StateArray.Of(StatePrimitive.Real), "x");
            result.AssertNamed(StatePrimitive.Real, "lb");
            result.AssertNamed(StatePrimitive.Bool, "la","y");
        }
        [Test]
        public void Fold_ConcreteLambda_GetSum()
        {
            //         5  0  4              132
            //y = fold(x, f(a,b:i32):i64=a+b)
            var graph = new GraphBuilder();

            graph.SetVar("x", 0);
            graph.SetVar("la", 1);
            graph.SetVarType("lb", StatePrimitive.I32);
            graph.SetVar("lb", 2);
            graph.SetArith(1, 2, 3);
            graph.CreateLambda(3, 4, StatePrimitive.I64, "la", "lb");
            graph.SetFoldCall( 0, 4, 5);
            graph.SetDef("y", 5);

            var result = graph.Solve();

            result.AssertNoGenerics();

            result.AssertNamed(StatePrimitive.I64, "y", "la");
            result.AssertNamed(StatePrimitive.I32, "lb");
            result.AssertNamed(StateArray.Of(StatePrimitive.I32), "x");
            result.AssertNode(StateFun.Of(new[] { StatePrimitive.I64, StatePrimitive.I32}, StatePrimitive.I64), 4);
        }

        [Test]
        public void fold_GetSumWithImpossibleTypes_throws()
        {
            //        5  0  4              132
            //y = fold(x, f(a,b:i32):i64=a+b)
            var graph = new GraphBuilder();

            graph.SetVar("x", 0);
            graph.SetVar("la", 1);
            graph.SetVarType("lb", StatePrimitive.I32);
            graph.SetVar("lb", 2);
            graph.SetArith(1, 2, 3);
            TestHelper.AssertThrowsTicError(() =>
            {
                graph.CreateLambda(3, 4, StatePrimitive.I64, "la", "lb");
                graph.SetfoldCall(0, 4, 5);
                graph.SetDef("y", 5);
                graph.Solve();
            });
        }

    }
}
