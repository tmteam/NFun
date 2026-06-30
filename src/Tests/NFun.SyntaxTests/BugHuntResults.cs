using NFun;
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
    [Ignore("BugHunt#6: inline {v:float32?}[] with mixed value+none fails FU719; named-type wrapper works")]
    public void Bug6_InlineStructOptionalFloat32Field_WithNone_ShouldBuild() =>
        Assert.DoesNotThrow(() =>
            "arr:{v:float32?}[] = [{v=1.5}, {v=none}]".BuildWithFloatsAndOptional());
}
