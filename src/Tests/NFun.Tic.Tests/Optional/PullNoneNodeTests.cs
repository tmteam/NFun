using NFun.Tic.SolvingStates;
using NUnit.Framework;

namespace NFun.Tic.Tests.Optional;

using static StatePrimitive;

/// <summary>
/// Tests for the PullNoneNode transformation (Approach C):
/// When a None node has an ancestor with non-None primitive descendant,
/// PullNoneNode transforms the ancestor to Optional with an inner node
/// carrying the original constraints. Edge redirection ensures Push/Destruction
/// operate at the element level.
/// </summary>
class PullNoneNodeTests {

    #region Core transformation: None + primitive → Optional

    [Test(Description = "if(a) 42i else none → opt(i32): PullNoneNode transforms ancestor to Optional")]
    public void ConcreteI32_AndNone_BecomesOptionalI32() {
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

    [Test(Description = "if(a) none else 42i → opt(i32): None first, concrete second")]
    public void None_AndConcreteI32_BecomesOptionalI32() {
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

    [Test(Description = "if(a) true else none → opt(bool): Bool is non-numeric but still transforms")]
    public void ConcreteBool_AndNone_BecomesOptionalBool() {
        var graph = new GraphBuilder();
        graph.SetVar("a", 0);
        graph.SetConst(1, Bool);
        graph.SetConst(2, None);
        graph.SetIfElse(new[] { 0 }, new[] { 1, 2 }, 3);
        graph.SetDef("y", 3);

        var result = graph.Solve();
        result.AssertNoGenerics();
        result.AssertNamed(StateOptional.Of(Bool), "y");
    }

    [Test(Description = "if(a) 'x' else none → opt(char)")]
    public void ConcreteChar_AndNone_BecomesOptionalChar() {
        var graph = new GraphBuilder();
        graph.SetVar("a", 0);
        graph.SetConst(1, Char);
        graph.SetConst(2, None);
        graph.SetIfElse(new[] { 0 }, new[] { 1, 2 }, 3);
        graph.SetDef("y", 3);

        var result = graph.Solve();
        result.AssertNoGenerics();
        result.AssertNamed(StateOptional.Of(Char), "y");
    }

    [Test(Description = "if(a) 2.0 else none → opt(real)")]
    public void ConcreteReal_AndNone_BecomesOptionalReal() {
        var graph = new GraphBuilder();
        graph.SetVar("a", 0);
        graph.SetConst(1, Real);
        graph.SetConst(2, None);
        graph.SetIfElse(new[] { 0 }, new[] { 1, 2 }, 3);
        graph.SetDef("y", 3);

        var result = graph.Solve();
        result.AssertNoGenerics();
        result.AssertNamed(StateOptional.Of(Real), "y");
    }

    #endregion

    #region Generic preservation inside Optional (edge redirection)

    [Test(Description = "if(a) intConst else none → opt([U8..Real]Real!): inner constraints preserved")]
    public void IntConst_AndNone_InnerConstraintsPreserved() {
        // This is the key test for Approach C: intConst creates [U8..Real]Real!
        // PullNoneNode wraps in Optional, edge redirection lets Push/Destruction
        // propagate the Real ancestor to the inner node.
        var graph = new GraphBuilder();
        graph.SetVar("a", 0);
        graph.SetIntConst(1, U8);
        graph.SetConst(2, None);
        graph.SetIfElse(new[] { 0 }, new[] { 1, 2 }, 3);
        graph.SetDef("y", 3);

        var result = graph.Solve();
        var yNode = result.GetVariableNode("y").GetNonReference();
        Assert.IsInstanceOf<StateOptional>(yNode.State,
            $"Expected StateOptional but got {yNode.State}");

        var innerNode = ((StateOptional)yNode.State).ElementNode.GetNonReference();
        Assert.IsInstanceOf<ConstraintsState>(innerNode.State,
            $"Inner should be generic ConstraintsState but got {innerNode.State}");

        var cs = (ConstraintsState)innerNode.State;
        Assert.IsTrue(cs.HasDescendant, "Inner should have descendant (lower bound)");
        Assert.AreEqual(U8, cs.Descendant, "Inner descendant should be U8");
        Assert.IsTrue(cs.HasAncestor, "Inner should have ancestor (upper bound from edge redirect)");
        Assert.AreEqual(Real, cs.Ancestor, "Inner ancestor should be Real");
    }

    [Test(Description = "if(a) intConst else none → opt([U8..Real]Real!): Preferred survives")]
    public void IntConst_AndNone_PreferredSurvivesInInner() {
        var graph = new GraphBuilder();
        graph.SetVar("a", 0);
        graph.SetIntConst(1, U8);
        graph.SetConst(2, None);
        graph.SetIfElse(new[] { 0 }, new[] { 1, 2 }, 3);
        graph.SetDef("y", 3);

        var result = graph.Solve();
        var yNode = result.GetVariableNode("y").GetNonReference();
        var innerNode = ((StateOptional)yNode.State).ElementNode.GetNonReference();
        var cs = (ConstraintsState)innerNode.State;
        Assert.AreEqual(Real, cs.Preferred, "Preferred should be Real for int constants");
    }

    [Test(Description = "if(a) none else intConst → opt([U8..Real]): reversed order same result")]
    public void None_AndIntConst_InnerConstraintsPreserved() {
        var graph = new GraphBuilder();
        graph.SetVar("a", 0);
        graph.SetConst(1, None);
        graph.SetIntConst(2, U8);
        graph.SetIfElse(new[] { 0 }, new[] { 1, 2 }, 3);
        graph.SetDef("y", 3);

        var result = graph.Solve();
        var yNode = result.GetVariableNode("y").GetNonReference();
        Assert.IsInstanceOf<StateOptional>(yNode.State);

        var innerNode = ((StateOptional)yNode.State).ElementNode.GetNonReference();
        Assert.IsInstanceOf<ConstraintsState>(innerNode.State,
            $"Inner should be generic ConstraintsState but got {innerNode.State}");
        var cs = (ConstraintsState)innerNode.State;
        Assert.AreEqual(U8, cs.Descendant);
        Assert.AreEqual(Real, cs.Ancestor);
    }

    #endregion

    #region No transformation cases (PullNoneNode fallback)

    [Test(Description = "if(a) none else none → None: no transformation when both are None")]
    public void NoneAndNone_StaysNone() {
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

    [Test(Description = "y = none → None: standalone None, no ancestor to transform")]
    public void StandaloneNone_StaysNone() {
        var graph = new GraphBuilder();
        graph.SetConst(0, None);
        graph.SetDef("y", 0);

        var result = graph.Solve();
        result.AssertNoGenerics();
        result.AssertNamed(None, "y");
    }

    [Test(Description = "x:opt(i32); if(a) x else none → opt(i32), not opt(opt(i32))")]
    public void OptionalDescendant_NoDoubleWrap() {
        // When ancestor's descendant is StateOptional (not StatePrimitive),
        // PullNoneNode doesn't fire — avoids double wrapping.
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

    #region PropagateOptionalUpward

    [Test(Description = "z = if(a) 42i else none; y = z → both y and z are opt(i32)")]
    public void PropagateUpward_ThroughAssignment() {
        // z = if(a) 42i else none → z = opt(i32)
        // y = z → y = opt(i32) via PropagateOptionalUpward
        var graph = new GraphBuilder();
        graph.SetVar("a", 0);
        graph.SetConst(1, I32);
        graph.SetConst(2, None);
        graph.SetIfElse(new[] { 0 }, new[] { 1, 2 }, 3);
        graph.SetDef("z", 3);
        graph.SetVar("z", 4);
        graph.SetDef("y", 4);

        var result = graph.Solve();
        result.AssertNoGenerics();
        result.AssertNamed(StateOptional.Of(I32), "z");
        result.AssertNamed(StateOptional.Of(I32), "y");
    }

    [Test(Description = "z = if(a) intConst else none; y = z → both opt([U8..Real])")]
    public void PropagateUpward_ThroughAssignment_Generic() {
        // z = if(a) intConst else none → z = opt([U8..Real])
        // y = z → y = opt([U8..Real]) via PropagateOptionalUpward
        var graph = new GraphBuilder();
        graph.SetVar("a", 0);
        graph.SetIntConst(1, U8);
        graph.SetConst(2, None);
        graph.SetIfElse(new[] { 0 }, new[] { 1, 2 }, 3);
        graph.SetDef("z", 3);
        graph.SetVar("z", 4);
        graph.SetDef("y", 4);

        var result = graph.Solve();
        // Both y and z should be opt(generic)
        var zNode = result.GetVariableNode("z").GetNonReference();
        Assert.IsInstanceOf<StateOptional>(zNode.State);
        var zInner = ((StateOptional)zNode.State).ElementNode.GetNonReference();
        Assert.IsInstanceOf<ConstraintsState>(zInner.State,
            $"z inner should be generic but got {zInner.State}");

        var yNode = result.GetVariableNode("y").GetNonReference();
        Assert.IsInstanceOf<StateOptional>(yNode.State);
        var yInner = ((StateOptional)yNode.State).ElementNode.GetNonReference();
        Assert.IsInstanceOf<ConstraintsState>(yInner.State,
            $"y inner should be generic but got {yInner.State}");
    }

    [Test(Description = "z = if(a) 42i else none; y:opt(real) = z → z=opt(i32), y=opt(real)")]
    public void PropagateUpward_WithWidening() {
        // z = if(a) 42i else none → z = opt(i32)
        // y:opt(real) = z → y = opt(real), z ≤ y via Optional covariance
        var graph = new GraphBuilder();
        graph.SetVar("a", 0);
        graph.SetConst(1, I32);
        graph.SetConst(2, None);
        graph.SetIfElse(new[] { 0 }, new[] { 1, 2 }, 3);
        graph.SetDef("z", 3);
        graph.SetVarType("y", StateOptional.Of(Real));
        graph.SetVar("z", 4);
        graph.SetDef("y", 4);

        var result = graph.Solve();
        result.AssertNoGenerics();
        result.AssertNamed(StateOptional.Of(I32), "z");
        result.AssertNamed(StateOptional.Of(Real), "y");
    }

    #endregion

    #region Multi-branch with multiple None

    [Test(Description = "if(a) 42i elif(b) none else none → opt(i32)")]
    public void MultiBranch_ConcreteNoneNone() {
        var graph = new GraphBuilder();
        graph.SetVar("a", 0);
        graph.SetVar("b", 1);
        graph.SetConst(2, I32);
        graph.SetConst(3, None);
        graph.SetConst(4, None);
        graph.SetIfElse(new[] { 0, 1 }, new[] { 2, 3, 4 }, 5);
        graph.SetDef("y", 5);

        var result = graph.Solve();
        result.AssertNoGenerics();
        result.AssertNamed(StateOptional.Of(I32), "y");
    }

    [Test(Description = "if(a) none elif(b) 42i else none → opt(i32)")]
    public void MultiBranch_NoneConcreteNone() {
        var graph = new GraphBuilder();
        graph.SetVar("a", 0);
        graph.SetVar("b", 1);
        graph.SetConst(2, None);
        graph.SetConst(3, I32);
        graph.SetConst(4, None);
        graph.SetIfElse(new[] { 0, 1 }, new[] { 2, 3, 4 }, 5);
        graph.SetDef("y", 5);

        var result = graph.Solve();
        result.AssertNoGenerics();
        result.AssertNamed(StateOptional.Of(I32), "y");
    }

    [Test(Description = "if(a) none elif(b) none else none → None")]
    public void MultiBranch_AllNone() {
        var graph = new GraphBuilder();
        graph.SetVar("a", 0);
        graph.SetVar("b", 1);
        graph.SetConst(2, None);
        graph.SetConst(3, None);
        graph.SetConst(4, None);
        graph.SetIfElse(new[] { 0, 1 }, new[] { 2, 3, 4 }, 5);
        graph.SetDef("y", 5);

        var result = graph.Solve();
        result.AssertNoGenerics();
        result.AssertNamed(None, "y");
    }

    [Test(Description = "if(a) intConst elif(b) none else intConst → opt([U8..Real])")]
    public void MultiBranch_IntConstNoneIntConst_GenericPreserved() {
        var graph = new GraphBuilder();
        graph.SetVar("a", 0);
        graph.SetVar("b", 1);
        graph.SetIntConst(2, U8);
        graph.SetConst(3, None);
        graph.SetIntConst(4, U8);
        graph.SetIfElse(new[] { 0, 1 }, new[] { 2, 3, 4 }, 5);
        graph.SetDef("y", 5);

        var result = graph.Solve();
        var yNode = result.GetVariableNode("y").GetNonReference();
        Assert.IsInstanceOf<StateOptional>(yNode.State,
            $"Expected StateOptional but got {yNode.State}");
        var innerNode = ((StateOptional)yNode.State).ElementNode.GetNonReference();
        Assert.IsInstanceOf<ConstraintsState>(innerNode.State,
            $"Inner should be generic ConstraintsState but got {innerNode.State}");
        var cs = (ConstraintsState)innerNode.State;
        Assert.AreEqual(U8, cs.Descendant);
        Assert.AreEqual(Real, cs.Ancestor);
    }

    #endregion

    #region Composite types with None (handled by Destruction, not PullNoneNode)

    [Test(Description = "if(a) [1i,2i] else none → opt(i32[]): array + none")]
    public void Array_AndNone_BecomesOptionalArray() {
        var graph = new GraphBuilder();
        graph.SetVar("a", 0);
        graph.SetConst(1, I32);
        graph.SetConst(2, I32);
        graph.SetStrictArrayInit(3, 1, 2);
        graph.SetConst(4, None);
        graph.SetIfElse(new[] { 0 }, new[] { 3, 4 }, 5);
        graph.SetDef("y", 5);

        var result = graph.Solve();
        result.AssertNoGenerics();
        result.AssertNamed(StateOptional.Of(StateArray.Of(I32)), "y");
    }

    [Test(Description = "if(a) {age=42i} else none → opt({age:i32}): struct + none")]
    public void Struct_AndNone_BecomesOptionalStruct() {
        var graph = new GraphBuilder();
        graph.SetVar("a", 0);
        graph.SetConst(1, I32);
        graph.SetStructInit(new[] { "age" }, new[] { 1 }, 2);
        graph.SetConst(3, None);
        graph.SetIfElse(new[] { 0 }, new[] { 2, 3 }, 4);
        graph.SetDef("y", 4);

        var result = graph.Solve();
        result.AssertNoGenerics();
        result.AssertNamed(StateOptional.Of(StateStruct.Of("age", I32)), "y");
    }

    #endregion

    #region DestructionFunctions: Optional ancestor + primitive descendant

    [Test(Description = "f(x:opt(i32)):bool; f(42i) → bool: T ≤ opt(T) via element destruction")]
    public void FunOptionalArg_PassConcrete_ElementDestruction() {
        var graph = new GraphBuilder();
        var funType = StateFun.Of(StateOptional.Of(I32), Bool);
        graph.SetVarType("f", funType);
        graph.SetVar("f", 0);
        graph.SetConst(1, I32);
        graph.SetCall(0, 1, 2);
        graph.SetDef("y", 2);

        var result = graph.Solve();
        result.AssertNoGenerics();
        result.AssertNamed(Bool, "y");
    }

    [Test(Description = "f(x:opt(i32)):bool; f(none) → bool: None ≤ opt(T) is no-op")]
    public void FunOptionalArg_PassNone_NoOp() {
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

    [Test(Description = "f(x:i32):bool; f(none) → TicError: None !≤ non-Optional")]
    public void FunNonOptionalArg_PassNone_Fails() {
        TestHelper.AssertThrowsTicError(() => {
            var graph = new GraphBuilder();
            var funType = StateFun.Of(I32, Bool);
            graph.SetVarType("f", funType);
            graph.SetVar("f", 0);
            graph.SetConst(1, None);
            graph.SetCall(0, 1, 2);
            graph.SetDef("y", 2);
            graph.Solve();
        });
    }

    #endregion

    #region Optional in arithmetic (must remain error)

    [Test(Description = "x:opt(i32); y = x + 1 → TicError: Optional not arithmetic")]
    public void OptionalInArithmetic_StillFails() {
        TestHelper.AssertThrowsTicError(() => {
            var graph = new GraphBuilder();
            graph.SetVarType("x", StateOptional.Of(I32));
            graph.SetVar("x", 0);
            graph.SetIntConst(1, U8);
            graph.SetArith(0, 1, 2);
            graph.SetDef("y", 2);
            graph.Solve();
        });
    }

    #endregion

    #region Array element with None (PullNoneNode on array element level)

    [Test(Description = "[42i, none] → opt(i32)[]: array element transformed to Optional")]
    public void ArrayElement_ConcreteAndNone_BecomesOptional() {
        var graph = new GraphBuilder();
        graph.SetConst(0, I32);
        graph.SetConst(1, None);
        graph.SetSoftArrayInit(2, 0, 1);
        graph.SetDef("y", 2);

        var result = graph.Solve();
        var yNode = result.GetVariableNode("y").GetNonReference();
        Assert.IsInstanceOf<StateArray>(yNode.State);
        var elemNode = ((StateArray)yNode.State).ElementNode.GetNonReference();
        Assert.IsInstanceOf<StateOptional>(elemNode.State);
        Assert.AreEqual(StateOptional.Of(I32), elemNode.State);
    }

    [Test(Description = "[none, 42i] → opt(i32)[]: reversed order")]
    public void ArrayElement_NoneAndConcrete_BecomesOptional() {
        var graph = new GraphBuilder();
        graph.SetConst(0, None);
        graph.SetConst(1, I32);
        graph.SetSoftArrayInit(2, 0, 1);
        graph.SetDef("y", 2);

        var result = graph.Solve();
        var yNode = result.GetVariableNode("y").GetNonReference();
        Assert.IsInstanceOf<StateArray>(yNode.State);
        var elemNode = ((StateArray)yNode.State).ElementNode.GetNonReference();
        Assert.IsInstanceOf<StateOptional>(elemNode.State);
        Assert.AreEqual(StateOptional.Of(I32), elemNode.State);
    }

    #endregion

    #region Nested struct field with None

    [Test(Description = "{a = if(c) 1i else none} → {a: opt(i32)}: struct field transformed")]
    public void StructField_ConcreteAndNone_BecomesOptional() {
        var graph = new GraphBuilder();
        graph.SetVar("c", 0);
        graph.SetConst(1, I32);
        graph.SetConst(2, None);
        graph.SetIfElse(new[] { 0 }, new[] { 1, 2 }, 3);
        graph.SetStructInit(new[] { "a" }, new[] { 3 }, 4);
        graph.SetDef("y", 4);

        var result = graph.Solve();
        var yNode = result.GetVariableNode("y").GetNonReference();
        Assert.IsInstanceOf<StateStruct>(yNode.State);
        var fieldNode = ((StateStruct)yNode.State).GetFieldOrNull("a").GetNonReference();
        Assert.IsInstanceOf<StateOptional>(fieldNode.State);
        Assert.AreEqual(StateOptional.Of(I32), fieldNode.State);
    }

    #endregion
}
