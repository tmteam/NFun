using System;
using NFun.Tic.SolvingStates;
using NFun.TypeInferenceCalculator.Errors;
using NUnit.Framework;
using Array = NFun.Tic.SolvingStates.Array;

namespace NFun.Tic.Tests
{
    class CycleTests
    {
        [Test]
        public void OutEqualsToItself_SingleGenericFound()
        {
            //    0
            //y = y
            var graph = new GraphBuilder();
            graph.SetVar("y", 0);
            graph.SetDef("y", 0);
            var result = graph.Solve();
            var generic = result.AssertAndGetSingleGeneric(null, null, false);
            result.AssertAreGenerics(generic, "y");
        }

        [Test]
        public void OutEqualsToItself_TypeSpecified_EquationSolved()
        {
            //y:bool; y = y
            var graph = new GraphBuilder();
            graph.SetVarType("y", Primitive.Bool);
            graph.SetVar("y", 0);
            graph.SetDef("y", 0);
            var result = graph.Solve();
            result.AssertNoGenerics();
            result.AssertNamed(Primitive.Bool, "y");
        }

        [Test]
        public void OutEqualsToItself_TypeLimitedAfter_EquationSolved()
        {
            //y = y; y =1
            var graph = new GraphBuilder();
            graph.SetVar("y", 0);
            graph.SetDef("y", 0);
            graph.SetIntConst(1, Primitive.U8);
            graph.SetDef("y", 1);

            var result = graph.Solve();
            var generic = result.AssertAndGetSingleGeneric(Primitive.U8, Primitive.Real);
            result.AssertAreGenerics(generic, "y");
        }
        
        [Test]
        public void OutEqualsToItself_TypeLimitedBefore_EquationSolved()
        {
            //y = 1; y =y
            var graph = new GraphBuilder();
            graph.SetIntConst(0, Primitive.U8);
            graph.SetDef("y", 0);
            graph.SetVar("y", 1);
            graph.SetDef("y", 1);

            var result = graph.Solve();
            var generic = result.AssertAndGetSingleGeneric(Primitive.U8, Primitive.Real);
            result.AssertAreGenerics(generic, "y");
        }
        
        [Test]
        public void CircularDependencies_SingleGenericFound()
        {
            //    0      1      2
            //a = b; b = c; c = a
            var graph = new GraphBuilder();
            graph.SetVar("b", 0);
            graph.SetDef("a", 0);

            graph.SetVar("c", 1);
            graph.SetDef("b", 1);

            graph.SetVar("a", 2);
            graph.SetDef("c", 2);

            var result = graph.Solve();
            var generic = result.AssertAndGetSingleGeneric(null, null);
            result.AssertAreGenerics(generic, "a", "b", "c");
        }

        [Test]
        public void CircularDependencies_AllTypesSpecified_EquationSolved()
        {
            //    0      1      2
            //c:bool; a = b; b = c; c = a
            var graph = new GraphBuilder();
            graph.SetVarType("c", Primitive.Bool);
            graph.SetVar("b", 0);
            graph.SetDef("a", 0);

            graph.SetVar("c", 1);
            graph.SetDef("b", 1);

            graph.SetVar("a", 2);
            graph.SetDef("c", 2);

            var result = graph.Solve();
            result.AssertNoGenerics();
            result.AssertNamed(Primitive.Bool, "a", "b", "c");
        }

        [Test]
        public void Array_referencesItself()
        {
            //    1       0      
            //x = reverse(x)
            var graph = new GraphBuilder();
            graph.SetVar("x", 0);
            graph.SetReverse(0,1);
            graph.SetDef("x", 1);

            var result = graph.Solve();
            var res = result.AssertAndGetSingleGeneric(null,null);
            result.AssertNamed(Array.Of(res) , "x");
        }
        
        [Test]
        public void Array_referencesItselfManyTimes()
        {
            //    4      0    3   1 2       
            //x = concat(x,concat(x,x)
            var graph = new GraphBuilder();
            graph.SetVar("x", 0);
            graph.SetVar("x", 1);
            graph.SetVar("x", 2);
            graph.SetConcatCall(1,2,3);
            graph.SetConcatCall(0,3,4);
            graph.SetDef("x", 4);
            var result = graph.Solve();
            var res = result.AssertAndGetSingleGeneric(null, null);
            result.AssertNamed(Array.Of(res), "x");
        }

        [Test]
        public void Array_referencesItselfWithMap()
        {
            //    0  5  4    132         
            //x = x.map(f(a)=a+1)
            var graph = new GraphBuilder();
            graph.SetVar("x", 0);
            graph.SetVar("la", 1);
            graph.SetIntConst(2, Primitive.U8);
            graph.SetArith(1,2,3);
            graph.CreateLambda(3,4,"la");
            graph.SetMap(0,4,5);
            graph.SetDef("x", 5);
            var result = graph.Solve();
            var res = result.AssertAndGetSingleArithGeneric();
            result.AssertNamed(Array.Of(res), "x");
        }

        [Test]
        public void Array_referencesItselfWithMap_RetTypeIsConcrete()
        {
            //    0  5  4    132         
            //x = x.map(f(a):i64=a+1)
            var graph = new GraphBuilder();
            graph.SetVar("x", 0);
            graph.SetVar("la", 1);
            graph.SetIntConst(2, Primitive.U8);
            graph.SetArith(1, 2, 3);
            graph.CreateLambda(3, 4, Primitive.I64, "la");
            graph.SetMap(0, 4, 5);
            graph.SetDef("x", 5);
            var result = graph.Solve();
            result.AssertNoGenerics();
            result.AssertNamed(Array.Of(Primitive.I64), "x");
        }

        [Test]
        public void Array_referencesItselfWithMap_ArgTypeIsConcrete()
        {
            //    0  5  4    132         
            //x = x.map(f(a):i64=a+1)
            var graph = new GraphBuilder();
            graph.SetVar("x", 0);
            graph.SetVarType("la", Primitive.I64);
            graph.SetVar("la", 1);
            graph.SetIntConst(2, Primitive.U8);
            graph.SetArith(1, 2, 3);
            graph.CreateLambda(3, 4,  "la");
            graph.SetMap(0, 4, 5);
            graph.SetDef("x", 5);
            var result = graph.Solve();
            result.AssertNoGenerics();
            result.AssertNamed(Array.Of(Primitive.I64), "x");
        }
        [Test]
        public void Array_referencesItselfWithMap_DefTypeIsConcrete()
        {
            //          0  5  4    132         
            //x:i64[] = x.map(f(a)=a+1)
            var graph = new GraphBuilder();
            graph.SetVarType("x",Array.Of(Primitive.I64));
            graph.SetVar("x", 0);
            graph.SetVar("la", 1);
            graph.SetIntConst(2, Primitive.U8);
            graph.SetArith(1, 2, 3);
            graph.CreateLambda(3, 4, "la");
            graph.SetMap(0, 4, 5);
            graph.SetDef("x", 5);
            var result = graph.Solve();
            result.AssertNoGenerics();
            result.AssertNamed(Array.Of(Primitive.I64), "x");
        }

        [Test]
        public void Array_referencesItselfWithMap_DefTypeIsImpossible_Throws()
        {
            //          0  5  4    132         
            //x:int = x.map(f(a)=a+1)
            var graph = new GraphBuilder();
            graph.SetVarType("x", Primitive.I64);
            graph.SetVar("x", 0);
            graph.SetVar("la", 1);
            graph.SetIntConst(2, Primitive.U8);
            graph.SetArith(1, 2, 3);
            graph.CreateLambda(3, 4, "la");
            TestHelper.AssertThrowsTicError(() =>
            {
                graph.SetMap(0, 4, 5);
                graph.SetDef("x", 5);
                graph.Solve();
                Assert.Fail("Impossible equation solved");
            });
        }

        [Test]
        public void Array_referencesItselfWithMap_DefTypeIsImpossible2_Throws()
        {
            //          0  5  4    132         
            //x:bool[] = x.map(f(a)=a+1)
            var graph = new GraphBuilder();
            graph.SetVarType("x", Array.Of(Primitive.Bool));
            graph.SetVar("x", 0);
            graph.SetVar("la", 1);
            graph.SetIntConst(2, Primitive.U8);
            graph.SetArith(1, 2, 3);
            graph.CreateLambda(3, 4, "la");
            TestHelper.AssertThrowsTicError(() =>
            {
                graph.SetMap(0, 4, 5);
                graph.SetDef("x", 5);
                graph.Solve();
                Assert.Fail("Impossible equation solved");
            });
        }
        [Test]
        public void ArrayCycle_Solved()
        {
            //    4  2  0 1    3       
            //x = [ get(x,0) , 1]
            var graph = new GraphBuilder();
            graph.SetVar("x", 0);
            graph.SetIntConst(1, Primitive.U8);
            graph.SetArrGetCall(0,1,2);
            graph.SetIntConst(3, Primitive.U8);
            graph.SetStrictArrayInit(4,2, 3);
            graph.SetDef("x", 4);
            var res = graph.Solve();
            
            var t = res.AssertAndGetSingleGeneric(Primitive.U8, Primitive.Real);
            res.AssertNamed(Array.Of(t),"x");
        }
        [Test]
        public void Array_ReqursiveDefenition_throws()
        {
            //    1 0              
            //x = [ x]
            var graph = new GraphBuilder();
            graph.SetVar("x", 0);
            graph.SetStrictArrayInit(1,0);
            TestHelper.AssertThrowsTicError(() =>
            {
                graph.SetDef("x", 1);
                graph.Solve();
                Assert.Fail("Impossible equation solved");
            });
        }

        [Test]
        public void Array_TwinReqursiveDefenition_throws()
        {
            //    2 0 1              
            //x = [ x,x]
            var graph = new GraphBuilder();
            graph.SetVar("x", 0);
            graph.SetVar("x", 1);
            graph.SetStrictArrayInit(2, 0,1);
            TestHelper.AssertThrowsTicError(() =>
            {
                graph.SetDef("x", 2);
                graph.Solve();
                Assert.Fail("Impossible equation solved");
            });
        }
       [Test]
       public void Array_ComplexReqursiveDefenition_throws()
       {
           //    2 1 0             
           //x = [ [ x]]
           var graph = new GraphBuilder();
           graph.SetVar("x", 0);
           graph.SetStrictArrayInit(1, 0);
           graph.SetStrictArrayInit(2, 1);
           TestHelper.AssertThrowsTicError(() =>
           {
               graph.SetDef("x", 2);
               graph.Solve();
               Assert.Fail("Impossible equation solved");
           });
       }
        [Test]
        public void Array_ComplexReqursiveDefenition2_throws()
        {
            //    4 1 0  3 2           
            //x = [ [ a],[ x]]
            var graph = new GraphBuilder();
            graph.SetVar("a",0);
            graph.SetStrictArrayInit(1, 0);
            graph.SetVar("x", 2);
            graph.SetStrictArrayInit(3, 2);
            graph.SetStrictArrayInit(4, 1,3);
            TestHelper.AssertThrowsTicError(() =>
            {
                graph.SetDef("x", 4);
                graph.Solve();
                Assert.Fail("Impossible equation solved");
            });
        }
    }
}
