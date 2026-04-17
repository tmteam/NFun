namespace NFun.SyntaxTests.TypeNarrowing;

using NFun.TestTools;
using NUnit.Framework;

/// <summary>
/// Tests for collection type narrowing: when filtering out none from T?[],
/// the result type should narrow to T[].
///
/// compact() is now implemented: (T?[]) -> T[].
/// Tests using compact() are enabled; all()/any() guard narrowing remains [Ignored].
/// </summary>
[TestFixture]
public class CollectionNarrowingTest {

    // --- 1. Basic compact: filter none from int?[], then map without unwrap ---
    [Test]
    public void FilterNone_IntArray_MapAddsOne() {
        var r = "arr:int?[] = [1, none, 3]\r cleaned = arr.filterNotNull()\r y = cleaned.map(rule it + 1)"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled);
        Assert.AreEqual(new[] { 2, 4 }, r.Get("y"));
    }

    // --- 2. Compact + fold: sum of non-none elements ---
    [Test]
    public void FilterNone_IntArray_FoldSum() {
        var r = "arr:int?[] = [1, none, 3, none, 5]\r y = arr.filterNotNull().fold(rule it1 + it2)"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled);
        Assert.AreEqual(9, r.Get("y"));
    }

    // --- 3. Compact + count: count non-none elements ---
    [Test]
    public void FilterNone_IntArray_Count() {
        var r = "arr:int?[] = [1, none, 3, none, 5]\r y = arr.filterNotNull().count()"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled);
        Assert.AreEqual(3, r.Get("y"));
    }

    // --- 4. Compact + first: first non-none element ---
    [Test]
    public void FilterNone_IntArray_First() {
        var r = "arr:int?[] = [none, none, 42, 1]\r y = arr.filterNotNull().first()"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled);
        Assert.AreEqual(42, r.Get("y"));
    }

    // --- 5. Compact from real?[] ---
    [Test]
    public void FilterNone_RealArray_MapMultiplies() {
        var r = "arr:real?[] = [1.5, none, 2.5]\r y = arr.filterNotNull().map(rule it * 2)"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled);
        Assert.AreEqual(new[] { 3.0, 5.0 }, r.Get("y"));
    }

    // --- 6. Compact from text?[] ---
    [Test]
    public void FilterNone_TextArray_MapConcat() {
        var r = "arr:text?[] = ['hello', none, 'world']\r y = arr.filterNotNull().map(rule it.concat('!'))"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled);
        Assert.AreEqual(new[] { "hello!", "world!" }, r.Get("y"));
    }

    // --- 7. Compact from bool?[] ---
    [Test]
    public void FilterNone_BoolArray_AllTrue() {
        var r = "arr:bool?[] = [true, none, true]\r y = arr.filterNotNull().all(rule it)"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled);
        Assert.AreEqual(true, r.Get("y"));
    }

    // --- 8. Compound predicate: compact then filter by value ---
    [Test]
    public void FilterNone_CompoundPredicate_PositiveOnly() {
        var r = "arr:int?[] = [none, -1, 3, none, 0, 5]\r y = arr.filterNotNull().filter(rule it > 0).map(rule it * 10)"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled);
        Assert.AreEqual(new[] { 30, 50 }, r.Get("y"));
    }

    // --- 9. Compact preserves order ---
    [Test]
    public void FilterNone_PreservesOrder() {
        var r = "arr:int?[] = [none, 3, none, 1, none, 2]\r y = arr.filterNotNull()"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled);
        Assert.AreEqual(new[] { 3, 1, 2 }, r.Get("y"));
    }

    // --- 10. Compact on all-none array produces empty array ---
    [Test]
    public void FilterNone_AllNone_EmptyResult() {
        var r = "arr:int?[] = [none, none, none]\r y = arr.filterNotNull().count()"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled);
        Assert.AreEqual(0, r.Get("y"));
    }

    // --- 11. Compact on array with no nones: same result ---
    [Test]
    public void FilterNone_NoNones_SameElements() {
        var r = "arr:int?[] = [1, 2, 3]\r y = arr.filterNotNull().map(rule it + 10)"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled);
        Assert.AreEqual(new[] { 11, 12, 13 }, r.Get("y"));
    }

    // --- 12. Chained compact + map + fold: full pipeline ---
    [Test]
    public void FilterNone_ChainedPipeline_FilterMapFold() {
        var r = "arr:int?[] = [none, 1, none, 2, none, 3]\r y = arr.filterNotNull().map(rule it * it).fold(rule it1 + it2)"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled);
        Assert.AreEqual(14, r.Get("y")); // 1*1 + 2*2 + 3*3 = 1 + 4 + 9 = 14
    }

    // --- 13. Compact result assigned to typed variable: cleaned:int[] ---
    [Test]
    public void FilterNone_AssignToTypedVariable() {
        var r = "arr:int?[] = [1, none, 3]\r cleaned:int[] = arr.filterNotNull()\r y = cleaned[0]"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled);
        Assert.AreEqual(1, r.Get("y"));
    }

    // --- 14. Compact + sort: narrowed type is sortable ---
    [Test]
    public void FilterNone_ThenSort() {
        var r = "arr:int?[] = [none, 3, none, 1, none, 2]\r y = arr.filterNotNull().sort()"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled);
        Assert.AreEqual(new[] { 1, 2, 3 }, r.Get("y"));
    }

    // --- 15. Compact + reverse: chain operations on narrowed type ---
    [Test]
    public void FilterNone_ThenReverse() {
        var r = "arr:int?[] = [none, 1, none, 2, none, 3]\r y = arr.filterNotNull().reverse()"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled);
        Assert.AreEqual(new[] { 3, 2, 1 }, r.Get("y"));
    }

    // --- 16. Negative: map on int?[] without filter still requires unwrap ---
    [Test, Ignore("Collection narrowing: map on T?[] without filter should still require unwrap (! or ??)")]
    public void NoFilter_MapOnOptionalArray_RequiresUnwrap() {
        "arr:int?[] = [1, none, 3]\r y = arr.map(rule it + 1)"
            .AssertObviousFailsOnParse(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled);
    }

    // --- 17. Negative: filter with non-none predicate does not narrow ---
    [Test, Ignore("Collection narrowing: filter(rule it > 0) without none check should NOT narrow T?[] to T[]")]
    public void FilterWithoutNoneCheck_DoesNotNarrow() {
        // Filtering by it > 0 does not guarantee none is removed,
        // so the result should stay int?[] and map(rule it + 1) should fail
        "arr:int?[] = [1, none, 3]\r y = arr.filter(rule it > 0).map(rule it + 1)"
            .AssertObviousFailsOnParse(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled);
    }

    // --- 18. all() guard: if all elements are non-none, narrow inside the branch ---
    [Test, Ignore("Collection narrowing: if(arr.all(rule it != none)) should narrow arr to T[] in true branch")]
    public void AllGuard_NarrowsInTrueBranch() {
        var r = ("arr:int?[] = [1, 2, 3]\r" +
                 " y = if(arr.all(rule it != none)) arr.map(rule it + 1) else []")
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled);
        Assert.AreEqual(new[] { 2, 3, 4 }, r.Get("y"));
    }

    // --- 19. any() does NOT narrow: any(rule it != none) is not sufficient ---
    [Test, Ignore("Collection narrowing: any(rule it != none) does NOT guarantee all elements non-none, no narrowing")]
    public void AnyGuard_DoesNotNarrow() {
        // any() only guarantees at least one non-none, so arr still has type int?[]
        // and map(rule it + 1) should fail
        ("arr:int?[] = [1, none, 3]\r" +
         " y = if(arr.any(rule it != none)) arr.map(rule it + 1) else []")
            .AssertObviousFailsOnParse(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled);
    }

    // --- 20. Compact on nested optional: int??[] ---
    [Test, Ignore("Nested optional int??[] parse error: '??' in 'it ?? 0' conflicts with type annotation syntax")]
    public void FilterNone_NestedOptional_PeelsOneLayer() {
        // int??[] filtered by compact should become int?[] — one layer peeled
        // The inner optional remains: elements could still be none at the int? level
        var r = "arr:int??[] = [1, none, 3]\r cleaned = arr.filterNotNull()\r y = cleaned.map(rule it ?? 0)"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled);
        Assert.AreEqual(new[] { 1, 3 }, r.Get("y"));
    }
}
