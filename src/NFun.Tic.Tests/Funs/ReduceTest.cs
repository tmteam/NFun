using System;
using System.Collections.Generic;
using System.Text;
using NFun.Tic.SolvingStates;
using NFun.TypeInferenceCalculator;
using NUnit.Framework;
using Array = NFun.Tic.SolvingStates.Array;

namespace NFun.Tic.Tests.Funs
{
    public class foldTest
    {
        [SetUp] public void Initiazlize() => TraceLog.IsEnabled = true;
        [TearDown] public void Finalize() => TraceLog.IsEnabled = false;

        [Test]
        public void fold_foreachi()
        {
            // fun swapIfNotSorted(T_0[],Int32):T_0[]  where T_0: <>

            //             6  32   1      0         4      5
            //sorted = fold([0..count(input)], input, swapIfNotSorted)";
            var graph = new GraphBuilder();
            graph.SetVar("input", 0);

            graph.SetSizeOfArrayCall(0, 1 );  //count
            graph.SetIntConst(2, Primitive.U8);
            graph.SetRangeCall(2, 1, 3);      //range
            graph.SetVar("input", 4);

            var tOfSwap = graph.InitializeVarNode(isComparable: true);
            graph.SetVarType("swapIfNotSorted", Fun.Of(new IState[] { Array.Of(tOfSwap), Primitive.I32 }, Array.Of(tOfSwap)));
            graph.SetVar("swapIfNotSorted", 5);

            graph.SetfoldCall(3, 4, 5, 6);
            graph.SetDef("sorted", 6);

            var result = graph.Solve();
            var generic = result.AssertAndGetSingleGeneric(null, null, true);
            result.AssertNamed(Array.Of(generic), "sorted", "input");
        }

        [Test]
        public void fold_foreachi_rangeIsFixed()
        {
            // fun swapIfNotSorted(T_0[],Int32):T_0[]  where T_0: <>
            
            //             6  32   1      0         4      5
            //sorted = fold([0..count(input)], input, swapIfNotSorted)";
            var graph = new GraphBuilder();
            graph.SetVar("input",0);
            var tOfCount = graph.InitializeVarNode();
            //count
            graph.SetCall(new IState[]{Array.Of(tOfCount), Primitive.I32}, new[]{0,1});
            graph.SetIntConst(2, Primitive.U8);
            //range
            graph.SetCall(new IState[] {Primitive.I32, Primitive.I32, Array.Of(Primitive.I32)}, new []{2,1,3});
            graph.SetVar("input", 4);

            var tOfSwap = graph.InitializeVarNode(isComparable:true);

            graph.SetVarType("swapIfNotSorted", Fun.Of(new IState[]{Array.Of(tOfSwap), Primitive.I32}, Array.Of(tOfSwap)));
            graph.SetVar("swapIfNotSorted",5);

            graph.SetfoldCall(3,4,5,6);
            graph.SetDef("sorted",6);
            
            var result = graph.Solve();
            var generic = result.AssertAndGetSingleGeneric(null, null, true);
            result.AssertNamed(Array.Of(generic),"sorted","input");
        }

        [Test]
        public void fold_for()
        {
            // fun swapIfNotSorted(T_0[],Int32):T_0[]  where T_0: <>

            //             5  21   0    3         4      
            //sorted = fold([0..5], input, swapIfNotSorted)";
            var graph = new GraphBuilder();


            graph.SetIntConst(0, Primitive.U8);
            graph.SetIntConst(1, Primitive.U8);
            graph.SetCall(new IState[] { Primitive.I32, Primitive.I32, Array.Of(Primitive.I32) }, new[] { 1, 0, 2 });
            graph.SetVar("input", 3);

            var tOfSwap = graph.InitializeVarNode(isComparable: true);

            graph.SetVarType("swapIfNotSorted", Fun.Of(new IState[] { Array.Of(tOfSwap), Primitive.I32 }, Array.Of(tOfSwap)));
            graph.SetVar("swapIfNotSorted", 4);

            graph.SetfoldCall(2, 3, 4, 5);
            graph.SetDef("sorted", 5);

            var result = graph.Solve();
            var generic = result.AssertAndGetSingleGeneric(null, null, true);
            result.AssertNamed(Array.Of(generic), "sorted", "input");
        }
    }
}
