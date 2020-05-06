using System;
using NFun.Tic.SolvingStates;
using NUnit.Framework;
using Array = NFun.Tic.SolvingStates.Array;

namespace NFun.Tic.Tests.Arrays
{
    public class ArraySumCallTest
    {
        [Test(Description = "y = x.sum()")]
        public void Generic()
        {
            //     1  0
            //y = sum(x) 
            var graph = new GraphBuilder();
            graph.SetVar("x", 0);
            graph.SetSumCall(0, 1);
            graph.SetDef("y", 1);
            var result = graph.Solve();
            var generic = result.AssertAndGetSingleArithGeneric();
            result.AssertNamedEqualToArrayOf(generic, "x");
            result.AssertAreGenerics(generic, "y");
        }

        [Test(Description = "y = [1,-1].sum()")]
        public void ConstrainsGeneric()
        {
            //     3  2 0,  1  
            //y = sum([ 1, -1]) 
            var graph = new GraphBuilder();
            graph.SetIntConst(0, Primitive.U8);
            graph.SetIntConst(1, Primitive.I16);
            graph.SetArrayInit(2, 0, 1);
            graph.SetSumCall(2,3);
            graph.SetDef("y", 3);
            var result = graph.Solve();
            var generic = result.AssertAndGetSingleGeneric(Primitive.I32, Primitive.Real);
            result.AssertAreGenerics(generic, "y");
        }

        [Test(Description = "y:u32 = x.sum()")]
        public void ConcreteDefType()
        {
            //         1  0
            //y:u32 = sum(x) 
            var graph = new GraphBuilder();
            graph.SetVar("x", 0);
            graph.SetSumCall(0, 1);
            graph.SetVarType("y", Primitive.U32);
            graph.SetDef("y", 1);
            var result = graph.Solve();
            result.AssertNoGenerics();
            result.AssertNamedEqualToArrayOf(Primitive.U32, "x");
            result.AssertNamed(Primitive.U32, "y");
        }

        [Test(Description = "y:char = x.sum()")]
        public void ImpossibleDefType_Throws()
        {
            //          1  0
            //y:char = sum(x) 

            var graph = new GraphBuilder();
            try
            {
                graph.SetVar("x", 0);
                graph.SetSumCall(0, 1);
                graph.SetVarType("y", Primitive.Char);
                graph.SetDef("y", 1);
                graph.Solve();
                Assert.Fail();
            }
            catch (Exception e) 
            {
                Console.WriteLine(e);
            }
            
        }

        [Test(Description = "x:int[]; y = x.sum()")]
        public void ConcreteArg()
        {
            //               2 0
            //x:int[]; y = sum(x) 
            var graph = new GraphBuilder();
            graph.SetVarType("x", Array.Of(Primitive.I32));
            graph.SetVar("x", 0);
            graph.SetSumCall(0, 1);
            graph.SetDef("y", 1);
            var result = graph.Solve();
            result.AssertNoGenerics();
            result.AssertNamedEqualToArrayOf(Primitive.I32, "x");
            result.AssertNamed(Primitive.I32, "y");
        }

        [Test(Description = "x:int[]; y:real = x.sum()")]
        public void ConcreteArgAndDef_Upcast()
        {
            //                   2  0
            //x:int[]; y:real = sum(x) 
            var graph = new GraphBuilder();
            graph.SetVarType("x", Array.Of(Primitive.I32));
            graph.SetVar("x", 0);
            graph.SetSumCall(0, 1);
            graph.SetVarType("y", Primitive.Real);
            graph.SetDef("y", 1);
            var result = graph.Solve();
            result.AssertNoGenerics();
            result.AssertNamedEqualToArrayOf(Primitive.I32, "x");
            result.AssertNamed(Primitive.Real, "y");
        }

        [Test(Description = "x:real[]; y:int = x[0]")]
        public void Impossible_ConcreteArgAndDef_throws()
        {
            try
            {
                //                   1  0
                //x:real[]; y:int = sum(x) 
                var graph = new GraphBuilder();
                graph.SetVarType("x", Array.Of(Primitive.Real));
                graph.SetVar("x", 0);
                graph.SetSumCall(0, 1);
                graph.SetVarType("y", Primitive.I32);
                graph.SetDef("y", 1);
                var result = graph.Solve();
                Assert.Fail("Impossible equation solved");
            }
            catch (Exception e)
            {
                Console.WriteLine(e);

            }
        }
    }
}