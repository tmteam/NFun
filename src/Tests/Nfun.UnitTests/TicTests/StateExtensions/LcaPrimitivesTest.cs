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
            AssertLca(EmptyConstraints, primitive, primitive);
    }

    [Test]
    public void PrimitiveAndConstraint_ReturnsLcaOfTypeAndDesc() {
        foreach (var types in PrimitiveTypesLca)
            AssertLca(Constrains(types.Left), types.Right, types.Lca);
    }

    [Test]
    public void ConstraintAndConstraint_ReturnsLcaOfDescAndDesc() {
        foreach (var types in PrimitiveTypesLca)
        {
            // LCA of two open-ended constraints preserves constraint-ness for primitives
            // (since wider primitive types exist), but collapses to Any when LCA is Any.
            var expected = types.Lca.Equals(StatePrimitive.Any)
                ? (ITicNodeState)types.Lca
                : Constrains(types.Lca);
            AssertLca(Constrains(types.Left), Constrains(types.Right), expected);
        }
    }

    [Test]
    public void ConstraintAndBottom_ReturnsfDesc() {
        foreach (var primitive in PrimitiveTypes)
        {
            var expected = primitive.Equals(StatePrimitive.Any)
                ? (ITicNodeState)primitive
                : Constrains(primitive);
            AssertLca(Constrains(primitive), EmptyConstraints, expected);
        }
    }

    [Test]
    public void BottomAndBottom_returnBottom() =>
        AssertLca(EmptyConstraints, EmptyConstraints, EmptyConstraints);
}
