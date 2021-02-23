using System;
using NFun.Tic.SolvingStates;
using NUnit.Framework;

namespace NFun.Tic.Tests.Arrays
{
    public class ConcreteArrayFunTest
    {
        [Test(Description = "y = x.NoNans()")]
        public void ConcreteCall()
        {
            //        1  0
            //y = NoNans(x) 
            var graph = new GraphBuilder();
            graph.SetVar("x", 0);
            graph.SetCall(new  ITicNodeState[]{StateArray.Of(StatePrimitive.Real), StatePrimitive.Bool}, new []{0,1});
            graph.SetDef("y", 1);
            
            var result = graph.Solve();
            result.AssertNoGenerics();
            result.AssertNamed(StateArray.Of(StatePrimitive.Real), "x");
            result.AssertNamed(StatePrimitive.Bool, "y");
        }

        [Test(Description = "x:int[]; y = x.NoNans()")]
        public void ConcreteCall_WithUpCast()
        {
            //                 1  0
            //x:int[]; y = NoNans(x) 
            var graph = new GraphBuilder();
            graph.SetVarType("x", StateArray.Of(StatePrimitive.I32));
            graph.SetVar("x", 0);
            graph.SetCall(new ITicNodeState[] { StateArray.Of(StatePrimitive.Real), StatePrimitive.Bool }, new[] { 0, 1 });
            graph.SetDef("y", 1);

            var result = graph.Solve();
            result.AssertNoGenerics();
            result.AssertNamed(StateArray.Of(StatePrimitive.I32), "x");
            result.AssertNamed(StatePrimitive.Bool, "y");
        }


        [Test(Description = "y = [1i,-1i].NoNans()")]
        public void ConcreteCall_WithGenericArray()
        {
            //        3   2 0  1
            //y = NoNans( [ 1, -1]) 
            var graph = new GraphBuilder();
            graph.SetIntConst(0, StatePrimitive.U8);
            graph.SetIntConst(1, StatePrimitive.I16);
            graph.SetStrictArrayInit(2, 0, 1);

            graph.SetCall(new ITicNodeState[] { StateArray.Of(StatePrimitive.Real), StatePrimitive.Bool }, new[] { 2, 3 });
            graph.SetDef("y", 3);

            var result = graph.Solve();
            result.AssertNoGenerics();
            result.AssertNode(StateArray.Of(StatePrimitive.Real),2);
            result.AssertNode(StatePrimitive.Real, 0,1);
            result.AssertNamed(StatePrimitive.Bool, "y");
        }
        [Test(Description = "reverse( 'hello')")]
        public void SetArrayConst()
        {
            //        1       0
            //y = reverse( 'hello') 
            var graph = new GraphBuilder();
            graph.SetArrayConst(0, StatePrimitive.Char);
            var t = graph.InitializeVarNode();
            graph.SetCall(new []{StateArray.Of(t), StateArray.Of(t)},new []{0,1});
            graph.SetDef("y", 1);

            var result = graph.Solve();
            result.AssertNoGenerics();
            result.AssertNode(StateArray.Of(StatePrimitive.Char), 0);
            result.AssertNamed(StateArray.Of(StatePrimitive.Char), "y");
        }
        [Test]
        public void CompareTwoDifferentArrays_Solved()
        {
            //    1 0    3   2         
            //y = [1.0] == 'abc'
            var graph = new GraphBuilder();
            graph.SetConst(0, StatePrimitive.Real);
            graph.SetStrictArrayInit(1, 0);
            graph.SetArrayConst(2, StatePrimitive.Char);
            var generic = graph.SetEquality(1,2,3);
            graph.SetDef("y", 3);

            var res = graph.Solve();
            res.AssertNoGenerics();
            res.AssertNamed(StatePrimitive.Bool, "y");
            //Assert.AreEqual(Array.Of(Primitive.Any), generic.Element);

        }

        [Test]
        public void CompareTwoDifferentArrays_Solved2()
        {
            //    1 0    3    2         
            //y = [1.0] == emptyArrayOfAny
            var graph = new GraphBuilder();
            graph.SetConst(0, StatePrimitive.Real);
            graph.SetStrictArrayInit(1, 0);
            graph.SetArrayConst(2, StatePrimitive.Any);
            var generic = graph.SetEquality(1, 2, 3);
            graph.SetDef("y", 3);

            var res = graph.Solve();
            res.AssertNoGenerics();
            res.AssertNamed(StatePrimitive.Bool, "y");
            //Assert.AreEqual(Array.Of(Primitive.Any), generic.Element);
        }
        [Test]
        public void CompareConcreteAndGenericEmptyArray()
        {
            TraceLog.IsEnabled = true;
            //    1 0   3  2         
            //y = [1.0] == []
            var graph = new GraphBuilder();
            graph.SetConst(0, StatePrimitive.Real);
            graph.SetStrictArrayInit(1, 0);
            var arrayType = graph.SetStrictArrayInit(2);
            var eqGeneric = graph.SetEquality(1, 2, 3);
            graph.SetDef("y", 3);

            var res = graph.Solve();
            
            Console.WriteLine(eqGeneric.GetNonReference());

            res.AssertNoGenerics();
            res.AssertNamed(StatePrimitive.Bool, "y");
            Assert.AreEqual(StatePrimitive.Real, arrayType.GetNonReference());
        }
        [Test]
        public void CompareConcreteAndGenericEmptyArray2()
        {
            //         0      2  1         
            //y = arrayOfReal == []
            var graph = new GraphBuilder();
            graph.SetArrayConst(0, StatePrimitive.Real);
            var arrayType = graph.SetStrictArrayInit(1);
            var eqGeneric = graph.SetEquality(0, 1, 2);
            graph.SetDef("y", 2);

            var res = graph.Solve();

            Console.WriteLine(eqGeneric.GetNonReference());

            res.AssertNoGenerics();
            res.AssertNamed(StatePrimitive.Bool, "y");
            Assert.AreEqual(StatePrimitive.Real, arrayType.GetNonReference());
        }

        [Test]
        public void Count()
        {
            //     1      0               
            //y = count('abc')
            var graph = new GraphBuilder();
            graph.SetArrayConst(0, StatePrimitive.Char);

            graph.SetCall(new ITicNodeState[]{StateArray.Of(StatePrimitive.Any), StatePrimitive.I32}, new []{0,1});
            graph.SetDef("y", 1);

            var res = graph.Solve();
            res.AssertNoGenerics();
            res.AssertNamed(StatePrimitive.I32, "y");
        }

        [Test]
        public void ImpossibleArgType_Throws()
        {
            //                 1  0
            //x:Any[]; y = NoNans(x)
            var graph = new GraphBuilder();
            graph.SetVarType("x", StateArray.Of(StatePrimitive.Any));
            graph.SetVar("x", 0);
            TestHelper.AssertThrowsTicError(() =>
            {
                graph.SetCall(new ITicNodeState[] {StateArray.Of(StatePrimitive.Real), StatePrimitive.Bool}, new[] {0, 1});
                graph.SetDef("y", 1);
                graph.Solve();
                Assert.Fail();
            });

        }
    }
}