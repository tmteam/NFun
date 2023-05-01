namespace NFun.UnitTests.TicTests.Lca;

using Tic.SolvingStates;
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
            AssertLca(new ConstrainsState(), primitive, primitive);
    }

    [Test]
    public void PrimitiveAndConstraint_ReturnsLcaOfTypeAndDesc() {
        foreach (var types in PrimitiveTypesLca)
            AssertLca(new ConstrainsState(types.Left), types.Right, types.Lca);
    }

    [Test]
    public void ConstraintAndConstraint_ReturnsLcaOfDescAndDesc() {
        foreach (var types in PrimitiveTypesLca)
            AssertLca(new ConstrainsState(types.Left), new ConstrainsState(types.Right),
                types.Lca);
    }

    [Test]
    public void ConstraintAndBottom_ReturnsfDesc() {
        foreach (var primitive in PrimitiveTypes)
            AssertLca(new ConstrainsState(primitive), new ConstrainsState(), primitive);
    }

    [Test]
    public void BottomAndBottom_returnBottom() =>
        AssertLca(new ConstrainsState(), new ConstrainsState(), new ConstrainsState());
}
