using NFun.Tic.SolvingStates;
using NUnit.Framework;

namespace NFun.Tic.Tests;

using static StatePrimitive;

//& | ~ << >>
class BitOperations {
    [Test]
    public void InvertConstants() {
        //y = ~1u
        var graph = new GraphBuilder();
        graph.SetConst(0, U32);
        graph.SetBitwiseInvert(0, 1);
        graph.SetDef("y", 1);
        var result = graph.Solve();
        result.AssertNoGenerics();
        result.AssertNamed(U32, "y");
    }

    [Test]
    public void InvertGenericConstants() {
        //y = ~1
        var graph = new GraphBuilder();
        graph.SetIntConst(0, U8);
        graph.SetBitwiseInvert(0, 1);
        graph.SetDef("y", 1);
        var result = graph.Solve();
        var generic = result.AssertAndGetSingleGeneric(U8, I96);
        result.AssertAreGenerics(generic, "y");
    }

    [Test]
    public void InvertGenericNamed() {
        //y = ~x
        var graph = new GraphBuilder();
        graph.SetVar("x", 0);
        graph.SetBitwiseInvert(0, 1);
        graph.SetDef("y", 1);
        var result = graph.Solve();
        var generic = result.AssertAndGetSingleGeneric(U8, I96);
        result.AssertAreGenerics(generic, "x", "y");
    }

    [Test]
    public void BitwiseConstants() {
        //    0  2 1
        //y = 1u & 2u
        var graph = new GraphBuilder();
        graph.SetConst(0, U32);
        graph.SetConst(1, U32);
        graph.SetBitwise(0, 1, 2);
        graph.SetDef("y", 2);
        var result = graph.Solve();
        result.AssertNoGenerics();
        result.AssertNamed(U32, "y");
    }


    [Test]
    public void BitwiseDifferentConstants() {
        //    0  2 1
        //y = 1u & 2i — LCA(U32,I32) = I48, constraint [I48..I96], resolves to I64 at runtime
        var graph = new GraphBuilder();
        graph.SetConst(0, U32);
        graph.SetConst(1, I32);
        graph.SetBitwise(0, 1, 2);
        graph.SetDef("y", 2);
        var result = graph.Solve();
        // At TIC level, y is a generic [I48..I96]. Runtime resolves to I64.
        var generic = result.AssertAndGetSingleGeneric(I48, I96);
        result.AssertAreGenerics(generic, "y");
    }

    [Test]
    public void BitwiseGenericAndConstant() {
        //    0 2 1
        //y = 1 & x
        var graph = new GraphBuilder();
        graph.SetIntConst(0, U8);
        graph.SetVar("x", 1);
        graph.SetBitwise(0, 1, 2);
        graph.SetDef("y", 2);
        var result = graph.Solve();
        var generic = result.AssertAndGetSingleGeneric(U8, I96);
        result.AssertAreGenerics(generic, "x", "y");
    }

    [Test]
    public void BitwiseNamedAndConstant() {
        //    0 2 1
        //y = 1i & x
        var graph = new GraphBuilder();
        graph.SetConst(0, I32);
        graph.SetVar("x", 1);
        graph.SetBitwise(0, 1, 2);
        graph.SetDef("y", 2);
        var result = graph.Solve();
        result.AssertNoGenerics();
        result.AssertNamed(I32, "x", "y");
    }

    [Test]
    public void BitwiseComplexGenericEquation() {
        //    0 2 1 4   3 6 5
        //y = 1 & x | 256 | a
        var graph = new GraphBuilder();
        graph.SetIntConst(0, U8);
        graph.SetVar("x", 1);
        graph.SetBitwise(0, 1, 2);
        graph.SetIntConst(3, U12);
        graph.SetBitwise(2, 3, 4);
        graph.SetVar("a", 5);
        graph.SetBitwise(4, 5, 6);
        graph.SetDef("y", 6);
        var result = graph.Solve();
        var generic = result.AssertAndGetSingleGeneric(U12, I96);
        result.AssertAreGenerics(generic, "a", "x", "y");
    }

    [Test]
    public void BitwiseComplexGenericEquation2() {
        //    0 2 1 4   3 6 5
        //y = 1 & x | -1 | a
        var graph = new GraphBuilder();
        graph.SetIntConst(0, U8);
        graph.SetVar("x", 1);
        graph.SetBitwise(0, 1, 2);
        graph.SetIntConst(3, I16);
        graph.SetBitwise(2, 3, 4);
        graph.SetVar("a", 5);
        graph.SetBitwise(4, 5, 6);
        graph.SetDef("y", 6);
        var result = graph.Solve();
        var generic = result.AssertAndGetSingleGeneric(I16, I96);
        result.AssertAreGenerics(generic, "a", "x", "y");
    }

    [Test]
    public void BitwiseGenericConstants() {
        //    0 2 1
        //y = 1 & 2
        var graph = new GraphBuilder();
        graph.SetIntConst(0, U8);
        graph.SetIntConst(1, U8);
        graph.SetBitwise(0, 1, 2);
        graph.SetDef("y", 2);
        var result = graph.Solve();
        var generic = result.AssertAndGetSingleGeneric(U8, I96);
        result.AssertAreGenerics(generic, "y");
    }

    [Test]
    //[Ignore("Generic constants")]
    public void BitshiftGenericAndConstant() {
        //    0  2 1
        //y = 1 << x
        var graph = new GraphBuilder();
        graph.SetIntConst(0, U8);
        graph.SetVar("x", 1);
        graph.SetBitShift(0, 1, 2);
        graph.SetDef("y", 2);
        var result = graph.Solve();
        var generic = result.AssertAndGetSingleGeneric(U24, I96);
        result.AssertNamed(I48, "x");
        result.AssertAreGenerics(generic, "y");
    }

    [Test]
    //[Ignore("Generic constants")]
    public void BitshiftConstants() {
        //    0  2 1
        //y = 1i << 2
        var graph = new GraphBuilder();
        graph.SetConst(0, I32);
        graph.SetIntConst(1, U8);
        graph.SetBitShift(0, 1, 2);
        graph.SetDef("y", 2);
        var result = graph.Solve();
        result.AssertNoGenerics();
        result.AssertNamed(I32, "y");
    }

    // ── Mixed signed/unsigned bitwise: valid combinations ──────────
    // LCA(U16,I16) = I24, LCA(U32,I32) = I48 — both fit in [Ixxx..I96]

    [Test]
    public void BitwiseMixed_U16_I16() {
        // y = a:u16 & b:i16 — LCA(U16,I16) = I24, interval [I24..I96]
        var graph = new GraphBuilder();
        graph.SetConst(0, U16);
        graph.SetConst(1, I16);
        graph.SetBitwise(0, 1, 2);
        graph.SetDef("y", 2);
        var result = graph.Solve();
        var generic = result.AssertAndGetSingleGeneric(I24, I96);
        result.AssertAreGenerics(generic, "y");
    }

    [Test]
    public void BitwiseMixed_U32_I32() {
        // y = a:u32 & b:i32 — LCA(U32,I32) = I48, interval [I48..I96]
        var graph = new GraphBuilder();
        graph.SetConst(0, U32);
        graph.SetConst(1, I32);
        graph.SetBitwise(0, 1, 2);
        graph.SetDef("y", 2);
        var result = graph.Solve();
        var generic = result.AssertAndGetSingleGeneric(I48, I96);
        result.AssertAreGenerics(generic, "y");
    }

    // ── Mixed signed/unsigned bitwise: incompatible (abstract point) ──

    [Test]
    public void BitwiseMixed_U64_I64_Fails() {
        // y = a:u64 & b:i64 — LCA(U64,I64) = I96, interval [I96..I96] = abstract point → error
        var graph = new GraphBuilder();
        graph.SetConst(0, U64);
        graph.SetConst(1, I64);
        graph.SetBitwise(0, 1, 2);
        graph.SetDef("y", 2);
        Assert.Catch(() => graph.Solve()); // TicIncompatibleAncestorSyntaxNodeException
    }

    // ── Widening through bitwise chain ────────────────────────────

    [Test]
    public void BitwiseChain_U8_I16_U32() {
        // y = (1u8 & -1i16) | x:u32 — desc widens through chain
        var graph = new GraphBuilder();
        graph.SetConst(0, U8);
        graph.SetConst(1, I16);
        graph.SetBitwise(0, 1, 2);
        graph.SetConst(3, U32);
        graph.SetBitwise(2, 3, 4);
        graph.SetDef("y", 4);
        var result = graph.Solve();
        // LCA(U8,I16)=I16, then LCA(I16,U32)=I48. Interval [I48..I96]
        var generic = result.AssertAndGetSingleGeneric(I48, I96);
        result.AssertAreGenerics(generic, "y");
    }
}
