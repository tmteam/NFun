using System;
using System.Linq;
using NFun;
using NFun.Interpritation;
using NFun.SyntaxParsing.Visitors;
using NFun.Tic;
using NFun.Tic.SolvingStates;
using NFun.TypeInferenceAdapter;
using NUnit.Framework;

namespace NFun.ModuleTests.TicTests
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
        public static ITicResults Solve(string equation)
        {
            Console.WriteLine(equation);

            var flow = NFun.Tokenization.Tokenizer.ToFlow(equation);
            var tree = NFun.SyntaxParsing.Parser.Parse(flow);
            tree.ComeOver(new SetNodeNumberVisitor(0));


            var graph = new GraphBuilder();
            var resultsBuilder = new TypeInferenceResultsBuilder();

            var functions = new FlatMutableFunctionDictionary();
            foreach (var predefinedFunction in BaseFunctions.ConcreteFunctions)
                functions.TryAdd(predefinedFunction);
            foreach (var predefinedFunction in BaseFunctions.GenericFunctions)
                functions.TryAdd(predefinedFunction);

            TicSetupVisitor.SetupTicForBody(
                tree:         tree, 
                ticGraph:     graph, 
                functions:    functions, 
                constants:    new EmptyConstantList(), 
                aprioriTypes: AprioriTypesMap.Empty, 
                results:      resultsBuilder);
            
            return graph.Solve();
        }
        public static TypeInferenceResults SolveAndGetResults(string equation)
        {
            Console.WriteLine(equation);

            var flow = NFun.Tokenization.Tokenizer.ToFlow(equation);
            var tree = NFun.SyntaxParsing.Parser.Parse(flow);
            tree.ComeOver(new SetNodeNumberVisitor(0));

            var graph = new GraphBuilder();

            var functions = new FlatMutableFunctionDictionary();
            foreach (var predefinedFunction in BaseFunctions.ConcreteFunctions)
                functions.TryAdd(predefinedFunction);
            foreach (var predefinedFunction in BaseFunctions.GenericFunctions)
                functions.TryAdd(predefinedFunction);

            var resultsBuilder = new TypeInferenceResultsBuilder();

            TicSetupVisitor.SetupTicForBody(
                tree:      tree, 
                ticGraph:  graph, 
                functions: functions, 
                constants: new EmptyConstantList(), 
                aprioriTypes: AprioriTypesMap.Empty,  
                results:   resultsBuilder);

            var res =  graph.Solve();
            resultsBuilder.SetResults(res);
            return resultsBuilder.Build();
        }
        public static void AssertAreGenerics(this ITicResults result, TicNode targetGenericNode,
            params string[] varNames)
        {
            foreach (var varName in varNames)
            {
                Assert.AreEqual(targetGenericNode, result.GetVariableNode(varName).GetNonReference());
            }
        }

        public static TicNode AssertAndGetSingleArithGeneric(this ITicResults result)
            => AssertAndGetSingleGeneric(result, StatePrimitive.U24, StatePrimitive.Real, false);

        public static TicNode AssertAndGetSingleGeneric(this ITicResults result, StatePrimitive desc,
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

        public static void AssertNoGenerics(this ITicResults results) 
            => Assert.IsFalse(results.HasGenerics,"Unexpected generic types");

        public static void AssertNamedEqualToArrayOf(this ITicResults results, object typeOrNode, params string[] varNames)
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
        public static void AssertNamed(this ITicResults results, ITypeState type, params string[] varNames)
        {
            foreach (var varName in varNames)
            {
                Assert.AreEqual(type, results.GetVariableNode(varName).GetNonReference().State);
            }
        }
        public static void AssertNode(this ITicResults results, ITypeState type, params int[] nodeIds)
        {
            foreach (var id in nodeIds)
            {
                Assert.AreEqual(type, results.GetSyntaxNodeOrNull(id).GetNonReference().State);
            }

        }
    }
}