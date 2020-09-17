﻿using System;
using System.Linq;
using NFun;
using NFun.Interpritation;
using NFun.SyntaxParsing.Visitors;
using NFun.Tic;
using NFun.Tic.SolvingStates;
using NFun.TypeInferenceAdapter;
using NFun.TypeInferenceCalculator;
using NUnit.Framework;
using Array = NFun.Tic.SolvingStates.Array;

namespace Nfun.ModuleTests.TicTests
{
    public static class TestHelper
    {
        public static void AssertAreSame(IState expected, IState actual)
        {
            if (!AreSame(expected, actual))
            {
                if (expected is RefTo r1)
                    expected = r1.Node.GetNonReference().State;
                if (actual is RefTo r2)
                    actual = r2.Node.GetNonReference().State;
                Assert.Fail($"Expected: {expected} but was: {actual}");
            }
        }
        public static bool AreSame(IState a, IState b)
        {
            if (a is RefTo r1)
                a = r1.Node.GetNonReference().State;
            if (b is RefTo r2)
                b = r2.Node.GetNonReference().State;
            return a.Equals(b);
        }
        public static FinalizationResults Solve(string equation)
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
            var genericNode = result.GenericNodes.Single();

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
            => Assert.IsFalse(results.HasGenerics,"Unexpected generic types");

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
                Assert.AreEqual(type, results.GetSyntaxNodeOrNull(id).GetNonReference().State);
            }

        }
    }
}