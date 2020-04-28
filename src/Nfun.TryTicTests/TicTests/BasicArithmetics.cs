using System;
using NFun.Interpritation;
using NFun.SyntaxParsing.Visitors;
using NFun.Tic;
using NFun.Tic.SolvingStates;
using NFun.TypeInferenceAdapter;
using NUnit.Framework;

namespace Nfun.TryTicTests.TicTests
{
    class BasicArithmetics
    {
        public FinalizationResults Solve(string equation)
        {
            Console.WriteLine(equation);
            var flow = NFun.Tokenization.Tokenizer.ToFlow(equation);
            var tree = NFun.SyntaxParsing.Parser.Parse(flow);
            tree.ComeOver(new SetNodeNumberVisitor(0));

            var graph = new GraphBuilder();
            var state = new SetupTiState(graph);
            var enterVisitor = new SetupTiEnterVisitor(new SetupTiState(graph));
            var exitVisitor = new SetupTiExitVisitor(state, new FunctionsDictionary());
            tree.ComeOver(enterVisitor, exitVisitor);
            return graph.Solve();
        }
        [Test]
        public void SetGenericConstant()
        {
            var result = Solve("x = 10");
            var t = result.AssertAndGetSingleGeneric(Primitive.U8, Primitive.Real);
            result.AssertAreGenerics(t, "x");
        }
        [Test]
        public void SetConstants()
        {
            var result = Solve("x = 0x10");
            result.AssertNoGenerics();
            result.AssertNamed(Primitive.I32, "x");
        }
        [Test]
        public void SimpleDivideComputation()
        {
            var result = Solve("x = 3 / 2");
            result.AssertNoGenerics();
            result.AssertNamed(Primitive.Real, "x");
        }

        [Test]
        public void SolvingGenericWithSingleVar()
        {
            var result = Solve("y = 1 + 2 * x");
            var generic = result.AssertAndGetSingleArithGeneric();
            result.AssertAreGenerics(generic, "x","y");
        }

       
        [Test]
        public void ConcreteVarType2()
        {
            var result = Solve("x:int; y = x + 1;");
            result.AssertNoGenerics();
            result.AssertNamed(Primitive.I32, "y","x");
        }
        [Test]
        public void IncrementI64()
        {
            var result = Solve("y = x + 0xffff_ffff");
            result.AssertNoGenerics();
            result.AssertNamed(Primitive.I64, "x","y");
        }

        [Test]
        public void IncrementU64WithStrictInputType()
        {
            var result = Solve("x:uint64; y = x + 1");

            result.AssertNoGenerics();
            result.AssertNamed(Primitive.U64, "x","y");
        }
        [TestCase]
        public void IncrementU32WithStrictOutputType()
        {
            var result = Solve("y:uint = x + 1");
            result.AssertNoGenerics();
            result.AssertNamed(Primitive.U32, "x","y");
        }

        [TestCase]
        public void GenericIncrement()
        {
            var result = Solve("y = x + 1");
            var genericNode = result.AssertAndGetSingleArithGeneric();
            result.AssertAreGenerics(genericNode, "x", "y");
        }

        
        [Test]
        public void StrictOnEquationArithmetics()
        {
            var result = Solve("x:int= 10;   a = x*y + 10-x");

            result.AssertNoGenerics();
            result.AssertNamed(Primitive.I32, "x","y","a");
        }

        [Test]
        public void GenericOneEquatopmArithmetics()
        {
            var result = Solve("x= 10;   a = x*y + 10-x");

            var genericNode = result.AssertAndGetSingleArithGeneric();
            result.AssertAreGenerics(genericNode,"x","y","a");
        }

        
        [Test]
        public void GenericTwoEquationsArithmetic()
        {
            var result = Solve("a = x*y + 10-x; b = r*x + 10-r");
            var genericNode = result.AssertAndGetSingleArithGeneric();
            result.AssertAreGenerics(genericNode, "x","y","a","b");
        }

        [Test]
        public void InputRepeats_simpleGeneric()
        {
            var result = Solve("y = x + x"); 

            var generic = result.AssertAndGetSingleArithGeneric();
            result.AssertAreGenerics(generic, "x","y");
        }

       
        [Test]
        public void UpcastArgType_ArithmOp_EquationSolved()
        { 
            var result = Solve("a = 1.0; y = a + b;  b = 0x1");
            result.AssertNoGenerics();
            result.AssertNamed(Primitive.Real, "a");
            result.AssertNamed(Primitive.Real, "y");
            result.AssertNamed(Primitive.I32, "b");
        }
    }
}
