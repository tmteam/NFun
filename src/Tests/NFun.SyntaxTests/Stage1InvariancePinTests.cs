using System.Collections;
using NFun;
using NFun.TestTools;
using NFun.Tic;
using NUnit.Framework;

namespace NFun.SyntaxTests;

/// <summary>
/// Tests pinned to the Stage 1 uniform-invariance pin in
/// <see cref="NFun.Tic.SolvingStates.StateCollection.LcaOrShareIdentity"/>.
///
/// ## What the pin is
///
/// Quoting CLAUDE.md "Liskov direction at parameter position for new
/// collections": "Cross-kind LCA collapses to `Any` by uniform-invariance
/// rule when elements are concrete-equal or recursively LCA'able. Identity-
/// share via `MergeInplace` in `LcaOrShareIdentity` gated to non-composite
/// elements (composite element MergeInplace uses `NarrowerArrayBranchOrNull`
/// — opposite of LCA — so mixing levels produces inconsistent types)."
///
/// Concretely: <c>StateCollection.LcaOrShareIdentity</c> at
/// <c>SolvingStates/StateCollection.cs:223-234</c> has the guard
/// <c>Element is not ICompositeState &amp;&amp; xKindOther.Element is not
/// ICompositeState</c>. When the LCA is needed between two cross-kind
/// nested collections (e.g. <c>array&lt;array&lt;I32&gt;&gt;</c> vs
/// <c>list&lt;list&lt;I32&gt;&gt;</c>), the guard fires and the LCA collapses
/// to <c>Any</c> instead of the algebraic answer <c>array&lt;array&lt;I32&gt;&gt;</c>.
///
/// The guard exists because <c>MergeInplace</c> on cross-kind element nodes
/// routes through <c>NarrowerArrayBranchOrNull</c> which picks the
/// NARROWER constructor — opposite of LCA. Mixing widen-outer with
/// narrow-inner produces inconsistent types (0832 LeetCode regression
/// referenced in the source code).
///
/// ## Why these tests live together
///
/// Each test below is a confirmed surface of the same algebraic debt.
/// Bug hunt round 11 #54 (Destruction-time) was closed in
/// <c>DestructionFunctions.cs</c> via recursive Destruction (cross-kind
/// element pairs recurse rather than MergeInplace). The LCA-time analogue
/// (this family) requires a new primitive — recursive
/// <c>LcaOrShareIdentity</c> for nested composite elements — that doesn't
/// route through MergeInplace's narrowing semantics. Two attempts (round
/// 11 fix-tic) produced 11 syntax regressions; the proper fix is
/// structural and out of scope for a point patch.
///
/// ## Workarounds for users (today)
///
/// - Bind the literal to an unannotated variable first:
///   <c>tmp = [[1,2]]; out = x ?? tmp</c>.
/// - Or use the annotated-slot kind consistently:
///   <c>tmp:int[][] = [[1,2]]; out = x ?? tmp</c>.
/// - Or split the LCA: <c>if (x is none) [[1,2]] else x</c> (narrowing).
/// </summary>
public class Stage1InvariancePinTests {
    [SetUp]
    public void Initialize() => TraceLog.IsEnabled = false;

    [TearDown]
    public void Deinitialize() => TraceLog.IsEnabled = false;

    // ────────────────────────────────────────────────────────────────
    // [Ignore]'d — confirmed Stage 1 invariance pin surfaces
    // ────────────────────────────────────────────────────────────────

    /// <summary>
    /// Bug hunt round 11 #55 (the original surface). `??` operator with a
    /// 2D-literal as fallback on an `int[][]?` slot now resolves to the
    /// algebraic answer `array&lt;array&lt;Int32&gt;&gt;`. Closed by the
    /// LcaOrShareIdentity path (b) — recursive Lca + AddDescendant on the
    /// CS-side element node.
    /// </summary>
    [Test]
    public void Bug55_Coalesce_OuterOpt_Vs_Literal_2D() {
        var rt = Funny.Hardcore.BuildLang("x:int[][]? = none\nout = x ?? [[1,2]]");
        rt.Run();
        Assert.AreEqual("array<array<Int32>>", rt["out"].Type.ToString());
    }

    /// <summary>
    /// 3D extension of #55. Closed 2026-06-29 (debt #17): path (b) in
    /// <see cref="StateCollection.LcaOrShareIdentity"/> now recurses unboundedly
    /// via <c>elemA.Lca(elemB)</c> and returns a FRESH element node carrying the
    /// concrete elemLca — no CS-references in the descendant chain, so Push
    /// structural compare succeeds at arbitrary depth.
    /// </summary>
    [Test]
    public void Bug55_Family_3D_Coalesce_WidensToAny() {
        var rt = Funny.Hardcore.BuildLang("x:int[][][]? = none\nout = x ?? [[[1]]]");
        rt.Run();
        Assert.AreEqual("array<array<array<Int32>>>", rt["out"].Type.ToString());
    }

    /// <summary>
    /// 4D pin — confirms the recursive path (b) closure scales beyond 3D.
    /// </summary>
    [Test]
    public void Bug55_Family_4D_Coalesce_Widens() {
        var rt = Funny.Hardcore.BuildLang("x:int[][][][]? = none\nout = x ?? [[[[1]]]]");
        rt.Run();
        Assert.AreEqual("array<array<array<array<Int32>>>>", rt["out"].Type.ToString());
    }

    /// <summary>
    /// Same family at if-else (instead of `??`). Annotated 2D array slot +
    /// LCA against unannotated 2D literal in the other branch now resolves
    /// via the same path (b) path. Closed.
    /// </summary>
    [Test]
    public void Bug55_Family_IfElse_AnnotatedVsLiteral_2D() {
        var rt = Funny.Hardcore.BuildLang(
            "x:int[][] = [[1]]\n" +
            "out = if(true) x else [[2]]");
        rt.Run();
        var arr = (IList)rt["out"].Value;
        Assert.IsNotNull(arr);
    }

    /// <summary>
    /// `??` family with an actual non-none value. Method call on the result
    /// now resolves cleanly through the LCA path (b) — result type is
    /// `array&lt;array&lt;Int32&gt;&gt;` which IS enumerable.
    /// </summary>
    [Test]
    public void Bug55_Family_CoalesceResult_MethodCall() {
        var rt = Funny.Hardcore.BuildLang(
            "x:int[][]? = [[10]]\n" +
            "out = (x ?? [[1]]).count()");
        rt.Run();
        Assert.AreEqual(1, rt["out"].Value);
    }

    /// <summary>
    /// `if-else` with the OPTIONAL outer kind on one branch, literal on the
    /// other. Same root cause as the `??` case — closed by path (b).
    /// </summary>
    [Test]
    public void Bug55_Family_IfElse_OptionalVsLiteral_2D() {
        var rt = Funny.Hardcore.BuildLang(
            "x:int[][]? = none\n" +
            "out = if(true) x else [[2]]");
        rt.Run();
        Assert.AreEqual("array<array<Int32>>?", rt["out"].Type.ToString());
    }

    // ────────────────────────────────────────────────────────────────
    // Passing pins — adjacent cases that the LCA path solves correctly.
    // If any starts failing, the invariance debt has spread.
    // ────────────────────────────────────────────────────────────────

    /// <summary>
    /// 1D baseline: `??` with 1D-literal on `int[]?` slot resolves correctly
    /// to `array&lt;Int32&gt;`. The inner element is primitive (`I32`), which
    /// doesn't trigger the Stage 1 guard. This is the discriminator that
    /// proves the bug is specific to NESTED-composite elements.
    /// </summary>
    [Test]
    public void StageOne_1D_Coalesce_BaselineWorks() {
        var rt = Funny.Hardcore.BuildLang("x:int[]? = none\nout = x ?? [1,2]");
        rt.Run();
        Assert.AreEqual("array<Int32>", rt["out"].Type.ToString());
        var arr = (IList)rt["out"].Value;
        Assert.AreEqual(2, arr.Count);
    }

    /// <summary>
    /// `if-else` between two 2D-literals (both inferred as `list&lt;list&lt;...&gt;&gt;`
    /// — same kind on both sides) resolves cleanly. The same-kind path
    /// doesn't hit the cross-kind guard.
    /// </summary>
    [Test]
    public void StageOne_IfElse_BothLiterals_2D_Works() {
        var rt = Funny.Hardcore.BuildLang("out = if(true) [[1]] else [[2]]");
        rt.Run();
        Assert.AreEqual("list<list<Int32>>", rt["out"].Type.ToString());
    }

    /// <summary>
    /// `??`-like workaround: bind the literal to an unannotated var first,
    /// then `?? tmp`. The intermediate var carries the LCA off-path, so
    /// the cross-kind doesn't materialize at the operator.
    /// </summary>
    [Test]
    public void StageOne_Workaround_BindLiteralToVar_Works() {
        var rt = Funny.Hardcore.BuildLang(
            "x:int[][]? = none\n" +
            "tmp = [[1,2]]\n" +
            "out = x ?? tmp");
        rt.Run();
        Assert.IsNotNull(rt["out"].Value);
    }

    /// <summary>
    /// Same workaround at if-else: bind literal to an unannotated var.
    /// </summary>
    [Test]
    public void StageOne_Workaround_IfElseWithBoundLiteral_Works() {
        var rt = Funny.Hardcore.BuildLang(
            "x:int[][] = [[1]]\n" +
            "fallback = [[2]]\n" +
            "out = if(true) x else fallback");
        rt.Run();
        Assert.IsNotNull(rt["out"].Value);
    }

    /// <summary>
    /// Bug hunt round 11 #54 (Destruction-time variant of this family) is
    /// CLOSED by the recursive Destruction fix in DestructionFunctions.cs.
    /// Pin that close so a future LCA refactor doesn't accidentally regress
    /// the Destruction side.
    /// </summary>
    [Test]
    public void StageOne_Bug54_DirectAssignment_RemainsClosed() {
        var rt = Funny.Hardcore.BuildLang("a:int[][]? = [[1,2,3]]\nout = a");
        rt.Run();
        Assert.IsNotNull(rt["out"].Value);
    }
}
