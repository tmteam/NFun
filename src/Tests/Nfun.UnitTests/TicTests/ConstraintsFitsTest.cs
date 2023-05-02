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
        var target = StateArray.Of(TicNode.CreateNamedNode("foo", new ConstrainsState(U8)));
        constrains.Fits(target).AssertFalse();
    }

    [Test]
    public void ArrayFits_returnTrue() {
        //arr([u32..]) FITS in  [arr(u8)..]
        var constrains = new ConstrainsState(StateArray.Of(U8));
        var target = StateArray.Of(TicNode.CreateNamedNode("foo", new ConstrainsState(U32)));
        constrains.Fits(target).AssertTrue();
    }

    [Test]
    public void ArrayFits_returnTrue2() {
        //arr([u8..Re]) FITS into  [arr([..])..]

        var constrains = new ConstrainsState(
            StateArray.Of(TicNode.CreateNamedNode("foo", new ConstrainsState())));
        var target =
            StateArray.Of(TicNode.CreateNamedNode("foo", new ConstrainsState(U8, Real)));
        constrains.Fits(target).AssertTrue();
    }

    [Test]
    public void HiOrderFun_returnsTrue() {
        //(Any->(Any->U24))-> (Re->(Re->U24))
        Assert.Fail("TODO");
    }

}
