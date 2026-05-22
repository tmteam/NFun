using System.Collections.Generic;
using NFun.Tic.SolvingStates;
using NUnit.Framework;

namespace NFun.Tic.Tests.Structs;

using static StatePrimitive;

/// <summary>
/// MR5Bug7 — `a:opt(struct{f:int->int}); y = a?.f(7)` loses the precise return type.
///
/// Background: TicSetupVisitor (line 931..959) handles `?.method()` as a 3-step graph:
///
///   1. unwrap source:   SetCallArgument(opt(unwrappedNode), sourceId)
///                       — establishes `source ≤ opt(unwrappedNode)`
///   2. method call:     SetCall(unwrappedNode, [arg=I32, rawResult]) when the unwrapped
///                       node's state resolves to a StateFun([I32]→I32)
///   3. wrap result:     rawResult.AddAncestor(actualResult); none.AddAncestor(actualResult)
///                       — actualResult = LCA(rawResult, None) = opt(I32)
///
/// The 3-step chain (opt → struct(open) → fun) requires Pull to propagate field
/// types from a's struct through the unwrappedNode into the SetCall machinery. The
/// hypothesis is that topological Pull doesn't complete this chain — the unwrappedNode
/// stays empty (or open-struct) at the moment SetCall is built around it, so the call's
/// return position resolves to Any instead of I32.
///
/// These tests probe the chain directly via GraphBuilder, no syntax pipeline.
/// </summary>
public class MR5Bug7SafeMethodCallTest {

    [SetUp]
    public void Initialize() => TraceLog.IsEnabled = true;

    [TearDown]
    public void Deinitialize() => TraceLog.IsEnabled = false;

    // ═══════════════════════════════════════════════════════════════════
    // 1. DIRECT REPRO — `a:opt(struct{f:int->int}); y = a?.f(7)`
    //
    //    Build the exact 3-step graph that TicSetupVisitor produces:
    //      a       : opt(struct{f: (I32)->I32})
    //      arg     : I32 (7)
    //      result  : LCA(rawResult, None) = opt(I32)
    //
    //    Expected: y.State is StateOptional(I32 or RefTo(I32))
    //    On master: y.State is opt(Any) — return type was widened
    // ═══════════════════════════════════════════════════════════════════
    [Test]
    public void OptStruct_SafeMethodCall_PreservesReturnType() {
        // After the MR5Bug7 fix, `?.method()` is emitted via the single TIC special form
        // SetSafeMethodCall (mirroring SetSafeFieldAccess), which connects the field-
        // function's return slot directly to the result's Optional element. Pull threads
        // the source's concrete function-typed field through to the result in one cascade.
        //
        // Equivalent of: `a:opt(struct{f:(I32)->I32}); y = a?.f(7)`
        var graph = new GraphBuilder();

        // a : opt(struct{f: (I32)->I32})
        var funType = StateFun.Of(I32, I32);
        var structType = StateStruct.Of("f", funType);
        graph.SetVarType("a", StateOptional.Of(structType));

        // node 0 = a (variable read), node 1 = literal 7 (I32), node 2 = call result (y)
        graph.SetVar("a", 0);
        graph.SetConst(1, I32);
        graph.SetSafeMethodCall(0, new[] { 1 }, 2, "f");
        graph.SetDef("y", 2);

        var result = graph.Solve();
        var y = result.GetVariableNode("y").GetNonReference();

        TestContext.WriteLine($"y final state = {y.State}");
        if (y.State is StateOptional optY)
            TestContext.WriteLine($"  y element state = {optY.ElementNode.GetNonReference().State}");

        Assert.IsTrue(y.State is StateOptional,
            $"y should be Optional (?.call always returns opt(R)), got {y.State}");

        var elem = ((StateOptional)y.State).ElementNode.GetNonReference().State;
        Assert.IsTrue(
            elem is StatePrimitive { Name: PrimitiveTypeName.I32 }
            || (elem is StateRefTo r && r.Node.GetNonReference().State is StatePrimitive { Name: PrimitiveTypeName.I32 }),
            $"y should be opt(I32), but element is {elem}");
    }

    // ═══════════════════════════════════════════════════════════════════
    // 2. DIAGNOSTIC — emit ONLY the receiver-unwrap edge, then assert.
    //
    //    Build:
    //      a : opt(struct{f: (I32)->I32})
    //      SetCallArgument(opt(unwrapped), a-source)
    //
    //    After Solve, the unwrappedNode SHOULD be struct{f: (I32)->I32}.
    //    If it's still ConstraintsState/open-struct, that confirms Pull
    //    does not propagate through opt-element edges before downstream
    //    consumers (like SetCall) read it.
    //
    //    NB: in the real TicSetupVisitor, SetCall around `unwrapped` runs
    //    immediately at graph-construction time — so even if Solve later
    //    propagates, the SetCall snapshot is captured too early. This test
    //    isolates whether Pull propagates AT ALL through the opt-unwrap.
    // ═══════════════════════════════════════════════════════════════════
    [Test]
    public void OptStruct_UnwrapOnly_PropagatesStructToUnwrappedNode() {
        var graph = new GraphBuilder();
        graph.IsRecursion = true;

        var funType = StateFun.Of(I32, I32);
        var structType = StateStruct.Of("f", funType);
        graph.SetVarType("a", StateOptional.Of(structType));

        graph.SetVar("a", 0);

        var unwrapped = graph.GetOrCreateNode(1);
        graph.SetCallArgument(StateOptional.Of(unwrapped), 0);

        // Pin unwrapped as an output so Finalize doesn't drop it.
        graph.SetDef("u", 1);

        var result = graph.Solve();
        var u = result.GetVariableNode("u").GetNonReference();

        TestContext.WriteLine($"u final state = {u.State}");

        // After Pull, unwrapped should carry the struct{f: (I32)->I32} shape.
        Assert.IsTrue(u.State is StateStruct,
            $"u (unwrapped element of opt) should be StateStruct, got {u.State}");
        var s = (StateStruct)u.State;
        var fNode = s.GetFieldOrNull("f");
        Assert.IsNotNull(fNode, "unwrapped struct should have field 'f' from source");
        var fState = fNode.GetNonReference().State;
        TestContext.WriteLine($"  u.f state = {fState}");
        Assert.IsTrue(fState is StateFun,
            $"u.f should be StateFun (I32->I32), got {fState}");
    }

    // ═══════════════════════════════════════════════════════════════════
    // 3. FIX-ANTICIPATING — wire the call directly to the source struct's field.
    //
    //    Bypass the open-struct-via-unwrap dance: instead, SetFieldAccess on the
    //    source-struct-node (the element-node of `a`'s Optional state) directly,
    //    then SetCall on the resulting RefTo. This is what SHOULD happen if Pull
    //    correctly propagated the struct shape into the unwrapped node before
    //    SetCall ran.
    //
    //    Expected: y resolves to opt(I32) cleanly.
    // ═══════════════════════════════════════════════════════════════════
    [Test]
    public void OptStruct_DirectFieldThenCall_ResolvesCleanly() {
        var graph = new GraphBuilder();
        graph.IsRecursion = true;

        // Build a's element struct-node manually so we can hand it to SetFieldAccess directly.
        var fNode = graph.CreateVarType(StateFun.Of(I32, I32));
        var fields = new Dictionary<string, TicNode>(System.StringComparer.OrdinalIgnoreCase) { { "f", fNode } };
        var structNode = graph.CreateVarType(new StateStruct(fields, isFrozen: false, isOpen: false));
        graph.SetVarType("a", StateOptional.Of(structNode));

        // Node 0 = direct alias to the struct-node (bypass the opt-unwrap dance —
        // simulate what `unwrapped` SHOULD resolve to BEFORE SetCall runs).
        graph.GetOrCreateNode(0).State = new StateRefTo(structNode);

        graph.SetFieldAccess(0, 1, "f");      // node 1 = ref to f
        graph.SetConst(2, I32);                // node 2 = arg 7
        graph.SetCall(1, 2, 3);                // node 3 = call result

        // Wrap raw result in Optional: actualResult = LCA(rawResult, None) = opt(I32)
        var actualResult = graph.GetOrCreateNode(4);
        graph.GetOrCreateNode(3).AddAncestor(actualResult);
        var noneNode = graph.CreateVarType(None);
        noneNode.AddAncestor(actualResult);
        graph.SetDef("y", 4);

        var result = graph.Solve();
        var y = result.GetVariableNode("y").GetNonReference();
        TestContext.WriteLine($"y final state = {y.State}");

        Assert.IsTrue(y.State is StateOptional,
            $"y should be Optional, got {y.State}");
        var elem = ((StateOptional)y.State).ElementNode.GetNonReference().State;
        TestContext.WriteLine($"  y element = {elem}");
        Assert.IsTrue(
            elem is StatePrimitive { Name: PrimitiveTypeName.I32 }
            || (elem is StateRefTo r && r.Node.GetNonReference().State is StatePrimitive { Name: PrimitiveTypeName.I32 }),
            $"y should be opt(I32), but element is {elem}");
    }

    // ═══════════════════════════════════════════════════════════════════
    // 4. CONTROL: NO-CALL PATH — `g = a?.f` (field-only, no invocation)
    //
    //    Build:  a : opt(struct{f: (I32)->I32})
    //            g = a?.f      via SetSafeFieldAccess
    //
    //    Expected: g.State is opt(fun(I32)->I32)
    //    Master: works correctly. This is the baseline that proves the chain
    //    breaks ONLY when the second hop (SetCall on the unwrapped) is added.
    // ═══════════════════════════════════════════════════════════════════
    [Test]
    public void OptStruct_SafeFieldOnly_PreservesFunType() {
        var graph = new GraphBuilder();
        graph.IsRecursion = true;

        var funType = StateFun.Of(I32, I32);
        var structType = StateStruct.Of("f", funType);
        graph.SetVarType("a", StateOptional.Of(structType));

        graph.SetVar("a", 0);
        graph.SetSafeFieldAccess(0, 1, "f");
        graph.SetDef("g", 1);

        var result = graph.Solve();
        var g = result.GetVariableNode("g").GetNonReference();

        TestContext.WriteLine($"g final state = {g.State}");

        Assert.IsTrue(g.State is StateOptional,
            $"g should be Optional, got {g.State}");

        var elem = ((StateOptional)g.State).ElementNode.GetNonReference().State;
        TestContext.WriteLine($"  g element = {elem}");

        Assert.IsTrue(
            elem is StateFun
            || (elem is StateRefTo r && r.Node.GetNonReference().State is StateFun),
            $"g element should be StateFun (I32->I32), got {elem}");
    }

    // ═══════════════════════════════════════════════════════════════════
    // 5. CONTROL: NON-OPT METHOD CALL — `a:struct{f:I32->I32}; y = a.f(7)`
    //
    //    The non-Optional analogue. Should resolve cleanly to I32 (NOT opt(I32)).
    //    Master: works. Confirms the bug is purely the opt-unwrap → struct → fun
    //    propagation, not the field-then-call composition itself.
    // ═══════════════════════════════════════════════════════════════════
    [Test]
    public void NonOptStruct_MethodCall_ResolvesCleanly() {
        var graph = new GraphBuilder();

        var funType = StateFun.Of(I32, I32);
        var structType = StateStruct.Of("f", funType);
        graph.SetVarType("a", structType);

        graph.SetVar("a", 0);
        graph.SetFieldAccess(0, 1, "f");      // node 1 = ref to f
        graph.SetConst(2, I32);                // node 2 = arg 7
        graph.SetCall(1, 2, 3);                // node 3 = call result
        graph.SetDef("y", 3);

        var result = graph.Solve();
        var y = result.GetVariableNode("y").GetNonReference();

        TestContext.WriteLine($"y final state = {y.State}");

        Assert.IsTrue(
            y.State is StatePrimitive { Name: PrimitiveTypeName.I32 }
            || (y.State is StateRefTo r && r.Node.GetNonReference().State is StatePrimitive { Name: PrimitiveTypeName.I32 }),
            $"y should be I32, got {y.State}");
    }
}
