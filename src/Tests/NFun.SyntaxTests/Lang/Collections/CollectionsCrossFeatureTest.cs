using System.Collections.Generic;
using NFun.Exceptions;
using NFun.TestTools;
using NFun.Tic;
using NUnit.Framework;

namespace NFun.SyntaxTests.Lang.Collections;

[TestFixture]
public class CollectionsCrossFeatureTest {
    [SetUp]    public void Initialize()   => TraceLog.IsEnabled = true;
    [TearDown] public void Deinitialize() => TraceLog.IsEnabled = false;

    // ──────────────────────────────────────────────
    // Collection × Optional
    // ──────────────────────────────────────────────

    /// Array of optional ints: count includes nones
    [Test]
    public void CrossFeature_ArrayOfOptionals_CountIncludesNones() {
        var rt = Funny.Hardcore.BuildLang("out = [1,none,2].count()");
        rt.Run();
        Assert.AreEqual(3, rt["out"].Value);
    }

    /// Filter nones from array of optional ints; count remaining
    [Test]
    public void CrossFeature_ArrayOfOptionals_FilterNone_CountNonNull() {
        var rt = Funny.Hardcore.BuildLang("out = [none, none, 1].filter(rule it != none).count()");
        rt.Run();
        Assert.AreEqual(1, rt["out"].Value);
    }

    /// Array of optional ints, filter nones then count — more elements
    [Test]
    public void CrossFeature_ArrayOfOptionals_FilterNone_KeepsNonNullElements() {
        var rt = Funny.Hardcore.BuildLang("out = [1,none,2,none,3].filter(rule it != none).count()");
        rt.Run();
        Assert.AreEqual(3, rt["out"].Value);
    }

    /// Set of optional ints (none makes cardinality 2, not 3 because set deduplicates)
    [Test]
    public void CrossFeature_SetOfOptionals_DuplicateNone_DeduplicatedToOne() {
        var rt = Funny.Hardcore.BuildLang("out = set(1, none, none).count()");
        rt.Run();
        Assert.AreEqual(2, rt["out"].Value);
    }

    /// Map with struct value: get missing key returns none, coalesced to fallback
    [Test]
    public void CrossFeature_MapGetMissingKey_CoalescedToDefault() {
        var rt = Funny.Hardcore.BuildLang(
            "out = (__mkMap({key=99, value='x'})).get(1) ?? 'default'");
        rt.Run();
        Assert.AreEqual("default", rt["out"].Value);
    }

    /// Map with struct value + optional chaining: present key, field access
    [Test]
    public void CrossFeature_MapWithStructValue_OptionalChain_FieldAccess() {
        var rt = Funny.Hardcore.BuildLang(
            "out = __mkMap({key=1,value={a=10}}).get(1)?.a ?? -1");
        rt.Run();
        Assert.AreEqual(10, rt["out"].Value);
    }

    /// Map with list value + optional chaining: present key, call count()
    [Test]
    public void CrossFeature_MapWithListValue_OptionalChain_Count() {
        var rt = Funny.Hardcore.BuildLang(
            "out = __mkMap({key=1,value=list(10,20)}).get(1)?.count() ?? -1");
        rt.Run();
        Assert.AreEqual(2, rt["out"].Value);
    }

    // ──────────────────────────────────────────────
    // Collection × Struct
    // ──────────────────────────────────────────────

    /// Array of structs: map over field, then fold to sum
    [Test]
    public void CrossFeature_ArrayOfStructs_MapField_FoldSum() {
        var rt = Funny.Hardcore.BuildLang(
            "out = [{x=1},{x=2},{x=3}].map(rule it.x).fold(rule(a,b)=a+b)");
        rt.Run();
        Assert.AreEqual(6, rt["out"].Value);
    }

    /// Array of structs: filter by field, then count
    [Test]
    public void CrossFeature_ArrayOfStructs_FilterByField_Count() {
        var rt = Funny.Hardcore.BuildLang(
            "out = [{x=1},{x=2},{x=3}].filter(rule it.x > 1).count()");
        rt.Run();
        Assert.AreEqual(2, rt["out"].Value);
    }

    /// Set of structs: count
    [Test]
    public void CrossFeature_SetOfStructs_CountTwoDistinct() {
        var rt = Funny.Hardcore.BuildLang("out = set({a=1},{a=2}).count()");
        rt.Run();
        Assert.AreEqual(2, rt["out"].Value);
    }

    /// Map with struct value: get existing key, access field
    [Test]
    public void CrossFeature_MapWithStructValue_GetPresentKey_Field() {
        var rt = Funny.Hardcore.BuildLang(
            "out = __mkMap({key=1,value={a=10}}).get(1)?.a ?? -1");
        rt.Run();
        Assert.AreEqual(10, rt["out"].Value);
    }

    /// Struct containing list and set fields; access both counts
    [Test]
    public void CrossFeature_StructWithListAndSet_BothFieldCounts() {
        var rt = Funny.Hardcore.BuildLang(
            "s = {a=[1,2], b=set(3,4)}\nout = s.a.count() + s.b.count()");
        rt.Run();
        Assert.AreEqual(4, rt["out"].Value);
    }

    // ──────────────────────────────────────────────
    // Collection × Collection nested
    // ──────────────────────────────────────────────

    /// Array of arrays: map count over each inner array
    [Test]
    public void CrossFeature_ArrayOfArrays_MapCount() {
        var rt = Funny.Hardcore.BuildLang("out = [[1,2],[3,4]].map(rule it.count())");
        rt.Run();
        var vals = (int[])rt["out"].Value;
        Assert.AreEqual(2, vals[0]);
        Assert.AreEqual(2, vals[1]);
    }

    /// Array of arrays: map first-element access
    [Test]
    public void CrossFeature_ArrayOfArrays_MapIndexZero() {
        var rt = Funny.Hardcore.BuildLang("out = [[10,20],[30,40]].map(rule it[0])");
        rt.Run();
        var vals = (int[])rt["out"].Value;
        Assert.AreEqual(10, vals[0]);
        Assert.AreEqual(30, vals[1]);
    }

    /// Array of lists: map count over each list
    [Test]
    public void CrossFeature_ArrayOfLists_MapCount() {
        var rt = Funny.Hardcore.BuildLang("out = [list(1,2), list(3,4)].map(rule it.count())");
        rt.Run();
        var vals = (int[])rt["out"].Value;
        Assert.AreEqual(2, vals[0]);
        Assert.AreEqual(2, vals[1]);
    }

    /// Array of sets: map count over each set
    [Test]
    public void CrossFeature_ArrayOfSets_MapCount() {
        var rt = Funny.Hardcore.BuildLang("out = [set(1,2), set(3,4)].map(rule it.count())");
        rt.Run();
        var vals = (int[])rt["out"].Value;
        Assert.AreEqual(2, vals[0]);
        Assert.AreEqual(2, vals[1]);
    }

    /// LCA of list vs set in if-else: falls back to Any; count still works
    [Test]
    public void CrossFeature_LCA_ListVsSet_IfElse_CountStillWorks() {
        var rt = Funny.Hardcore.BuildLang("out = [list(1,2), set(3,4)].count()");
        rt.Run();
        Assert.AreEqual(2, rt["out"].Value);
    }

    // ──────────────────────────────────────────────
    // Collection × LCA
    // ──────────────────────────────────────────────

    /// LCA of int[] vs optional-int[]: if-else produces int?[], count works
    [Test]
    public void CrossFeature_LCA_IntArray_vs_OptionalIntArray_IfElse_Count() {
        var rt = Funny.Hardcore.BuildLang("out = (if(true) [1,2] else [none,3]).count()");
        rt.Run();
        Assert.AreEqual(2, rt["out"].Value);
    }

    /// LCA of fixedArray vs list: collapses to Any; outer count works
    [Test]
    public void CrossFeature_LCA_FixedArrayVsList_IfElse_IsAny() {
        var rt = Funny.Hardcore.BuildLang("out = [if(true) fixedArray(1,2) else list(3,4)].count()");
        rt.Run();
        Assert.AreEqual(1, rt["out"].Value);
    }

    // ──────────────────────────────────────────────
    // Collection × Generic user fn
    // ──────────────────────────────────────────────

    /// Generic user fn over ee-mode array: map * 2
    [Test]
    public void CrossFeature_GenericFn_MapDouble_OverArray() {
        var rt = Funny.Hardcore.Build("y = [1,2,3].map(rule it*2)");
        rt.Run();
        var vals = (int[])rt["y"].Value;
        Assert.AreEqual(new[] { 2, 4, 6 }, vals);
    }

    /// Generic fn: count of array passed to count() still generic
    [Test]
    public void CrossFeature_GenericFn_CountOverArrayViaGenericLambda() {
        var rt = Funny.Hardcore.Build("y = [10,20,30].filter(rule it > 10).count()");
        rt.Run();
        Assert.AreEqual(2, rt["y"].Value);
    }

    /// User function with explicit int[] param: called with set converted to list
    [Test]
    public void CrossFeature_GenericFn_SetToList_PassedToCountFn() {
        var rt = Funny.Hardcore.BuildLang(
            "g(xs:int[]) = xs.count()\nout = g(set(1,2,3).toList())");
        rt.Run();
        Assert.AreEqual(3, rt["out"].Value);
    }

    // ──────────────────────────────────────────────
    // CLR defaults
    // ──────────────────────────────────────────────

    /// WithApriori<List<int>> builds and count works
    [Test]
    public void CrossFeature_CLR_WithAprioriListOfInt_Count() {
        var rt = Funny.Hardcore
            .WithApriori<List<int>>("xs")
            .BuildLang("out = xs.count()");
        rt["xs"].Value = new List<int> { 1, 2, 3 };
        rt.Run();
        Assert.AreEqual(3, rt["out"].Value);
    }

    /// WithApriori<Dictionary<int,string>> builds and count works
    [Test]
    public void CrossFeature_CLR_WithAprioriDictionaryIntString_Count() {
        var rt = Funny.Hardcore
            .WithApriori<Dictionary<int, string>>("m")
            .BuildLang("out = m.count()");
        rt["m"].Value = new Dictionary<int, string> { { 1, "a" }, { 2, "b" } };
        rt.Run();
        Assert.AreEqual(2, rt["out"].Value);
    }

    // ──────────────────────────────────────────────
    // Should-be-rejected
    // ──────────────────────────────────────────────

    /// append with wrong element type: parse error (generic T0 mismatch)
    [Test]
    public void CrossFeature_Rejected_AppendWrongElementType_ParseError() {
        Assert.Throws<FunnyParseException>(() =>
            Funny.Hardcore.Build("out = append([1,2,3], 'text')"));
    }

    /// set(1, 'hello') — set accepts Any LCA of int and text, so no parse error is thrown.
    /// Bug: set should reject mixed element types since set(T...) implies homogeneous T.
    [Test, Ignore("Bug: set(1,'hello') resolves element to Any instead of rejecting mixed types")]
    public void CrossFeature_Rejected_SetWithMixedIntAndText_ParseError() {
        // set deduces element T from all arguments; int vs text is a type mismatch
        Assert.Throws<FunnyParseException>(() =>
            Funny.Hardcore.Build("out = set(1,'hello').count()"));
    }
}
