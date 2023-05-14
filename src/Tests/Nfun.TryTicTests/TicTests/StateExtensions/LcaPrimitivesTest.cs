namespace NFun.UnitTests.TicTests.StateExtensions;

using NFun.Tic.SolvingStates;
using NUnit.Framework;
using static LcaTestTools;

public class LcaPrimitivesTest {
    [Test]
    public void Primitives() {
        foreach (var types in PrimitiveTypesLca)
            AssertLca(types.Left, types.Right, types.Lca);
    }

    [Test]
    public void PrimitiveAndBottom_ReturnsPrimitive() {
        foreach (var primitive in PrimitiveTypes)
            AssertLca(ConstrainsState.Empty, primitive, primitive);
    }

    [Test]
    public void PrimitiveAndConstraint_ReturnsLcaOfTypeAndDesc() {
        foreach (var types in PrimitiveTypesLca)
            AssertLca(ConstrainsState.Of(types.Left), types.Right, types.Lca);
    }

    [Test]
    public void ConstraintAndConstraint_ReturnsLcaOfDescAndDesc() {
        foreach (var types in PrimitiveTypesLca)
            AssertLca(ConstrainsState.Of(types.Left), ConstrainsState.Of(types.Right),
                types.Lca);
    }

    [Test]
    public void ConstraintAndBottom_ReturnsfDesc() {
        foreach (var primitive in PrimitiveTypes)
            AssertLca(ConstrainsState.Of(primitive), ConstrainsState.Empty, primitive);
    }

    [Test]
    public void BottomAndBottom_returnBottom() =>
        AssertLca(ConstrainsState.Empty, ConstrainsState.Empty, ConstrainsState.Empty);
}
