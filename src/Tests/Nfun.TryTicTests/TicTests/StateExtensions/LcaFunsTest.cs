namespace NFun.UnitTests.TicTests.StateExtensions;

using NUnit.Framework;
using static LcaTestTools;
using static SolvingStates;
using static Tic.SolvingStates.StatePrimitive;

public class LcaFunsTest {

    [Test]
    public void ConstFunOfPrimitiveTypeAndFunOfConstrainTypeWithDesc_ReturnsFunToLca() {
        foreach (var types in PrimitiveTypesLca)
            AssertLca(
                Fun(types.Left),
                Fun(Constrains(desc: types.Right)),
                Fun(types.Lca));
    }

    [Test]
    public void SameArgumentsFunOfPrimitiveTypeAndFunOfConstrainTypeWithDesc_ReturnsFunToLca() {
        foreach (var types in PrimitiveTypesLca)
            AssertLca(
                Fun(Any, Bool, types.Left),
                Fun(Any, Bool, Constrains(desc: types.Right)),
                Fun(Any, Bool, types.Lca));
    }

    [Test]
    public void Fun_withDifferentArgumentNumber() =>
        AssertLca(
            Fun(U64, U64, U64),
            Fun(U64, U64),
            Any);

    [Test]
    public void Fun_primitiveLca() =>
        AssertLca(
            Fun(I32, Real, I16),
            Fun(U64, I64, U16),
            Fun(U24, I64, I24));

    [Test]
    public void Fun_primitiveLcaCannotBeSolved() =>
        AssertLca(
            Fun(I32, Bool, I16),
            Fun(U64, I64 , U16),
            Any);

    [Test]
    public void PrimitiveAndFunOfBottoms_ReturnsAny() {
        foreach (var primitive in PrimitiveTypes)
            AssertLca(Fun(EmptyConstrains, EmptyConstrains), primitive, Any);
    }

    [Test]
    public void PrimitiveAndFunOfPrimitive_ReturnsAny() {
        foreach (var primitive in PrimitiveTypes)
            AssertLca(Fun(new[] { Any }, Any), primitive, Any);
    }

    [Test]
    public void FunOfPrimitiveToSelf_ReturnsSelf() {
        foreach (var primitive in PrimitiveTypes)
            AssertLca(
                Fun(primitive, primitive, primitive),
                Fun(primitive, primitive, primitive),
                Fun(primitive, primitive, primitive)
            );
    }

    [Test]
    public void FunReturnsPrimitiveTypes_ReturnsFunLca() {
        foreach (var types in PrimitiveTypesLca)
            AssertLca(
                Fun(Any, types.Left),
                Fun(Any, types.Right),
                Fun(Any, types.Lca));
    }

    [Test]
    public void FunReturnsPrimitiveTypeAndConstrainType_ReturnsFunThatReturnsPrimitive() {
        foreach (var types in PrimitiveTypesLca)
            AssertLca(
                Fun(Any , types.Left),
                Fun(Any , EmptyConstrains),
                Fun(Any , types.Left));
    }

    [Test]
    public void FunReturnsPrimitiveTypeAndConstrainWithDescType_ReturnsFunThatReturnsLca() {
        foreach (var types in PrimitiveTypesLca)
            AssertLca(
                Fun(Any, types.Left),
                Fun(Any, Constrains(desc: types.Right)),
                Fun(Any, types.Lca));
    }

    [Test]
    public void FunReturnsConstrainWithDescType_ReturnsFunThatReturnsLca() {
        foreach (var types in PrimitiveTypesLca)
            AssertLca(
                Fun(Any, Constrains(desc: types.Left)),
                Fun(Any, Constrains(desc: types.Right)),
                Fun(Any, types.Lca));
    }

    [Test]
    public void FunReturnsArrayConstains() {
        foreach (var types in PrimitiveTypesLca)
            AssertLca(
                Fun(Any,
                    Constrains(desc: Array(Constrains(desc: Array(types.Left))))),
                Fun(Any,
                    Constrains(desc: Array(Constrains(desc: Array(types.Right))))),
                Fun(Any,
                    Array(Array(types.Lca))));
    }

    [Test]
    public void FunPrimitiveTypeAndConstrain() {
        foreach (var types in PrimitiveTypesLca)
        {
            var lcd = types.Left.GetFirstCommonDescendantOrNull(types.Right);
            if (lcd == null)
                AssertLca(
                    Fun(types.Left , Any),
                    Fun(Constrains(anc: types.Right), Any),
                    Any);
            else
                AssertLca(
                    Fun(types.Left , Any),
                    Fun(Constrains(anc: types.Right), Any),
                    Fun(lcd, Any));
        }
    }

    [Test]
    public void FunConstrainsAndConstrain() {
        foreach (var types in PrimitiveTypesLca)
        {
            var lcd = types.Left.GetFirstCommonDescendantOrNull(types.Right);
            if (lcd == null)
                AssertLca(
                    Fun(Constrains(anc: types.Left), Any),
                    Fun(Constrains(anc: types.Right), Any),
                    Any);
            else
                AssertLca(
                    Fun(Constrains(anc: types.Left), Any),
                    Fun(Constrains(anc: types.Right), Any),
                    Fun(lcd, Any));
        }
    }

    [Test]
    public void FunOfFun() {
        foreach (var types in PrimitiveTypesLca)
        {
            AssertLca(
                Fun(Fun(types.Left, I64), Any),
                Fun(Fun(types.Right, U64), Any),
                Fun(Fun(types.Lca, U48), Any));
        }
    }

    [Test]
    public void FunReturnsConstrain_ReturnsFunThatReturnsConstrains() =>
        AssertLca(
            Fun(EmptyConstrains, EmptyConstrains, EmptyConstrains),
            Fun(EmptyConstrains, EmptyConstrains, EmptyConstrains),
            Fun(Any, Any, EmptyConstrains));
}
