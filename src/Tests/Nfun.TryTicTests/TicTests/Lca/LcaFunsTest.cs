namespace NFun.UnitTests.TicTests.Lca;

using Tic.SolvingStates;
using NUnit.Framework;
using static LcaTestTools;
using static Tic.SolvingStates.StatePrimitive;

public class LcaFunsTest {

    [Test]
    public void ConstFunOfPrimitiveTypeAndFunOfConstrainTypeWithDesc_ReturnsFunToLca() {
        foreach (var types in PrimitiveTypesLca)
            AssertLca(
                StateFun.Of(new StatePrimitive[0], types.Left),
                StateFun.Of(new ITicNodeState[0], new ConstrainsState(desc: types.Right)),
                StateFun.Of(new StatePrimitive[0], types.Lca));
    }

    [Test]
    public void SameArgumentsFunOfPrimitiveTypeAndFunOfConstrainTypeWithDesc_ReturnsFunToLca() {
        foreach (var types in PrimitiveTypesLca)
            AssertLca(
                StateFun.Of(new[] { Any, Bool }, types.Left),
                StateFun.Of(new[] { Any, Bool }, new ConstrainsState(desc: types.Right)),
                StateFun.Of(new[] { Any, Bool }, types.Lca));
    }

    [Test]
    public void Fun_withDifferentArgumentNumber() =>
        AssertLca(
            StateFun.Of(new[] { U64, U64 }, U64),
            StateFun.Of(new[] { U64 }, U64),
            Any);

    [Test]
    public void Fun_primitiveLca() =>
        AssertLca(
            StateFun.Of(new[] { I32, Real }, I16),
            StateFun.Of(new[] { U64, I64 }, U16),
            StateFun.Of(new[] { U24, I64 }, I24));

    [Test]
    public void Fun_primitiveLcaCannotBeSolved() =>
        AssertLca(
            StateFun.Of(new[] { I32, Bool }, I16),
            StateFun.Of(new[] { U64, I64  }, U16),
            Any);

    [Test]
    public void PrimitiveAndFunOfBottoms_ReturnsAny() {
        foreach (var primitive in PrimitiveTypes)
            AssertLca(StateFun.Of(new[] { new ConstrainsState() }, new ConstrainsState()), primitive, Any);
    }

    [Test]
    public void PrimitiveAndFunOfPrimitive_ReturnsAny() {
        foreach (var primitive in PrimitiveTypes)
            AssertLca(StateFun.Of(new[] { Any }, Any), primitive, Any);
    }

    [Test]
    public void FunOfPrimitiveToSelf_ReturnsSelf() {
        foreach (var primitive in PrimitiveTypes)
            AssertLca(
                StateFun.Of(new[] { primitive, primitive }, primitive),
                StateFun.Of(new[] { primitive, primitive }, primitive),
                StateFun.Of(new[] { primitive, primitive }, primitive)
            );
    }

    [Test]
    public void FunReturnsPrimitiveTypes_ReturnsFunLca() {
        foreach (var types in PrimitiveTypesLca)
            AssertLca(
                StateFun.Of(new[] { Any }, types.Left),
                StateFun.Of(new[] { Any }, types.Right),
                StateFun.Of(new[] { Any }, types.Lca));
    }

    [Test]
    public void FunReturnsPrimitiveTypeAndConstrainType_ReturnsFunThatReturnsPrimitive() {
        foreach (var types in PrimitiveTypesLca)
            AssertLca(
                StateFun.Of(new[] { Any }, types.Left),
                StateFun.Of(new[] { Any }, new ConstrainsState()),
                StateFun.Of(new[] { Any }, types.Left));
    }

    [Test]
    public void FunReturnsPrimitiveTypeAndConstrainWithDescType_ReturnsFunThatReturnsLca() {
        foreach (var types in PrimitiveTypesLca)
            AssertLca(
                StateFun.Of(new[] { Any }, types.Left),
                StateFun.Of(new[] { Any }, new ConstrainsState(desc: types.Right)),
                StateFun.Of(new[] { Any }, types.Lca));
    }

    [Test]
    public void FunReturnsConstrainWithDescType_ReturnsFunThatReturnsLca() {
        foreach (var types in PrimitiveTypesLca)
            AssertLca(
                StateFun.Of(new[] { Any }, new ConstrainsState(desc: types.Left)),
                StateFun.Of(new[] { Any }, new ConstrainsState(desc: types.Right)),
                StateFun.Of(new[] { Any }, types.Lca));
    }

    [Test]
    public void FunReturnsArrayConstains() {
        foreach (var types in PrimitiveTypesLca)
            AssertLca(
                StateFun.Of(new[] { Any },
                    new ConstrainsState(desc: StateArray.Of(new ConstrainsState(desc: StateArray.Of(types.Left))))),
                StateFun.Of(new[] { Any },
                    new ConstrainsState(desc: StateArray.Of(new ConstrainsState(desc: StateArray.Of(types.Right))))),
                StateFun.Of(new[] { Any },
                    StateArray.Of(StateArray.Of(types.Lca))));
    }

    [Test]
    public void FunPrimitiveTypeAndConstrain() {
        foreach (var types in PrimitiveTypesLca)
        {
            var lcd = types.Left.GetFirstCommonDescendantOrNull(types.Right);
            if (lcd == null)
                AssertLca(
                    StateFun.Of(new[] { types.Left }, Any),
                    StateFun.Of(new[] { new ConstrainsState(anc: types.Right) }, Any),
                    Any);
            else
                AssertLca(
                    StateFun.Of(new[] { types.Left }, Any),
                    StateFun.Of(new[] { new ConstrainsState(anc: types.Right) }, Any),
                    StateFun.Of(new[] { lcd }, Any));
        }
    }

    [Test]
    public void FunConstrainsAndConstrain() {
        foreach (var types in PrimitiveTypesLca)
        {
            var lcd = types.Left.GetFirstCommonDescendantOrNull(types.Right);
            if (lcd == null)
                AssertLca(
                    StateFun.Of(new[] { new ConstrainsState(anc: types.Left) }, Any),
                    StateFun.Of(new[] { new ConstrainsState(anc: types.Right) }, Any),
                    Any);
            else
                AssertLca(
                    StateFun.Of(new[] { new ConstrainsState(anc: types.Left) }, Any),
                    StateFun.Of(new[] { new ConstrainsState(anc: types.Right) }, Any),
                    StateFun.Of(new[] { lcd }, Any));
        }
    }

    [Test]
    public void FunOfFun() {
        foreach (var types in PrimitiveTypesLca)
        {
            AssertLca(
                StateFun.Of(new[] { StateFun.Of(new[] { types.Left }, I64) }, Any),
                StateFun.Of(new[] { StateFun.Of(new[] { types.Right }, U64) }, Any),
                StateFun.Of(new[] { StateFun.Of(new[] { types.Lca }, U48) }, Any));
        }
    }

    [Test]
    public void FunReturnsConstrain_ReturnsFunThatReturnsConstrains() =>
        AssertLca(
            StateFun.Of(new[] { new ConstrainsState(), new ConstrainsState() }, new ConstrainsState()),
            StateFun.Of(new[] { new ConstrainsState(), new ConstrainsState() }, new ConstrainsState()),
            StateFun.Of(new[] { Any, Any }, new ConstrainsState()));
}
