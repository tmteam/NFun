using NFun.TestTools;
using NFun.Tic;
using NUnit.Framework;

namespace NFun.SyntaxTests;

/// <summary>
/// Bugs found by automated bug-hunt (2026-07-02, 300-iteration run with float focus).
/// Each remaining test pins the spec-expected behavior for an unfixed bug. Un-ignore when fixed.
///
/// Resolved in this pass (regression pins live in their feature files):
///   Bug #1 — convert(float32):int now truncates (spec-compliant, matches real→int)
///   Bug #2 — max/min propagate NaN uniformly (float32 and real)
///   Bug #3 — convert(float32 65.0):char yields 'A' (Java-style: truncate then cast)
///   Bug #5 — if-else array literals preserve real-literal precision via LCA of Preferreds
/// </summary>
public class BugHuntResults {
    [SetUp]
    public void Initialize() => TraceLog.IsEnabled = true;

    [TearDown]
    public void Deinitialize() => TraceLog.IsEnabled = false;

    // ── Bug #6 — inline struct + optional narrowing-numeric field + none ────────
    // `arr:{v:float32?}[] = [{v=1.5}, {v=none}]` throws FU719, but:
    // - named-type wrapper `type S = {v:float32?}; arr:S[] = [S{v=1.5}, S{v=none}]` works
    // - `arr:{v:int32?}[] = [{v=1}, {v=none}]` works (I32 Preferred fits I32 target)
    // - `arr:float32?[] = [1.5, none]` (no struct) works
    // Triggers for narrowing numeric optional field (float32, byte, uint16, int16, uint32)
    // where the literal's Preferred can't satisfy the target field's primitive type.
    // Workaround: use a named type wrapper.
    [Test]
    public void Bug6_InlineStructOptionalFloat32Field_WithNone_ShouldBuild() =>
        Assert.DoesNotThrow(() =>
            "arr:{v:float32?}[] = [{v=1.5}, {v=none}]".BuildWithFloatsAndOptional());

    // ── Bug-hunt 2026-07-03 (pre-push run on b89f3183) ───────────────────────
    // Regression pin: nested struct-arrays with an all-none inner sibling must
    // infer without annotation ([[{v=1.5},{v=none}],[{v=none}]] → {v:Real?}[][]).
    // Worked on origin/master; the ported Push CS{Desc=arr}×arr descent arm broke
    // it and was removed as unnecessary for master (the rest of the Bug#6 set
    // covers all previously-failing shapes).
    [Test]
    public void NestedStructArrays_AllNoneInnerSibling_Infers() {
        var rt = "m = [[{v=1.5},{v=none}],[{v=none}]]\rout = m.count()"
            .BuildWithFloatsAndOptional();
        Assert.DoesNotThrow(() => rt.Run());
    }

    // Pre-existing (fails on origin/master too, as FU719; FU710 here): a struct-
    // array literal with a numeric optional field rejects a {v=none} element when
    // the array has 3+ elements and none is NOT first. 2-element and none-first
    // shapes work. Boundary: [{v=none},{v=1},{v=255}] OK; [{v=1},{v=none},{v=255}]
    // and [{v=1},{v=255},{v=none}] fail.
    [Test]
    [Ignore("Bug hunt 2026-07-03 #A: 3+ element struct-array literal with none after a value fails; none-first and 2-element shapes work")]
    public void ThreeElementStructArray_NoneAfterValue_ShouldBuild() =>
        Assert.DoesNotThrow(() =>
            "arr:{v:byte?}[] = [{v=1},{v=none},{v=255}]".BuildWithFloatsAndOptional());
}
