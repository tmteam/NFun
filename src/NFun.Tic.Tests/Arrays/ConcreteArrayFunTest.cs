using System;
using NFun.Tic.SolvingStates;
using NFun.TypeInferenceCalculator.Errors;
using NUnit.Framework;
using Array = NFun.Tic.SolvingStates.Array;

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
            graph.SetCall(new  IState[]{Array.Of(Primitive.Real), Primitive.Bool}, new []{0,1});
            graph.SetDef("y", 1);
            
            var result = graph.Solve();
            result.AssertNoGenerics();
            result.AssertNamed(Array.Of(Primitive.Real), "x");
            result.AssertNamed(Primitive.Bool, "y");
        }

        [Test(Description = "x:int[]; y = x.NoNans()")]
        public void ConcreteCall_WithUpCast()
        {
            //                 1  0
            //x:int[]; y = NoNans(x) 
            var graph = new GraphBuilder();
            graph.SetVarType("x", Array.Of(Primitive.I32));
            graph.SetVar("x", 0);
            graph.SetCall(new IState[] { Array.Of(Primitive.Real), Primitive.Bool }, new[] { 0, 1 });
            graph.SetDef("y", 1);

            var result = graph.Solve();
            result.AssertNoGenerics();
            result.AssertNamed(Array.Of(Primitive.I32), "x");
            result.AssertNamed(Primitive.Bool, "y");
        }


        [Test(Description = "y = [1i,-1i].NoNans()")]
        public void ConcreteCall_WithGenericArray()
        {
            //        3   2 0  1
            //y = NoNans( [ 1, -1]) 
            var graph = new GraphBuilder();
            graph.SetIntConst(0, Primitive.U8);
            graph.SetIntConst(1, Primitive.I16);
            graph.SetArrayInit(2, 0, 1);

            graph.SetCall(new IState[] { Array.Of(Primitive.Real), Primitive.Bool }, new[] { 2, 3 });
            graph.SetDef("y", 3);

            var result = graph.Solve();
            result.AssertNoGenerics();
            result.AssertNode(Array.Of(Primitive.Real),2);
            result.AssertNode(Primitive.Real, 0,1);
            result.AssertNamed(Primitive.Bool, "y");
        }
        [Test(Description = "reverse( 'hello')")]
        public void SetArrayConst()
        {
            //        1       0
            //y = reverse( 'hello') 
            var graph = new GraphBuilder();
            graph.SetArrayConst(0, Primitive.Char);
            var t = graph.InitializeVarNode();
            graph.SetCall(new []{Array.Of(t), Array.Of(t)},new []{0,1});
            graph.SetDef("y", 1);

            var result = graph.Solve();
            result.AssertNoGenerics();
            result.AssertNode(Array.Of(Primitive.Char), 0);
            result.AssertNamed(Array.Of(Primitive.Char), "y");
        }
        [Test]
        public void CompareTwoDifferentArrays_Solved()
        {
            //    1 0    3   2         
            //y = [1.0] == 'abc'
            var graph = new GraphBuilder();
            graph.SetConst(0, Primitive.Real);
            graph.SetArrayInit(1, 0);
            graph.SetArrayConst(2, Primitive.Char);
            var generic = graph.SetEquality(1,2,3);
            graph.SetDef("y", 3);

            var res = graph.Solve();
            res.AssertNoGenerics();
            res.AssertNamed(Primitive.Bool, "y");
            //Assert.AreEqual(Array.Of(Primitive.Any), generic.Element);

        }

        [Test]
        public void CompareTwoDifferentArrays_Solved2()
        {
            //    1 0    3    2         
            //y = [1.0] == emptyArrayOfAny
            var graph = new GraphBuilder();
            graph.SetConst(0, Primitive.Real);
            graph.SetArrayInit(1, 0);
            graph.SetArrayConst(2, Primitive.Any);
            var generic = graph.SetEquality(1, 2, 3);
            graph.SetDef("y", 3);

            var res = graph.Solve();
            res.AssertNoGenerics();
            res.AssertNamed(Primitive.Bool, "y");
            //Assert.AreEqual(Array.Of(Primitive.Any), generic.Element);
        }
        [Test]
        public void CompareConcreteAndGenericEmptyArray()
        {
            //    1 0   3  2         
            //y = [1.0] == []
            var graph = new GraphBuilder();
            graph.SetConst(0, Primitive.Real);
            graph.SetArrayInit(1, 0);
            var arrayType = graph.SetArrayInit(2);
            var eqGeneric = graph.SetEquality(1, 2, 3);
            graph.SetDef("y", 3);

            var res = graph.Solve();
            
            Console.WriteLine(eqGeneric.GetNonReference());

            res.AssertNoGenerics();
            res.AssertNamed(Primitive.Bool, "y");
            Assert.AreEqual(Primitive.Real, arrayType.GetNonReference());
        }
        [Test]
        public void CompareConcreteAndGenericEmptyArray2()
        {
            //         0      2  1         
            //y = arrayOfReal == []
            var graph = new GraphBuilder();
            graph.SetArrayConst(0, Primitive.Real);
            var arrayType = graph.SetArrayInit(1);
            var eqGeneric = graph.SetEquality(0, 1, 2);
            graph.SetDef("y", 2);

            var res = graph.Solve();

            Console.WriteLine(eqGeneric.GetNonReference());

            res.AssertNoGenerics();
            res.AssertNamed(Primitive.Bool, "y");
            Assert.AreEqual(Primitive.Real, arrayType.GetNonReference());
        }

        [Test]
        public void Count()
        {
            //     1      0               
            //y = count('abc')
            var graph = new GraphBuilder();
            graph.SetArrayConst(0, Primitive.Char);

            graph.SetCall(new IState[]{Array.Of(Primitive.Any), Primitive.I32}, new []{0,1});
            graph.SetDef("y", 1);

            var res = graph.Solve();
            res.AssertNoGenerics();
            res.AssertNamed(Primitive.I32, "y");
        }

        [Test]
        public void ImpossibleArgType_Throws()
        {
            //                 1  0
            //x:Any[]; y = NoNans(x)
            var graph = new GraphBuilder();
            graph.SetVarType("x", Array.Of(Primitive.Any));
            graph.SetVar("x", 0);
            TestHelper.AssertThrowsTicError(() =>
            {
                graph.SetCall(new IState[] {Array.Of(Primitive.Real), Primitive.Bool}, new[] {0, 1});
                graph.SetDef("y", 1);
                graph.Solve();
                Assert.Fail();
            });

        }
    }
}