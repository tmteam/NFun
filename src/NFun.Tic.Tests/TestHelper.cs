using System;
using System.Linq;
using NFun.Tic.SolvingStates;
using NFun.TypeInferenceCalculator;
using NFun.TypeInferenceCalculator.Errors;
using NUnit.Framework;
using Array = NFun.Tic.SolvingStates.Array;

namespace NFun.Tic.Tests
{
    public static class TestHelper
    {
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
                Assert.AreEqual(desc, generic.Descedant,"Actual generic type is "+generic);

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
                Assert.AreEqual(type, results.GetSyntaxNodeOrNull(id).GetNonReference().State);
            }

        }
        public static void AssertNode(this FinalizationResults results, SolvingNode generic, params int[] nodeIds)
        {
            foreach (var id in nodeIds)
            {
                Assert.AreEqual(generic.GetNonReference(), results.GetSyntaxNodeOrNull(id).GetNonReference());
            }

        }
    }
}