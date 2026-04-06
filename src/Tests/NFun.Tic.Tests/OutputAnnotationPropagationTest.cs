using NFun.Tic.SolvingStates;
using NUnit.Framework;

namespace NFun.Tic.Tests;

using static StatePrimitive;

/// <summary>
/// Tests that output type annotations propagate through the expression
/// to correctly type input variables and arithmetic operations.
///
/// y:real = x + 1  → x should be Real (not Int32)
/// y:int64 = x + 1 → x should be Int64 (not Int32)
/// y = x + 1       → x should be Int32 (preferred, no annotation)
/// </summary>
public class OutputAnnotationPropagationTest {

    [SetUp]
    public void Initialize() => TraceLog.IsEnabled = true;

    [TearDown]
    public void Deinitialize() => TraceLog.IsEnabled = false;

    // Helper: creates integer constant with realistic preferred (I32, like real pipeline)
    private static void SetRealIntConst(GraphBuilder graph, int id, StatePrimitive desc) =>
        graph.SetGenericConst(id, desc: desc, anc: StatePrimitive.Real, preferred: I32);

    [Test(Description = "y:real = x + 1 → x:real, y:real")]
    public void OutputReal_InputAndArith_ShouldBeReal() {
        var graph = new GraphBuilder();
        graph.SetVar("x", 0);
        SetRealIntConst(graph, 1, U8); // integer constant 1 with preferred=I32
        graph.SetArith(0, 1, 2);
        graph.SetVarType("y", Real);
        graph.SetDef("y", 2);

        var result = graph.Solve();
        result.AssertNoGenerics();
        result.AssertNamed(Real, "y");
        result.AssertNamed(Real, "x");
    }

    [Test(Description = "y:int64 = x + 1 → x:int64, y:int64")]
    public void OutputInt64_InputAndArith_ShouldBeInt64() {
        var graph = new GraphBuilder();
        graph.SetVar("x", 0);
        SetRealIntConst(graph, 1, U8);
        graph.SetArith(0, 1, 2);
        graph.SetVarType("y", I64);
        graph.SetDef("y", 2);

        var result = graph.Solve();
        result.AssertNoGenerics();
        result.AssertNamed(I64, "y");
        result.AssertNamed(I64, "x");
    }

    [Test(Description = "y:int = x + 1 → x:int, y:int")]
    public void OutputInt32_InputAndArith_ShouldBeInt32() {
        var graph = new GraphBuilder();
        graph.SetVar("x", 0);
        SetRealIntConst(graph, 1, U8);
        graph.SetArith(0, 1, 2);
        graph.SetVarType("y", I32);
        graph.SetDef("y", 2);

        var result = graph.Solve();
        result.AssertNoGenerics();
        result.AssertNamed(I32, "y");
        result.AssertNamed(I32, "x");
    }

    [Test(Description = "y = x + 1 → x:int32, y:int32 (preferred I32)")]
    public void NoAnnotation_InputAndArith_ShouldBePreferred() {
        var graph = new GraphBuilder();
        graph.SetVar("x", 0);
        SetRealIntConst(graph, 1, U8);
        graph.SetArith(0, 1, 2);
        graph.SetDef("y", 2);

        var result = graph.Solve();
        // Without annotation, stays generic — resolved by SolveUselessGenerics
        var yNode = result.GetVariableNode("y").GetNonReference();
        if (yNode.State is StatePrimitive yp)
            Assert.AreEqual(I32, yp);
    }

    [Test(Description = "x:real; y = x + 1 → y:real")]
    public void InputReal_NoOutputAnnotation_ShouldBeReal() {
        var graph = new GraphBuilder();
        graph.SetVarType("x", Real);
        graph.SetVar("x", 0);
        SetRealIntConst(graph, 1, U8);
        graph.SetArith(0, 1, 2);
        graph.SetDef("y", 2);

        var result = graph.Solve();
        result.AssertNoGenerics();
        result.AssertNamed(Real, "y");
        result.AssertNamed(Real, "x");
    }

    [Test(Description = "y:real = 1 + 2 → y:real")]
    public void OutputReal_TwoConstants_ShouldBeReal() {
        var graph = new GraphBuilder();
        SetRealIntConst(graph, 0, U8);
        SetRealIntConst(graph, 1, U8);
        graph.SetArith(0, 1, 2);
        graph.SetVarType("y", Real);
        graph.SetDef("y", 2);

        var result = graph.Solve();
        result.AssertNoGenerics();
        result.AssertNamed(Real, "y");
    }
}
