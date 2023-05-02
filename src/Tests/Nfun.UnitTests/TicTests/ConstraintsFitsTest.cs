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
        var constrains = new ConstrainsState();
        var target = new ConstrainsState(U8, Real);
        constrains.Fits(target).AssertTrue();
    }

    [TestCase(PrimitiveTypeName.Any)]
    [TestCase(PrimitiveTypeName.Ip)]
    [TestCase(PrimitiveTypeName.U8)]
    public void Fits_returnTrue2(PrimitiveTypeName primitive) {
        //primitives FIT into  [..]
        var constrains = new ConstrainsState();
        var target = new StatePrimitive(primitive);
        constrains.Fits(target).AssertTrue();
    }

    [Test]
    public void Fits_returnTrue3() {
        //u32 Does not FITS into  [u32..Re]
        var constrains = new ConstrainsState(U32, Real);
        var target = U32;
        constrains.Fits(target).AssertTrue();
    }

    [Test]
    public void Fits_returnFalse() {
        //[...] Does not FITS into  [u8..Re]
        var constrains = new ConstrainsState(U8, Real);
        var target = new ConstrainsState();
        constrains.Fits(target).AssertFalse();
    }

    [Test]
    public void Fits_returnFalse2() {
        //u32 Does not FITS into  [u64..Re]
        var constrains = new ConstrainsState(U64, Real);
        var target = U32;
        constrains.Fits(target).AssertFalse();
    }

    [Test]
    public void ArrayFits_returnFalse() {
        //arr([u8..]) does not fit in  [arr(any)..]
        var constrains = new ConstrainsState(StateArray.Of(Any));
        var target = StateArray.Of(new ConstrainsState(U8));
        constrains.Fits(target).AssertFalse();
    }

    [Test]
    public void ArrayFits_returnTrue() {
        //arr([u32..]) FITS in  [arr(u8)..]
        var constrains = new ConstrainsState(StateArray.Of(U8));
        var target = StateArray.Of(new ConstrainsState(U32));
        constrains.Fits(target).AssertTrue();
    }

    [Test]
    public void ArrayFits_returnTrue2() {
        //arr([u8..Re]) FITS into  [arr([..])..]
        var constrains = new ConstrainsState(StateArray.Of(new ConstrainsState()));
        var target = StateArray.Of(
            TicNode.CreateInvisibleNode(new ConstrainsState(U8, Real)));
        constrains.Fits(target).AssertTrue();
    }

    [Test]
    public void ArrayFits_returnTrue3() {
        //arr([u32..]) FITS in  [arr(u8)..]
        var constrains = new ConstrainsState(StateArray.Of(U8));
        var target = StateArray.Of(U32);
        constrains.Fits(target).AssertTrue();
    }

    [Test]
    public void HiOrderFun_returnsFalse() {
        //(Any->(Any->U24))-> (Re->(Re->U24))
        var constrains = new ConstrainsState(StateArray.Of(new ConstrainsState()));
        constrains.Fits(StateFun.Of(new[] { Any }, Any)).AssertFalse();
    }

    [Test]
    public void HiOrderFun_returnsTrue() {
        var constrains = new ConstrainsState(StateFun.Of(new[] { Any }, U16));
        var target = StateFun.Of(new[] { Real }, U24);
        constrains.Fits(target).AssertTrue();
    }

    [Test]
    public void HiOrderFun_returnsTrue2() {
        var constrains = new ConstrainsState(StateFun.Of(new[] { new ConstrainsState() }, new ConstrainsState(U16)));
        var target = StateFun.Of(new[] { Any }, U16);
        constrains.Fits(target).AssertTrue();
    }

    [Test]
    public void HiOrderFun_returnsTrue3() {
        var constrains = new ConstrainsState(StateFun.Of(
            new[] { TicNode.CreateInvisibleNode(new ConstrainsState(desc: U16, anc: Real)) },
            TicNode.CreateInvisibleNode(new ConstrainsState(desc: U16, anc: Real))));
        var target = StateFun.Of(new[] { TicNode.CreateInvisibleNode(new ConstrainsState(desc: U32, anc: U64)) },
            TicNode.CreateInvisibleNode(new ConstrainsState(desc: U32, anc: U64)));
        constrains.Fits(target).AssertTrue();
    }

    [Test]
    public void HiOrderFun_returnsTrue4() {
        var constrains = new ConstrainsState(StateFun.Of(new[] { new ConstrainsState() }, new ConstrainsState()));
        var target = StateFun.Of(new[] { Any }, U16);
        constrains.Fits(target).AssertTrue();
    }

    [Test]
    public void HiOrderFun_returnsTrue5() {
        var constrains = new ConstrainsState(
            StateFun.Of(new[] { Any },
                StateFun.Of(new[] { Any },
                    StateFun.Of(new[] { Any },
                        U24))));
        var target =
            StateFun.Of(new[] { new ConstrainsState(U24, Real) },
                StateFun.Of(new[] { new ConstrainsState(U24, Real) },
                    StateFun.Of(new[] { new ConstrainsState(U24, Real) },
                        new ConstrainsState(U24, Real))));
        constrains.Fits(target).AssertTrue();
    }
}
