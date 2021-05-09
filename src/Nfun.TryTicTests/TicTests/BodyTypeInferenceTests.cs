using System.Linq;
using NFun.Tic.SolvingStates;
using NUnit.Framework;

namespace NFun.UnitTests.TicTests
{
    public class BodyTypeInferenceTests
    {
        [Test]
        public void SetGenericConstant()
        {
            var result =  TestHelper.Solve("x = 10");
            var t = result.AssertAndGetSingleGeneric(StatePrimitive.U8, StatePrimitive.Real);
            result.AssertAreGenerics(t, "x");
        }
        [Test]
        public void SetConstants()
        {
            var result = TestHelper.Solve("x = 0x10");
            var t = result.AssertAndGetSingleGeneric(StatePrimitive.U8, StatePrimitive.I96);
            result.AssertAreGenerics(t, "x");

        }
        [Test]
        public void SimpleDivideComputation()
        {
            var result = TestHelper.Solve("x = 3 / 2");
            result.AssertNoGenerics();
            result.AssertNamed(StatePrimitive.Real, "x");
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
            result.AssertNamed(StatePrimitive.I32, "y","x");
        }
        [Test]
        public void IncrementI64()
        {
            var result = TestHelper.Solve("y = x + 0xFFFF_FFFF_FFFF_FFFF");
            result.AssertNoGenerics();
            result.AssertNamed(StatePrimitive.U64, "x","y");
        }

        [Test]
        public void IncrementU64WithStrictInputType()
        {
            var result = TestHelper.Solve("x:uint64; y = x + 1");

            result.AssertNoGenerics();
            result.AssertNamed(StatePrimitive.U64, "x","y");
        }
        [TestCase]
        public void IncrementU32WithStrictOutputType()
        {
            var result = TestHelper.Solve("y:uint = x + 1");
            result.AssertNoGenerics();
            result.AssertNamed(StatePrimitive.U32, "x","y");
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
            result.AssertNamed(StatePrimitive.I32, "x","y","a");
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
        [Ignore("UB")]
        public void UpcastArgType_ArithmOp_EquationSolved()
        { 
            var result = TestHelper.Solve("a = 1.0; y = a + b;  b = 0x1");
            result.AssertNoGenerics();
            result.AssertNamed(StatePrimitive.Real, "a");
            result.AssertNamed(StatePrimitive.Real, "y");
            result.AssertNamed(StatePrimitive.I32, "b");
        }

        [Test]
        public void SolvingGenericCaseWithIfs()
        {
            var result = TestHelper.Solve("y = if (a) x; else (z + 1);");
            var arithGeneric = result.AssertAndGetSingleArithGeneric();
            result.AssertNamed(StatePrimitive.Bool, "a");
            result.AssertAreGenerics(arithGeneric,  "y","x","z");
        }

        [Test(Description = "y = if |a: x| else: (z + 1.0)")]
        public void SolvingConcreteCaseWithIfs()
        {
            var result = TestHelper.Solve("y = if (a) x; else (z + 1.0);");
            result.AssertNoGenerics();
            result.AssertNamed(StatePrimitive.Bool, "a");
            result.AssertNamed(StatePrimitive.Real, "y", "x", "z");
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
            var result = TestHelper.Solve("y = a+b");
            var generic = result.AssertAndGetSingleArithGeneric();
            result.AssertAreGenerics(generic, "y", "a", "b");
        }
        [Test]
        public void GenericFunctionCallWithConcreteArg()
        {
            var result = TestHelper.Solve("y:int  = a+b");
            result.AssertNoGenerics();
            result.AssertNamed(StatePrimitive.I32 , "y", "a", "b");
        }

        [Test]
        public void GenericArrInit()
        {
            var result = TestHelper.Solve("y  = [1..4]");
            var generic = result.AssertAndGetSingleGeneric(StatePrimitive.U8, StatePrimitive.Real);
            result.AssertNamed(StateArray.Of(generic), "y");
        }
        [Test]
        public void GenericArray()
        {
            var result = TestHelper.Solve("y = [1,2,3,4].any() # true");
            result.AssertNoGenerics();
            result.AssertNamed(StatePrimitive.Bool, "y");
        }
        [Test]
        public void MapWithLambda()
        {
            var result = TestHelper.Solve("y  = a.map(i:int->i+1)");
            result.AssertNoGenerics();
            result.AssertNamed(StateArray.Of(StatePrimitive.I32), "y");
            result.AssertNamed(StateArray.Of(StatePrimitive.I32), "a");
        }
        [Test]
        public void IfWithEmptyArray()
        {
            var result = TestHelper.Solve("y  =if (true) [1.0,2,3] else []");
            result.AssertNoGenerics();
            result.AssertNamed(StateArray.Of(StatePrimitive.Real), "y");
        }

        [Test]
        public void CompareArrays()
        {
            //y = [1.0] ==[]
            var result = TestHelper.Solve("y = [1.0] ==[]");
            result.AssertNoGenerics();
            result.AssertNamed(StatePrimitive.Bool, "y");
        }

        [Test]
        public void CompareArrays_GenericTypeSolvedWell()
        {
            //y = [1.0] ==[]
            var result = TestHelper.SolveAndGetResults("y = [1.0] ==[]");
            var equalGenericType = result.GenericFunctionTypes.Where(g => g != null).Single();
            Assert.AreEqual(equalGenericType.Length,1);
            var state = equalGenericType[0];
            TestHelper.AssertAreSame(StateArray.Of(StatePrimitive.Real), state);
        }
        [Test]
        public void CompareArraysReversed_GenericTypeSolvedWell()
        {
            //y = []==[1.0]
            var result = TestHelper.SolveAndGetResults("y = [1.0] ==[]");
            var equalGenericType = result.GenericFunctionTypes.Where(g => g != null).Single();
            Assert.AreEqual(equalGenericType.Length, 1);
            var state = equalGenericType[0];
            TestHelper.AssertAreSame(StateArray.Of(StatePrimitive.Real), state);
        }

        [Test]
        public void CompareBools_GenericTypeSolvedWell()
        {
            var result = TestHelper.SolveAndGetResults("y = true == false");
            var equalGenericType = result.GenericFunctionTypes.Where(g => g != null).Single();
            Assert.AreEqual(equalGenericType.Length, 1);
            var state = equalGenericType[0];
            TestHelper.AssertAreSame(StatePrimitive.Bool, state);
        }
        [Test]
        public void CompareSameArrays_GenericTypeSolvedWell()
        {
            var result = TestHelper.SolveAndGetResults("y = [1.0] == [1.0]");
            var equalGenericType = result.GenericFunctionTypes.Where(g => g != null).Single();
            Assert.AreEqual(equalGenericType.Length, 1);
            var state = equalGenericType[0];
            TestHelper.AssertAreSame(StateArray.Of(StatePrimitive.Real), state);
        }
        [Test]
        public void Count()
        {
            var result = TestHelper.Solve("y = 'a'.count()");
            result.AssertNoGenerics();
            result.AssertNamed(StatePrimitive.I32, "y");
        }
        [Test]
        public void Sort()
        {
            var result = TestHelper.Solve("y = [4,3,5,1].sort()");
            var g = result.AssertAndGetSingleGeneric(StatePrimitive.U8, StatePrimitive.Real, true);
            result.AssertNamed(StateArray.Of(g), "y");
        }
        [Test]
        public void SortConcrete()
        {
            var result = TestHelper.Solve("y:int[] = [4,3,5,1].sort()");
            result.AssertNoGenerics();
            result.AssertNamed(StateArray.Of(StatePrimitive.I32), "y");
        }

        [Test]
        public void InitArrayWithVar()
        {
            var result = TestHelper.Solve("y = [x,2,3]");
            var generic = result.AssertAndGetSingleGeneric(StatePrimitive.U8, StatePrimitive.Real);

            result.AssertNamed(StateArray.Of(generic), "y");
            result.AssertAreGenerics(generic, "x");
        }
        [Test]
        public void FilterComparable()
        {
            var result = TestHelper.Solve("y:int[]= [1,2,3].filter(x->x>2)");
            result.AssertNoGenerics();
            result.AssertNamed(StateArray.Of(StatePrimitive.I32),"y");
        }
        [Test]
        public void HiOrderMap()
        {
            var result = TestHelper.Solve("y = '12'.map(toText)");
            result.AssertNoGenerics();
            result.AssertNamed(StateArray.Of(StateArray.Of(StatePrimitive.Char)), "y");
        }
        

        [Test]
        public void ConcatAndSplit()
        {
            var result = TestHelper.Solve("y = split(concat('a b ','c'),' ')");
            result.AssertNoGenerics();
            result.AssertNamed(StateArray.Of(StateArray.Of(StatePrimitive.Char)), "y");
        }
        [Test]
        public void Split()
        {
            var result = TestHelper.Solve("y = split('a b c',' ')");
            result.AssertNoGenerics();
            result.AssertNamed(StateArray.Of(StateArray.Of(StatePrimitive.Char)), "y");
        }
        [Test]
        public void ConcatSolveGenerics()
        {
            var result = TestHelper.Solve("y = x.concat(x)");
            var generic = result.AssertAndGetSingleGeneric(null, null);

            result.AssertNamed(StateArray.Of(generic), "y","x");
        }

        [Test]
        public void ReqursiveTypeDefinitionThrows()
        {
            Assert.Throws<NFun.Tic.Errors.RecursiveTypeDefinitionException>(
                ()=>TestHelper.Solve("y = t.concat(t[0])"));
        }
    }
}
