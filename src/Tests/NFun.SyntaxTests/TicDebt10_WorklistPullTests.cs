using System.Collections;
using NFun;
using NFun.Exceptions;
using NFun.TestTools;
using NFun.Tic;
using NUnit.Framework;

namespace NFun.SyntaxTests;

/// <summary>
/// Tests pinned to TIC Technical Debt #10 — worklist Pull architecture.
///
/// ## What debt #10 is
///
/// Pull is currently a streaming single-pass over toposorted nodes. When an
/// Apply cell adds a new edge (<c>AddAncestor</c>) after the source was already
/// visited, the new edge would not propagate. Current code mitigates with
/// <c>PullConstraintsForNode</c> eager re-Pull calls in specific cells.
///
/// The principled fix is a **worklist Pull** architecture: every time a node's
/// constraints tighten, re-fire Pull on its descendants. Specification:
/// <c>specs_tic/Advanced/WorklistPull.md</c>. Closing debt #10 also closes
/// debts #5 (stale Pull snapshots), #15 (Transform* element-node reuse
/// identity guards), and #16's Descendant axis (CompCs cross-Apply
/// Preferred / Descendant precision through MergeInplace-fallback).
///
/// ## Why these tests live together
///
/// Each test below is either:
/// - <b>A confirmed P3b Monotonicity counterexample</b> ([Ignore]'d): an
///   expression where the inner element of a nested LINQ chain widens to
///   <c>Any</c> because the worklist Pull re-fire never happens; or
/// - <b>A passing pin</b>: a near-miss case or workaround that demonstrates
///   where the streaming Pull happens to land in the right order. If a
///   passing pin starts failing, the debt has spread and the [Ignore]'d
///   ones need re-evaluation.
///
/// Two narrow ad-hoc fixes have been attempted at the deferred-accept branch
/// in <c>PullConstraintsFunctions.cs:660-690</c> (RefTo-promote, with and
/// without an <c>Ancestors.Count==1</c> guard); they broke 41 and 8 working
/// tests respectively. The same code path is the WORKING route for cases
/// like <c>b.items.sum()</c> where the struct field's CS resolves before
/// sum's Pull. Distinguishing "must re-fire on tightening" from "already
/// resolved, no re-fire needed" is exactly what worklist Pull encodes for
/// free; ad-hoc predicates can't tell them apart.
/// </summary>
public class TicDebt10_WorklistPullTests {
    [SetUp]
    public void Initialize() => TraceLog.IsEnabled = true;

    [TearDown]
    public void Deinitialize() => TraceLog.IsEnabled = false;

    // ────────────────────────────────────────────────────────────────
    // [Ignore]'d — confirmed P3b counterexamples awaiting worklist Pull
    // ────────────────────────────────────────────────────────────────

    /// <summary>
    /// Bug hunt round 8 #47. <c>data.map(rule it.first())</c> on
    /// <c>data:list&lt;list&lt;int&gt;&gt;</c> widens output element to
    /// <c>Any</c> instead of <c>Int32</c>. The runtime value is correct;
    /// only the static type is wrong. Same family: <c>.last()</c>.
    /// </summary>
    [Test, Ignore("Bug hunt #47: it.first() in lambda widens to Any — blocked on TIC debt #10 (worklist Pull)")]
    public void Bug47_MapItFirstOnNestedList_WidensToAny() {
        var rt = Funny.Hardcore.BuildLang(
            "data = [[1,2],[3,4]]\n" +
            "out = data.map(rule it.first())");
        rt.Run();
        Assert.AreEqual("fixedArray<Int32>", rt["out"].Type.ToString());
    }

    /// <summary>
    /// Bug hunt round 9 #49 (#47 family extension). Same TIC defect surfaces
    /// for every <c>[[...]].map(rule it.F())</c> where F is a generic
    /// collection transform that returns a collection without a downstream
    /// constraint forcing the element type. Confirmed broader scope:
    /// <c>toArray</c>, <c>toList</c>, <c>toFixedArray</c>, <c>toSet</c>,
    /// <c>reverse</c>, <c>take</c>, <c>skip</c>.
    /// </summary>
    [Test, Ignore("Bug hunt #49 (#47 family): nested map.toArray() widens inner element to Any — same TIC debt #10")]
    public void Bug49_NestedMapToArray_WidensElementToAny() {
        var rt = Funny.Hardcore.BuildLang("x = [[1,2,3]].map(rule it.toArray())");
        rt.Run();
        Assert.AreEqual("fixedArray<array<Int32>>", rt["x"].Type.ToString());
    }

    /// <summary>
    /// Practical impact: with the inner element widened to <c>Any</c>,
    /// <c>toSet</c> is rejected (FU580 — element type Any is not Immutable),
    /// even though the actual element values are plainly <c>Int32</c> (which
    /// IS Immutable). The expression is unusable as written until debt #10
    /// is closed.
    /// </summary>
    [Test, Ignore("Bug hunt #49 (#47 family): nested map.toSet() blocked by Any element (FU580 reject) — same TIC debt #10")]
    public void Bug49_NestedMapToSet_RejectedDueToAnyElement() {
        var rt = Funny.Hardcore.BuildLang("x = [[1,2,3]].map(rule it.toSet())");
        rt.Run();
        Assert.AreEqual("fixedArray<set<Int32>>", rt["x"].Type.ToString());
    }

    // ────────────────────────────────────────────────────────────────
    // Passing pins — adjacent cases the streaming Pull happens to solve.
    // If any of these starts failing the debt has spread; the [Ignore]'d
    // tests above need re-evaluation against the broader breakage.
    // ────────────────────────────────────────────────────────────────

    /// <summary>
    /// User-facing workaround for Bug #47: passing <c>first</c> as a function
    /// reference (<c>data.map(first)</c>) instead of a lambda body
    /// (<c>data.map(rule it.first())</c>) resolves correctly because the
    /// StateFun unification path through map's signature doesn't go through
    /// the deferred-accept branch.
    /// </summary>
    [Test]
    public void Bug47_Workaround_MapFunctionReference_Works() {
        "out = [[1,2],[3,4]].map(first)"
            .Calc().AssertResultHas("out", new[] { 1, 3 });
    }

    /// <summary>
    /// Lang-mode mirror of the ee-mode-failing
    /// <c>Closure_ArrayOfClosures_IndependentCells</c>. Lang-mode's
    /// <c>StateCollection.List</c> is INVARIANT in element, so back-prop
    /// stays tight and the closure-typed list-of-closures inference
    /// resolves cleanly. Passes today.
    /// </summary>
    [Test]
    public void LangMirror_ClosureArrayMap_IntsPinned() {
        var rt = Funny.Hardcore.BuildLang(
            "fun mk(a, b): return rule(c) = a + b + c\n" +
            "fun f(): return list(mk(1, 2), mk(3, 4), mk(5, 6)).map(rule it(10))\n" +
            "out = f()");
        rt.Run();
        var arr = (IList)rt["out"].Value;
        Assert.AreEqual(3, arr.Count);
        Assert.AreEqual(13, arr[0]);
        Assert.AreEqual(17, arr[1]);
        Assert.AreEqual(21, arr[2]);
    }

    /// <summary>
    /// Lang-mode mirror of
    /// <c>MR4Bug2_CorrectArityCallOn1ArgLambda_TypedAsElementReturnType</c>.
    /// Same precision guarantee as the closure mirror: list-of-lambdas
    /// inferred element type survives back-prop.
    /// </summary>
    [Test]
    public void LangMirror_RuleArrayMap_IntsPinned() {
        var rt = Funny.Hardcore.BuildLang(
            "fun f(): return list(rule it + 1, rule it + 2).map(rule it(10))\n" +
            "out = f()");
        rt.Run();
        var arr = (IList)rt["out"].Value;
        Assert.AreEqual(2, arr.Count);
        Assert.AreEqual(11, arr[0]);
        Assert.AreEqual(12, arr[1]);
    }

    /// <summary>
    /// Lang-mode counterpart of debt #16's nested-byte-upcast scenario.
    /// Was previously a P3b runtime counterexample (TIC inferred Real for
    /// the outer element but the inner <c>.map()</c>'s lambda received
    /// CLR-native Int32 / byte values). Closed at runtime by per-element
    /// coercion in <c>MapFunction.ConcreteMap.Calc</c> /
    /// <c>MapEnumerableFunction.ConcreteMap.Calc</c> (TicTechnicalDebt.md
    /// #16 closed-runtime side-effect). Passing pin: if it starts failing
    /// the runtime coercion regressed and debt #10 / #16 needs revisit.
    /// </summary>
    [Test]
    public void LangMirror_NestedByteUpcastMap_RealResult() {
        var rt = Funny.Hardcore.BuildLang(
            "fun f():\n    x:byte = 5\n" +
            "    return [[0,1],[2,3],[x]].map(rule it.map(rule it+1).sum()).sum()\n" +
            "out:real = f()");
        rt.Run();
        Assert.AreEqual(16.0, rt["out"].Value);
    }

    /// <summary>
    /// Lang-mode mirror of
    /// <c>MR4Bug2_ZeroArgCallOn1ArgLambda_InMapRule_SilentlyAccepted</c>.
    /// Arity mismatch in a map lambda must REJECT, not silently fill the
    /// missing argument with the type's default. Worklist Pull closure on
    /// debt #10 must preserve this rejection — passing pin.
    /// </summary>
    [Test]
    public void LangMirror_ZeroArgCallOn1ArgLambda_StillRejected() {
        Assert.Throws<FunnyParseException>(() =>
            Funny.Hardcore.BuildLang(
                "fun f(): return list(rule it + 1).map(rule it())\n" +
                "out = f()"));
    }
}
