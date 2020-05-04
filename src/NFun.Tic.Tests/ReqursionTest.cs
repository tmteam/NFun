using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NFun.Tic.SolvingStates;
using NUnit.Framework;

namespace NFun.Tic.Tests
{
    class ReqursionTest
    {
        [Test]
        public void SimpleReqursive()
        {
            //node |       1 0  3 2
            //expr |y(x) = y(x) + 1i 
            var graph = new GraphBuilder();
            var fun = graph.CreateFunction(3, null, "x");
            graph.SetVar("x", 0);
            graph.SetCall(fun, 0, 1);
            graph.SetConst(2, Primitive.I32);
            graph.SetArith(1, 2, 3);
            var result = graph.Solve();
            result.AssertNoGenerics();
            Assert.AreEqual(fun.ReturnType, Primitive.I32);
            Assert.AreEqual(fun.Args.First(), Primitive.Any);
            //var generic = result.AssertAndGetSingleGeneric(null, null, false);
            //Assert.AreEqual(fun.ReturnType, Primitive.I32);
            //Assert.AreEqual(fun.Args.First(), generic.State);
        }
        [Test]
        public void ReqursiveWithoutGenerics()
        {
            //node |       4 3  5 1 2  0
            //expr |y(x) = y(x) + x + 1.0
            var graph = new GraphBuilder();
            
            var fun = graph.CreateFunction(5, null, "x");
            graph.SetConst(0, Primitive.Real);
            graph.SetVar("x", 1);
            graph.SetArith(0,1,2);
            
            graph.SetVar("x", 3);
            graph.SetCall(fun, 3, 4);

            graph.SetArith(2, 4, 5);
            var result = graph.Solve();
            result.AssertNoGenerics();
            Assert.AreEqual(fun.ReturnType, Primitive.Real);
            Assert.AreEqual(fun.Args.First(), Primitive.Real);
        }
        [Test]
        public void ReqursiveWithGeneric()
        {
            //node |       4    0   2 1    3
            //expr |y(x) = if true: y(x) | x 
            var graph = new GraphBuilder();
            var fun = graph.CreateFunction(4, null, "x");
            graph.SetConst(0, Primitive.Bool);
            graph.SetVar("x", 1);
            graph.SetCall(fun, 1, 2);
            graph.SetVar("x", 3);
            graph.SetIfElse(new[] {0}, new[] {2, 3}, 4);
            var result = graph.Solve();
            var generic = result.AssertAndGetSingleGeneric(null, null, false);
            Assert.AreEqual(fun.ReturnType, generic.GetNonReference().State);
            Assert.AreEqual(fun.ArgNodes.First().GetNonReference().State, generic.GetNonReference().State);
        }
    }
}
