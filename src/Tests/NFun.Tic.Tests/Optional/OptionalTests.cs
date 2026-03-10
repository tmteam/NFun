using System;
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

    [Test(Description = "y = if(a) 1 else none → y = opt([U8..Real])")]
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
        var yNode = result.GetVariableNode("y").GetNonReference();
        // y should be opt(T) where T = [U8..Real] — generic survives inside Optional
        Assert.IsInstanceOf<StateOptional>(yNode.State,
            $"Expected StateOptional but got {yNode.State} ({yNode.State.GetType().Name})");
        var innerNode = ((StateOptional)yNode.State).ElementNode.GetNonReference();
        Assert.IsInstanceOf<ConstraintsState>(innerNode.State,
            $"Inner element should be generic ConstraintsState but got {innerNode.State} ({innerNode.State.GetType().Name})");
        var cs = (ConstraintsState)innerNode.State;
        Assert.IsTrue(cs.HasDescendant, "Inner generic should have descendant");
        Assert.AreEqual(U8, cs.Descendant, "Inner generic descendant should be U8");
        Assert.AreEqual(Real, cs.Ancestor, "Inner generic ancestor should be Real");
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

    [Test(Description = "y = [42i, none] → y = opt(i32)[]")]
    public void ArrayInit_WithNone() {
        //     2 0    1
        // y = [42i, none]
        var graph = new GraphBuilder();
        graph.SetConst(0, I32);
        graph.SetConst(1, None);
        graph.SetSoftArrayInit(2, 0, 1);
        graph.SetDef("y", 2);

        var result = graph.Solve();
        // Element type is opt(I32) — PullNoneNode transforms the element type
        // to Optional when a concrete primitive and None are combined.
        var yNode = result.GetVariableNode("y").GetNonReference();
        Assert.IsInstanceOf<StateArray>(yNode.State);
        var elemNode = ((StateArray)yNode.State).ElementNode.GetNonReference();
        Assert.IsInstanceOf<StateOptional>(elemNode.State);
        Assert.AreEqual(StateOptional.Of(I32), elemNode.State);
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

    #region Group 7: Baseline — if-else generic WITHOUT optional (control tests)

    [Test(Description = "BASELINE: y = if(a) 1 else 0 → y = [U8..Real]Real! (generic preserved)")]
    public void Baseline_IfElse_TwoIntConsts_GenericSurvives() {
        //     3  0  1      2
        // y = if(a) 1 else 0
        var graph = new GraphBuilder();
        graph.SetVar("a", 0);
        graph.SetIntConst(1, U8);
        graph.SetIntConst(2, U8);
        graph.SetIfElse(new[] { 0 }, new[] { 1, 2 }, 3);
        graph.SetDef("y", 3);

        var result = graph.Solve();
        var t = result.AssertAndGetSingleGeneric(U8, Real);
        result.AssertAreGenerics(t, "y");
        // Check preferred survived
        var cs = (ConstraintsState)t.State;
        Assert.AreEqual(Real, cs.Preferred, "Preferred should be Real for int constants");
    }

    [Test(Description = "BASELINE: y = if(a) [1] else [2.0] → y = Real[] (array element resolved to Real)")]
    public void Baseline_IfElse_ArrayIntConstOrArrayReal() {
        //     5  0  2       4
        // y = if(a) [1] else [2.0]
        var graph = new GraphBuilder();
        graph.SetVar("a", 0);
        graph.SetIntConst(1, U8);
        graph.SetStrictArrayInit(2, 1);
        graph.SetConst(3, Real);
        graph.SetStrictArrayInit(4, 3);
        graph.SetIfElse(new[] { 0 }, new[] { 2, 4 }, 5);
        graph.SetDef("y", 5);

        var result = graph.Solve();
        result.AssertNoGenerics();
        result.AssertNamed(StateArray.Of(Real), "y");
    }

    [Test(Description = "BASELINE: y = if(a) 1 else 2.0 → y = Real (intConst collapses to Real)")]
    public void Baseline_IfElse_IntConstOrConcreteReal() {
        //     3  0  1      2
        // y = if(a) 1 else 2.0
        var graph = new GraphBuilder();
        graph.SetVar("a", 0);
        graph.SetIntConst(1, U8);
        graph.SetConst(2, Real);
        graph.SetIfElse(new[] { 0 }, new[] { 1, 2 }, 3);
        graph.SetDef("y", 3);

        var result = graph.Solve();
        result.AssertNoGenerics();
        result.AssertNamed(Real, "y");
    }

    #endregion

    #region Group 8: Generic functions with None/Optional

    [Test(Description = "wrap:T→opt(T); y = wrap(1) → y = opt([U8..Real])")]
    public void GenericWrap_IntConstToOptional() {
        //     1    0
        // y = wrap(1)
        // wrap signature: T → opt(T)
        var graph = new GraphBuilder();
        var t = graph.InitializeVarNode();
        graph.SetIntConst(0, U8);  // [U8..Real]Real!
        graph.SetCall(new ITicNodeState[] { t, StateOptional.Of(t) }, new[] { 0, 1 });
        graph.SetDef("y", 1);

        var result = graph.Solve();
        // y should be opt(T) where T = [U8..Real] — generic preserved inside Optional
        var yNode = result.GetVariableNode("y").GetNonReference();
        Assert.IsInstanceOf<StateOptional>(yNode.State,
            $"Expected StateOptional but got {yNode.State} ({yNode.State.GetType().Name})");
        var innerNode = ((StateOptional)yNode.State).ElementNode.GetNonReference();
        Assert.IsInstanceOf<ConstraintsState>(innerNode.State,
            $"Inner should be generic [U8..Real] but got {innerNode.State} ({innerNode.State.GetType().Name})");
        var cs = (ConstraintsState)innerNode.State;
        Assert.IsTrue(cs.HasDescendant, "Inner generic should have descendant");
        Assert.AreEqual(U8, cs.Descendant, "Inner generic descendant should be U8");
        Assert.AreEqual(Real, cs.Ancestor, "Inner generic ancestor should be Real");
        Assert.AreEqual(Real, cs.Preferred, "Preferred should survive inside Optional");
    }

    [Test(Description = "wrap:T→opt(T); y = wrap(42i) → y = opt(i32)")]
    public void GenericWrap_ConcreteIntToOptional() {
        //     1    0
        // y = wrap(42i)
        // wrap signature: T → opt(T)
        var graph = new GraphBuilder();
        var t = graph.InitializeVarNode();
        graph.SetConst(0, I32);
        graph.SetCall(new ITicNodeState[] { t, StateOptional.Of(t) }, new[] { 0, 1 });
        graph.SetDef("y", 1);

        var result = graph.Solve();
        result.AssertNoGenerics();
        result.AssertNamed(StateOptional.Of(I32), "y");
    }

    [Test(Description = "wrap:T→opt(T); y = wrap(none) → y = opt(None)")]
    public void GenericWrap_NoneToOptional() {
        //     1    0
        // y = wrap(none)
        // wrap signature: T → opt(T)
        var graph = new GraphBuilder();
        var t = graph.InitializeVarNode();
        graph.SetConst(0, None);
        graph.SetCall(new ITicNodeState[] { t, StateOptional.Of(t) }, new[] { 0, 1 });
        graph.SetDef("y", 1);

        var result = graph.Solve();
        result.AssertNoGenerics();
        result.AssertNamed(StateOptional.Of(None), "y");
    }

    [Test(Description = "identity:T→T; y = identity(opt(i32 var)) → y = opt(i32)")]
    public void GenericIdentity_OptionalPassthrough() {
        //     1        0
        // y = identity(x)
        // identity: T → T
        var graph = new GraphBuilder();
        var t = graph.InitializeVarNode();
        graph.SetVarType("x", StateOptional.Of(I32));
        graph.SetVar("x", 0);
        graph.SetCall(new ITicNodeState[] { t, t }, new[] { 0, 1 });
        graph.SetDef("y", 1);

        var result = graph.Solve();
        result.AssertNoGenerics();
        result.AssertNamed(StateOptional.Of(I32), "y");
    }

    #endregion

    #region Implicit lift: T ≤ opt(T) via Pull/Push

    [Test(Description = "x:i32; x ≤ opt(i32) node — i32 lifts into opt, T constrained")]
    public void ImplicitLift_ConcreteI32_IntoOpt() {
        // x:i32 → node0 → V(opt(T))
        // T should become i32
        var graph = new GraphBuilder();
        var t = graph.InitializeVarNode();
        graph.SetVarType("x", I32);
        graph.SetVar("x", 0);
        // node 0 ≤ opt(T)
        graph.SetCallArgument(StateOptional.Of(t), 0);
        // y = T
        graph.SetCall(new ITicNodeState[] { t }, new[] { 1 });
        graph.SetDef("y", 1);

        var result = graph.Solve();
        result.AssertNamed(I32, "y");
    }

    [Test(Description = "none ≤ opt(i32) — None lifts, T stays i32")]
    public void ImplicitLift_None_IntoOptI32() {
        // y:opt(i32) = none
        var graph = new GraphBuilder();
        graph.SetVarType("y", StateOptional.Of(I32));
        graph.SetConst(0, None);
        graph.SetDef("y", 0);

        var result = graph.Solve();
        result.AssertNoGenerics();
        result.AssertNamed(StateOptional.Of(I32), "y");
    }

    [Test(Description = "literal [U8..Real] ≤ opt(T) — T gets arith constraints")]
    public void ImplicitLift_IntLiteral_IntoOpt() {
        // 42 → opt(T), y = T
        var graph = new GraphBuilder();
        var t = graph.InitializeVarNode();
        graph.SetIntConst(0, U8); // [U8..Real]
        graph.SetCallArgument(StateOptional.Of(t), 0);
        graph.SetCall(new ITicNodeState[] { t }, new[] { 1 });
        graph.SetDef("y", 1);

        var result = graph.Solve();
        var yNode = result.GetVariableNode("y").GetNonReference();
        Assert.AreNotEqual(Any, yNode.State);
        // T should have arith constraints from the literal
        yNode.AssertGenericType(U8, Real);
    }

    [Test(Description = "i32 and none both ≤ same LCA node — result is opt(i32)")]
    public void ImplicitLift_I32AndNone_SameLcaTarget() {
        // Models: array [42, none] or if-else (42, none)
        // Both 42:i32 and none go into the same element/result node
        // node0 = i32, node1 = none, both ≤ node2 (LCA target)
        // y = node2, should be opt(i32)
        var graph = new GraphBuilder();
        graph.SetConst(0, I32);
        graph.SetConst(1, None);
        var lcaNode = graph.GetOrCreateNode(2);
        graph.GetOrCreateNode(0).AddAncestor(lcaNode);
        graph.GetOrCreateNode(1).AddAncestor(lcaNode);
        graph.SetDef("y", 2);

        var result = graph.Solve();
        result.AssertNoGenerics();
        result.AssertNamed(StateOptional.Of(I32), "y");
    }

    [Test(Description = "text and none both ≤ same LCA node — result is opt(text)")]
    public void ImplicitLift_TextAndNone_SameLcaTarget() {
        var graph = new GraphBuilder();
        graph.SetConst(0, StatePrimitive.Char); // text ~ char[]
        graph.SetConst(1, None);
        var lcaNode = graph.GetOrCreateNode(2);
        graph.GetOrCreateNode(0).AddAncestor(lcaNode);
        graph.GetOrCreateNode(1).AddAncestor(lcaNode);
        graph.SetDef("y", 2);

        var result = graph.Solve();
        result.AssertNoGenerics();
        // Char (not text) — just checking the mechanism
        result.AssertNamed(StateOptional.Of(StatePrimitive.Char), "y");
    }

    [Test(Description = "char[] (text) and none both ≤ LCA node → opt(char[])")]
    public void ImplicitLift_CharArrayAndNone_SameLcaTarget() {
        // Models: if(b) 'hi' else none  or  ['hello', none]
        // 'hi' = char[], none = None, both ≤ result node, y = result
        var graph = new GraphBuilder();
        var charArrayNode = graph.GetOrCreateNode(0);
        charArrayNode.State = StateArray.Of(StatePrimitive.Char);
        graph.SetConst(1, None);
        var lcaNode = graph.GetOrCreateNode(2);
        charArrayNode.AddAncestor(lcaNode);
        graph.GetOrCreateNode(1).AddAncestor(lcaNode);
        graph.SetDef("y", 2);

        var result = graph.Solve();
        result.AssertNoGenerics();
        result.AssertNamed(StateOptional.Of(StateArray.Of(StatePrimitive.Char)), "y");
    }

    [Test(Description = "int[] and none both ≤ LCA node → opt(int[])")]
    public void ImplicitLift_IntArrayAndNone_SameLcaTarget() {
        var graph = new GraphBuilder();
        var intArrayNode = graph.GetOrCreateNode(0);
        intArrayNode.State = StateArray.Of(I32);
        graph.SetConst(1, None);
        var lcaNode = graph.GetOrCreateNode(2);
        intArrayNode.AddAncestor(lcaNode);
        graph.GetOrCreateNode(1).AddAncestor(lcaNode);
        graph.SetDef("y", 2);

        var result = graph.Solve();
        result.AssertNoGenerics();
        result.AssertNamed(StateOptional.Of(StateArray.Of(I32)), "y");
    }

    // --- With explicit type annotation (reproducing syntax-level regressions) ---

    [Test(Description = "y:text? = if(true) 'hi' else none — char[] + none with opt(char[]) annotation")]
    public void ImplicitLift_CharArrayAndNone_WithOptAnnotation() {
        // y:text? = if(true) 'hi' else none
        // 'hi' = char[], none = None, both ≤ LCA node, y:opt(char[])
        var graph = new GraphBuilder();
        var charArrayNode = graph.GetOrCreateNode(0);
        charArrayNode.State = StateArray.Of(StatePrimitive.Char);
        graph.SetConst(1, None);
        var lcaNode = graph.GetOrCreateNode(2);
        charArrayNode.AddAncestor(lcaNode);
        graph.GetOrCreateNode(1).AddAncestor(lcaNode);
        graph.SetVarType("y", StateOptional.Of(StateArray.Of(StatePrimitive.Char)));
        graph.SetDef("y", 2);

        var result = graph.Solve();
        result.AssertNoGenerics();
        result.AssertNamed(StateOptional.Of(StateArray.Of(StatePrimitive.Char)), "y");
    }

    [Test(Description = "Minimal: char[] + none ≤ LCA node, LCA ≤ opt(char[]) — no y")]
    public void ImplicitLift_CharArrayAndNone_WithOptAncestor_Minimal() {
        // Minimal reproduction: remove y, just add opt(char[]) as ancestor
        var graph = new GraphBuilder();
        var charArrayNode = graph.GetOrCreateNode(0);
        charArrayNode.State = StateArray.Of(StatePrimitive.Char);
        graph.SetConst(1, None);
        var lcaNode = graph.GetOrCreateNode(2);
        charArrayNode.AddAncestor(lcaNode);
        graph.GetOrCreateNode(1).AddAncestor(lcaNode);
        // Add explicit opt ancestor — like type annotation would do
        var optNode = graph.GetOrCreateNode(3);
        optNode.State = StateOptional.Of(StateArray.Of(StatePrimitive.Char));
        lcaNode.AddAncestor(optNode);
        graph.SetDef("y", 2);

        var result = graph.Solve();
        result.AssertNoGenerics();
        result.AssertNamed(StateOptional.Of(StateArray.Of(StatePrimitive.Char)), "y");
    }

    [Test(Description = "Same but with I32 + none ≤ LCA ≤ opt(I32) — should pass")]
    public void ImplicitLift_I32AndNone_WithOptAncestor() {
        var graph = new GraphBuilder();
        graph.SetConst(0, I32);
        graph.SetConst(1, None);
        var lcaNode = graph.GetOrCreateNode(2);
        graph.GetOrCreateNode(0).AddAncestor(lcaNode);
        graph.GetOrCreateNode(1).AddAncestor(lcaNode);
        graph.SetVarType("y", StateOptional.Of(I32));
        graph.SetDef("y", 2);

        var result = graph.Solve();
        result.AssertNoGenerics();
        result.AssertNamed(StateOptional.Of(I32), "y");
    }

    [Test(Description = "y:int[]?[] = [[1,2], none, [3]] — array of opt(int[]) with nested arrays")]
    public void ImplicitLift_IntArrayAndNone_InOuterArray() {
        // y:int[]?[] = [[1,2], none, [3]]
        // Each element: int[] or none → opt(int[])
        // Outer: array of opt(int[])
        var graph = new GraphBuilder();
        // Element 0: int[] (simplified — just int[] node)
        var arr0 = graph.GetOrCreateNode(0);
        arr0.State = StateArray.Of(I32);
        // Element 1: none
        graph.SetConst(1, None);
        // Element 2: int[]
        var arr2 = graph.GetOrCreateNode(2);
        arr2.State = StateArray.Of(I32);
        // Outer array element type node
        var elemNode = graph.GetOrCreateNode(3);
        arr0.AddAncestor(elemNode);
        graph.GetOrCreateNode(1).AddAncestor(elemNode);
        arr2.AddAncestor(elemNode);
        // Outer array result
        graph.GetOrCreateArrayNode(4, elemNode);
        graph.SetVarType("y", StateArray.Of(StateOptional.Of(StateArray.Of(I32))));
        graph.SetDef("y", 4);

        var result = graph.Solve();
        result.AssertNoGenerics();
        result.AssertNamed(StateArray.Of(StateOptional.Of(StateArray.Of(I32))), "y");
    }

    #endregion

    #region ForceUnwrap / NullCoalesce with non-optional arguments

    // Helper: SetForceUnwrap models the function opt(T) -> T
    private static void SetForceUnwrap(GraphBuilder graph, int argId, int resultId) {
        var t = graph.InitializeVarNode();
        graph.SetCall(
            new ITicNodeState[] { StateOptional.Of(t), t },
            new[] { argId, resultId });
    }

    // Helper: SetNullCoalesce models the function (opt(T), T) -> T
    private static void SetNullCoalesce(GraphBuilder graph, int leftId, int rightId, int resultId) {
        var t = graph.InitializeVarNode();
        graph.SetCall(
            new ITicNodeState[] { StateOptional.Of(t), t, t },
            new[] { leftId, rightId, resultId });
    }

    [Test(Description = "y = forceUnwrap(42) — literal int passed to opt(T)->T, T should be arith generic")]
    public void ForceUnwrap_IntLiteral() {
        //       1  0
        // y = unwrap(42)
        var graph = new GraphBuilder();
        graph.SetIntConst(0, U8);  // 42 literal: [U8..Real]
        SetForceUnwrap(graph, 0, 1);
        graph.SetDef("y", 1);

        var result = graph.Solve();
        var yNode = result.GetVariableNode("y").GetNonReference();
        // y should be a generic constrained by the literal's range [U8..Real], not Any
        Assert.AreNotEqual(Any, yNode.State, $"y should not be Any, but got: {yNode.State}");
        yNode.AssertGenericType(U8, Real);
    }

    [Test(Description = "y = forceUnwrap(x:i32) — concrete i32 passed to opt(T)->T, y must be i32")]
    public void ForceUnwrap_ConcreteI32() {
        //       1  0
        // y = unwrap(x)
        var graph = new GraphBuilder();
        graph.SetVarType("x", I32);
        graph.SetVar("x", 0);
        SetForceUnwrap(graph, 0, 1);
        graph.SetDef("y", 1);

        var result = graph.Solve();
        result.AssertNamed(I32, "y");
    }

    [Test(Description = "y = nullCoalesce(42, 0) — both literals, y should be arith")]
    public void NullCoalesce_TwoIntLiterals() {
        //       2  0  1
        // y = coalesce(42, 0)
        var graph = new GraphBuilder();
        graph.SetIntConst(0, U8);  // 42
        graph.SetIntConst(1, U8);  // 0
        SetNullCoalesce(graph, 0, 1, 2);
        graph.SetDef("y", 2);

        var result = graph.Solve();
        var yState = result.GetVariableNode("y").GetNonReference().State;
        Assert.AreNotEqual(Any, yState, $"y should not be Any, but got: {yState}");
    }

    [Test(Description = "y = nullCoalesce(x:i32, 0) — i32 input to coalesce, y must be i32")]
    public void NullCoalesce_ConcreteI32() {
        //       2  0  1
        // y = coalesce(x, 0)
        var graph = new GraphBuilder();
        graph.SetVarType("x", I32);
        graph.SetVar("x", 0);
        graph.SetIntConst(1, U8);
        SetNullCoalesce(graph, 0, 1, 2);
        graph.SetDef("y", 2);

        var result = graph.Solve();
        result.AssertNamed(I32, "y");
    }

    #endregion

    #region Group 7: Preferred type preservation through Optional wrapping

    [Test(Description = "y = if(true) [intConst, intConst] else none → y = opt(arr([U8..Re]))")]
    public void PreferredType_IfElseArrayOrNone() {
        //     5    0   3    1  2          4
        // y = if(true) [  1,  2 ]   else none
        var graph = new GraphBuilder();
        graph.SetConst(0, Bool);
        graph.SetGenericConst(1, U8, Real, I32);  // Preferred=I32 like real parser
        graph.SetGenericConst(2, U8, Real, I32);
        graph.SetSoftArrayInit(3, 1, 2);
        graph.SetConst(4, StatePrimitive.None);
        graph.SetIfElse(new[] { 0 }, new[] { 3, 4 }, 5);
        graph.SetDef("y", 5);

        var result = graph.Solve();
        var yNode = result.GetVariableNode("y");
        // y should be opt(arr(generic)) where the inner array element preserves constraints
        Assert.That(yNode.State, Is.TypeOf<StateOptional>(), $"y should be StateOptional but was {yNode.State}");
        var opt = (StateOptional)yNode.State;
        Assert.That(opt.ElementNode.State, Is.TypeOf<StateArray>(), $"inner should be StateArray but was {opt.ElementNode.State}");
        var arr = (StateArray)opt.ElementNode.State;
        Assert.That(arr.ElementNode.State, Is.TypeOf<ConstraintsState>(), $"element should be ConstraintsState but was {arr.ElementNode.State}");
        var c = (ConstraintsState)arr.ElementNode.State;
        Assert.That(c.Descendant, Is.EqualTo(U8), $"element desc should be U8 but was {c.Descendant}");
        Assert.That(c.Ancestor, Is.EqualTo(Real), $"element anc should be Real but was {c.Ancestor}");
    }

    [Test(Description = "y = if(true) intConst else none → y = opt([U8..Re]I32!)")]
    public void PreferredType_IfElseIntOrNone() {
        //     3    0   1          2
        // y = if(true) 42   else none
        var graph = new GraphBuilder();
        graph.SetConst(0, Bool);
        graph.SetGenericConst(1, U8, Real, I32);  // Preferred=I32 like real parser
        graph.SetConst(2, StatePrimitive.None);
        graph.SetIfElse(new[] { 0 }, new[] { 1, 2 }, 3);
        graph.SetDef("y", 3);

        var result = graph.Solve();
        var yNode = result.GetVariableNode("y");
        Assert.That(yNode.State, Is.TypeOf<StateOptional>(), $"y should be StateOptional but was {yNode.State}");
        var opt = (StateOptional)yNode.State;
        Assert.That(opt.ElementNode.State, Is.TypeOf<ConstraintsState>(), $"inner should be ConstraintsState but was {opt.ElementNode.State}");
        var c = (ConstraintsState)opt.ElementNode.State;
        Assert.That(c.Descendant, Is.EqualTo(U8), $"inner desc should be U8 but was {c.Descendant}");
        Assert.That(c.Ancestor, Is.EqualTo(Real), $"inner anc should be Real but was {c.Ancestor}");
        Assert.That(c.Preferred, Is.EqualTo(I32), $"inner preferred should be I32 but was {c.Preferred}");
    }

    [Test(Description = "y = [intConst, none, intConst] → arr(opt([U8..Re]I32!))")]
    public void PreferredType_ArrayWithNoneElement() {
        //     3     0     1     2
        // y = [    1,   none,   3 ]
        var graph = new GraphBuilder();
        graph.SetGenericConst(0, U8, Real, I32);  // Preferred=I32 like real parser
        graph.SetConst(1, StatePrimitive.None);
        graph.SetGenericConst(2, U8, Real, I32);
        graph.SetSoftArrayInit(3, 0, 1, 2);
        graph.SetDef("y", 3);

        var result = graph.Solve();
        var yNode = result.GetVariableNode("y");

        // y should be arr(opt(generic)) where the inner element preserves constraints
        Assert.That(yNode.State, Is.TypeOf<StateArray>(), $"y should be StateArray but was {yNode.State}");
        var arr = (StateArray)yNode.State;
        Assert.That(arr.ElementNode.State, Is.TypeOf<StateOptional>(), $"element should be StateOptional but was {arr.ElementNode.State}");
        var opt = (StateOptional)arr.ElementNode.State;
        Assert.That(opt.ElementNode.State, Is.TypeOf<ConstraintsState>(), $"inner should be ConstraintsState but was {opt.ElementNode.State}");
        var c = (ConstraintsState)opt.ElementNode.State;
        Assert.That(c.Descendant, Is.EqualTo(U8), $"inner desc should be U8 but was {c.Descendant}");
        Assert.That(c.Ancestor, Is.EqualTo(Real), $"inner anc should be Real but was {c.Ancestor}");
        Assert.That(c.Preferred, Is.EqualTo(I32), $"inner preferred should be I32 but was {c.Preferred}");
    }

    #endregion

    #region Group 7: Coalesce + IfElse

    [Test(Description = "flag:bool? = true; y = if(flag ?? false) 1 else 0")]
    public void CoalesceResultUsedAsIfCondition() {
        //                  0       1      2        3   4    5
        // y = if( coalesce(flag, false) ) 1  else  0
        var graph = new GraphBuilder();

        // flag:opt(bool)
        graph.SetVarType("flag", StateOptional.Of(Bool));
        graph.SetVar("flag", 0);

        // false constant (default for coalesce)
        graph.SetConst(1, Bool);

        // coalesce: (opt(T), T) → T, args=[0,1], return=2
        var t = graph.InitializeVarNode();
        graph.SetCall(
            new ITicNodeState[] { StateOptional.Of(t), t, t },
            new[] { 0, 1, 2 });

        // if-else: condition=2 (coalesce result), branches=[3,4], result=5
        graph.SetGenericConst(3, U8, Real, I32);  // 1
        graph.SetGenericConst(4, U8, Real, I32);  // 0
        graph.SetIfElse(new[] { 2 }, new[] { 3, 4 }, 5);
        graph.SetDef("y", 5);

        var result = graph.Solve();
        // y stays generic [U8..Re]I32! — output generics are resolved by runtime
        var yState = result.GetVariableNode("y").State;
        Assert.That(yState, Is.TypeOf<ConstraintsState>());
        var c = (ConstraintsState)yState;
        Assert.That(c.Preferred, Is.EqualTo(I32));
    }

    #endregion
}
