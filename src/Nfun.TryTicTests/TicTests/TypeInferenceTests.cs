using System;
using NFun;
using NFun.Interpritation;
using NFun.SyntaxParsing.Visitors;
using NFun.Tic;
using NFun.Tic.SolvingStates;
using NFun.TypeInferenceAdapter;
using NUnit.Framework;
using Array = NFun.Tic.SolvingStates.Array;

namespace Nfun.TryTicTests.TicTests
{
    public class TypeInferenceTests
    {
      
        [Test]
        public void SetGenericConstant()
        {
            var result =  TestHelper.Solve("x = 10");
            var t = result.AssertAndGetSingleGeneric(Primitive.U8, Primitive.Real);
            result.AssertAreGenerics(t, "x");
        }
        [Test]
        public void SetConstants()
        {
            var result = TestHelper.Solve("x = 0x10");
            result.AssertNoGenerics();
            result.AssertNamed(Primitive.I32, "x");
        }
        [Test]
        public void SimpleDivideComputation()
        {
            var result = TestHelper.Solve("x = 3 / 2");
            result.AssertNoGenerics();
            result.AssertNamed(Primitive.Real, "x");
        }

        [Test]
        public void SolvingGenericWithSingleVar()
        {
            var result = TestHelper.Solve("y = 1 + 2 * x");
            var generic = result.AssertAndGetSingleArithGeneric();
            result.AssertAreGenerics(generic, "x","y");
        }

       
        [Test]
        public void ConcreteVarType2()
        {
            var result = TestHelper.Solve("x:int; y = x + 1;");
            result.AssertNoGenerics();
            result.AssertNamed(Primitive.I32, "y","x");
        }
        [Test]
        public void IncrementI64()
        {
            var result = TestHelper.Solve("y = x + 0xffff_ffff");
            result.AssertNoGenerics();
            result.AssertNamed(Primitive.I64, "x","y");
        }

        [Test]
        public void IncrementU64WithStrictInputType()
        {
            var result = TestHelper.Solve("x:uint64; y = x + 1");

            result.AssertNoGenerics();
            result.AssertNamed(Primitive.U64, "x","y");
        }
        [TestCase]
        public void IncrementU32WithStrictOutputType()
        {
            var result = TestHelper.Solve("y:uint = x + 1");
            result.AssertNoGenerics();
            result.AssertNamed(Primitive.U32, "x","y");
        }

        [TestCase]
        public void GenericIncrement()
        {
            var result = TestHelper.Solve("y = x + 1");
            var genericNode = result.AssertAndGetSingleArithGeneric();
            result.AssertAreGenerics(genericNode, "x", "y");
        }

        
        [Test]
        public void StrictOnEquationArithmetics()
        {
            var result = TestHelper.Solve("x:int= 10;   a = x*y + 10-x");

            result.AssertNoGenerics();
            result.AssertNamed(Primitive.I32, "x","y","a");
        }

        [Test]
        public void GenericOneEquatopmArithmetics()
        {
            var result = TestHelper.Solve("x= 10;   a = x*y + 10-x");

            var genericNode = result.AssertAndGetSingleArithGeneric();
            result.AssertAreGenerics(genericNode,"x","y","a");
        }

        
        [Test]
        public void GenericTwoEquationsArithmetic()
        {
            var result = TestHelper.Solve("a = x*y + 10-x; b = r*x + 10-r");
            var genericNode = result.AssertAndGetSingleArithGeneric();
            result.AssertAreGenerics(genericNode, "x","y","a","b");
        }

        [Test]
        public void InputRepeats_simpleGeneric()
        {
            var result = TestHelper.Solve("y = x + x"); 

            var generic = result.AssertAndGetSingleArithGeneric();
            result.AssertAreGenerics(generic, "x","y");
        }

       
        [Test]
        public void UpcastArgType_ArithmOp_EquationSolved()
        { 
            var result = TestHelper.Solve("a = 1.0; y = a + b;  b = 0x1");
            result.AssertNoGenerics();
            result.AssertNamed(Primitive.Real, "a");
            result.AssertNamed(Primitive.Real, "y");
            result.AssertNamed(Primitive.I32, "b");
        }

        [Test]
        public void SolvingGenericCaseWithIfs()
        {
            var result = TestHelper.Solve("y = if (a) x; else (z + 1);");
            var arithGeneric = result.AssertAndGetSingleArithGeneric();
            result.AssertNamed(Primitive.Bool, "a");
            result.AssertAreGenerics(arithGeneric,  "y","x","z");
        }

        [Test]
        public void SolvingConcreteCaseWithIfs()
        {
            var result = TestHelper.Solve("y = if (a) x; else (z + 1.0);");
            result.AssertNoGenerics();
            result.AssertNamed(Primitive.Bool, "a");
            result.AssertNamed(Primitive.Real, "y", "x", "z");
        }

        [Test]
        public void If_withMultipleAncestorRules_EquationSolved()
        {
            //      3     0   1      2       4        5 7 6
            //y1  = if (true) 1 else x; y2 = y1; y3 = y1 * 2
            var result = TestHelper.Solve("y1  = if (true) 1 else x; y2 = y1; y3 = y1 * 2");
            var generic = result.AssertAndGetSingleArithGeneric();
            result.AssertAreGenerics(generic, "y1", "y2", "y3");
        }


        [Test]
        public void GenericFunctionCall()
        {
            var result = TestHelper.Solve("y  = myAdd(a,b)");
            var generic = result.AssertAndGetSingleArithGeneric();
            result.AssertAreGenerics(generic, "y", "a", "b");
        }
        [Test]
        public void GenericFunctionCallWithConcreteArg()
        {
            var result = TestHelper.Solve("y:int  = myAdd(a,b)");
            result.AssertNoGenerics();
            result.AssertNamed(Primitive.I32 , "y", "a", "b");
        }

        [Test]
        public void MapWithLambda()
        {
            //var result = TestHelper.Solve("y  = a.map(f(i)=i+1)");
            //var result = TestHelper.Solve("y  = a.map{it+1}");

            var result = TestHelper.Solve("y  = a.map(i:int->i+1)");
            result.AssertNoGenerics();
            result.AssertNamed(Array.Of(Primitive.I32), "y");
            result.AssertNamed(Array.Of(Primitive.I32), "a");
        }
    }
}
