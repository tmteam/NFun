using NFun.Tic.SolvingStates;
using NUnit.Framework;

namespace NFun.Tic.Tests.Optional;

using static StatePrimitive;

class OptionalCompositeTests {

    #region Optional struct

    [Test(Description = "x:opt({a:i32}); y = x → y = opt({a:i32})")]
    public void OptionalStruct_Propagates() {
        var graph = new GraphBuilder();
        var s = StateStruct.Of("a", I32);
        graph.SetVarType("x", StateOptional.Of(s));
        graph.SetVar("x", 0);
        graph.SetDef("y", 0);

        var result = graph.Solve();
        result.AssertNoGenerics();
        result.AssertNamed(StateOptional.Of(s), "y");
    }

    [Test(Description = "y = if(a) {age=42i} else none → y = opt({age:i32})")]
    public void IfElse_StructOrNone() {
        var graph = new GraphBuilder();
        graph.SetVar("a", 0);
        graph.SetConst(1, I32);
        graph.SetStructInit(new[] { "age" }, new[] { 1 }, 2);
        graph.SetConst(3, None);
        graph.SetIfElse(new[] { 0 }, new[] { 2, 3 }, 4);
        graph.SetDef("y", 4);

        var result = graph.Solve();
        result.AssertNoGenerics();
        var s = StateStruct.Of("age", I32);
        result.AssertNamed(StateOptional.Of(s), "y");
    }

    [Test(Description = "y = if(a) none else {age=42i} → y = opt({age:i32})")]
    public void IfElse_NoneOrStruct() {
        var graph = new GraphBuilder();
        graph.SetVar("a", 0);
        graph.SetConst(1, None);
        graph.SetConst(2, I32);
        graph.SetStructInit(new[] { "age" }, new[] { 2 }, 3);
        graph.SetIfElse(new[] { 0 }, new[] { 1, 3 }, 4);
        graph.SetDef("y", 4);

        var result = graph.Solve();
        result.AssertNoGenerics();
        var s = StateStruct.Of("age", I32);
        result.AssertNamed(StateOptional.Of(s), "y");
    }

    #endregion

    #region Struct with optional field

    [Test(Description = "y = {a = none} → y = {a:None}")]
    public void StructWithNoneField() {
        var graph = new GraphBuilder();
        graph.SetConst(0, None);
        graph.SetStructInit(new[] { "a" }, new[] { 0 }, 1);
        graph.SetDef("y", 1);

        var result = graph.Solve();
        result.AssertNoGenerics();
        var s = StateStruct.Of("a", None);
        result.AssertNamed(s, "y");
    }

    [Test(Description = "y = {a = if(c) 1i else none} → y = {a:[opt(i32)..]}")]
    public void StructWithOptionalField_IfElse() {
        var graph = new GraphBuilder();
        graph.SetVar("c", 0);
        graph.SetConst(1, I32);
        graph.SetConst(2, None);
        graph.SetIfElse(new[] { 0 }, new[] { 1, 2 }, 3);
        graph.SetStructInit(new[] { "a" }, new[] { 3 }, 4);
        graph.SetDef("y", 4);

        var result = graph.Solve();
        // Field "a" is an output generic [opt(I32)..] — LCA(I32, None) = opt(I32) as descendant
        var yNode = result.GetVariableNode("y").GetNonReference();
        Assert.IsInstanceOf<StateStruct>(yNode.State);
        var fieldNode = ((StateStruct)yNode.State).GetFieldOrNull("a").GetNonReference();
        Assert.IsInstanceOf<ConstraintsState>(fieldNode.State);
        var cs = (ConstraintsState)fieldNode.State;
        Assert.IsTrue(cs.HasDescendant);
        Assert.IsInstanceOf<StateOptional>(cs.Descendant);
        Assert.AreEqual(StateOptional.Of(I32), cs.Descendant);
    }

    #endregion

    #region Optional array

    [Test(Description = "x:opt(i32[]); y = x → y = opt(i32[])")]
    public void OptionalArray_Propagates() {
        var graph = new GraphBuilder();
        graph.SetVarType("x", StateOptional.Of(StateArray.Of(I32)));
        graph.SetVar("x", 0);
        graph.SetDef("y", 0);

        var result = graph.Solve();
        result.AssertNoGenerics();
        result.AssertNamed(StateOptional.Of(StateArray.Of(I32)), "y");
    }

    [Test(Description = "y = if(a) [1i,2i] else none → y = opt(i32[])")]
    public void IfElse_ArrayOrNone() {
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

    #endregion

    #region Function returning Optional

    [Test(Description = "f():opt(i32); y = f() → y = opt(i32)")]
    public void FunReturnsOptional() {
        var graph = new GraphBuilder();
        var funType = StateFun.Of(StateOptional.Of(I32));
        graph.SetVarType("f", funType);
        graph.SetVar("f", 0);
        graph.SetCall(0, 1); // f() → node 1
        graph.SetDef("y", 1);

        var result = graph.Solve();
        result.AssertNoGenerics();
        result.AssertNamed(StateOptional.Of(I32), "y");
    }

    [Test(Description = "f(x:opt(i32)):bool; y = f(none) → y = bool")]
    public void FunTakesOptionalArg_PassNone() {
        var graph = new GraphBuilder();
        var funType = StateFun.Of(StateOptional.Of(I32), Bool);
        graph.SetVarType("f", funType);
        graph.SetVar("f", 0);
        graph.SetConst(1, None);
        graph.SetCall(0, 1, 2); // f(none) → node 2
        graph.SetDef("y", 2);

        var result = graph.Solve();
        result.AssertNoGenerics();
        result.AssertNamed(Bool, "y");
    }

    [Test(Description = "f(x:opt(i32)):bool; y = f(42i) → y = bool")]
    public void FunTakesOptionalArg_PassConcrete() {
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

    #endregion

    #region Optional of Optional

    [Test(Description = "x:opt(opt(i32)); y = x → y = opt(opt(i32))")]
    public void NestedOptional_Propagates() {
        var graph = new GraphBuilder();
        graph.SetVarType("x", StateOptional.Of(StateOptional.Of(I32)));
        graph.SetVar("x", 0);
        graph.SetDef("y", 0);

        var result = graph.Solve();
        result.AssertNoGenerics();
        result.AssertNamed(StateOptional.Of(StateOptional.Of(I32)), "y");
    }

    [Test(Description = "x:opt(i32); y = if(a) x else none → y = opt(i32), not opt(opt(i32))")]
    public void IfElse_OptOrNone_NoDoubleWrap() {
        // None ≤ opt(T) directly, so LCA(opt(i32), None) = opt(i32), not opt(opt(i32))
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

    #region If-else with Bool/Char + None

    [Test(Description = "y = if(a) true else none → y = opt(bool)")]
    public void IfElse_BoolOrNone() {
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

    [Test(Description = "y = if(a) /'x' else none → y = opt(char)")]
    public void IfElse_CharOrNone() {
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

    #endregion

    #region Multi-branch if-else

    [Test(Description = "y = if(a) 42i elif(b) none else 10i → y = opt(i32)")]
    public void IfElif_ConcreteNoneConcrete() {
        var graph = new GraphBuilder();
        graph.SetVar("a", 0);
        graph.SetVar("b", 1);
        graph.SetConst(2, I32);
        graph.SetConst(3, None);
        graph.SetConst(4, I32);
        graph.SetIfElse(new[] { 0, 1 }, new[] { 2, 3, 4 }, 5);
        graph.SetDef("y", 5);

        var result = graph.Solve();
        result.AssertNoGenerics();
        result.AssertNamed(StateOptional.Of(I32), "y");
    }

    [Test(Description = "x:opt(i32); y = if(a) x elif(b) none else 10i → y = opt(i32)")]
    public void IfElif_OptNoneConcrete() {
        var graph = new GraphBuilder();
        graph.SetVarType("x", StateOptional.Of(I32));
        graph.SetVar("a", 0);
        graph.SetVar("b", 1);
        graph.SetVar("x", 2);
        graph.SetConst(3, None);
        graph.SetConst(4, I32);
        graph.SetIfElse(new[] { 0, 1 }, new[] { 2, 3, 4 }, 5);
        graph.SetDef("y", 5);

        var result = graph.Solve();
        result.AssertNoGenerics();
        result.AssertNamed(StateOptional.Of(I32), "y");
    }

    #endregion

    #region Error cases

    [Test(Description = "x:opt(i32); y = x + 1 → TicError (opt not arithmetic)")]
    public void OptionalInArithmetic_Fails() {
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

    [Test(Description = "x:opt(i32); y:opt(real) = x → y = opt(real)")]
    public void OptionalCovariantAssign() {
        var graph = new GraphBuilder();
        graph.SetVarType("x", StateOptional.Of(I32));
        graph.SetVarType("y", StateOptional.Of(Real));
        graph.SetVar("x", 0);
        graph.SetDef("y", 0);

        var result = graph.Solve();
        result.AssertNoGenerics();
        result.AssertNamed(StateOptional.Of(I32), "x");
        result.AssertNamed(StateOptional.Of(Real), "y");
    }

    [Test(Description = "x:opt(real); y:opt(i32) = x → TicError (real !≤ i32)")]
    public void OptionalContravariantAssign_Fails() {
        TestHelper.AssertThrowsTicError(() => {
            var graph = new GraphBuilder();
            graph.SetVarType("x", StateOptional.Of(Real));
            graph.SetVarType("y", StateOptional.Of(I32));
            graph.SetVar("x", 0);
            graph.SetDef("y", 0);
            graph.Solve();
        });
    }

    [Test(Description = "f(x:i32):bool; f(none) → TicError")]
    public void FunTakesNonOptional_PassNone_Fails() {
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

    #region Generic function + Optional

    [Test(Description = "y = if(cond) x else none → x:T (generic), y:opt(T)")]
    public void GenericFun_IfElseXOrNone_Unconstrained() {
        // None branch wraps result in Optional; x stays generic.
        var graph = new GraphBuilder();
        graph.SetVar("cond", 0);
        graph.SetVar("x", 1);
        graph.SetConst(2, None);
        graph.SetIfElse(new[] { 0 }, new[] { 1, 2 }, 3);
        graph.SetDef("y", 3);

        var result = graph.Solve();
        // y should be StateOptional wrapping x's node
        var yNode = result.GetVariableNode("y").GetNonReference();
        Assert.IsInstanceOf<StateOptional>(yNode.State);
        // The element should be linked to x
        var elemNode = ((StateOptional)yNode.State).ElementNode.GetNonReference();
        var xNode = result.GetVariableNode("x").GetNonReference();
        Assert.AreEqual(elemNode, xNode, "Optional element should be linked to x");
    }

    [Test(Description = "z = if(cond) x else none; x:i32 → z = opt(i32)")]
    public void GenericFun_IfElseXOrNone_XConstrained() {
        // When x has a concrete type, LCA(I32, None) = opt(I32)
        var graph = new GraphBuilder();
        graph.SetVar("cond", 0);
        graph.SetVarType("x", I32);
        graph.SetVar("x", 1);
        graph.SetConst(2, None);
        graph.SetIfElse(new[] { 0 }, new[] { 1, 2 }, 3);
        graph.SetDef("z", 3);

        var result = graph.Solve();
        result.AssertNoGenerics();
        result.AssertNamed(StateOptional.Of(I32), "z");
    }

    #endregion

    #region Call chain with Optional

    [Test(Description = "g:opt(i32)→i32; f:i32→bool; y = f(g(none)) → y = bool")]
    public void CallChain_FofGofNone() {
        var graph = new GraphBuilder();
        graph.SetVarType("g", StateFun.Of(StateOptional.Of(I32), I32));
        graph.SetVarType("f", StateFun.Of(I32, Bool));
        graph.SetVar("g", 0);
        graph.SetConst(1, None);
        graph.SetCall(0, 1, 2);   // g(none) → node 2
        graph.SetVar("f", 3);
        graph.SetCall(3, 2, 4);   // f(g(none)) → node 4
        graph.SetDef("y", 4);

        var result = graph.Solve();
        result.AssertNoGenerics();
        result.AssertNamed(Bool, "y");
    }

    #endregion
}
