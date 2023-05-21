namespace NFun.UnitTests.TicTests.StateExtensions;

using Tic;
using NFun.Tic.SolvingStates;
using NUnit.Framework;
using static Tic.SolvingStates.StatePrimitive;
using static LcaTestTools;
using static SolvingStates;

public class LcaArraysTest {

    [Test]
    public void PrimitiveAndArrayOfBottoms_ReturnsAny() {
        foreach (var primitive in PrimitiveTypes)
            AssertLca(Array(EmptyConstrains), primitive, Any);
    }

    [Test]
    public void PrimitiveAndArrayOfComposite_ReturnsAny() {
        foreach (var primitive in PrimitiveTypes)
            AssertLca(Array(Array(EmptyConstrains)), primitive, Any);
    }

    [Test]
    public void PrimitiveAndArrayOfPrimitive_ReturnsAny() {
        foreach (var primitive in PrimitiveTypes)
            AssertLca(Array(Any), primitive, Any);
    }

    [Test]
    public void ArrayOfPrimitiveTypes_ReturnsArrayOfLca() {
        foreach (var types in PrimitiveTypesLca)
            AssertLca(
                Array(types.Left),
                Array(types.Right),
                Array(types.Lca));
    }

    [Test]
    public void ArrayOfPrimitiveTypeAndArrayOfConstrainType_ReturnsArrayOfLca() {
        foreach (var types in PrimitiveTypesLca)
            AssertLca(
                Array(types.Left),
                Array(EmptyConstrains),
                Array(types.Left));
    }


    [Test]
    public void ArrayOfPrimitiveTypeAndArrayOfConstrainTypeWithDesc_ReturnsArrayOfLca() {
        foreach (var types in PrimitiveTypesLca)
            AssertLca(
                Array(types.Left),
                Array(Constrains(desc: types.Right)),
                Array(types.Lca));
    }

    [Test]
    public void ComplexNestedArray() {
        foreach (var types in PrimitiveTypesLca)
        {
            var array1 = Array(Ref(Array(Constrains(desc: types.Left))));
            var array2 = Array(Array(types.Right));

            AssertLca(array1, array2, Array(Array(types.Lca)));
        }
    }
}
