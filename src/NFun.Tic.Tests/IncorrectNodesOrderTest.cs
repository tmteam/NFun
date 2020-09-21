using System;
using System.Collections.Generic;
using System.Text;
using NFun.Tic.SolvingStates;
using NUnit.Framework;

namespace NFun.Tic.Tests
{
    public class IncorrectNodesOrderTest
    {
        [Test]
        public void ZeroNodeIsSkipped_Solved()
        {

            //x = 16i
            var graph = new GraphBuilder();
            graph.SetConst(2, StatePrimitive.I32);
            graph.SetDef("x", 2);
            var result = graph.Solve();
            result.AssertNoGenerics();
            result.AssertNamed(StatePrimitive.I32, "x");
        }
        [Test]
        public void ArithmeticIfIncorrectOrder()
        {
            //   2    4  5        7 6 8
            //y = if (a) x; else (z + 1);
            var graph = new GraphBuilder();

            graph.SetVar("a", 4);
            graph.SetVar("x", 5);
            graph.SetVar("z", 7);
            graph.SetIntConst(8, StatePrimitive.U8);
            graph.SetArith(7, 8, 6);
            graph.SetIfElse(new[] { 4 }, new[] { 5, 6 }, 2);
            graph.SetDef("y", 2);

            var result = graph.Solve();

            var generic = result.AssertAndGetSingleArithGeneric();
            result.AssertAreGenerics(generic, "y", "x", "z");
            result.AssertNamed(StatePrimitive.Bool, "a");
        }
        [Test]
        public void ArithmeticIfIncorrectOrderAndApplySequence()
        {
            //   2    4  5        7 6 8
            //y = if (a) x; else (z + 1);
            var graph = new GraphBuilder();
            /*
             * Exit:4. VAR a
             * Exit:5. VAR x
             * Exit:7. VAR z
             * Exit:8. Constant 1 
Exit:6. Call +(2)  
Exit:2. if(4): 5 else 6 
Exit:1. y:Empty = 2 
             */

            graph.SetVar("a", 4);
            graph.SetVar("x", 5);
            graph.SetVar("z", 7);
            graph.SetIntConst(8, StatePrimitive.U8);
            graph.SetArith(7, 8, 6);
            graph.SetIfElse(new[] { 4 }, new[] { 5, 6 }, 2);
            graph.SetDef("y", 2);

            var result = graph.Solve();

            var generic = result.AssertAndGetSingleArithGeneric();
            result.AssertAreGenerics(generic, "y", "x", "z");
            result.AssertNamed(StatePrimitive.Bool, "a");
        }
    }
}
