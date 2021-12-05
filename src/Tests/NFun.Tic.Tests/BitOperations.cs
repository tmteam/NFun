using NFun.Tic.SolvingStates;
using NUnit.Framework;

namespace NFun.Tic.Tests {

//& | ~ << >>
class BitOperations {
    [Test]
    public void InvertConstants() {
        //y = ~1u 
        var graph = new GraphBuilder();
        graph.SetConst(0, StatePrimitive.U32);
        graph.SetBitwiseInvert(0, 1);
        graph.SetDef("y", 1);
        var result = graph.Solve();
        result.AssertNoGenerics();
        result.AssertNamed(StatePrimitive.U32, "y");
    }

    [Test]
    public void InvertGenericConstants() {
        //y = ~1 
        var graph = new GraphBuilder();
        graph.SetIntConst(0, StatePrimitive.U8);
        graph.SetBitwiseInvert(0, 1);
        graph.SetDef("y", 1);
        var result = graph.Solve();
        var generic = result.AssertAndGetSingleGeneric(StatePrimitive.U8, StatePrimitive.I96);
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
        var generic = result.AssertAndGetSingleGeneric(StatePrimitive.U8, StatePrimitive.I96);
        result.AssertAreGenerics(generic, "x", "y");
    }

    [Test]
    public void BitwiseConstants() {
        //    0  2 1
        //y = 1u & 2u
        var graph = new GraphBuilder();
        graph.SetConst(0, StatePrimitive.U32);
        graph.SetConst(1, StatePrimitive.U32);
        graph.SetBitwise(0, 1, 2);
        graph.SetDef("y", 2);
        var result = graph.Solve();
        result.AssertNoGenerics();
        result.AssertNamed(StatePrimitive.U32, "y");
    }


    [Test]
    [Ignore("Select concrete integer type")]
    public void BitwiseDifferentConstants() {
        //    0  2 1
        //y = 1u & 2i
        var graph = new GraphBuilder();
        graph.SetConst(0, StatePrimitive.U32);
        graph.SetConst(1, StatePrimitive.I32);
        graph.SetBitwise(0, 1, 2);
        graph.SetDef("y", 2);
        var result = graph.Solve();
        result.AssertNoGenerics();
        result.AssertNamed(StatePrimitive.I64, "y");
    }

    [Test]
    public void BitwiseGenericAndConstant() {
        //    0 2 1
        //y = 1 & x
        var graph = new GraphBuilder();
        graph.SetIntConst(0, StatePrimitive.U8);
        graph.SetVar("x", 1);
        graph.SetBitwise(0, 1, 2);
        graph.SetDef("y", 2);
        var result = graph.Solve();
        var generic = result.AssertAndGetSingleGeneric(StatePrimitive.U8, StatePrimitive.I96);
        result.AssertAreGenerics(generic, "x", "y");
    }

    [Test]
    public void BitwiseNamedAndConstant() {
        //    0 2 1
        //y = 1i & x
        var graph = new GraphBuilder();
        graph.SetConst(0, StatePrimitive.I32);
        graph.SetVar("x", 1);
        graph.SetBitwise(0, 1, 2);
        graph.SetDef("y", 2);
        var result = graph.Solve();
        result.AssertNoGenerics();
        result.AssertNamed(StatePrimitive.I32, "x", "y");
    }

    [Test]
    public void BitwiseComplexGenericEquation() {
        //    0 2 1 4   3 6 5
        //y = 1 & x | 256 | a
        var graph = new GraphBuilder();
        graph.SetIntConst(0, StatePrimitive.U8);
        graph.SetVar("x", 1);
        graph.SetBitwise(0, 1, 2);
        graph.SetIntConst(3, StatePrimitive.U12);
        graph.SetBitwise(2, 3, 4);
        graph.SetVar("a", 5);
        graph.SetBitwise(4, 5, 6);
        graph.SetDef("y", 6);
        var result = graph.Solve();
        var generic = result.AssertAndGetSingleGeneric(StatePrimitive.U12, StatePrimitive.I96);
        result.AssertAreGenerics(generic, "a", "x", "y");
    }

    [Test]
    public void BitwiseComplexGenericEquation2() {
        //    0 2 1 4   3 6 5
        //y = 1 & x | -1 | a
        var graph = new GraphBuilder();
        graph.SetIntConst(0, StatePrimitive.U8);
        graph.SetVar("x", 1);
        graph.SetBitwise(0, 1, 2);
        graph.SetIntConst(3, StatePrimitive.I16);
        graph.SetBitwise(2, 3, 4);
        graph.SetVar("a", 5);
        graph.SetBitwise(4, 5, 6);
        graph.SetDef("y", 6);
        var result = graph.Solve();
        var generic = result.AssertAndGetSingleGeneric(StatePrimitive.I16, StatePrimitive.I96);
        result.AssertAreGenerics(generic, "a", "x", "y");
    }

    [Test]
    public void BitwiseGenericConstants() {
        //    0 2 1
        //y = 1 & 2
        var graph = new GraphBuilder();
        graph.SetIntConst(0, StatePrimitive.U8);
        graph.SetIntConst(1, StatePrimitive.U8);
        graph.SetBitwise(0, 1, 2);
        graph.SetDef("y", 2);
        var result = graph.Solve();
        var generic = result.AssertAndGetSingleGeneric(StatePrimitive.U8, StatePrimitive.I96);
        result.AssertAreGenerics(generic, "y");
    }

    [Test]
    //[Ignore("Generic constants")]
    public void BitshiftGenericAndConstant() {
        //    0  2 1
        //y = 1 << x
        var graph = new GraphBuilder();
        graph.SetIntConst(0, StatePrimitive.U8);
        graph.SetVar("x", 1);
        graph.SetBitShift(0, 1, 2);
        graph.SetDef("y", 2);
        var result = graph.Solve();
        var generic = result.AssertAndGetSingleGeneric(StatePrimitive.U24, StatePrimitive.I96);
        result.AssertNamed(StatePrimitive.I48, "x");
        result.AssertAreGenerics(generic, "y");
    }

    [Test]
    //[Ignore("Generic constants")]
    public void BitshiftConstants() {
        //    0  2 1
        //y = 1i << 2
        var graph = new GraphBuilder();
        graph.SetConst(0, StatePrimitive.I32);
        graph.SetIntConst(1, StatePrimitive.U8);
        graph.SetBitShift(0, 1, 2);
        graph.SetDef("y", 2);
        var result = graph.Solve();
        result.AssertNoGenerics();
        result.AssertNamed(StatePrimitive.I32, "y");
    }
}

}