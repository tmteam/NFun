using NFun.Tic.SolvingStates;
using NUnit.Framework;

namespace NFun.Tic.Tests.Arrays
{
    class ArrayInit
    {
        [Test]
        public void ArrayInitWithSpecifiedArrayType()
        {
            //           3 0  1  2 
            // y:int[] = [1i,2i,3i]
            var graph = new GraphBuilder();
            graph.SetVarType("y", StateArray.Of(StatePrimitive.I32));
            graph.SetConst(0, StatePrimitive.I32);
            graph.SetConst(1, StatePrimitive.I32);
            graph.SetConst(2, StatePrimitive.I32);
            graph.SetStrictArrayInit(3, 0, 1, 2);
            graph.SetDef("y", 3);
            var result = graph.Solve();
            result.AssertNoGenerics();
            result.AssertNamedEqualToArrayOf(StatePrimitive.I32, "y");
        }
        [Test]
        public void ArrayInitWithSpecifiedArrayTypeAndUpcast()
        {
            //            3 0  1  2 
            // y:real[] = [1i,2i,3i]
            var graph = new GraphBuilder();
            graph.SetVarType("y", StateArray.Of(StatePrimitive.Real));
            graph.SetConst(0, StatePrimitive.I32);
            graph.SetConst(1, StatePrimitive.I32);
            graph.SetConst(2, StatePrimitive.I32);
            graph.SetStrictArrayInit(3, 0, 1, 2);
            graph.SetDef("y", 3);

            var result = graph.Solve();
            result.AssertNoGenerics();
            result.AssertNamedEqualToArrayOf(StatePrimitive.Real, "y");
        }

        [Test]
        public void ArrayInitWithSpecifiedArrayTypeAndDowncast_fails()
        {
            //            3 0  1  2 
            // y:byte[] = [1i,2i,3i]
            var graph = new GraphBuilder();
            graph.SetVarType("y", StateArray.Of(StatePrimitive.U8));
            graph.SetConst(0, StatePrimitive.I32);
            graph.SetConst(1, StatePrimitive.I32);
            graph.SetConst(2, StatePrimitive.I32);
            graph.SetStrictArrayInit(3, 0, 1, 2);
            TestHelper.AssertThrowsTicError(() =>
            {
                graph.SetDef("y", 3);
                graph.Solve();
            });
        }

        [Test]
        public void GenericArrayInitWithSpecifiedArrayType()
        {
            //          3 0 1 2 
            // y:int[] = [1,2,3]
            var graph = new GraphBuilder();
            graph.SetVarType("y", StateArray.Of(StatePrimitive.I32));
            graph.SetIntConst(0, StatePrimitive.U8);
            graph.SetIntConst(1, StatePrimitive.U8);
            graph.SetIntConst(2, StatePrimitive.U8);
            graph.SetStrictArrayInit(3, 0, 1, 2);
            graph.SetDef("y", 3);

            var result = graph.Solve();
            result.AssertNoGenerics();
            result.AssertNamedEqualToArrayOf(StatePrimitive.I32, "y");
        }
        [Test]
        public void GenericArrayInit()
        {
            //    3 0 1 2 
            // y = [1,2,3]
            var graph = new GraphBuilder();
            graph.SetIntConst(0, StatePrimitive.U8);
            graph.SetIntConst(1, StatePrimitive.U8);
            graph.SetIntConst(2, StatePrimitive.U8);
            graph.SetStrictArrayInit(3, 0,1,2);
            graph.SetDef("y", 3);

            var result = graph.Solve();
            var generic = result.AssertAndGetSingleGeneric(StatePrimitive.U8, StatePrimitive.Real);
            result.AssertNamedEqualToArrayOf(generic, "y");
        }

        [Test]
        public void GenericArrayInitWithVariable()
        {
            //    3 0 1 2 
            // y = [1,2,x]
            var graph = new GraphBuilder();
            graph.SetIntConst(0, StatePrimitive.U8);
            graph.SetIntConst(1, StatePrimitive.U8);
            graph.SetVar("x",2);
            graph.SetStrictArrayInit(3, 0, 1, 2);
            graph.SetDef("y",3);

            var result = graph.Solve();
            var generic = result.AssertAndGetSingleGeneric(StatePrimitive.U8, StatePrimitive.Real);
            result.AssertNode(generic,0,1);
            result.AssertNamedEqualToArrayOf(generic, "y");
            result.AssertAreGenerics(generic, "x");
        }

        [Test]
        public void GenericArrayInitWithVariable2()
        {
            //    3 0 1 2 
            // y = [x,1,2]
            var graph = new GraphBuilder();
            graph.SetVar("x", 0);

            graph.SetIntConst(1, StatePrimitive.U8);
            graph.SetIntConst(2, StatePrimitive.U8);
            graph.SetStrictArrayInit(3, 0, 1, 2);
            graph.SetDef("y", 3);

            var result = graph.Solve();
            var generic = result.AssertAndGetSingleGeneric(StatePrimitive.U8, StatePrimitive.Real);
            result.AssertNode(generic, 0, 1);
            result.AssertNamedEqualToArrayOf(generic, "y");
            result.AssertAreGenerics(generic, "x");
        }
        [Test]
        public void GenericArrayInitWithTwoVariables()
        {
            //    2 0 1  
            // y = [a,b]
            var graph = new GraphBuilder();
            graph.SetVar("a", 0);
            graph.SetVar("b", 1);
            graph.SetStrictArrayInit(2, 0, 1);
            graph.SetDef("y", 2);

            var result = graph.Solve();
            var generic = result.AssertAndGetSingleGeneric(null, null);
            result.AssertNamedEqualToArrayOf(generic, "y");
            result.AssertAreGenerics(generic, "a","b");
        }
        [Test]
        public void GenericArrayInitWithTwoVariablesOneOfThemHasConcreteType()
        {
            //       2 0 1  
            //a:int; y = [a,b]
            var graph = new GraphBuilder();
            graph.SetVarType("a", StatePrimitive.I32);
            graph.SetVar("a", 0);
            graph.SetVar("b", 1);
            graph.SetStrictArrayInit(2, 0, 1);
            graph.SetDef("y", 2);

            var result = graph.Solve();
            result.AssertNoGenerics();
            result.AssertNamedEqualToArrayOf(StatePrimitive.I32, "y");
            result.AssertNamed(StatePrimitive.I32, "a", "b");
        }
        [Test]
        public void GenericArrayInitWithComplexVariables()
        {
            //    3 0  21  
            // y = [x,-x]
            var graph = new GraphBuilder();
            graph.SetVar("x", 0);
            graph.SetVar("x", 1);
            graph.SetNegateCall(1, 2);
            graph.SetStrictArrayInit(3, 0, 2);
            graph.SetDef("y", 3);

            var result = graph.Solve();
            var generic = result.AssertAndGetSingleGeneric(StatePrimitive.I16, StatePrimitive.Real);
            result.AssertNamedEqualToArrayOf(generic, "y");
            result.AssertAreGenerics(generic, "x");
        }
        [Test]
        public void GenericArrayInitWithTwoSameVariables()
        {
            //    2 0 1  
            // y = [x,x]
            var graph = new GraphBuilder();
            graph.SetVar("x", 0);
            graph.SetVar("x", 1);
            graph.SetStrictArrayInit(2, 0, 1);
            graph.SetDef("y", 2);

            var result = graph.Solve();
            var generic = result.AssertAndGetSingleGeneric(null, null);
            result.AssertNamedEqualToArrayOf(generic, "y");
            result.AssertAreGenerics(generic, "x");
        }


        [Test]
        public void ArrayInitWithConcreteConstant()
        {
            //    3 0 1 2 
            // y = [1.0,2,3]
            var graph = new GraphBuilder();
            graph.SetConst(0, StatePrimitive.Real);
            graph.SetIntConst(1, StatePrimitive.U8);
            graph.SetIntConst(2, StatePrimitive.U8);
            graph.SetStrictArrayInit(3, 0, 1, 2);
            graph.SetDef("y", 3);
        
            var result = graph.Solve();
            result.AssertNoGenerics();
            result.AssertNamedEqualToArrayOf(StatePrimitive.Real, "y");
        }

        [Test]
        public void TwoDimention_InitConcrete()
        {
            //     4 3 0 1 2 
            // y = [[1i,2i,3i]]
            var graph = new GraphBuilder();
            graph.SetConst(0, StatePrimitive.I32);
            graph.SetConst(1, StatePrimitive.I32);
            graph.SetConst(2, StatePrimitive.I32);
            graph.SetStrictArrayInit(3, 0, 1, 2);
            graph.SetStrictArrayInit(4,3);
            graph.SetDef("y", 4);
            var result = graph.Solve();
            result.AssertNoGenerics();
            result.AssertNamed(StateArray.Of(StateArray.Of(StatePrimitive.I32)), "y");
        }

        [Test]
        public void TwoDimention_InitConcrete_ConcreteDef()
        {
            //             4 3 0 1 2 
            // y:int[][] = [[1i,2i,3i]]
            var graph = new GraphBuilder();
            graph.SetConst(0, StatePrimitive.I32);
            graph.SetConst(1, StatePrimitive.I32);
            graph.SetConst(2, StatePrimitive.I32);
            graph.SetStrictArrayInit(3, 0, 1, 2);
            graph.SetStrictArrayInit(4, 3);
            graph.SetVarType("y", StateArray.Of(StateArray.Of(StatePrimitive.I32)));
            graph.SetDef("y", 4);
            var result = graph.Solve();
            result.AssertNoGenerics();
            result.AssertNamed(StateArray.Of(StateArray.Of(StatePrimitive.I32)), "y");
        }
    }
}
