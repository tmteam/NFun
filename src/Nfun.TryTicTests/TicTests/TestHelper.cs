using System;
using System.Linq;
using NFun;
using NFun.Interpritation;
using NFun.SyntaxParsing.Visitors;
using NFun.Tic;
using NFun.Tic.SolvingStates;
using NFun.TypeInferenceAdapter;
using NFun.TypeInferenceCalculator;
using NUnit.Framework;

namespace Nfun.ModuleTests.TicTests
{
    public static class TestHelper
    {
        public static void AssertAreSame(ITicNodeState expected, ITicNodeState actual)
        {
            if (!AreSame(expected, actual))
            {
                if (expected is StateRefTo r1)
                    expected = r1.Node.GetNonReference().State;
                if (actual is StateRefTo r2)
                    actual = r2.Node.GetNonReference().State;
                Assert.Fail($"Expected: {expected} but was: {actual}");
            }
        }
        public static bool AreSame(ITicNodeState a, ITicNodeState b)
        {
            if (a is StateRefTo r1)
                a = r1.Node.GetNonReference().State;
            if (b is StateRefTo r2)
                b = r2.Node.GetNonReference().State;
            return a.Equals(b);
        }
        public static TicResults Solve(string equation)
        {
            Console.WriteLine(equation);

            var flow = NFun.Tokenization.Tokenizer.ToFlow(equation);
            var tree = NFun.SyntaxParsing.Parser.Parse(flow);
            tree.ComeOver(new SetNodeNumberVisitor(0));


            var graph = new GraphBuilder();
            var resultsBuilder = new TypeInferenceResultsBuilder();

            var functions = new FunctionDictionary();
            foreach (var predefinedFunction in BaseFunctions.ConcreteFunctions)
                functions.TryAdd(predefinedFunction);
            foreach (var predefinedFunction in BaseFunctions.GenericFunctions)
                functions.TryAdd(predefinedFunction);

            TicSetupVisitor.Run(tree.Children, graph, functions, new EmptyConstantList(), resultsBuilder);
            return graph.Solve();
        }
        public static TypeInferenceResults SolveAndGetResults(string equation)
        {
            Console.WriteLine(equation);

            var flow = NFun.Tokenization.Tokenizer.ToFlow(equation);
            var tree = NFun.SyntaxParsing.Parser.Parse(flow);
            tree.ComeOver(new SetNodeNumberVisitor(0));

            var graph = new GraphBuilder();

            var functions = new FunctionDictionary();
            foreach (var predefinedFunction in BaseFunctions.ConcreteFunctions)
                functions.TryAdd(predefinedFunction);
            foreach (var predefinedFunction in BaseFunctions.GenericFunctions)
                functions.TryAdd(predefinedFunction);

            var resultsBuilder = new TypeInferenceResultsBuilder();

            TicSetupVisitor.Run(tree.Children, graph, functions, new EmptyConstantList(), resultsBuilder);

            var res =  graph.Solve();
            resultsBuilder.SetResults(res);
            return resultsBuilder.Build();
        }
        public static void AssertAreGenerics(this TicResults result, TicNode targetGenericNode,
            params string[] varNames)
        {
            foreach (var varName in varNames)
            {
                Assert.AreEqual(targetGenericNode, result.GetVariableNode(varName).GetNonReference());
            }
        }

        public static TicNode AssertAndGetSingleArithGeneric(this TicResults result)
            => AssertAndGetSingleGeneric(result, StatePrimitive.U24, StatePrimitive.Real, false);

        public static TicNode AssertAndGetSingleGeneric(this TicResults result, StatePrimitive desc,
            StatePrimitive anc, bool isComparable = false)
        {
            Assert.AreEqual(1, result.GenericsCount,"Incorrect generics count");
            var genericNode = result.GenericNodes.Single();

            AssertGenericType(genericNode, desc, anc, isComparable);
            return genericNode;
        }

        public static void AssertGenericTypeIsArith(this TicNode node)
        {
            AssertGenericType(node, StatePrimitive.U24, StatePrimitive.Real, false);
        }

        public static void AssertGenericType(this TicNode node, StatePrimitive desc, StatePrimitive anc,
            bool isComparable = false)
        {
            var generic = node.State as ConstrainsState;
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

        public static void AssertNoGenerics(this TicResults results) 
            => Assert.IsFalse(results.HasGenerics,"Unexpected generic types");

        public static void AssertNamedEqualToArrayOf(this TicResults results, object typeOrNode, params string[] varNames)
        {
            foreach (var varName in varNames)
            {
                var node = results.GetVariableNode(varName).GetNonReference();
                if (node.State is StateArray array)
                {
                    var element = array.ElementNode;
                    if (typeOrNode is StatePrimitive concrete)
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
        public static void AssertNamed(this TicResults results, ITypeState type, params string[] varNames)
        {
            foreach (var varName in varNames)
            {
                Assert.AreEqual(type, results.GetVariableNode(varName).GetNonReference().State);
            }
        }
        public static void AssertNode(this TicResults results, ITypeState type, params int[] nodeIds)
        {
            foreach (var id in nodeIds)
            {
                Assert.AreEqual(type, results.GetSyntaxNodeOrNull(id).GetNonReference().State);
            }

        }
    }
}