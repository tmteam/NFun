using NFun.Tic.SolvingStates;
using NUnit.Framework;

namespace NFun.Tic.Tests.Arrays;

using static StatePrimitive;

public class ArrayGetCallTest {
    [Test(Description = "y = x[0]")]
    public void Generic() {
        //     2  0,1
        //y = get(x,0)
        var graph = new GraphBuilder();
        graph.SetVar("x", 0);
        graph.SetConst(1, I32);
        graph.SetArrGetCall(0, 1, 2);
        graph.SetDef("y", 2);
        var result = graph.Solve();
        var generic = result.AssertAndGetSingleGeneric(null, null);
        result.AssertNamedEqualToArrayOf(generic, "x");
        result.AssertAreGenerics(generic, "y");
    }

    [Test(Description = "y = [1,2][0]")]
    public void ConstrainsGeneric() {
        //     4  2 0,  1  3
        //y = get([ 1, -1],0)
        var graph = new GraphBuilder();
        graph.SetIntConst(0, U8);
        graph.SetIntConst(1, I16);
        graph.SetStrictArrayInit(2, 0, 1);
        graph.SetConst(3, I32);
        graph.SetArrGetCall(2, 3, 4);
        graph.SetDef("y", 4);
        var result = graph.Solve();
        var generic = result.AssertAndGetSingleGeneric(I16, Real);
        result.AssertAreGenerics(generic, "y");
    }

    [Test(Description = "y:char = x[0]")]
    public void ConcreteDef() {
        //          2  0,1
        //y:char = get(x,0)
        var graph = new GraphBuilder();
        graph.SetVar("x", 0);
        graph.SetConst(1, I32);
        graph.SetArrGetCall(0, 1, 2);
        graph.SetVarType("y", Char);
        graph.SetDef("y", 2);
        var result = graph.Solve();
        result.AssertNoGenerics();
        result.AssertNamedEqualToArrayOf(Char, "x");
        result.AssertNamed(Char, "y");
    }

    [Test(Description = "x:int[]; y = x[0]")]
    public void ConcreteArg() {
        //          2  0,1
        //x:int[]; y = get(x,0)
        var graph = new GraphBuilder();
        graph.SetVarType("x", StateArray.Of(I32));
        graph.SetVar("x", 0);
        graph.SetConst(1, I32);
        graph.SetArrGetCall(0, 1, 2);
        graph.SetDef("y", 2);
        var result = graph.Solve();
        result.AssertNoGenerics();
        result.AssertNamedEqualToArrayOf(I32, "x");
        result.AssertNamed(I32, "y");
    }

    [Test(Description = "x:int[]; y = x[0]")]
    public void ConcreteArgAndDef_Upcast() {
        //          2  0,1
        //x:int[]; y:real = get(x,0)
        var graph = new GraphBuilder();
        graph.SetVarType("x", StateArray.Of(I32));
        graph.SetVar("x", 0);
        graph.SetConst(1, I32);
        graph.SetArrGetCall(0, 1, 2);
        graph.SetVarType("y", Real);
        graph.SetDef("y", 2);
        var result = graph.Solve();
        result.AssertNoGenerics();
        result.AssertNamedEqualToArrayOf(I32, "x");
        result.AssertNamed(Real, "y");
    }

    [Test(Description = "x:int[]; y = x[0]")]
    public void ConcreteArgAndDef_Impossible() {
        //          2  0,1
        //x:real[]; y:int = get(x,0)
        var graph = new GraphBuilder();
        graph.SetVarType("x", StateArray.Of(Real));
        graph.SetVar("x", 0);
        graph.SetConst(1, I32);
        graph.SetArrGetCall(0, 1, 2);
        graph.SetVarType("y", I32);
        TestHelper.AssertThrowsTicError(
            () => {
                graph.SetDef("y", 2);
                graph.Solve();
            });
    }

    [Test(Description = "y = x[0][0]")]
    public void TwoDimentions_Generic() {
        //    4    2  0,1  3
        //y = get(get(x,0),0)
        var graph = new GraphBuilder();
        graph.SetVar("x", 0);
        graph.SetConst(1, I32);
        graph.SetArrGetCall(0, 1, 2);
        graph.SetConst(3, I32);
        graph.SetArrGetCall(2, 3, 4);
        graph.SetDef("y", 4);
        var result = graph.Solve();
        var generic = result.AssertAndGetSingleGeneric(null, null);
        result.AssertNamed(StateArray.Of(new StateArray(generic)), "x");
        result.AssertAreGenerics(generic, "y");
    }


    [Test(Description = "y:int = x[0][0]")]
    public void TwoDimentions_ConcreteDef() {
        //    4    2  0,1  3
        //y:int = get(get(x,0),0)
        var graph = new GraphBuilder();
        graph.SetVar("x", 0);
        graph.SetConst(1, I32);
        graph.SetArrGetCall(0, 1, 2);
        graph.SetConst(3, I32);
        graph.SetArrGetCall(2, 3, 4);
        graph.SetVarType("y", I32);
        graph.SetDef("y", 4);
        var result = graph.Solve();
        result.AssertNoGenerics();
        result.AssertNamed(StateArray.Of(StateArray.Of(I32)), "x");
        result.AssertNamed(I32, "y");
    }

    [Test(Description = "x:int[][]; y = x[0][0]")]
    public void TwoDimentions_ConcreteArg() {
        //    4    2  0,1  3
        //x:int[][]; y = get(get(x,0),0)
        var graph = new GraphBuilder();
        graph.SetVarType("x", StateArray.Of(StateArray.Of(I32)));
        graph.SetVar("x", 0);
        graph.SetConst(1, I32);
        graph.SetArrGetCall(0, 1, 2);
        graph.SetConst(3, I32);
        graph.SetArrGetCall(2, 3, 4);
        graph.SetDef("y", 4);
        var result = graph.Solve();
        result.AssertNoGenerics();
        result.AssertNamed(StateArray.Of(StateArray.Of(I32)), "x");
        result.AssertNamed(I32, "y");
    }

    [Test(Description = "x:int[][]; y:int = x[0][0]")]
    public void TwoDimentions_ConcreteArgAndDef() {
        //                   4    2  0,1  3
        //x:int[][]; y:int = get(get(x,0),0)
        var graph = new GraphBuilder();
        graph.SetVarType("x", StateArray.Of(StateArray.Of(I32)));
        graph.SetVar("x", 0);
        graph.SetConst(1, I32);
        graph.SetArrGetCall(0, 1, 2);
        graph.SetConst(3, I32);
        graph.SetArrGetCall(2, 3, 4);
        graph.SetVarType("y", I32);
        graph.SetDef("y", 4);
        var result = graph.Solve();
        result.AssertNoGenerics();
        result.AssertNamed(StateArray.Of(StateArray.Of(I32)), "x");
        result.AssertNamed(I32, "y");
    }

    [Test(Description = "x:int[][]; y:real = x[0][0]")]
    public void TwoDimentions_ConcreteArgAndDefWithUpcast() {
        //                    4    2  0,1  3
        //x:int[][]; y:real = get(get(x,0),0)
        var graph = new GraphBuilder();
        graph.SetVarType("x", StateArray.Of(StateArray.Of(I32)));
        graph.SetVar("x", 0);
        graph.SetConst(1, I32);
        graph.SetArrGetCall(0, 1, 2);
        graph.SetConst(3, I32);
        graph.SetArrGetCall(2, 3, 4);
        graph.SetVarType("y", Real);
        graph.SetDef("y", 4);
        var result = graph.Solve();
        result.AssertNoGenerics();
        result.AssertNamed(StateArray.Of(StateArray.Of(I32)), "x");
        result.AssertNamed(Real, "y");
    }

    [Test(Description = "x:int[]; y:i16 = x[0]")]
    public void OneDimention_ImpossibleConcreteArgAndDef() {
        //                  2  0,1
        //x:int[]; y:i16 = get(x,0)
        var graph = new GraphBuilder();
        graph.SetVarType("x", StateArray.Of(I32));
        graph.SetVar("x", 0);
        graph.SetConst(1, I32);
        graph.SetArrGetCall(0, 1, 2);
        TestHelper.AssertThrowsTicError(
            () => {
                graph.SetVarType("y", I16);
                graph.SetDef("y", 2);
                graph.Solve();
            });
    }

    [Test(Description = "x:int[][]; y:i16 = x[0][0]")]
    public void TwoDimentions_ImpossibleConcreteArgAndDef() {
        //                   4    2  0,1  3
        //x:int[][]; y:i16 = get(get(x,0),0)
        var graph = new GraphBuilder();
        graph.SetVarType("x", StateArray.Of(StateArray.Of(I32)));
        graph.SetVar("x", 0);
        graph.SetConst(1, I32);
        graph.SetArrGetCall(0, 1, 2);
        graph.SetConst(3, I32);
        graph.SetArrGetCall(2, 3, 4);
        TestHelper.AssertThrowsTicError(
            () => {
                graph.SetVarType("y", I16);
                graph.SetDef("y", 4);
                graph.Solve();
            });
    }

    [Test(Description = "x:int[][]; y:i16 = x[0][0]")]
    public void ThreeDimentions_ConcreteDefArrayOf() {
        //           4    2  0,1  3
        //y:real[] = get(get(x,0),0)
        var graph = new GraphBuilder();
        graph.SetVar("x", 0);
        graph.SetConst(1, I32);
        graph.SetArrGetCall(0, 1, 2);
        graph.SetConst(3, I32);
        graph.SetArrGetCall(2, 3, 4);
        graph.SetVarType("y", StateArray.Of(Real));
        graph.SetDef("y", 4);
        var result = graph.Solve();
        result.AssertNoGenerics();
        result.AssertNamed(StateArray.Of(StateArray.Of(StateArray.Of(Real))), "x");
        result.AssertNamed(StateArray.Of(Real), "y");
    }
}
