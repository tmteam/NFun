namespace NFun.UnitTests.TicTests.StateExtensions;

using NFun.Tic;
using NFun.Tic.SolvingStates;
using NUnit.Framework;
using static Tic.SolvingStates.StatePrimitive;
using static LcaTestTools;

public class LcaArraysTest {

    [Test]
    public void PrimitiveAndArrayOfBottoms_ReturnsAny() {
        foreach (var primitive in PrimitiveTypes)
            AssertLca(StateArray.Of(ConstrainsState.Empty), primitive, Any);
    }

    [Test]
    public void PrimitiveAndArrayOfComposite_ReturnsAny() {
        foreach (var primitive in PrimitiveTypes)
            AssertLca(StateArray.Of(StateArray.Of(ConstrainsState.Empty)), primitive, Any);
    }

    [Test]
    public void PrimitiveAndArrayOfPrimitive_ReturnsAny() {
        foreach (var primitive in PrimitiveTypes)
            AssertLca(StateArray.Of(Any), primitive, Any);
    }

    [Test]
    public void ArrayOfPrimitiveTypes_ReturnsArrayOfLca() {
        foreach (var types in PrimitiveTypesLca)
            AssertLca(
                StateArray.Of(types.Left),
                StateArray.Of(types.Right),
                StateArray.Of(types.Lca));
    }

    [Test]
    public void ArrayOfPrimitiveTypeAndArrayOfConstrainType_ReturnsArrayOfLca() {
        foreach (var types in PrimitiveTypesLca)
            AssertLca(
                StateArray.Of(types.Left),
                StateArray.Of(ConstrainsState.Empty),
                StateArray.Of(types.Left));
    }


    [Test]
    public void ArrayOfPrimitiveTypeAndArrayOfConstrainTypeWithDesc_ReturnsArrayOfLca() {
        foreach (var types in PrimitiveTypesLca)
            AssertLca(
                StateArray.Of(types.Left),
                StateArray.Of(ConstrainsState.Of(desc: types.Right)),
                StateArray.Of(types.Lca));
    }

    [Test]
    public void ComplexNestedArray() {
        foreach (var types in PrimitiveTypesLca)
        {
            var array1 = StateArray.Of(
                new StateRefTo(
                    TicNode.CreateNamedNode("foo", StateArray.Of(ConstrainsState.Of(desc: types.Left)))));
            var array2 = StateArray.Of(
                StateArray.Of(types.Right));

            AssertLca(array1, array2, StateArray.Of(StateArray.Of(types.Lca)));
        }
    }
}
