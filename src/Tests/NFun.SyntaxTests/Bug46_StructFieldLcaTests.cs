using System.Linq;
using NFun;
using NFun.TestTools;
using NFun.Tic;
using NUnit.Framework;

namespace NFun.SyntaxTests;

/// <summary>
/// Boundary probes for Bug #46 — struct-field LCA mishandles mixed
/// bound-composite-var vs composite-literal in if-else struct branches.
///
/// Root cause (per professor's analysis): LcaStructFields
/// (StateExtensions.Lca.cs:176-226) uses UnifyOrNull(CS, SC) which
/// short-circuits via FitsInto and returns SC unchanged WITHOUT calling
/// MergeInplace on element nodes. V0 (left's CS{Desc=composite(V0)}) and V1
/// (right's composite(V1)) stay identity-distinct — Push later crashes in
/// MergeInplace as FU761.
///
/// Adjacent to closed Bug #32 (Round 6) — fixed expression-position joins
/// via LcaOrShareIdentity + StateCollection.GetLastCommonAncestorOrNull.
/// Struct-field LCA never got the same treatment.
///
/// Three probes survive after filtering: depth (Probe1), rescue path
/// (Probe3 — must continue to pass), and scope-beyond-collections (Probe5).
/// </summary>
public class Bug46_StructFieldLcaTests {
    [SetUp]
    public void Initialize() => TraceLog.IsEnabled = false;

    [TearDown]
    public void Deinitiazlize() => TraceLog.IsEnabled = false;

    // -------- Probe 1: NESTED collection element (depth) — FIXED --------
    // `list<list<int>>` at field position. Confirms the fix recurses through
    // nested element kinds via the existing (StateCollection, StateCollection)
    // merge cell.
    [Test]
    public void Bug46_Probe1_NestedListInField_MixedVarVsLiteral() {
        var rt = Funny.Hardcore.BuildLang(
            "y = [[1,2]]\n" +
            "out = if(true) {x=y} else {x=[[3]]}");
        rt.Run();
        Assert.IsNotNull(rt["out"].Value);
    }

    // -------- Probe 3: rescue path (must keep passing) --------
    // Wrapping the offending struct field in another struct level escapes
    // the bug because the outer LCA dispatches as UnifyOrNull(SC,SC).
    // Pin against regressions when fixing LcaStructFields.
    [Test]
    public void Bug46_Probe3_NestedStructWraps_PassesToday() {
        var rt = Funny.Hardcore.BuildLang(
            "y = [1,2]\n" +
            "out = if(true) {a={b=y}} else {a={b=[3]}}");
        rt.Run();
        var outer = (System.Collections.Generic.IReadOnlyDictionary<string, object>)rt["out"].Value;
        var inner = (System.Collections.Generic.IReadOnlyDictionary<string, object>)outer["a"];
        Assert.AreEqual(new[] { 1, 2 },
            ((System.Collections.IEnumerable)inner["b"])
                .Cast<object>().Select(System.Convert.ToInt32).ToArray());
    }

    // -------- Probe 5: scope beyond StateCollection — FIXED --------
    // Same shape with function-typed (StateFun) field. Closed by two-part fix:
    //   (1) ConstraintsState.AddDescendant skips Concretest when type is a
    //       live StateFun with unresolved (CS-typed) arg/ret nodes and no
    //       IsSignatureParam component — preserves identity to the lambda's
    //       binder/body nodes instead of fabricating (Any -> Desc.ret) via
    //       ConcretestFun (StateExtensions.Concretest.cs:66-72).
    //   (2) GetMergedStateOrNull adds (StateFun, CS{Desc=StateFun}) cell
    //       mirroring the StateCollection precedent — pointwise MergeInplace
    //       fuses arg/ret nodes between the two lambdas joined by struct LCA.
    [Test]
    public void Bug46_Probe5_FunctionTypedField_MixedVarVsLiteralLambda() {
        var rt = Funny.Hardcore.BuildLang(
            "y = rule it+1\n" +
            "out = if(true) {x=y} else {x=rule it*2}");
        rt.Run();
        Assert.IsNotNull(rt["out"].Value);
    }
}
