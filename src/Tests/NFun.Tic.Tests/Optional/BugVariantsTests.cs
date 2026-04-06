using NFun.Tic.SolvingStates;
using NUnit.Framework;

namespace NFun.Tic.Tests.Optional;

using static StatePrimitive;

/// <summary>
/// Variant tests probing 3 known Optional bugs in different contexts.
/// These complement the original failing tests in PullNoneNodeTests and OptionalCompositeTests.
/// </summary>
class BugVariantsTests {

    #region Bug A variants: None into non-optional param should fail

    [Test(Description = "Bug A: f(x:real):bool; f(none) → TicError")]
    public void BugA_NoneIntoNonOptionalReal_ShouldFail() {
        TestHelper.AssertThrowsTicError(() => {
            var graph = new GraphBuilder();
            var funType = StateFun.Of(Real, Bool);
            graph.SetVarType("f", funType);
            graph.SetVar("f", 0);
            graph.SetConst(1, None);
            graph.SetCall(0, 1, 2);
            graph.SetDef("y", 2);
            graph.Solve();
        });
    }

    [Test(Description = "Bug A: f(x:i32, y:i32):bool; f(none, 1) → TicError")]
    public void BugA_NoneInFirstArgOfMultiArgFunc_ShouldFail() {
        TestHelper.AssertThrowsTicError(() => {
            var graph = new GraphBuilder();
            var funType = StateFun.Of(I32, I32, Bool);
            graph.SetVarType("f", funType);
            graph.SetVar("f", 0);
            graph.SetConst(1, None);
            graph.SetConst(2, I32);
            graph.SetCall(0, 1, 2, 3);
            graph.SetDef("y", 3);
            graph.Solve();
        });
    }

    [Test(Description = "Bug A boundary: x:i32 = none → correctly fails (SetDef catches it)")]
    public void BugA_DirectAssignNoneToI32_CorrectlyFails() {
        TestHelper.AssertThrowsTicError(() => {
            var graph = new GraphBuilder();
            graph.SetVarType("x", I32);
            graph.SetConst(0, None);
            graph.SetDef("x", 0);
            graph.Solve();
        });
    }

    [Test(Description = "Bug A sanity: f(x:opt(i32)):bool; f(none) → OK")]
    public void BugA_NoneToOptionalParam_Succeeds() {
        var graph = new GraphBuilder();
        var funType = StateFun.Of(StateOptional.Of(I32), Bool);
        graph.SetVarType("f", funType);
        graph.SetVar("f", 0);
        graph.SetConst(1, None);
        graph.SetCall(0, 1, 2);
        graph.SetDef("y", 2);

        var result = graph.Solve();
        result.AssertNoGenerics();
        result.AssertNamed(Bool, "y");
    }

    #endregion

    #region Bug B variants: if-else with unconstrained generic + none

    [Test(Description = "Bug B: y = if(a) x else none; z = y + 1 → should fail (opt not arithmetic)")]
    public void BugB_GenericPlusNone_ThenArithmetic() {
        var graph = new GraphBuilder();
        graph.SetVar("a", 0);
        graph.SetVar("x", 1);
        graph.SetConst(2, None);
        graph.SetIfElse(new[] { 0 }, new[] { 1, 2 }, 3);
        graph.SetDef("y", 3);
        graph.SetVar("y", 4);
        graph.SetIntConst(5, U8);
        graph.SetArith(4, 5, 6);
        graph.SetDef("z", 6);

        TestHelper.AssertThrowsTicError(() => { graph.Solve(); });
    }

    [Test(Description = "Bug B boundary: [x, none] → array element is StateOptional (works correctly)")]
    public void BugB_ArrayLiteralGenericPlusNone_Works() {
        var graph = new GraphBuilder();
        graph.SetVar("x", 0);
        graph.SetConst(1, None);
        graph.SetSoftArrayInit(2, 0, 1);
        graph.SetDef("arr", 2);

        var result = graph.Solve();
        var arrNode = result.GetVariableNode("arr").GetNonReference();
        Assert.IsInstanceOf<StateArray>(arrNode.State);
        var elemNode = ((StateArray)arrNode.State).ElementNode.GetNonReference();
        Assert.IsInstanceOf<StateOptional>(elemNode.State,
            $"Array element should be StateOptional but got {elemNode.State}");
    }

    [Test(Description = "Bug B reversed: y = if(a) none else x → y should be StateOptional")]
    public void BugB_IfElseNoneAndGeneric_ResultShouldBeOptional() {
        var graph = new GraphBuilder();
        graph.SetVar("c", 0);
        graph.SetConst(1, None);
        graph.SetVar("x", 2);
        graph.SetIfElse(new[] { 0 }, new[] { 1, 2 }, 3);
        graph.SetDef("y", 3);

        var result = graph.Solve();
        var yNode = result.GetVariableNode("y").GetNonReference();
        Assert.IsInstanceOf<StateOptional>(yNode.State,
            $"y should be StateOptional but got {yNode.State}");
    }

    [Test(Description = "Bug B with typing: z = if(c) x else none; y:opt(i32) = z → works")]
    public void BugB_GenericNone_ThenConsumedAsOptI32() {
        var graph = new GraphBuilder();
        graph.SetVar("c", 0);
        graph.SetVar("x", 1);
        graph.SetConst(2, None);
        graph.SetIfElse(new[] { 0 }, new[] { 1, 2 }, 3);
        graph.SetDef("z", 3);
        graph.SetVarType("y", StateOptional.Of(I32));
        graph.SetVar("z", 4);
        graph.SetDef("y", 4);

        var result = graph.Solve();
        result.AssertNoGenerics();
        result.AssertNamed(StateOptional.Of(I32), "z");
        result.AssertNamed(StateOptional.Of(I32), "y");
    }

    #endregion

    #region Bug C variants: widening leak through Optional

    [Test(Description = "Bug C: z = if(a) 42i else none; y:opt(i64) = z → z stays opt(i32)")]
    public void BugC_WideningLeak_I32ToI64() {
        var graph = new GraphBuilder();
        graph.SetVar("a", 0);
        graph.SetConst(1, I32);
        graph.SetConst(2, None);
        graph.SetIfElse(new[] { 0 }, new[] { 1, 2 }, 3);
        graph.SetDef("z", 3);
        graph.SetVarType("y", StateOptional.Of(I64));
        graph.SetVar("z", 4);
        graph.SetDef("y", 4);

        var result = graph.Solve();
        result.AssertNoGenerics();
        result.AssertNamed(StateOptional.Of(I32), "z");
        result.AssertNamed(StateOptional.Of(I64), "y");
    }

    [Test(Description = "Bug C: z = if(a) true else none; y:opt(any) = z → z stays opt(bool)")]
    public void BugC_WideningLeak_BoolToAny() {
        var graph = new GraphBuilder();
        graph.SetVar("a", 0);
        graph.SetConst(1, Bool);
        graph.SetConst(2, None);
        graph.SetIfElse(new[] { 0 }, new[] { 1, 2 }, 3);
        graph.SetDef("z", 3);
        graph.SetVarType("y", StateOptional.Of(Any));
        graph.SetVar("z", 4);
        graph.SetDef("y", 4);

        var result = graph.Solve();
        result.AssertNoGenerics();
        result.AssertNamed(StateOptional.Of(Bool), "z");
        result.AssertNamed(StateOptional.Of(Any), "y");
    }

    [Test(Description = "Bug C boundary: z = if(a) 42i else none → z=opt(i32) without assignment (works)")]
    public void BugC_OptI32_WithoutAssignment_Works() {
        var graph = new GraphBuilder();
        graph.SetVar("a", 0);
        graph.SetConst(1, I32);
        graph.SetConst(2, None);
        graph.SetIfElse(new[] { 0 }, new[] { 1, 2 }, 3);
        graph.SetDef("z", 3);

        var result = graph.Solve();
        result.AssertNoGenerics();
        result.AssertNamed(StateOptional.Of(I32), "z");
    }

    #endregion
}
