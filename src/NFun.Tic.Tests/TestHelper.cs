using System;
using System.Linq;
using NFun.Tic.Errors;
using NFun.Tic.SolvingStates;
using NUnit.Framework;

namespace NFun.Tic.Tests {

public static class TestHelper {
    public static void AssertThrowsRecursiveTicTypedDefinition(Action delegateCode) {
        try
        {
            delegateCode();
            Assert.Fail("Impossible equation solved");
        }
        catch (RecursiveTypeDefinitionException e)
        {
            Console.WriteLine(e);
        }
        catch (AssertionException)
        { }
        catch (Exception e)
        {
            Assert.Fail(
                $"Invalid exception. {e.GetType().Name} was thrown. RecursiveTypeDefinition expected.\r\n{e}");
        }
    }

    public static void AssertThrowsTicError(Action delegateCode) {
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
        { }
        catch (Exception e)
        {
            Assert.Fail($"Invalid exception. {e.GetType().Name} was thrown. TicExcpetion expected.\r\n{e}");
        }
    }

    public static void AssertAreGenerics(
        this ITicResults result, TicNode targetGenericNode,
        params string[] varNames) {
        foreach (var varName in varNames)
        {
            Assert.AreEqual(targetGenericNode, result.GetVariableNode(varName).GetNonReference());
        }
    }

    public static TicNode AssertAndGetSingleArithGeneric(this ITicResults result)
        => AssertAndGetSingleGeneric(result, StatePrimitive.U24, StatePrimitive.Real, false);

    public static TicNode AssertAndGetSingleGeneric(
        this ITicResults result, StatePrimitive desc,
        StatePrimitive anc, bool isComparable = false) {
        Assert.AreEqual(1, result.GenericsStates.Count(), "Incorrect generics count");
        var genericNode = result.GenericNodes.Single();
        AssertGenericType(genericNode, desc, anc, isComparable);
        return genericNode;
    }

    public static void AssertGenericTypeIsArith(this TicNode node) {
        AssertGenericType(node, StatePrimitive.U24, StatePrimitive.Real, false);
    }

    public static void AssertGenericType(
        this ITicNodeState state, StatePrimitive desc, StatePrimitive anc,
        bool isComparable = false) {
        var generic = state as ConstrainsState;
        Assert.IsNotNull(generic);
        if (desc == null)
            Assert.IsFalse(generic.HasDescendant);
        else
            Assert.AreEqual(desc, generic.Descendant, "Actual generic type is " + generic);

        if (anc == null)
            Assert.IsFalse(generic.HasAncestor);
        else
            Assert.AreEqual(anc, generic.Ancestor);

        Assert.AreEqual(isComparable, generic.IsComparable, "IsComparable claim missed");
    }

    public static void AssertGenericType(
        this TicNode node, StatePrimitive desc, StatePrimitive anc,
        bool isComparable = false) {
        var generic = node.State as ConstrainsState;
        Assert.IsNotNull(generic);
        if (desc == null)
            Assert.IsFalse(generic.HasDescendant);
        else
            Assert.AreEqual(desc, generic.Descendant, "Actual generic type is " + generic);

        if (anc == null)
            Assert.IsFalse(generic.HasAncestor);
        else
            Assert.AreEqual(anc, generic.Ancestor);

        Assert.AreEqual(isComparable, generic.IsComparable, "IsComparable claim missed");
    }

    public static void AssertNoGenerics(this ITicResults results)
        => Assert.AreEqual(
            0, results.GenericsCount, $"Unexpected generic types. Generic nodes: " +
                                      $"{string.Join(",", results.GenericNodes)}");

    public static void AssertNamedEqualToArrayOf(
        this ITicResults results, object typeOrNode,
        params string[] varNames) {
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

    public static void AssertNamed(this ITicResults results, ITypeState expectedState, params string[] varNames) {
        foreach (var varName in varNames)
        {
            AssertNodeStateEqualToState(
                expected: expectedState,
                actual: results.GetVariableNode(varName).State,
                id: varName);
        }
    }

    public static void AssertNode(this ITicResults results, ITypeState type, params int[] nodeIds) {
        foreach (var id in nodeIds)
        {
            var actual = results.GetSyntaxNodeOrNull(id).State;
            AssertNodeStateEqualToState(type, actual, id);
        }
    }

    private static void AssertNodeStateEqualToState(ITypeState expected, ITicNodeState actual, object id) =>
        Assert.IsTrue(
            AreStatesEqualByValue(actual, expected),
            $"States are not equal for '{id}': \r\nExpected: {expected}\r\nActual: {actual}");

    public static void AssertNode(this ITicResults results, TicNode generic, params int[] nodeIds) {
        foreach (var id in nodeIds)
        {
            Assert.AreEqual(generic.GetNonReference(), results.GetSyntaxNodeOrNull(id).GetNonReference());
        }
    }

    private static bool AreStatesEqualByValue(ITicNodeState a, ITicNodeState b) {
        while (a is StateRefTo arefTo) a = arefTo.Node.State;
        while (b is StateRefTo brefTo) b = brefTo.Node.State;

        if (a.GetType() != b.GetType())
            return false;

        if (a is StatePrimitive)
            return a.Equals(b);
        if (a is ConstrainsState)
            return a.Equals(b);
        if (a is ICompositeState aComposite && b is ICompositeState bComposite)
        {
            var aMembers = aComposite.Members.ToArray();
            var bMembers = bComposite.Members.ToArray();
            if (aMembers.Length != bMembers.Length)
                return false;
            for (int i = 0; i < aMembers.Length; i++)
            {
                if (!AreStatesEqualByValue(aMembers[i].State, bMembers[i].State))
                    return false;
            }

            return true;
        }

        return false;
    }
}

}