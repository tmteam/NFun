namespace NFun.UnitTests.TicTests.StateExtensions;

using NFun.Tic.SolvingStates;
using NUnit.Framework;
using static LcaTestTools;
using static SolvingStates;

public class LcaPrimitivesTest {
    [Test]
    public void Primitives() {
        foreach (var types in PrimitiveTypesLca)
            AssertLca(types.Left, types.Right, types.Lca);
    }

    [Test]
    public void PrimitiveAndBottom_ReturnsPrimitive() {
        foreach (var primitive in PrimitiveTypes)
            AssertLca(EmptyConstrains, primitive, primitive);
    }

    [Test]
    public void PrimitiveAndConstraint_ReturnsLcaOfTypeAndDesc() {
        foreach (var types in PrimitiveTypesLca)
            AssertLca(Constrains(types.Left), types.Right, types.Lca);
    }

    [Test]
    public void ConstraintAndConstraint_ReturnsLcaOfDescAndDesc() {
        foreach (var types in PrimitiveTypesLca)
            AssertLca(Constrains(types.Left), Constrains(types.Right),
                types.Lca);
    }

    [Test]
    public void ConstraintAndBottom_ReturnsfDesc() {
        foreach (var primitive in PrimitiveTypes)
            AssertLca(Constrains(primitive), EmptyConstrains, primitive);
    }

    [Test]
    public void BottomAndBottom_returnBottom() =>
        AssertLca(EmptyConstrains, EmptyConstrains, EmptyConstrains);
}
