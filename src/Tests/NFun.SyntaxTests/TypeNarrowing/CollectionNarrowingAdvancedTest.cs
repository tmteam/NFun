namespace NFun.SyntaxTests.TypeNarrowing;

using NFun.TestTools;
using NUnit.Framework;

/// <summary>
/// Collection type narrowing tests (advanced scenarios).
///
/// compact() is now implemented: (T?[]) -> T[].
/// Tests using compact() are enabled; all() guard narrowing remains [Ignored].
/// </summary>
[TestFixture]
public class CollectionNarrowingAdvancedTest {

    // ═══════════════════════════════════════════════════════════════
    // 1. Compact + map pipeline: compact narrows T?[] to T[],
    //    then map can use arithmetic on non-optional elements
    // ═══════════════════════════════════════════════════════════════
    [Test]
    public void FilterNarrowing_ThenMap_Arithmetic() {
        "arr:int?[] = [1,none,3]\r y:int[] = arr.filterNotNull().map(rule it * 2)"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled)
            .AssertResultHas("y", new[] { 2, 6 });
    }

    // ═══════════════════════════════════════════════════════════════
    // 2. Compact + struct field access:
    //    struct?[] -> compact() -> struct[] -> map(.name)
    // ═══════════════════════════════════════════════════════════════
    [Test]
    public void FilterNarrowing_StructFieldAccess() {
        var expr =
            "arr = [if(true) {name='Alice'} else none, if(false) {name='Bob'} else none, if(true) {name='Carol'} else none]\r" +
            "y = arr.filterNotNull().map(rule it.name)";
        expr.CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled)
            .AssertResultHas("y", new[] { "Alice", "Carol" });
    }

    // ═══════════════════════════════════════════════════════════════
    // 3. Compact + method call on narrowed element:
    //    text?[] -> compact() -> text[] -> map(.count())
    // ═══════════════════════════════════════════════════════════════
    [Test]
    public void FilterNarrowing_MethodCallOnElement() {
        "arr:text?[] = ['hello', none, 'hi']\r y:int[] = arr.filterNotNull().map(rule it.count())"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled)
            .AssertResultHas("y", new[] { 5, 2 });
    }

    // ═══════════════════════════════════════════════════════════════
    // 4. Double narrowing: optional array of optional elements
    //    int?[]? -> if(!=none) unwrap outer -> int?[] -> compact() -> int[]
    // ═══════════════════════════════════════════════════════════════
    [Test]
    public void DoubleNarrowing_OptionalArrayOfOptionalElements() {
        var expr =
            "arr:int?[]? = [1,none,3]\r" +
            "y:int[] = if(arr != none) arr.filterNotNull() else []";
        expr.CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled)
            .AssertResultHas("y", new[] { 1, 3 });
    }

    // ═══════════════════════════════════════════════════════════════
    // 5. Compact result used in fold: narrowed elements support
    //    arithmetic in fold's binary rule
    // ═══════════════════════════════════════════════════════════════
    [Test]
    public void FilterNarrowing_ThenFold() {
        "arr:int?[] = [1,none,2,none,3]\r y:int = arr.filterNotNull().fold(rule it1 + it2)"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled)
            .AssertResultHas("y", 6);
    }

    // ═══════════════════════════════════════════════════════════════
    // 6. Compact + second filter on value:
    //    int?[] -> compact() -> int[] -> filter(> 0)
    // ═══════════════════════════════════════════════════════════════
    [Test]
    public void FilterNarrowing_ThenFilterByValue() {
        "arr:int?[] = [none, -1, none, 2, 0, 3]\r y:int[] = arr.filterNotNull().filter(rule it > 0)"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled)
            .AssertResultHas("y", new[] { 2, 3 });
    }

    // ═══════════════════════════════════════════════════════════════
    // 7. Compact assigned to explicitly typed variable:
    //    int?[] -> compact() -> assignable to int[]
    // ═══════════════════════════════════════════════════════════════
    [Test]
    public void FilterNarrowing_AssignedToExplicitNonOptionalArrayType() {
        "arr:int?[] = [1,none,3]\r cleaned:int[] = arr.filterNotNull()"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled)
            .AssertResultHas("cleaned", new[] { 1, 3 });
    }

    // ═══════════════════════════════════════════════════════════════
    // 8. Negative: filter with unrelated predicate does NOT narrow
    //    int?[] -> filter(rule true) -> still int?[] (not int[])
    // ═══════════════════════════════════════════════════════════════
    [Test, Ignore("Collection narrowing not implemented: non-narrowing filter preserves optionality")]
    public void FilterWithUnrelatedPredicate_DoesNotNarrow() {
        // The result type should still be int?[], not int[]
        // This test verifies that narrowing only happens for != none predicates
        var runtime = "arr:int?[] = [1,none,3]\r y = arr.filter(rule true)"
            .BuildWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled);
        Assert.AreEqual(
            FunnyType.ArrayOf(FunnyType.OptionalOf(FunnyType.Int32)),
            runtime["y"].Type);
    }

    // ═══════════════════════════════════════════════════════════════
    // 9. Negative: map on unfiltered optional array should fail
    //    int?[] -> map(rule it + 1) -> error (can't add int? + int)
    // ═══════════════════════════════════════════════════════════════
    [Test, Ignore("Collection narrowing not implemented: arithmetic on optional elements should fail")]
    public void MapOnUnfilteredOptionalArray_FailsOnArithmetic() {
        "arr:int?[] = [1,none,3]\r y = arr.map(rule it + 1)"
            .AssertObviousFailsOnParse(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled);
    }

    // ═══════════════════════════════════════════════════════════════
    // 10. Compact + count: count() works on any array
    // ═══════════════════════════════════════════════════════════════
    [Test]
    public void FilterNarrowing_ThenCount() {
        "arr:int?[] = [1,none,2,none,3]\r y:int = arr.filterNotNull().count()"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled)
            .AssertResultHas("y", 3);
    }

    // ═══════════════════════════════════════════════════════════════
    // 11. all() guard + map + arithmetic:
    //     if all elements are non-none, the array is narrowed in then-branch
    // ═══════════════════════════════════════════════════════════════
    [Test, Ignore("Collection narrowing not implemented: all() guard narrows in then-branch")]
    public void AllGuard_ThenMap_Arithmetic() {
        "arr:int?[] = [1,2,3]\r y:int[] = if(arr.all(rule it != none)) arr.map(rule it * 2 + 1) else []"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled)
            .AssertResultHas("y", new[] { 3, 5, 7 });
    }

    // ═══════════════════════════════════════════════════════════════
    // 12. all() guard + sort + reverse:
    //     narrowed array supports sort/reverse which require comparable elements
    // ═══════════════════════════════════════════════════════════════
    [Test, Ignore("Collection narrowing not implemented: all() guard then sort().reverse()")]
    public void AllGuard_ThenSortReverse() {
        "arr:int?[] = [3,1,2]\r y:int[] = if(arr.all(rule it != none)) arr.sort().reverse() else []"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled)
            .AssertResultHas("y", new[] { 3, 2, 1 });
    }

    // ═══════════════════════════════════════════════════════════════
    // 13. Combined scalar + collection narrowing:
    //     outer if guards optional scalar, compact() strips array elements
    // ═══════════════════════════════════════════════════════════════
    [Test]
    public void CombinedScalarAndCollectionNarrowing() {
        var expr =
            "x:int? = 10\r" +
            "arr:int?[] = [1, none, 2]\r" +
            "y:int[] = if(x != none) arr.filterNotNull().map(rule it + x) else []";
        expr.CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled)
            .AssertResultHas("y", new[] { 11, 12 });
    }

    // ═══════════════════════════════════════════════════════════════
    // 14. Array of optional structs: compact then access struct field
    //     {name:text, age:int}?[] -> compact() -> map(.age)
    // ═══════════════════════════════════════════════════════════════
    [Test]
    public void FilterNarrowing_OptionalStructArray_MapField() {
        var expr =
            "a = if(true) {name='Alice', age=30} else none\r" +
            "b = if(false) {name='Bob', age=25} else none\r" +
            "c = if(true) {name='Carol', age=35} else none\r" +
            "items = [a, b, c]\r" +
            "y:int[] = items.filterNotNull().map(rule it.age)";
        expr.CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled)
            .AssertResultHas("y", new[] { 30, 35 });
    }

    // ═══════════════════════════════════════════════════════════════
    // 15. Filter chain: compact then two value filters
    //     int?[] -> compact() -> filter(> 0) -> filter(< 100)
    // ═══════════════════════════════════════════════════════════════
    [Test]
    public void FilterChain_NarrowThenValueFilters() {
        "arr:int?[] = [none, -5, none, 50, 200, 0, 10]\r y:int[] = arr.filterNotNull().filter(rule it > 0).filter(rule it < 100)"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled)
            .AssertResultHas("y", new[] { 50, 10 });
    }

    // ═══════════════════════════════════════════════════════════════
    // 16. all() with named lambda parameter (rule(x) = ...) syntax
    //     should behave identically to anonymous `rule it != none`
    // ═══════════════════════════════════════════════════════════════
    [Test, Ignore("Collection narrowing not implemented: all() guard with named lambda")]
    public void AllGuard_NamedLambdaParameter() {
        "arr:int?[] = [1,2,3]\r y:int[] = if(arr.all(rule(x) = x != none)) arr.map(rule it + 1) else []"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled)
            .AssertResultHas("y", new[] { 2, 3, 4 });
    }

    // ═══════════════════════════════════════════════════════════════
    // 17. Compact + toText(): narrowed int elements converted to text
    //     int?[] -> compact() -> int[] -> map(toText()) -> text[]
    // ═══════════════════════════════════════════════════════════════
    [Test]
    public void FilterNarrowing_ThenToText() {
        "arr:int?[] = [1, none, 42]\r y:text[] = arr.filterNotNull().map(rule it.toText())"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled)
            .AssertResultHas("y", new[] { "1", "42" });
    }

    // ═══════════════════════════════════════════════════════════════
    // 18. Performance sanity: literal array with nones,
    //     compact + fold should produce correct sum
    // ═══════════════════════════════════════════════════════════════
    [Test]
    public void FilterNarrowing_LiteralArray_FoldSum() {
        "y:int = [1,none,2,none,3,none,4,none,5].filterNotNull().fold(rule it1 + it2)"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled)
            .AssertResultHas("y", 15);
    }

    // ═══════════════════════════════════════════════════════════════
    // 19. Empty result: all elements are none,
    //     compact produces empty array, count() returns 0
    // ═══════════════════════════════════════════════════════════════
    [Test]
    public void FilterNarrowing_AllNone_EmptyResult() {
        "arr:int?[] = [none, none, none]\r y:int = arr.filterNotNull().count()"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled)
            .AssertResultHas("y", 0);
    }

    // ═══════════════════════════════════════════════════════════════
    // 20. Regression: filter on non-optional array must still work
    //     normally — compact logic must not break existing behavior
    // ═══════════════════════════════════════════════════════════════
    [Test]
    public void NonOptionalArray_FilterUnchanged() {
        "y:int[] = [1,2,3,4,5].filter(rule it > 2)"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled)
            .AssertResultHas("y", new[] { 3, 4, 5 });
    }
}
