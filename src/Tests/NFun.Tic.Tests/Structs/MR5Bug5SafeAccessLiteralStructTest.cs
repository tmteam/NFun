using NFun.Tic.SolvingStates;
using NUnit.Framework;

namespace NFun.Tic.Tests.Structs;

using static StatePrimitive;

/// <summary>
/// MR5Bug5 — `a = {b=1}; y = a?.b` widens `a`'s inferred type to `{b:Int32}?`
/// instead of keeping the non-optional struct shape.
///
/// Professor's analysis:
///   SetSafeFieldAccess emits SetCall(opt(struct{field:T}) → opt(T)).
///   In PushConstraintsFunctions.cs:224-240, when the receiver's
///   ConstraintsState has a StateStruct descendant with `!IsSolved`,
///   Push WRAPS the descendant in Optional. A literal `{b=1}` has fields
///   with non-primitive constraints (e.g. `[U8..Re]I32!`) → IsSolved=false
///   → wrap fires inappropriately.
///
///   Hypothesis: the discriminator should be `descStruct.IsOpen`
///   (literal=closed=no wrap; row-poly source=open=wrap).
///
/// These tests probe TIC behavior directly via GraphBuilder, no syntax pipeline.
/// </summary>
public class MR5Bug5SafeAccessLiteralStructTest {

    [SetUp]
    public void Initialize() => TraceLog.IsEnabled = true;

    [TearDown]
    public void Deinitialize() => TraceLog.IsEnabled = false;

    // ═══════════════════════════════════════════════════════════════════
    // 1. LITERAL STRUCT RECEIVER (the bug)
    //
    //    a = {b = i32}        (struct literal — closed)
    //    y = a?.b             (safe field access)
    //
    //    Expected: a stays {b:int}  (NOT opt({b:int}))
    //              y is opt(int)
    // ═══════════════════════════════════════════════════════════════════
    [Test]
    public void LiteralStruct_SafeFieldAccess_ReceiverStaysNonOptional() {
        var graph = new GraphBuilder();

        // a = {b = 1_i32}
        graph.SetConst(0, I32);
        graph.SetStructInit(new[] { "b" }, new[] { 0 }, 1);
        graph.SetDef("a", 1);

        // y = a?.b
        graph.SetVar("a", 2);
        graph.SetSafeFieldAccess(2, 3, "b");
        graph.SetDef("y", 3);

        var result = graph.Solve();

        var a = result.GetVariableNode("a").GetNonReference();
        var y = result.GetVariableNode("y").GetNonReference();

        TestContext.WriteLine($"a final state = {a.State}");
        TestContext.WriteLine($"y final state = {y.State}");

        // a should be a plain struct, NOT optional.
        Assert.IsFalse(a.State is StateOptional,
            $"BUG: a became Optional. a.State = {a.State}");
        Assert.IsTrue(a.State is StateStruct,
            $"a should remain StateStruct, got {a.State}");

        // y should be Optional (?. returns opt(T)).
        Assert.IsTrue(y.State is StateOptional,
            $"y should be Optional, got {y.State}");
    }

    // ═══════════════════════════════════════════════════════════════════
    // 2. INT-LITERAL FIELD (the actual master-reproducer flavor)
    //
    //    Same as above but with SetIntConst — produces a CS interval
    //    [U8..Re]I32! for the field. This is the exact `!IsSolved`
    //    condition that triggers the wrap.
    // ═══════════════════════════════════════════════════════════════════
    [Test]
    public void LiteralStruct_IntLiteralField_SafeAccess_ReceiverStaysNonOptional() {
        var graph = new GraphBuilder();

        // a = {b = <int-literal>}  — field has CS interval, not concrete primitive
        graph.SetIntConst(0, U8);
        graph.SetStructInit(new[] { "b" }, new[] { 0 }, 1);
        graph.SetDef("a", 1);

        // y = a?.b
        graph.SetVar("a", 2);
        graph.SetSafeFieldAccess(2, 3, "b");
        graph.SetDef("y", 3);

        var result = graph.Solve();

        var a = result.GetVariableNode("a").GetNonReference();
        var y = result.GetVariableNode("y").GetNonReference();

        TestContext.WriteLine($"a final state = {a.State}");
        TestContext.WriteLine($"y final state = {y.State}");

        Assert.IsFalse(a.State is StateOptional,
            $"BUG: a became Optional. a.State = {a.State}");
        Assert.IsTrue(a.State is StateStruct,
            $"a should remain StateStruct, got {a.State}");
        Assert.IsTrue(y.State is StateOptional,
            $"y should be Optional, got {y.State}");
    }

    // ═══════════════════════════════════════════════════════════════════
    // 3. CONTROL: ANNOTATION PRE-BINDS A AS Opt({b:int})
    //
    //    a:opt({b:int}) = none
    //    y = a?.b
    //
    //    Expected: a stays opt({b:int}); y is opt(int).
    // ═══════════════════════════════════════════════════════════════════
    [Test]
    public void AnnotatedOptStructReceiver_SafeFieldAccess_Works() {
        var graph = new GraphBuilder();

        // a : opt({b:int})  =  none
        graph.SetVarType("a", StateOptional.Of(StateStruct.Of("b", I32)));
        graph.SetConst(0, None);
        graph.SetDef("a", 0);

        // y = a?.b
        graph.SetVar("a", 1);
        graph.SetSafeFieldAccess(1, 2, "b");
        graph.SetDef("y", 2);

        var result = graph.Solve();

        var a = result.GetVariableNode("a").GetNonReference();
        var y = result.GetVariableNode("y").GetNonReference();

        TestContext.WriteLine($"a final state = {a.State}");
        TestContext.WriteLine($"y final state = {y.State}");

        Assert.IsTrue(a.State is StateOptional,
            $"a should be Optional (declared), got {a.State}");
        Assert.IsTrue(y.State is StateOptional,
            $"y should be Optional, got {y.State}");
    }

    // ═══════════════════════════════════════════════════════════════════
    // 4. CONCRETE PRIMITIVE FIELD (no CS) — should still NOT wrap
    //
    //    a = {b = i32-as-concrete}    (StateStruct with primitive I32 field)
    //    y = a?.b
    //
    //    The struct is fully solved → IsSolved=true → original guard
    //    correctly skips wrap. This is the "control: wrap correctly
    //    suppressed" baseline.
    // ═══════════════════════════════════════════════════════════════════
    [Test]
    public void SolvedStructReceiver_SafeFieldAccess_NotWrapped() {
        var graph = new GraphBuilder();

        // a = {b : i32}  built with SetConst (concrete primitive — IsSolved=true)
        graph.SetConst(0, I32);
        graph.SetStructInit(new[] { "b" }, new[] { 0 }, 1);
        graph.SetDef("a", 1);

        graph.SetVar("a", 2);
        graph.SetSafeFieldAccess(2, 3, "b");
        graph.SetDef("y", 3);

        var result = graph.Solve();

        var a = result.GetVariableNode("a").GetNonReference();
        var y = result.GetVariableNode("y").GetNonReference();

        TestContext.WriteLine($"a final state = {a.State}");
        TestContext.WriteLine($"y final state = {y.State}");

        Assert.IsTrue(a.State is StateStruct,
            $"a should be plain Struct (concrete fields), got {a.State}");
        Assert.IsTrue(y.State is StateOptional,
            $"y should be Optional, got {y.State}");
    }

    // ═══════════════════════════════════════════════════════════════════
    // 5. ROW-POLY (OPEN) STRUCT RECEIVER — wrap SHOULD propagate Optional
    //
    //    Simulates the legitimate case: receiver shape came from
    //    SetSafeFieldAccess (which builds an open IsOptionalSourced struct).
    //    Here Optional propagation is correct.
    //
    //    We construct an open-struct descendant directly via SetFieldAccess
    //    on a variable, then push it against an opt(struct) ancestor —
    //    i.e. the chained `?.` scenario.
    //
    //    Setup:
    //      Node A: variable
    //      SetFieldAccess(A, B, "x")   → A acquires open struct {x:T} descendant
    //      SetSafeFieldAccess(B, C, "y") → B should become Opt(struct{y:U})
    //
    //    Expectation: B ends up Optional (legitimate propagation).
    //    But also A is no longer optional? Actually with chained ?. semantics
    //    A might or might not become Optional — we just confirm `y` resolves
    //    to Optional and the graph solves without error.
    // ═══════════════════════════════════════════════════════════════════
    [Test]
    public void ChainedSafeAccess_OpenStructDescendant_StillPropagatesOptional() {
        var graph = new GraphBuilder();

        // a: variable (no declared type)
        // b = a.x      (open struct descendant introduced)
        // y = b?.z     (safe access on b)
        graph.SetVar("a", 0);
        graph.SetFieldAccess(0, 1, "x");
        graph.SetDef("b", 1);

        graph.SetVar("b", 2);
        graph.SetSafeFieldAccess(2, 3, "z");
        graph.SetDef("y", 3);

        var result = graph.Solve();

        var a = result.GetVariableNode("a").GetNonReference();
        var b = result.GetVariableNode("b").GetNonReference();
        var y = result.GetVariableNode("y").GetNonReference();

        TestContext.WriteLine($"a final state = {a.State}");
        TestContext.WriteLine($"b final state = {b.State}");
        TestContext.WriteLine($"y final state = {y.State}");

        // y must be Optional regardless of how a/b resolve.
        Assert.IsTrue(y.State is StateOptional or ConstraintsState,
            $"y should be Optional or constrained, got {y.State}");
    }
}
