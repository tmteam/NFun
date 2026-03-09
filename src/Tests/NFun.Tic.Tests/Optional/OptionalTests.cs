using NFun.Tic.SolvingStates;
using NUnit.Framework;

namespace NFun.Tic.Tests.Optional;

using static StatePrimitive;

class OptionalTests {

    #region Group 1: Propagation of type annotations

    [Test(Description = "x:opt(i32); y = x → y = opt(i32)")]
    public void OptionalVar_PropagatesType() {
        //     1 0
        // y = x
        var graph = new GraphBuilder();
        graph.SetVarType("x", StateOptional.Of(I32));
        graph.SetVar("x", 0);
        graph.SetDef("y", 0);

        var result = graph.Solve();
        result.AssertNoGenerics();
        result.AssertNamed(StateOptional.Of(I32), "y");
        result.AssertNamed(StateOptional.Of(I32), "x");
    }

    [Test(Description = "x:opt(i32); y:opt(i32) = x → both opt(i32)")]
    public void OptionalVar_AssignToSameType() {
        var graph = new GraphBuilder();
        graph.SetVarType("x", StateOptional.Of(I32));
        graph.SetVarType("y", StateOptional.Of(I32));
        graph.SetVar("x", 0);
        graph.SetDef("y", 0);

        var result = graph.Solve();
        result.AssertNoGenerics();
        result.AssertNamed(StateOptional.Of(I32), "x");
        result.AssertNamed(StateOptional.Of(I32), "y");
    }

    [Test(Description = "x:opt(i32); y:opt(bool) = x → TicError")]
    public void OptionalVar_IncompatibleTypes() {
        TestHelper.AssertThrowsTicError(() => {
            var graph = new GraphBuilder();
            graph.SetVarType("x", StateOptional.Of(I32));
            graph.SetVarType("y", StateOptional.Of(Bool));
            graph.SetVar("x", 0);
            graph.SetDef("y", 0);
            graph.Solve();
        });
    }

    #endregion

    #region Group 2: None constant

    [Test(Description = "y = none → y = None")]
    public void NoneConst_Standalone() {
        //     0
        // y = none
        var graph = new GraphBuilder();
        graph.SetConst(0, None);
        graph.SetDef("y", 0);

        var result = graph.Solve();
        result.AssertNoGenerics();
        result.AssertNamed(None, "y");
    }

    [Test(Description = "y:opt(i32) = none → y = opt(i32)")]
    public void NoneConst_AssignToOptionalVar() {
        //     0
        // y:opt(i32) = none
        var graph = new GraphBuilder();
        graph.SetVarType("y", StateOptional.Of(I32));
        graph.SetConst(0, None);
        graph.SetDef("y", 0);

        var result = graph.Solve();
        result.AssertNoGenerics();
        result.AssertNamed(StateOptional.Of(I32), "y");
    }

    #endregion

    #region Group 3: If-else with None + concrete

    [Test(Description = "y = if(a) 42i else none → y = opt(i32)")]
    public void IfElse_ConcreteOrNone() {
        //     3  0  1      2
        // y = if(a) 42i else none
        var graph = new GraphBuilder();
        graph.SetVar("a", 0);
        graph.SetConst(1, I32);
        graph.SetConst(2, None);
        graph.SetIfElse(new[] { 0 }, new[] { 1, 2 }, 3);
        graph.SetDef("y", 3);

        var result = graph.Solve();
        result.AssertNoGenerics();
        result.AssertNamed(StateOptional.Of(I32), "y");
    }

    [Test(Description = "y = if(a) none else 42i → y = opt(i32)")]
    public void IfElse_NoneOrConcrete() {
        //     3  0  1         2
        // y = if(a) none else 42i
        var graph = new GraphBuilder();
        graph.SetVar("a", 0);
        graph.SetConst(1, None);
        graph.SetConst(2, I32);
        graph.SetIfElse(new[] { 0 }, new[] { 1, 2 }, 3);
        graph.SetDef("y", 3);

        var result = graph.Solve();
        result.AssertNoGenerics();
        result.AssertNamed(StateOptional.Of(I32), "y");
    }

    [Test(Description = "y = if(a) 1 else none → y = opt(U8)")]
    public void IfElse_IntConstOrNone() {
        //     3  0  1      2
        // y = if(a) 1 else none
        var graph = new GraphBuilder();
        graph.SetVar("a", 0);
        graph.SetIntConst(1, U8);
        graph.SetConst(2, None);
        graph.SetIfElse(new[] { 0 }, new[] { 1, 2 }, 3);
        graph.SetDef("y", 3);

        var result = graph.Solve();
        // LCA(int_const[U8..Real], None) = opt(U8) — solver uses most concrete descendant.
        // Since opt(U8) is solved, it collapses from generic [opt(U8)..] to concrete opt(U8).
        result.AssertNoGenerics();
        result.AssertNamed(StateOptional.Of(U8), "y");
    }

    [Test(Description = "y = if(a) none else none → y = None")]
    public void IfElse_NoneOrNone() {
        //     3  0  1         2
        // y = if(a) none else none
        var graph = new GraphBuilder();
        graph.SetVar("a", 0);
        graph.SetConst(1, None);
        graph.SetConst(2, None);
        graph.SetIfElse(new[] { 0 }, new[] { 1, 2 }, 3);
        graph.SetDef("y", 3);

        var result = graph.Solve();
        result.AssertNoGenerics();
        result.AssertNamed(None, "y");
    }

    #endregion

    #region Group 4: If-else with Optional types

    [Test(Description = "x:opt(i32); z:opt(real); y = if(a) x else z → y = opt(real)")]
    public void IfElse_OptionalOrOptional() {
        //     4  0  1      2
        // y = if(a) x else z
        var graph = new GraphBuilder();
        graph.SetVarType("x", StateOptional.Of(I32));
        graph.SetVarType("z", StateOptional.Of(Real));
        graph.SetVar("a", 0);
        graph.SetVar("x", 1);
        graph.SetVar("z", 2);
        graph.SetIfElse(new[] { 0 }, new[] { 1, 2 }, 3);
        graph.SetDef("y", 3);

        var result = graph.Solve();
        result.AssertNoGenerics();
        result.AssertNamed(StateOptional.Of(Real), "y");
    }

    [Test(Description = "x:opt(i32); y = if(a) x else 42i → y = opt(i32)")]
    public void IfElse_OptionalOrConcrete() {
        //     3  0  1      2
        // y = if(a) x else 42i
        var graph = new GraphBuilder();
        graph.SetVarType("x", StateOptional.Of(I32));
        graph.SetVar("a", 0);
        graph.SetVar("x", 1);
        graph.SetConst(2, I32);
        graph.SetIfElse(new[] { 0 }, new[] { 1, 2 }, 3);
        graph.SetDef("y", 3);

        var result = graph.Solve();
        result.AssertNoGenerics();
        result.AssertNamed(StateOptional.Of(I32), "y");
    }

    [Test(Description = "x:opt(i32); y = if(a) x else none → y = opt(i32)")]
    public void IfElse_OptionalOrNone() {
        //     3  0  1      2
        // y = if(a) x else none
        var graph = new GraphBuilder();
        graph.SetVarType("x", StateOptional.Of(I32));
        graph.SetVar("a", 0);
        graph.SetVar("x", 1);
        graph.SetConst(2, None);
        graph.SetIfElse(new[] { 0 }, new[] { 1, 2 }, 3);
        graph.SetDef("y", 3);

        var result = graph.Solve();
        result.AssertNoGenerics();
        result.AssertNamed(StateOptional.Of(I32), "y");
    }

    #endregion

    #region Group 5: Array with None/Optional

    [Test(Description = "y = [42i, none] → y = [opt(i32)..][]")]
    public void ArrayInit_WithNone() {
        //     2 0    1
        // y = [42i, none]
        var graph = new GraphBuilder();
        graph.SetConst(0, I32);
        graph.SetConst(1, None);
        graph.SetSoftArrayInit(2, 0, 1);
        graph.SetDef("y", 2);

        var result = graph.Solve();
        // Element type is a generic [opt(I32)..] — LCA(I32, None) = opt(I32) as descendant.
        // Output generics are not resolved by the solver.
        var yNode = result.GetVariableNode("y").GetNonReference();
        Assert.IsInstanceOf<StateArray>(yNode.State);
        var elemNode = ((StateArray)yNode.State).ElementNode.GetNonReference();
        Assert.IsInstanceOf<ConstraintsState>(elemNode.State);
        var cs = (ConstraintsState)elemNode.State;
        Assert.IsTrue(cs.HasDescendant);
        Assert.IsInstanceOf<StateOptional>(cs.Descendant);
        Assert.AreEqual(StateOptional.Of(I32), cs.Descendant);
    }

    [Test(Description = "x:opt(i32); z:opt(real); y = [x, z] → y = opt(real)[]")]
    public void ArrayInit_WithOptionals() {
        //     2 0 1
        // y = [x, z]
        var graph = new GraphBuilder();
        graph.SetVarType("x", StateOptional.Of(I32));
        graph.SetVarType("z", StateOptional.Of(Real));
        graph.SetVar("x", 0);
        graph.SetVar("z", 1);
        graph.SetStrictArrayInit(2, 0, 1);
        graph.SetDef("y", 2);

        var result = graph.Solve();
        result.AssertNoGenerics();
        result.AssertNamed(StateArray.Of(StateOptional.Of(Real)), "y");
    }

    #endregion

    #region Group 6: Error case

    [Test(Description = "x:opt(i32); y:i32 = x → TicError")]
    public void OptionalVar_CannotFitIntoNonOptional() {
        TestHelper.AssertThrowsTicError(() => {
            var graph = new GraphBuilder();
            graph.SetVarType("x", StateOptional.Of(I32));
            graph.SetVarType("y", I32);
            graph.SetVar("x", 0);
            graph.SetDef("y", 0);
            graph.Solve();
        });
    }

    #endregion
}
