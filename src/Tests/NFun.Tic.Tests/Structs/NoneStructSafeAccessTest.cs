using NFun.Tic.SolvingStates;
using NUnit.Framework;

namespace NFun.Tic.Tests.Structs;

using static StatePrimitive;

/// <summary>
/// Atomic tests for None + safe field access.
/// Bug: None as source for SetSafeFieldAccess fails during TIC solving.
/// </summary>
public class NoneStructSafeAccessTest {

    // ═══════ ATOMIC: None as direct source for safe field access ═══════

    /// <summary>
    /// Simplest possible case:
    ///   0: none
    ///   SafeFieldAccess(0, 1, "x")  →  none?.x
    ///   y = 1
    ///
    /// none?.x should produce opt(T) where T is unconstrained.
    /// </summary>
    [Test]
    public void None_SafeFieldAccess_Solves() {
        var graph = new GraphBuilder();
        graph.SetConst(0, None);
        graph.SetSafeFieldAccess(0, 1, "x");
        graph.SetDef("y", 1);

        var result = graph.Solve();
        var y = result.GetVariableNode("y").GetNonReference();
        // y should be opt(T) — the safe access on None
        Assert.IsTrue(y.State is StateOptional or ConstraintsState,
            $"y should be optional or constrained, got {y.State}");
    }

    /// <summary>
    /// None as source + coalesce to force concrete type:
    ///   0: none
    ///   SafeFieldAccess(0, 1, "x")  →  none?.x  →  opt(T)
    ///   2: const i32
    ///   Coalesce(1, 2, 3)  →  T = i32
    ///   y = 3
    /// </summary>
    [Test]
    public void None_SafeFieldAccess_Coalesce_ReturnsI32() {
        var graph = new GraphBuilder();
        graph.SetConst(0, None);
        graph.SetSafeFieldAccess(0, 1, "x");
        graph.SetConst(2, I32);

        var t = graph.InitializeVarNode();
        graph.SetCall(new ITicNodeState[] { StateOptional.Of(t), t, t }, new[] { 1, 2, 3 });
        graph.SetDef("y", 3);

        var result = graph.Solve();
        result.AssertNoGenerics();
        result.AssertNamed(I32, "y");
    }

    // ═══════ ATOMIC: struct field = None, then safe access on that field ═══════

    /// <summary>
    /// struct{inner=none}.inner?.x
    ///   0: none
    ///   1: struct{inner=0}
    ///   FieldAccess(1, 2, "inner")  →  s.inner = None
    ///   SafeFieldAccess(2, 3, "x")  →  s.inner?.x
    ///   y = 3
    /// </summary>
    [Test]
    public void StructWithNoneField_SafeAccess() {
        var graph = new GraphBuilder();
        graph.SetConst(0, None);
        graph.SetStructInit(new[] { "inner" }, new[] { 0 }, 1);
        graph.SetDef("s", 1);

        graph.SetVar("s", 2);
        graph.SetFieldAccess(2, 3, "inner");
        graph.SetSafeFieldAccess(3, 4, "x");
        graph.SetDef("y", 4);

        var result = graph.Solve();
        var y = result.GetVariableNode("y").GetNonReference();
        Assert.IsTrue(y.State is StateOptional or ConstraintsState,
            $"y should be optional or constrained, got {y.State}");
    }

    /// <summary>
    /// Same + coalesce:
    /// struct{inner=none}.inner?.x ?? -1
    /// </summary>
    [Test]
    public void StructWithNoneField_SafeAccess_Coalesce() {
        var graph = new GraphBuilder();
        graph.SetConst(0, None);
        graph.SetStructInit(new[] { "inner" }, new[] { 0 }, 1);
        graph.SetDef("s", 1);

        graph.SetVar("s", 2);
        graph.SetFieldAccess(2, 3, "inner");
        graph.SetSafeFieldAccess(3, 4, "x");
        graph.SetConst(5, I32);

        var t = graph.InitializeVarNode();
        graph.SetCall(new ITicNodeState[] { StateOptional.Of(t), t, t }, new[] { 4, 5, 6 });
        graph.SetDef("y", 6);

        var result = graph.Solve();
        result.AssertNoGenerics();
        result.AssertNamed(I32, "y");
    }

    // ═══════ THE BUG: nested struct + chained safe access ═══════

    /// <summary>
    /// Minimal bug repro:
    ///   inner = {x = 2, inner = none}
    ///   outer = {inner = inner_struct}
    ///   outer.inner?.inner?.x
    ///
    /// Two-level safe access chain where inner field is None.
    /// </summary>
    [Test]
    public void NestedStruct_NoneField_ChainedSafeAccess() {
        var graph = new GraphBuilder();

        // inner = {x = i32, inner = none}
        graph.SetConst(0, I32);
        graph.SetConst(1, None);
        graph.SetStructInit(new[] { "x", "inner" }, new[] { 0, 1 }, 2);

        // outer = {inner = inner_struct}
        graph.SetStructInit(new[] { "inner" }, new[] { 2 }, 3);
        graph.SetDef("a", 3);

        // a.inner?.inner?.x
        graph.SetVar("a", 4);
        graph.SetFieldAccess(4, 5, "inner");
        graph.SetSafeFieldAccess(5, 6, "inner");
        graph.SetSafeFieldAccess(6, 7, "x");
        graph.SetDef("y", 7);

        var result = graph.Solve();
        var y = result.GetVariableNode("y").GetNonReference();
        Assert.IsTrue(y.State is StateOptional or ConstraintsState,
            $"y should be optional or constrained, got {y.State}");
    }

    /// <summary>
    /// Same as above + coalesce (exact repro of the Syntax-level bug).
    /// </summary>
    [Test]
    public void NestedStruct_NoneField_ChainedSafeAccess_Coalesce() {
        var graph = new GraphBuilder();

        // inner = {x = i32, inner = none}
        graph.SetConst(0, I32);
        graph.SetConst(1, None);
        graph.SetStructInit(new[] { "x", "inner" }, new[] { 0, 1 }, 2);

        // outer = {inner = inner_struct}
        graph.SetStructInit(new[] { "inner" }, new[] { 2 }, 3);
        graph.SetDef("a", 3);

        // a.inner?.inner?.x ?? -1
        graph.SetVar("a", 4);
        graph.SetFieldAccess(4, 5, "inner");
        graph.SetSafeFieldAccess(5, 6, "inner");
        graph.SetSafeFieldAccess(6, 7, "x");

        graph.SetConst(8, I32);
        var t = graph.InitializeVarNode();
        graph.SetCall(new ITicNodeState[] { StateOptional.Of(t), t, t }, new[] { 7, 8, 9 });
        graph.SetDef("y", 9);

        var result = graph.Solve();
        result.AssertNoGenerics();
        result.AssertNamed(I32, "y");
    }

    // ═══════ THE BUG with ancestor constraint (named type) ═══════

    /// <summary>
    /// Named type scenario: struct init has ancestor constraint from type definition.
    /// type node = {v:int, next:node?}
    /// node{v=3} — no "next" provided, default = none
    ///
    /// The ancestor constraint adds opt(struct{v:int, next:opt(Empty)}) on the "next" field.
    /// Deep access chain conflicts with none default.
    /// </summary>
    [Test]
    public void NestedStruct_WithAncestorConstraint_ChainedSafeAccess() {
        var graph = new GraphBuilder();

        // Ancestor type: struct{v:int, next:opt(struct{v:int, next:opt(Empty)})}
        // This is what ResolveNamedStruct creates at depth=0 + depth=1
        var innerOptElement = StateStruct.Of(false,
            ("v", (ITicNodeState)I32),
            ("next", StateOptional.Of(ConstraintsState.Empty)));
        var ancestorType = StateStruct.Of(false,
            ("v", (ITicNodeState)I32),
            ("next", StateOptional.Of(innerOptElement)));

        // inner = {v = i32, next = none}  with ancestor
        graph.SetConst(0, I32);
        graph.SetConst(1, None);
        graph.SetStructInit(new[] { "v", "next" }, new[] { 0, 1 }, 2);
        graph.SetStructInitType(2, ancestorType);

        // outer = {v = i32, next = inner}  with ancestor
        graph.SetConst(3, I32);
        graph.SetStructInit(new[] { "v", "next" }, new[] { 3, 2 }, 4);
        graph.SetStructInitType(4, ancestorType);

        // outermost = {v = i32, next = outer}  with ancestor
        graph.SetConst(5, I32);
        graph.SetStructInit(new[] { "v", "next" }, new[] { 5, 4 }, 6);
        graph.SetStructInitType(6, ancestorType);
        graph.SetDef("n", 6);

        // n.next?.next?.next?.v ?? -1
        graph.SetVar("n", 7);
        graph.SetFieldAccess(7, 8, "next");
        graph.SetSafeFieldAccess(8, 9, "next");
        graph.SetSafeFieldAccess(9, 10, "next");
        graph.SetSafeFieldAccess(10, 11, "v");

        graph.SetConst(12, I32);
        var t = graph.InitializeVarNode();
        graph.SetCall(new ITicNodeState[] { StateOptional.Of(t), t, t }, new[] { 11, 12, 13 });
        graph.SetDef("y", 13);

        var result = graph.Solve();
        result.AssertNoGenerics();
        result.AssertNamed(I32, "y");
    }

    // ═══════ CONTROL: explicit opt type — should work ═══════

    /// <summary>
    /// Same structure but with explicit Optional type on the inner field.
    /// inner_val: opt({x:i32}) = none; outer = {inner = inner_val}
    /// outer.inner?.x ?? -1
    /// </summary>
    [Test]
    public void ExplicitOptionalField_SafeAccess_Works() {
        var graph = new GraphBuilder();

        graph.SetVarType("inner_val", StateOptional.Of(StateStruct.Of("x", I32)));
        graph.SetConst(0, None);
        graph.SetDef("inner_val", 0);

        graph.SetConst(1, I32);
        graph.SetVar("inner_val", 2);
        graph.SetStructInit(new[] { "x", "inner" }, new[] { 1, 2 }, 3);
        graph.SetDef("a", 3);

        graph.SetVar("a", 4);
        graph.SetFieldAccess(4, 5, "inner");
        graph.SetSafeFieldAccess(5, 6, "x");

        graph.SetConst(7, I32);
        var t = graph.InitializeVarNode();
        graph.SetCall(new ITicNodeState[] { StateOptional.Of(t), t, t }, new[] { 6, 7, 8 });
        graph.SetDef("y", 8);

        var result = graph.Solve();
        result.AssertNoGenerics();
        result.AssertNamed(I32, "y");
    }
}
