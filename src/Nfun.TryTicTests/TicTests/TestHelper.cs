using System;
using System.Linq;
using NFun;
using NFun.BuiltInFunctions;
using NFun.Interpritation;
using NFun.SyntaxParsing.Visitors;
using NFun.Tic;
using NFun.Tic.SolvingStates;
using NFun.TypeInferenceAdapter;
using NUnit.Framework;
using Array = NFun.Tic.SolvingStates.Array;

namespace Nfun.TryTicTests.TicTests
{
    public static class TestHelper
    {
        public static FinalizationResults Solve(string equation)
        {
            Console.WriteLine(equation);

            var flow = NFun.Tokenization.Tokenizer.ToFlow(equation);
            var tree = NFun.SyntaxParsing.Parser.Parse(flow);
            tree.ComeOver(new SetNodeNumberVisitor(0));


            var graph = new GraphBuilder();
            var state = new SetupTiState(graph);
            var enterVisitor = new SetupTiEnterVisitor(new SetupTiState(graph));

            var functions = new FunDictionaryNew();
            foreach (var predefinedFunction in BaseFunctions.ConcreteFunctions)
                functions.Add(predefinedFunction);

            functions.Add(new AddFunction("myAdd"));
            functions.Add(new MapFunction());


            var exitVisitor = new SetupTiExitVisitor(state, functions);
            tree.ComeOver(enterVisitor, exitVisitor);
            return graph.Solve();
        }
        public static void AssertAreGenerics(this FinalizationResults result, SolvingNode targetGenericNode,
            params string[] varNames)
        {
            foreach (var varName in varNames)
            {
                Assert.AreEqual(targetGenericNode, result.GetVariableNode(varName).GetNonReference());
            }
        }

        public static SolvingNode AssertAndGetSingleArithGeneric(this FinalizationResults result)
            => AssertAndGetSingleGeneric(result, Primitive.U24, Primitive.Real, false);

        public static SolvingNode AssertAndGetSingleGeneric(this FinalizationResults result, Primitive desc,
            Primitive anc, bool isComparable = false)
        {
            Assert.AreEqual(1, result.GenericsCount,"Incorrect generics count");
            var genericNode = result.Generics.Single();

            AssertGenericType(genericNode, desc, anc, isComparable);
            return genericNode;
        }

        public static void AssertGenericTypeIsArith(this SolvingNode node)
        {
            AssertGenericType(node, Primitive.U24, Primitive.Real, false);
        }

        public static void AssertGenericType(this SolvingNode node, Primitive desc, Primitive anc,
            bool isComparable = false)
        {
            var generic = node.State as Constrains;
            Assert.IsNotNull(generic);
            if (desc == null)
                Assert.IsFalse(generic.HasDescendant);
            else
                Assert.AreEqual(desc, generic.Descedant);

            if (anc == null)
                Assert.IsFalse(generic.HasAncestor);
            else
                Assert.AreEqual(anc, generic.Ancestor);

            Assert.AreEqual(isComparable, generic.IsComparable,"IsComparable claim missed");
        }

        public static void AssertNoGenerics(this FinalizationResults results) 
            => Assert.AreEqual(0, results.GenericsCount,"Unexpected generic types");

        public static void AssertNamedEqualToArrayOf(this FinalizationResults results, object typeOrNode, params string[] varNames)
        {
            foreach (var varName in varNames)
            {
                var node = results.GetVariableNode(varName).GetNonReference();
                if (node.State is Array array)
                {
                    var element = array.ElementNode;
                    if (typeOrNode is Primitive concrete)
                        Assert.IsTrue(concrete.Equals(element.State));
                    else
                        Assert.AreEqual(typeOrNode, array.ElementNode);
                }
                else 
                {
                    Assert.Fail();
                }
            }
        }
        public static void AssertNamed(this FinalizationResults results, IType type, params string[] varNames)
        {
            foreach (var varName in varNames)
            {
                Assert.AreEqual(type, results.GetVariableNode(varName).GetNonReference().State);
            }
        }
        public static void AssertNode(this FinalizationResults results, IType type, params int[] nodeIds)
        {
            foreach (var id in nodeIds)
            {
                Assert.AreEqual(type, results.GetSyntaxNode(id).GetNonReference().State);
            }

        }
    }
}