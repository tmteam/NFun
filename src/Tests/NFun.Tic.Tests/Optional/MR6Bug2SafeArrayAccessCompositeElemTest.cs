using NFun.Tic.SolvingStates;
using NUnit.Framework;

namespace NFun.Tic.Tests.Optional;

using static StatePrimitive;

/// <summary>
/// TIC-level isolation for MR6Bug2:
///   arr:int[][]? = none
///   out = arr?[0].count()        # Runtime: cast FunnyNone → IFunnyArray
///
/// Hypothesis from task: SetSafeArrayAccess uses LCA-with-None pattern, which
/// loses the Optional layer when elemType is composite.
///
/// FINDING (Agent B): At TIC level the hypothesis is FALSE. SetSafeArrayAccess
/// correctly produces opt(arr(int)) for `opt(arr(arr(int))) ?[ i32 ]`, including
/// 3-deep nesting and struct element types. The runtime cast crash therefore
/// originates downstream of TIC (likely ExpressionBuilder unwrapping the Optional
/// layer when binding `.count()` to its argument, or a missing Optional check
/// in SafeArrayAccessExpressionNode). All tests below PASS on master — they pin
/// the algebraic contract that any future "fix" must preserve.
///
/// The pattern: SetSafeArrayAccess(src, idx, res) where src state contains
/// composite element type. We solve, then assert result is the expected
/// `opt(elem)`.
/// </summary>
public class MR6Bug2SafeArrayAccessCompositeElemTest {

    /// <summary>
    /// Control: primitive elem type — should produce opt(I32).
    /// arr:int[]? = none; out = arr?[0]   →   out : opt(int)
    /// </summary>
    [Test]
    public void Primitive_ElemType_ResultIsOptInt() {
        var graph = new GraphBuilder();
        graph.SetVarType("arr", StateOptional.Of(StateArray.Of(I32)));
        graph.SetVar("arr", 0);
        graph.SetConst(1, I32);                  // index
        graph.SetSafeArrayAccess(0, 1, 2);
        graph.SetDef("out", 2);

        var result = graph.Solve();
        result.AssertNoGenerics();
        result.AssertNamed(StateOptional.Of(I32), "out");
    }

    /// <summary>
    /// Bug scope: opt(arr(arr(int))) ?[ i ] should produce opt(arr(int)).
    /// arr:int[][]? = none; out = arr?[0]   →   out : opt(int[])
    ///
    /// If SetSafeArrayAccess lost the Optional, result would be bare arr(int).
    /// </summary>
    [Test]
    public void ArrayElem_ResultIsOptArrInt() {
        var graph = new GraphBuilder();
        graph.SetVarType("arr", StateOptional.Of(StateArray.Of(StateArray.Of(I32))));
        graph.SetVar("arr", 0);
        graph.SetConst(1, I32);
        graph.SetSafeArrayAccess(0, 1, 2);
        graph.SetDef("out", 2);

        var result = graph.Solve();
        var outState = result.GetVariableNode("out").GetNonReference().State;
        TestContext.WriteLine($"out.State = {outState}");

        result.AssertNoGenerics();
        result.AssertNamed(StateOptional.Of(StateArray.Of(I32)), "out");
    }

    /// <summary>
    /// 3-deep composite: opt(arr(arr(arr(int)))) ?[ i ] should produce opt(arr(arr(int))).
    /// arr:int[][][]? = none; out = arr?[0]   →   out : opt(int[][])
    /// </summary>
    [Test]
    public void DoubleNestedArrayElem_ResultIsOptArrArrInt() {
        var graph = new GraphBuilder();
        graph.SetVarType("arr",
            StateOptional.Of(StateArray.Of(StateArray.Of(StateArray.Of(I32)))));
        graph.SetVar("arr", 0);
        graph.SetConst(1, I32);
        graph.SetSafeArrayAccess(0, 1, 2);
        graph.SetDef("out", 2);

        var result = graph.Solve();
        var outState = result.GetVariableNode("out").GetNonReference().State;
        TestContext.WriteLine($"out.State = {outState}");

        result.AssertNoGenerics();
        result.AssertNamed(
            StateOptional.Of(StateArray.Of(StateArray.Of(I32))), "out");
    }

    /// <summary>
    /// Struct elem control: opt(arr(struct{v:int})) ?[ i ] should produce opt(struct{v:int}).
    /// arr:{v:int}[]? = none; out = arr?[0]   →   out : opt({v:int})
    /// </summary>
    [Test]
    public void StructElem_ResultIsOptStruct() {
        var graph = new GraphBuilder();
        var s = StateStruct.Of("v", I32);
        graph.SetVarType("arr", StateOptional.Of(StateArray.Of(s)));
        graph.SetVar("arr", 0);
        graph.SetConst(1, I32);
        graph.SetSafeArrayAccess(0, 1, 2);
        graph.SetDef("out", 2);

        var result = graph.Solve();
        var outState = result.GetVariableNode("out").GetNonReference().State;
        TestContext.WriteLine($"out.State = {outState}");

        result.AssertNoGenerics();
        result.AssertNamed(StateOptional.Of(s), "out");
    }

    /// <summary>
    /// Source is literal None — SetSafeArrayAccess on None with composite elem.
    ///
    /// At TIC level there is no source variable type carrying arr(arr(int)). The
    /// element T is unconstrained, so result = LCA(T, None) collapses to opt(T).
    /// To pin T, we add a constraint: T ≤ arr(int) via an explicit ancestor wire
    /// (simulating the user annotating downstream with arr(int)).
    /// arr = none (typed opt(arr(arr(int)))); out = arr?[0]   →   out : opt(arr(int))
    /// </summary>
    [Test]
    public void NoneSource_WithExplicitOptArrArrIntType_ResultIsOptArrInt() {
        var graph = new GraphBuilder();
        // arr is declared opt(arr(arr(int))) and assigned None.
        graph.SetVarType("arr", StateOptional.Of(StateArray.Of(StateArray.Of(I32))));
        graph.SetConst(0, None);
        graph.SetDef("arr", 0);

        graph.SetVar("arr", 1);
        graph.SetConst(2, I32);
        graph.SetSafeArrayAccess(1, 2, 3);
        graph.SetDef("out", 3);

        var result = graph.Solve();
        var outState = result.GetVariableNode("out").GetNonReference().State;
        TestContext.WriteLine($"out.State = {outState}");

        result.AssertNoGenerics();
        result.AssertNamed(StateOptional.Of(StateArray.Of(I32)), "out");
    }
}
