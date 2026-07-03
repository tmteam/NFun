using NFun.Tic.SolvingStates;
using NUnit.Framework;
using static NFun.Tic.SolvingStates.StatePrimitive;

namespace NFun.Tic.Tests.Collections;

/// <summary>
/// Output-generic protection must reach INSIDE lang collections: GetAllOutputTypes
/// previously enumerated StateFun/StateArray/StateStruct/StateOptional but not
/// StateCollection, so an unresolved element of an output list/set/map was treated
/// as body-internal and collapsed by SolveCovariant instead of staying generic.
/// Mirror of Arrays/ArrayInit.GenericArrayInitWithComplexVariables for the List kind.
/// </summary>
public class CollectionOutputGenericTest {

    [Test]
    public void GenericListInitWithComplexVariables_KeepsOutputGeneric() {
        //    3 0  21
        // y = [x,-x]   (lang list literal)
        var graph = new GraphBuilder();
        graph.SetVar("x", 0);
        graph.SetVar("x", 1);
        graph.SetNegateCall(1, 2);
        graph.SetSoftListInit(3, 0, 2);
        graph.SetDef("y", 3);

        var result = graph.Solve();

        var generic = result.AssertAndGetSingleGeneric(I16, Real);
        var yNode = result.GetVariableNode("y").GetNonReference();
        Assert.IsInstanceOf<StateCollection>(yNode.State, $"got {yNode.State}");
        var sc = (StateCollection)yNode.State;
        Assert.AreEqual(ConstructorKind.List, sc.Constructor);
        Assert.AreEqual(generic, sc.ElementNode.GetNonReference(),
            "list element must BE the output generic node");
    }
}
