namespace NFun.UnitTests.TicTests;

using NUnit.Framework;
using TestTools;
using Tic;
using Tic.SolvingStates;
using static Tic.SolvingStates.StatePrimitive;

public class ConstraintsFitsTest {
    [Test]
    public void Fits_returnTrue() {
        //[u8..Re] FITS into  [..]
        var constrains = ConstrainsState.Empty;
        var target = ConstrainsState.Of(U8, Real);
        constrains.Fits(target).AssertTrue();
    }

    [TestCase(PrimitiveTypeName.Any)]
    [TestCase(PrimitiveTypeName.Ip)]
    [TestCase(PrimitiveTypeName.U8)]
    public void Fits_returnTrue2(PrimitiveTypeName primitive) {
        //primitives FIT into  [..]
        var constrains = ConstrainsState.Empty;
        var target = new StatePrimitive(primitive);
        constrains.Fits(target).AssertTrue();
    }

    [Test]
    public void Fits_returnTrue3() {
        //u32 Does not FITS into  [u32..Re]
        var constrains = ConstrainsState.Of(U32, Real);
        var target = U32;
        constrains.Fits(target).AssertTrue();
    }

    [Test]
    public void Fits_returnFalse() {
        //[...] Does not FITS into  [u8..Re]
        var constrains = ConstrainsState.Of(U8, Real);
        var target = ConstrainsState.Empty;
        constrains.Fits(target).AssertFalse();
    }

    [Test]
    public void Fits_returnFalse2() {
        //u32 Does not FITS into  [u64..Re]
        var constrains = ConstrainsState.Of(U64, Real);
        var target = U32;
        constrains.Fits(target).AssertFalse();
    }

    [Test]
    public void ArrayFits_returnFalse() {
        //arr([u8..]) does not fit in  [arr(any)..]
        var constrains = ConstrainsState.Of(StateArray.Of(Any));
        var target = StateArray.Of(ConstrainsState.Of(U8));
        constrains.Fits(target).AssertFalse();
    }

    [Test]
    public void ArrayFits_returnTrue() {
        //arr([u32..]) FITS in  [arr(u8)..]
        var constrains = ConstrainsState.Of(StateArray.Of(U8));
        var target = StateArray.Of(ConstrainsState.Of(U32));
        constrains.Fits(target).AssertTrue();
    }

    [Test]
    public void ArrayFits_returnTrue2() {
        //arr([u8..Re]) FITS into  [arr([..])..]
        var constrains = ConstrainsState.Of(StateArray.Of(ConstrainsState.Empty));
        var target = StateArray.Of(
            TicNode.CreateInvisibleNode(ConstrainsState.Of(U8, Real)));
        constrains.Fits(target).AssertTrue();
    }

    [Test]
    public void ArrayFits_returnTrue3() {
        //arr([u32..]) FITS in  [arr(u8)..]
        var constrains = ConstrainsState.Of(StateArray.Of(U8));
        var target = StateArray.Of(U32);
        constrains.Fits(target).AssertTrue();
    }

    [Test]
    public void HiOrderFun_returnsFalse() {
        //(Any->(Any->U24))-> (Re->(Re->U24))
        var constrains = ConstrainsState.Of(StateArray.Of(ConstrainsState.Empty));
        constrains.Fits(StateFun.Of(new[] { Any }, Any)).AssertFalse();
    }

    [Test]
    public void HiOrderFun_returnsTrue() {
        var constrains = ConstrainsState.Of(StateFun.Of(new[] { Any }, U16));
        var target = StateFun.Of(new[] { Real }, U24);
        constrains.Fits(target).AssertTrue();
    }

    [Test]
    public void HiOrderFun_returnsTrue2() {
        var constrains = ConstrainsState.Of(StateFun.Of(ConstrainsState.Empty, ConstrainsState.Of(U16)));
        var target = StateFun.Of(new[] { Any }, U16);
        constrains.Fits(target).AssertTrue();
    }

    [Test]
    public void HiOrderFun_returnsTrue3() {
        var constrains = ConstrainsState.Of(StateFun.Of(
            new[] { TicNode.CreateInvisibleNode(ConstrainsState.Of(desc: U16, anc: Real)) },
            TicNode.CreateInvisibleNode(ConstrainsState.Of(desc: U16, anc: Real))));
        var target = StateFun.Of(new[] { TicNode.CreateInvisibleNode(ConstrainsState.Of(desc: U32, anc: U64)) },
            TicNode.CreateInvisibleNode(ConstrainsState.Of(desc: U32, anc: U64)));
        constrains.Fits(target).AssertTrue();
    }

    [Test]
    public void HiOrderFun_returnsTrue4() {
        var constrains = ConstrainsState.Of(StateFun.Of(ConstrainsState.Empty, ConstrainsState.Empty));
        var target = StateFun.Of(new[] { Any }, U16);
        constrains.Fits(target).AssertTrue();
    }

    [Test]
    public void HiOrderFun_returnsTrue5() {
        var constrains = ConstrainsState.Of(
            StateFun.Of(new[] { Any },
                StateFun.Of(new[] { Any },
                    StateFun.Of(new[] { Any },
                        U24))));
        var target =
            StateFun.Of(ConstrainsState.Of(U24, Real) ,
                StateFun.Of(ConstrainsState.Of(U24, Real),
                    StateFun.Of(ConstrainsState.Of(U24, Real),
                        ConstrainsState.Of(U24, Real))));
        constrains.Fits(target).AssertTrue();
    }
}
