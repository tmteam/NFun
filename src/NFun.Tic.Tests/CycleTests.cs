using NFun.Tic.SolvingStates;
using NFun.TypeInferenceCalculator;
using NUnit.Framework;

namespace NFun.Tic.Tests
{
    class CycleTests
    {
        [SetUp] public void Initiazlize() => TraceLog.IsEnabled = true;
        [TearDown] public void Finalize() => TraceLog.IsEnabled = false;

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
            graph.SetVarType("y", StatePrimitive.Bool);
            graph.SetVar("y", 0);
            graph.SetDef("y", 0);
            var result = graph.Solve();
            result.AssertNoGenerics();
            result.AssertNamed(StatePrimitive.Bool, "y");
        }

        [Test]
        public void OutEqualsToItself_TypeLimitedAfter_EquationSolved()
        {
            //y = y; y =1
            var graph = new GraphBuilder();
            graph.SetVar("y", 0);
            graph.SetDef("y", 0);
            graph.SetIntConst(1, StatePrimitive.U8);
            graph.SetDef("y", 1);

            var result = graph.Solve();
            var generic = result.AssertAndGetSingleGeneric(StatePrimitive.U8, StatePrimitive.Real);
            result.AssertAreGenerics(generic, "y");
        }
        
        [Test]
        public void OutEqualsToItself_TypeLimitedBefore_EquationSolved()
        {
            //y = 1; y =y
            var graph = new GraphBuilder();
            graph.SetIntConst(0, StatePrimitive.U8);
            graph.SetDef("y", 0);
            graph.SetVar("y", 1);
            graph.SetDef("y", 1);

            var result = graph.Solve();
            var generic = result.AssertAndGetSingleGeneric(StatePrimitive.U8, StatePrimitive.Real);
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
            graph.SetVarType("c", StatePrimitive.Bool);
            graph.SetVar("b", 0);
            graph.SetDef("a", 0);

            graph.SetVar("c", 1);
            graph.SetDef("b", 1);

            graph.SetVar("a", 2);
            graph.SetDef("c", 2);

            var result = graph.Solve();
            result.AssertNoGenerics();
            result.AssertNamed(StatePrimitive.Bool, "a", "b", "c");
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
            result.AssertNamed(StateArray.Of(res) , "x");
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
            result.AssertNamed(StateArray.Of(res), "x");
        }

        [Test]
        public void Array_referencesItselfWithMap()
        {
            //    0  5  4    132         
            //x = x.map(f(a)=a+1)
            var graph = new GraphBuilder();
            graph.SetVar("x", 0);
            graph.SetVar("la", 1);
            graph.SetIntConst(2, StatePrimitive.U8);
            graph.SetArith(1,2,3);
            graph.CreateLambda(3,4,"la");
            graph.SetMap(0,4,5);
            graph.SetDef("x", 5);
            var result = graph.Solve();
            var res = result.AssertAndGetSingleArithGeneric();
            result.AssertNamed(StateArray.Of(res), "x");
        }

        [Test]
        public void Array_referencesItselfWithMap_RetTypeIsConcrete()
        {
            //    0  5  4    132         
            //x = x.map(f(a):i64=a+1)
            var graph = new GraphBuilder();
            graph.SetVar("x", 0);
            graph.SetVar("la", 1);
            graph.SetIntConst(2, StatePrimitive.U8);
            graph.SetArith(1, 2, 3);
            graph.CreateLambda(3, 4, StatePrimitive.I64, "la");
            graph.SetMap(0, 4, 5);
            graph.SetDef("x", 5);
            var result = graph.Solve();
            result.AssertNoGenerics();
            result.AssertNamed(StateArray.Of(StatePrimitive.I64), "x");
        }

        [Test]
        public void Array_referencesItselfWithMap_ArgTypeIsConcrete()
        {
            //    0  5  4    132         
            //x = x.map(f(a):i64=a+1)
            var graph = new GraphBuilder();
            graph.SetVar("x", 0);
            graph.SetVarType("la", StatePrimitive.I64);
            graph.SetVar("la", 1);
            graph.SetIntConst(2, StatePrimitive.U8);
            graph.SetArith(1, 2, 3);
            graph.CreateLambda(3, 4,  "la");
            graph.SetMap(0, 4, 5);
            graph.SetDef("x", 5);
            var result = graph.Solve();
            result.AssertNoGenerics();
            result.AssertNamed(StateArray.Of(StatePrimitive.I64), "x");
        }
        [Test]
        public void Array_referencesItselfWithMap_DefTypeIsConcrete()
        {
            //          0  5  4    132         
            //x:i64[] = x.map(f(a)=a+1)
            var graph = new GraphBuilder();
            graph.SetVarType("x",StateArray.Of(StatePrimitive.I64));
            graph.SetVar("x", 0);
            graph.SetVar("la", 1);
            graph.SetIntConst(2, StatePrimitive.U8);
            graph.SetArith(1, 2, 3);
            graph.CreateLambda(3, 4, "la");
            graph.SetMap(0, 4, 5);
            graph.SetDef("x", 5);
            var result = graph.Solve();
            result.AssertNoGenerics();
            result.AssertNamed(StateArray.Of(StatePrimitive.I64), "x");
        }

        [Test]
        public void Array_referencesItselfWithMap_DefTypeIsImpossible_Throws()
        {
            //          0  5  4    132         
            //x:int = x.map(f(a)=a+1)
            var graph = new GraphBuilder();
            graph.SetVarType("x", StatePrimitive.I64);
            graph.SetVar("x", 0);
            graph.SetVar("la", 1);
            graph.SetIntConst(2, StatePrimitive.U8);
            graph.SetArith(1, 2, 3);
            graph.CreateLambda(3, 4, "la");
            TestHelper.AssertThrowsTicError(() =>
            {
                graph.SetMap(0, 4, 5);
                graph.SetDef("x", 5);
                graph.Solve();
            });
        }

        [Test]
        public void Array_referencesItselfWithMap_DefTypeIsImpossible2_Throws()
        {
            //          0  5  4    132         
            //x:bool[] = x.map(f(a)=a+1)
            var graph = new GraphBuilder();
            graph.SetVarType("x", StateArray.Of(StatePrimitive.Bool));
            graph.SetVar("x", 0);
            graph.SetVar("la", 1);
            graph.SetIntConst(2, StatePrimitive.U8);
            graph.SetArith(1, 2, 3);
            graph.CreateLambda(3, 4, "la");
            TestHelper.AssertThrowsTicError(() =>
            {
                graph.SetMap(0, 4, 5);
                graph.SetDef("x", 5);
                graph.Solve();
            });
        }
        [Test]
        public void ArrayCycle_Solved()
        {
            //    4  2  0 1    3       
            //x = [ get(x,0) , 1]
            var graph = new GraphBuilder();
            graph.SetVar("x", 0);
            graph.SetIntConst(1, StatePrimitive.U8);
            graph.SetArrGetCall(0,1,2);
            graph.SetIntConst(3, StatePrimitive.U8);
            graph.SetStrictArrayInit(4,2, 3);
            graph.SetDef("x", 4);
            var res = graph.Solve();
            
            var t = res.AssertAndGetSingleGeneric(StatePrimitive.U8, StatePrimitive.Real);
            res.AssertNamed(StateArray.Of(t),"x");
        }
        [Test]
        public void Array_ReqursiveDefinition_throws()
        {
            //    1 0              
            //x = [ x]
            var graph = new GraphBuilder();
            graph.SetVar("x", 0);
            graph.SetStrictArrayInit(1,0);
            TestHelper.AssertThrowsRecursiveTicTypedDefinition(() =>
            {
                graph.SetDef("x", 1);
                graph.Solve();
                Assert.Fail("Impossible equation solved");
            });
        }

        [Test]
        public void Array_TwinReqursiveDefinition_throws()
        {
            //    2 0 1              
            //x = [ x,x]
            var graph = new GraphBuilder();
            graph.SetVar("x", 0);
            graph.SetVar("x", 1);
            graph.SetStrictArrayInit(2, 0,1);
            TestHelper.AssertThrowsRecursiveTicTypedDefinition(() =>
            {
                graph.SetDef("x", 2);
                graph.Solve();
                Assert.Fail("Impossible equation solved");
            });
        }
       [Test]
       public void Array_ComplexReqursiveDefinition_throws()
       {
           //    2 1 0             
           //x = [ [ x]]
           var graph = new GraphBuilder();
           graph.SetVar("x", 0);
           graph.SetStrictArrayInit(1, 0);
           graph.SetStrictArrayInit(2, 1);
           TestHelper.AssertThrowsRecursiveTicTypedDefinition(() =>
           {
               graph.SetDef("x", 2);
               graph.Solve();
               Assert.Fail("Impossible equation solved");
           });
       }
        [Test]
        public void Array_ComplexReqursiveDefinition2_throws()
        {
            //    4 1 0  3 2           
            //x = [ [ a],[ x]]
            var graph = new GraphBuilder();
            graph.SetVar("a",0);
            graph.SetStrictArrayInit(1, 0);
            graph.SetVar("x", 2);
            graph.SetStrictArrayInit(3, 2);
            graph.SetStrictArrayInit(4, 1,3);
            TestHelper.AssertThrowsRecursiveTicTypedDefinition(() =>
            {
                graph.SetDef("x", 4);
                graph.Solve();
                Assert.Fail("Impossible equation solved");
            });
        }
        [Test]
        public void Array_recursiveConcatDefinition_throws()
        {
            //     4     0   3  1 2
            //y = concat(t, get(t,0i))

            var graph = new GraphBuilder();
            graph.SetVar("t", 0);
            graph.SetVar("t", 1);
            graph.SetConst(2, StatePrimitive.I32);
            graph.SetArrGetCall(1,2,3);
            TestHelper.AssertThrowsRecursiveTicTypedDefinition(() =>
            {
                graph.SetConcatCall(0, 3, 4);
                graph.SetDef("y", 4);
                graph.Solve();
            });
        }
    }
}
