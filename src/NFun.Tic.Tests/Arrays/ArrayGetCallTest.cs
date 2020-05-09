using System;
using NFun.Tic.SolvingStates;
using NFun.TypeInferenceCalculator.Errors;
using NUnit.Framework;
using Array = NFun.Tic.SolvingStates.Array;

namespace NFun.Tic.Tests.Arrays
{
    public class ArrayGetCallTest
    {
        [Test(Description = "y = x[0]")]
        public void Generic()
        {
            //     2  0,1
            //y = get(x,0) 
            var graph = new GraphBuilder();
            graph.SetVar("x", 0);
            graph.SetConst(1, Primitive.I32);
            graph.SetArrGetCall(0, 1, 2);
            graph.SetDef("y", 2);
            var result = graph.Solve();
            var generic = result.AssertAndGetSingleGeneric(null, null);
            result.AssertNamedEqualToArrayOf(generic, "x");
            result.AssertAreGenerics(generic, "y");
        }

        [Test(Description = "y = [1,2][0]")]
        public void ConstrainsGeneric()
        {
            //     4  2 0,  1  3
            //y = get([ 1, -1],0) 
            var graph = new GraphBuilder();
            graph.SetIntConst(0, Primitive.U8);
            graph.SetIntConst(1, Primitive.I16);
            graph.SetArrayInit(2, 0, 1);
            graph.SetConst(3, Primitive.I32);
            graph.SetArrGetCall(2, 3, 4);
            graph.SetDef("y", 4);
            var result = graph.Solve();
            var generic = result.AssertAndGetSingleGeneric(Primitive.I16, Primitive.Real);
            result.AssertAreGenerics(generic, "y");

        }

        [Test(Description = "y:char = x[0]")]
        public void ConcreteDef()
        {
            //          2  0,1
            //y:char = get(x,0) 
            var graph = new GraphBuilder();
            graph.SetVar("x", 0);
            graph.SetConst(1, Primitive.I32);
            graph.SetArrGetCall(0, 1, 2);
            graph.SetVarType("y", Primitive.Char);
            graph.SetDef("y", 2);
            var result = graph.Solve();
            result.AssertNoGenerics();
            result.AssertNamedEqualToArrayOf(Primitive.Char, "x");
            result.AssertNamed(Primitive.Char, "y");
        }

        [Test(Description = "x:int[]; y = x[0]")]
        public void ConcreteArg()
        {
            //          2  0,1
            //x:int[]; y = get(x,0) 
            var graph = new GraphBuilder();
            graph.SetVarType("x", Array.Of(Primitive.I32));
            graph.SetVar("x", 0);
            graph.SetConst(1, Primitive.I32);
            graph.SetArrGetCall(0, 1, 2);
            graph.SetDef("y", 2);
            var result = graph.Solve();
            result.AssertNoGenerics();
            result.AssertNamedEqualToArrayOf(Primitive.I32, "x");
            result.AssertNamed(Primitive.I32, "y");
        }

        [Test(Description = "x:int[]; y = x[0]")]
        public void ConcreteArgAndDef_Upcast()
        {
            //          2  0,1
            //x:int[]; y:real = get(x,0) 
            var graph = new GraphBuilder();
            graph.SetVarType("x", Array.Of(Primitive.I32));
            graph.SetVar("x", 0);
            graph.SetConst(1, Primitive.I32);
            graph.SetArrGetCall(0, 1, 2);
            graph.SetVarType("y", Primitive.Real);
            graph.SetDef("y", 2);
            var result = graph.Solve();
            result.AssertNoGenerics();
            result.AssertNamedEqualToArrayOf(Primitive.I32, "x");
            result.AssertNamed(Primitive.Real, "y");
        }

        [Test(Description = "x:int[]; y = x[0]")]
        public void ConcreteArgAndDef_Impossible()
        {
            
                //          2  0,1
                //x:real[]; y:int = get(x,0) 
                var graph = new GraphBuilder();
                graph.SetVarType("x", Array.Of(Primitive.Real));
                graph.SetVar("x", 0);
                graph.SetConst(1, Primitive.I32);
                graph.SetArrGetCall(0, 1, 2);
                graph.SetVarType("y", Primitive.I32);
                TestHelper.AssertThrowsTicError(() =>
                {
                    graph.SetDef("y", 2);
                    graph.Solve();
                });
        }

        [Test(Description = "y = x[0][0]")]
        public void TwoDimentions_Generic()
        {
            //    4    2  0,1  3
            //y = get(get(x,0),0) 
            var graph = new GraphBuilder();
            graph.SetVar("x", 0);
            graph.SetConst(1, Primitive.I32);
            graph.SetArrGetCall(0, 1, 2);
            graph.SetConst(3, Primitive.I32);
            graph.SetArrGetCall(2, 3, 4);
            graph.SetDef("y", 4);
            var result = graph.Solve();
            var generic = result.AssertAndGetSingleGeneric(null, null);
            result.AssertNamed(Array.Of(new Array(generic)), "x");
            result.AssertAreGenerics(generic, "y");
        }


        [Test(Description = "y:int = x[0][0]")]
        public void TwoDimentions_ConcreteDef()
        {
            //    4    2  0,1  3
            //y:int = get(get(x,0),0) 
            var graph = new GraphBuilder();
            graph.SetVar("x", 0);
            graph.SetConst(1, Primitive.I32);
            graph.SetArrGetCall(0, 1, 2);
            graph.SetConst(3, Primitive.I32);
            graph.SetArrGetCall(2, 3, 4);
            graph.SetVarType("y", Primitive.I32);
            graph.SetDef("y", 4);
            var result = graph.Solve();
            result.AssertNoGenerics();
            result.AssertNamed(Array.Of(Array.Of(Primitive.I32)), "x");
            result.AssertNamed(Primitive.I32, "y");
        }

        [Test(Description = "x:int[][]; y = x[0][0]")]
        public void TwoDimentions_ConcreteArg()
        {
            //    4    2  0,1  3
            //x:int[][]; y = get(get(x,0),0) 
            var graph = new GraphBuilder();
            graph.SetVarType("x", Array.Of(Array.Of(Primitive.I32)));
            graph.SetVar("x", 0);
            graph.SetConst(1, Primitive.I32);
            graph.SetArrGetCall(0, 1, 2);
            graph.SetConst(3, Primitive.I32);
            graph.SetArrGetCall(2, 3, 4);
            graph.SetDef("y", 4);
            var result = graph.Solve();
            result.AssertNoGenerics();
            result.AssertNamed(Array.Of(Array.Of(Primitive.I32)), "x");
            result.AssertNamed(Primitive.I32, "y");
        }

        [Test(Description = "x:int[][]; y:int = x[0][0]")]
        public void TwoDimentions_ConcreteArgAndDef()
        {
            //                   4    2  0,1  3
            //x:int[][]; y:int = get(get(x,0),0) 
            var graph = new GraphBuilder();
            graph.SetVarType("x", Array.Of(Array.Of(Primitive.I32)));
            graph.SetVar("x", 0);
            graph.SetConst(1, Primitive.I32);
            graph.SetArrGetCall(0, 1, 2);
            graph.SetConst(3, Primitive.I32);
            graph.SetArrGetCall(2, 3, 4);
            graph.SetVarType("y", Primitive.I32);
            graph.SetDef("y", 4);
            var result = graph.Solve();
            result.AssertNoGenerics();
            result.AssertNamed(Array.Of(Array.Of(Primitive.I32)), "x");
            result.AssertNamed(Primitive.I32, "y");
        }

        [Test(Description = "x:int[][]; y:real = x[0][0]")]
        public void TwoDimentions_ConcreteArgAndDefWithUpcast()
        {
            //                    4    2  0,1  3
            //x:int[][]; y:real = get(get(x,0),0) 
            var graph = new GraphBuilder();
            graph.SetVarType("x", Array.Of(Array.Of(Primitive.I32)));
            graph.SetVar("x", 0);
            graph.SetConst(1, Primitive.I32);
            graph.SetArrGetCall(0, 1, 2);
            graph.SetConst(3, Primitive.I32);
            graph.SetArrGetCall(2, 3, 4);
            graph.SetVarType("y", Primitive.Real);
            graph.SetDef("y", 4);
            var result = graph.Solve();
            result.AssertNoGenerics();
            result.AssertNamed(Array.Of(Array.Of(Primitive.I32)), "x");
            result.AssertNamed(Primitive.Real, "y");
        }
        [Test(Description = "x:int[]; y:i16 = x[0]")]
        public void OneDimention_ImpossibleConcreteArgAndDef()
        {
            //                  2  0,1 
            //x:int[]; y:i16 = get(x,0) 
            var graph = new GraphBuilder();
            graph.SetVarType("x", Array.Of(Primitive.I32));
            graph.SetVar("x", 0);
            graph.SetConst(1, Primitive.I32);
            graph.SetArrGetCall(0, 1, 2);
            TestHelper.AssertThrowsTicError(() =>
            {
                graph.SetVarType("y", Primitive.I16);
                graph.SetDef("y", 2);
                graph.Solve();
            });
        }
        [Test(Description = "x:int[][]; y:i16 = x[0][0]")]
        public void TwoDimentions_ImpossibleConcreteArgAndDef()
        {
            //                   4    2  0,1  3
            //x:int[][]; y:i16 = get(get(x,0),0) 
            var graph = new GraphBuilder();
            graph.SetVarType("x", Array.Of(Array.Of(Primitive.I32)));
            graph.SetVar("x", 0);
            graph.SetConst(1, Primitive.I32);
            graph.SetArrGetCall(0, 1, 2);
            graph.SetConst(3, Primitive.I32);
            graph.SetArrGetCall(2, 3, 4);
            TestHelper.AssertThrowsTicError(() =>
            {
                graph.SetVarType("y", Primitive.I16);
                graph.SetDef("y", 4);
                graph.Solve();
            });
        }

        [Test(Description = "x:int[][]; y:i16 = x[0][0]")]
        public void ThreeDimentions_ConcreteDefArrayOf()
        {
            //           4    2  0,1  3
            //y:real[] = get(get(x,0),0) 
            var graph = new GraphBuilder();
            graph.SetVar("x", 0);
            graph.SetConst(1, Primitive.I32);
            graph.SetArrGetCall(0, 1, 2);
            graph.SetConst(3, Primitive.I32);
            graph.SetArrGetCall(2, 3, 4);
            graph.SetVarType("y", Array.Of(Primitive.Real));
            graph.SetDef("y", 4);
            var result = graph.Solve();
            result.AssertNoGenerics();
            result.AssertNamed(Array.Of(Array.Of(Array.Of(Primitive.Real))), "x");
            result.AssertNamed(Array.Of(Primitive.Real), "y");
        }

    }
}