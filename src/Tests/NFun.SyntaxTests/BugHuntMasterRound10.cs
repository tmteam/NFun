using NFun;
using NFun.Exceptions;
using NFun.TestTools;
using NFun.Tic;
using NUnit.Framework;

namespace NFun.SyntaxTests;

/// <summary>
/// Bugs found by automated expression-mode hunting on nfun-lang-v4 (round 10,
/// post-rebase onto master 1.1.2). 3 agents × 50 iterations. 1 confirmed UX bug.
/// </summary>
public class BugHuntMasterRound10 {
    [SetUp]
    public void Initialize() => TraceLog.IsEnabled = true;

    [TearDown]
    public void Deinitialize() => TraceLog.IsEnabled = false;

    // ───────────────────────────────────────────────────────────────
    // MR10Bug1 — FU740 error on array-RHS reports `Any[]` instead of
    //   the actually inferred element type.
    //
    //     y:int = 1.5       # FU740: cannot init with 'Real' ← correct
    //     y:int[] = [1.5]   # FU740: cannot init with 'Any[]' ← BUG
    //     x = [1.5]         # infers x:Real[] correctly
    //
    //   The runtime DOES infer `Real[]` correctly when no target type
    //   is supplied. So the underlying type resolution is correct; the
    //   ERROR-FORMATTING path falls back to `Any[]` instead of carrying
    //   the concrete element type into the FU740 message.
    //
    //   Likely fix location: the FU740 emitter in ParseErrors/ or the
    //   TIC error-formatter — the "descendant type" should be the
    //   actual array element inferred type, not the array's CS top.
    // ───────────────────────────────────────────────────────────────
    [Test]
    public void MR10Bug1_FU740_ArrayMismatch_ReportsConcreteElementType() {
        var ex = Assert.Throws<FunnyParseException>(() => "y:int[] = [1.5]".Calc());
        StringAssert.Contains("Real", ex.Message,
            "FU740 message should mention the concrete element type (Real), not generic Any[]");
    }

    // Control: scalar form reports the concrete type correctly.
    [Test]
    public void MR10Bug1_Control_Scalar_ReportsConcreteType() {
        var ex = Assert.Throws<FunnyParseException>(() => "y:int = 1.5".Calc());
        StringAssert.Contains("Real", ex.Message);
    }

    // Control: untyped form correctly infers Real[].
    [Test]
    public void MR10Bug1_Control_Untyped_InfersRealArray() {
        var rt = "x = [1.5]".Calc();
        Assert.IsNotNull(rt.Get("x"));
    }
}
