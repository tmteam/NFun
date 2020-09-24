using System;
using System.Linq;
using NFun.Tic.Errors;
using NFun.Tic.SolvingStates;
using NFun.TypeInferenceCalculator;
using NFun.TypeInferenceCalculator.Errors;
using NUnit.Framework;

namespace NFun.Tic.Tests
{

    public static class TestHelper
    {
        public static void AssertThrowsRecursiveTicTypedDefenition(Action delegateCode)
        {
            try
            {
                delegateCode();
                Assert.Fail("Impossible equation solved");
            }
            catch (RecursiveTypeDefenitionException e)
            {
                Console.WriteLine(e);
            }
            catch (AssertionException)
            {

            }
            catch (Exception e)
            {
                Assert.Fail($"Invalid exception. {e.GetType().Name} was thrown. RecursiveTypeDefenition expected.\r\n{e}");

            }
        }
        public static void AssertThrowsTicError(Action delegateCode)
        {
            try
            {
                delegateCode();
                Assert.Fail("Impossible equation solved");
            }
            catch (TicException e)
            {
                Console.WriteLine(e);
            }
            catch (AssertionException)
            {

            }
            catch (Exception e)
            {
                Assert.Fail($"Invalid exception. {e.GetType().Name} was thrown. TicExcpetion expected.\r\n{e}");

            }
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
            Assert.AreEqual(1, result.GenericsStates.Count(),"Incorrect generics count");
            var genericNode = result.GenericNodes.Single();
            AssertGenericType(genericNode, desc, anc, isComparable);
            return genericNode;
        }

        public static void AssertGenericTypeIsArith(this TicNode node)
        {
            AssertGenericType(node, StatePrimitive.U24, StatePrimitive.Real, false);
        }
        public static void AssertGenericType(this ITicNodeState state, StatePrimitive desc, StatePrimitive anc,
            bool isComparable = false)
        {
            var generic = state as ConstrainsState;
            Assert.IsNotNull(generic);
            if (desc == null)
                Assert.IsFalse(generic.HasDescendant);
            else
                Assert.AreEqual(desc, generic.Descedant,"Actual generic type is "+generic);

            if (anc == null)
                Assert.IsFalse(generic.HasAncestor);
            else
                Assert.AreEqual(anc, generic.Ancestor);

            Assert.AreEqual(isComparable, generic.IsComparable,"IsComparable claim missed");
        }
        public static void AssertGenericType(this TicNode node, StatePrimitive desc, StatePrimitive anc,
            bool isComparable = false)
        {
            var generic = node.State as ConstrainsState;
            Assert.IsNotNull(generic);
            if (desc == null)
                Assert.IsFalse(generic.HasDescendant);
            else
                Assert.AreEqual(desc, generic.Descedant,"Actual generic type is "+generic);

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
        public static void AssertNode(this TicResults results, TicNode generic, params int[] nodeIds)
        {
            foreach (var id in nodeIds)
            {
                Assert.AreEqual(generic.GetNonReference(), results.GetSyntaxNodeOrNull(id).GetNonReference());
            }

        }
    }
}