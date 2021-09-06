using NFun.Tic.SolvingStates;
using NUnit.Framework;

namespace NFun.Tic.Tests.Arrays {

public class ArraySumCallTest {
    [Test(Description = "y = x.sum()")]
    public void Generic() {
        //     1  0
        //y = sum(x) 
        var graph = new GraphBuilder();
        graph.SetVar("x", 0);
        graph.SetSumCall(0, 1);
        graph.SetDef("y", 1);
        var result = graph.Solve();
        var generic = result.AssertAndGetSingleArithGeneric();
        result.AssertNamedEqualToArrayOf(generic, "x");
        result.AssertAreGenerics(generic, "y");
    }

    [Test(Description = "y = [1,-1].sum()")]
    public void ConstrainsGeneric() {
        //     3  2 0,  1  
        //y = sum([ 1, -1]) 
        var graph = new GraphBuilder();
        graph.SetIntConst(0, StatePrimitive.U8);
        graph.SetIntConst(1, StatePrimitive.I16);
        graph.SetStrictArrayInit(2, 0, 1);
        graph.SetSumCall(2, 3);
        graph.SetDef("y", 3);
        var result = graph.Solve();
        var generic = result.AssertAndGetSingleGeneric(StatePrimitive.I32, StatePrimitive.Real);
        result.AssertAreGenerics(generic, "y");
    }

    [Test(Description = "y:u32 = x.sum()")]
    public void ConcreteDefType() {
        //         1  0
        //y:u32 = sum(x) 
        var graph = new GraphBuilder();
        graph.SetVar("x", 0);
        graph.SetSumCall(0, 1);
        graph.SetVarType("y", StatePrimitive.U32);
        graph.SetDef("y", 1);
        var result = graph.Solve();
        result.AssertNoGenerics();
        result.AssertNamedEqualToArrayOf(StatePrimitive.U32, "x");
        result.AssertNamed(StatePrimitive.U32, "y");
    }

    [Test(Description = "y:char = x.sum()")]
    public void ImpossibleDefType_Throws() {
        //          1  0
        //y:char = sum(x) 

        var graph = new GraphBuilder();

        graph.SetVar("x", 0);
        graph.SetSumCall(0, 1);
        graph.SetVarType("y", StatePrimitive.Char);
        TestHelper.AssertThrowsTicError(() => {
            graph.SetDef("y", 1);
            graph.Solve();
        });
    }

    [Test(Description = "x:int[]; y = x.sum()")]
    public void ConcreteArg() {
        //               2 0
        //x:int[]; y = sum(x) 
        var graph = new GraphBuilder();
        graph.SetVarType("x", StateArray.Of(StatePrimitive.I32));
        graph.SetVar("x", 0);
        graph.SetSumCall(0, 1);
        graph.SetDef("y", 1);
        var result = graph.Solve();
        result.AssertNoGenerics();
        result.AssertNamedEqualToArrayOf(StatePrimitive.I32, "x");
        result.AssertNamed(StatePrimitive.I32, "y");
    }

    [Test(Description = "x:int[]; y:real = x.sum()")]
    public void ConcreteArgAndDef_Upcast() {
        //                   2  0
        //x:int[]; y:real = sum(x) 
        var graph = new GraphBuilder();
        graph.SetVarType("x", StateArray.Of(StatePrimitive.I32));
        graph.SetVar("x", 0);
        graph.SetSumCall(0, 1);
        graph.SetVarType("y", StatePrimitive.Real);
        graph.SetDef("y", 1);
        var result = graph.Solve();
        result.AssertNoGenerics();
        result.AssertNamedEqualToArrayOf(StatePrimitive.I32, "x");
        result.AssertNamed(StatePrimitive.Real, "y");
    }

    [Test(Description = "x:real[]; y:int = x[0]")]
    public void Impossible_ConcreteArgAndDef_throws() {
        //                   1  0
        //x:real[]; y:int = sum(x) 
        var graph = new GraphBuilder();
        graph.SetVarType("x", StateArray.Of(StatePrimitive.Real));
        graph.SetVar("x", 0);
        graph.SetSumCall(0, 1);
        graph.SetVarType("y", StatePrimitive.I32);
        TestHelper.AssertThrowsTicError(() => {
            graph.SetDef("y", 1);
            graph.Solve();
        });
    }
}

}