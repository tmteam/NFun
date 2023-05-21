namespace NFun.UnitTests.TicTests;

using NUnit.Framework;
using TestTools;
using Tic;
using Tic.SolvingStates;
using static Tic.SolvingStates.StatePrimitive;

public class ConstraintsFitsTest {

    [Test]
    public void PrimitiveFits_returnTrue1() =>
        Any.FitsInto(EmptyConstrains).AssertTrue();

    [Test]
    public void PrimitiveFits_returnTrue2() =>
        Ip.FitsInto(EmptyConstrains).AssertTrue();

    [Test]
    public void PrimitiveFits_returnTrue3() =>
        U32.FitsInto(Constrains(U32, Real)).AssertTrue();

    [Test]
    public void PrimitiveFits_returnTrue4() =>
        U64.FitsInto(Constrains(U32, Real)).AssertTrue();

    [Test]
    public void PrimitiveFits_returnTrue5() =>
        U32.FitsInto(Constrains(U32, Real, isComparable:true)).AssertTrue();

    [Test]
    public void PrimitiveFits_returnTrue6() =>
        U32.FitsInto(Constrains(isComparable: true)).AssertTrue();

    [Test]
    public void PrimitiveFits_returnTrue7() =>
        U32.FitsInto(Constrains(U32, isComparable:true)).AssertTrue();

    [Test]
    public void PrimitiveFits_returnTrue8() =>
        U32.FitsInto(Constrains(null, Real, isComparable:true)).AssertTrue();

    [Test]
    public void PrimitiveFits_returnFalse() =>
        EmptyConstrains.FitsInto(Constrains(U8, Real)).AssertFalse();

    [Test]
    public void PrimitiveFits_returnFalse2() =>
        U32.FitsInto(Constrains(U64, Real)).AssertFalse();

    [Test]
    public void PrimitiveFits_returnFalse3() =>
        Bool.FitsInto(Constrains(isComparable: true)).AssertFalse();

    [Test]
    public void ConstrainsFits_returnTrue() =>
        Constrains(U8, Real).FitsInto(EmptyConstrains).AssertTrue();

    [Test]
    public void ConstrainsFits_returnFalse() =>
        Constrains(U64, Real).FitsInto(U32).AssertFalse();

    [Test]
    public void ArrayFits_returnFalse() =>
        Array(Constrains(U8))
            .FitsInto(Constrains(Array(Any)))
            .AssertFalse();

    [Test]
    public void ArrayFits_returnTrue() =>
        Array(Constrains(U32))
            .FitsInto(Constrains(Array(U8)))
            .AssertTrue();

    [Test]
    public void ArrayFits_returnTrue2() =>
        Array(Constrains(U8, Real))
            .FitsInto(Constrains(Array(EmptyConstrains)))
            .AssertTrue();

    [Test]
    public void ArrayFits_returnTrue3() =>
        Array(U32)
            .FitsInto(Array(EmptyConstrains))
            .AssertTrue();

    [Test]
    public void ArrayFits_returnTrue4() =>
        Array(Constrains(Array(U8)))
            .FitsInto(Array(EmptyConstrains))
            .AssertTrue();

    [Test]
    public void ArrayFits_returnTrue5() =>
        Array(U32).FitsInto(Constrains(Array(U8))).AssertTrue();

    [Test]
    public void ArrayFits_returnTrue6() =>
        Array(U32)
            .FitsInto(Array(Constrains(U32, Real, isComparable:true)))
            .AssertTrue();

    [Test]
    public void ArrayFits_returnTrue7() =>
        Array(Constrains(U32, Real, isComparable:true))
            .FitsInto(Array(EmptyConstrains))
            .AssertTrue();

    [Test]
    public void ArrayFits_returnTrue8() =>
        Array(Array(EmptyConstrains))
            .FitsInto(Array(EmptyConstrains))
            .AssertTrue();

    [Test]
    public void FunFits_returnsFalse() =>
        StateFun.Of(new[] { Any }, Any)
            .FitsInto(Constrains(Array(EmptyConstrains)))
            .AssertFalse();

    [Test]
    public void FunFits_returnsTrue() =>
        StateFun.Of(new[] { Real }, U24)
            .FitsInto(Constrains(StateFun.Of(new[] { Any }, U16)))
            .AssertTrue();

    [Test]
    public void FunFits_returnsTrue2() {
        var constrains = Constrains(StateFun.Of(EmptyConstrains, Constrains(U16)));
        var target = StateFun.Of(new[] { Any }, U16);
        target.FitsInto(constrains).AssertTrue();
    }

    [Test]
    public void FunFits_returnsTrue3() {
        var constrains = Constrains(StateFun.Of(
            new[] { TicNode.CreateInvisibleNode(Constrains(desc: U16, anc: Real)) },
            TicNode.CreateInvisibleNode(Constrains(desc: U16, anc: Real))));
        var target = StateFun.Of(new[] { TicNode.CreateInvisibleNode(Constrains(desc: U32, anc: U64)) },
            TicNode.CreateInvisibleNode(Constrains(desc: U32, anc: U64)));
        target.FitsInto(constrains).AssertTrue();
    }

    [Test]
    public void FunFits_returnsTrue4() {
        var constrains = Constrains(StateFun.Of(EmptyConstrains, EmptyConstrains));
        var target = StateFun.Of(new[] { Any }, U16);
        target.FitsInto(constrains).AssertTrue();
    }

    [Test]
    public void FunFits_returnsTrue5() {
        var constrains = Constrains(
            StateFun.Of(new[] { Any },
                StateFun.Of(new[] { Any },
                    StateFun.Of(new[] { Any },
                        U24))));
        var target =
            StateFun.Of(Constrains(U24, Real) ,
                StateFun.Of(Constrains(U24, Real),
                    StateFun.Of(Constrains(U24, Real),
                        Constrains(U24, Real))));
        target.FitsInto(constrains).AssertTrue();
    }

    private static ConstrainsState EmptyConstrains => ConstrainsState.Empty;

    private static ITicNodeState Array(ITicNodeState state) => StateArray.Of(state);

    private static ITicNodeState Constrains(ITicNodeState desc = null, StatePrimitive anc = null,
        bool isComparable = false)
        => ConstrainsState.Of(desc, anc, isComparable);
}

