namespace NFun.SyntaxTests.TypeNarrowing;

using NFun.TestTools;
using NUnit.Framework;

/// <summary>
/// Collection type narrowing tests: all()/any() guard-based narrowing.
/// When arr.all(rule it != none) is used as an if-guard, elements in the then-branch
/// should be narrowed from T? to T, allowing non-optional operations without force unwrap.
///
/// NOTE: all()/any() guard narrowing is NOT yet implemented. All tests remain [Ignored].
/// For filter-based narrowing, see CollectionNarrowingTest and CollectionNarrowingAdvancedTest
/// which use the now-implemented compact() function.
/// </summary>
[TestFixture]
public class CollectionNarrowingAllAnyTest {

    // ---------------------------------------------------------------
    // 1. all() guard + map: elements narrowed, arithmetic works
    // ---------------------------------------------------------------
    [Test, Ignore("Collection narrowing: all() guard should narrow int?[] elements to int in then-branch")]
    public void AllGuard_Map_IntArithmetic() =>
        "arr:int?[] = [1, 2, 3]\r y = if(arr.all(rule it != none)) arr.map(rule it + 1) else []"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled)
            .AssertResultHas("y", new[] { 2, 3, 4 });

    // ---------------------------------------------------------------
    // 2. all() guard + fold: elements narrowed, fold accumulates
    // ---------------------------------------------------------------
    [Test, Ignore("Collection narrowing: all() guard should narrow elements for fold")]
    public void AllGuard_Fold_IntSum() =>
        "arr:int?[] = [10, 20, 30]\r y = if(arr.all(rule it != none)) arr.fold(rule it1 + it2) else 0"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled)
            .AssertResultHas("y", 60);

    // ---------------------------------------------------------------
    // 3. all() guard + count: sanity check (count ignores element type)
    // ---------------------------------------------------------------
    [Test, Ignore("Collection narrowing: all() guard with count (sanity, count doesn't need narrowing)")]
    public void AllGuard_Count_SanityCheck() =>
        "arr:int?[] = [1, 2, 3]\r y = if(arr.all(rule it != none)) arr.count() else -1"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled)
            .AssertResultHas("y", 3);

    // ---------------------------------------------------------------
    // 4. all() guard with none present: takes else branch
    // ---------------------------------------------------------------
    [Test, Ignore("Collection narrowing: all() guard with none element takes else branch")]
    public void AllGuard_NonePresent_TakesElseBranch() =>
        "arr:int?[] = [1, none, 3]\r y = if(arr.all(rule it != none)) arr.map(rule it + 1) else [-1]"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled)
            .AssertResultHas("y", new[] { -1 });

    // ---------------------------------------------------------------
    // 5. all() guard + sort: narrowed elements are sortable
    // ---------------------------------------------------------------
    [Test, Ignore("Collection narrowing: all() guard should narrow elements for sort")]
    public void AllGuard_Sort_IntArray() =>
        "arr:int?[] = [3, 1, 2]\r y = if(arr.all(rule it != none)) arr.sort() else []"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled)
            .AssertResultHas("y", new[] { 1, 2, 3 });

    // ---------------------------------------------------------------
    // 6. all() guard with real?[] elements
    // ---------------------------------------------------------------
    [Test, Ignore("Collection narrowing: all() guard should narrow real?[] to real[]")]
    public void AllGuard_Map_RealArray() =>
        "arr:real?[] = [1.5, 2.5, 3.5]\r y = if(arr.all(rule it != none)) arr.map(rule it * 2) else []"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled)
            .AssertResultHas("y", new[] { 3.0, 5.0, 7.0 });

    // ---------------------------------------------------------------
    // 7. all() guard with text?[] elements
    // ---------------------------------------------------------------
    [Test, Ignore("Collection narrowing: all() guard should narrow text?[] to text[]")]
    public void AllGuard_Map_TextArray() =>
        "arr:text?[] = ['a', 'b']\r y = if(arr.all(rule it != none)) arr.count() else -1"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled)
            .AssertResultHas("y", 2);

    // ---------------------------------------------------------------
    // 8. all() guard with bool?[] elements
    // ---------------------------------------------------------------
    [Test, Ignore("Collection narrowing: all() guard should narrow bool?[] to bool[]")]
    public void AllGuard_BoolArray_AllTrue() =>
        "arr:bool?[] = [true, true]\r y = if(arr.all(rule it != none)) arr.all(rule it) else false"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled)
            .AssertResultHas("y", true);

    // ---------------------------------------------------------------
    // 9. Negative: any() does NOT narrow (any guarantees at least one,
    //    not all, so elements remain optional)
    // ---------------------------------------------------------------
    [Test, Ignore("Collection narrowing: any() should NOT narrow elements (not all guaranteed non-none)")]
    public void AnyGuard_DoesNotNarrow_ShouldFail() =>
        "arr:int?[] = [1, none, 3]\r y = if(arr.any(rule it != none)) arr.map(rule it + 1) else []"
            .AssertObviousFailsOnParse(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled);

    // ---------------------------------------------------------------
    // 10. Negative: all() with wrong predicate (not a none check)
    //     does not narrow — it > 0 says nothing about nullability
    // ---------------------------------------------------------------
    [Test, Ignore("Collection narrowing: all(rule it > 0) is not a none check, should not narrow")]
    public void AllGuard_WrongPredicate_DoesNotNarrow() =>
        "arr:int?[] = [1, 2, 3]\r y = if(arr.all(rule it > 0)) arr.map(rule it + 1) else []"
            .AssertObviousFailsOnParse(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled);

    // ---------------------------------------------------------------
    // 11. all() + multiple arrays: both narrowed in then-branch
    // ---------------------------------------------------------------
    [Test, Ignore("Collection narrowing: multiple all() guards should narrow all arrays")]
    public void AllGuard_MultipleArrays_BothNarrowed() =>
        ("a:int?[] = [1, 2]\r b:int?[] = [3, 4]\r " +
         "y = if(a.all(rule it != none) and b.all(rule it != none)) " +
         "a.map(rule it + 1).concat(b.map(rule it + 1)) else []")
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled)
            .AssertResultHas("y", new[] { 2, 3, 4, 5 });

    // ---------------------------------------------------------------
    // 12. all() + scalar narrowing: both x and arr elements narrowed
    // ---------------------------------------------------------------
    [Test, Ignore("Collection narrowing: scalar and collection narrowing combined")]
    public void AllGuard_PlusScalarNarrowing_Combined() =>
        ("x:int? = 10\r arr:int?[] = [1, 2, 3]\r " +
         "y = if(x != none and arr.all(rule it != none)) x + arr.fold(rule it1 + it2) else 0")
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled)
            .AssertResultHas("y", 16);

    // ---------------------------------------------------------------
    // 13. Nested: all() in nested if — outer narrows nullability,
    //     inner checks value predicate on already-narrowed elements
    // ---------------------------------------------------------------
    [Test, Ignore("Collection narrowing: nested if after all() guard")]
    public void AllGuard_NestedIf_InnerPredicateOnNarrowed() =>
        ("arr:int?[] = [1, 2, 3]\r " +
         "y = if(arr.all(rule it != none)) if(arr.all(rule it > 0)) arr.fold(rule it1 + it2) else 0 else -1")
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled)
            .AssertResultHas("y", 6);

    // ---------------------------------------------------------------
    // 14. all() on empty array: vacuously true, narrowing applies
    //     but nothing to iterate
    // ---------------------------------------------------------------
    [Test, Ignore("Collection narrowing: empty array all() is vacuously true")]
    public void AllGuard_EmptyArray_VacuouslyTrue() =>
        "arr:int?[] = []\r y = if(arr.all(rule it != none)) arr.count() else -1"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled)
            .AssertResultHas("y", 0);

    // ---------------------------------------------------------------
    // 15. all() guard + array literal construction from narrowed elements
    // ---------------------------------------------------------------
    [Test, Ignore("Collection narrowing: indexing narrowed array in literal")]
    public void AllGuard_ArrayLiteralFromNarrowed() =>
        "arr:int?[] = [10, 20]\r y = if(arr.all(rule it != none)) [arr[0], arr[1]] else []"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled)
            .AssertResultHas("y", new[] { 10, 20 });

    // ---------------------------------------------------------------
    // 16. De Morgan: not(arr.any(rule it == none)) is equivalent to
    //     arr.all(rule it != none) and should also narrow
    // ---------------------------------------------------------------
    [Test, Ignore("Collection narrowing: De Morgan not(any(==none)) equivalent to all(!=none)")]
    public void DeMorgan_NotAnyEqualsNone_Narrows() =>
        "arr:int?[] = [1, 2, 3]\r y = if(not(arr.any(rule it == none))) arr.map(rule it + 1) else []"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled)
            .AssertResultHas("y", new[] { 2, 3, 4 });

    // ---------------------------------------------------------------
    // 17. Filter inside all() guard: narrowed elements support filter
    // ---------------------------------------------------------------
    [Test, Ignore("Collection narrowing: filter on narrowed elements inside guard")]
    public void AllGuard_FilterInsideGuard_CountPositive() =>
        "arr:int?[] = [1, -2, 3]\r y = if(arr.all(rule it != none)) arr.filter(rule it > 0).count() else 0"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled)
            .AssertResultHas("y", 2);

    // ---------------------------------------------------------------
    // 18. all() guard preserves optionality in else branch:
    //     elements still T? in else, must use ?? or ! to operate
    // ---------------------------------------------------------------
    [Test, Ignore("Collection narrowing: else branch elements remain optional")]
    public void AllGuard_ElseBranch_ElementsStillOptional() =>
        ("arr:int?[] = [1, none, 3]\r " +
         "y = if(arr.all(rule it != none)) arr.map(rule it + 1) else arr.map(rule it ?? 0)")
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled)
            .AssertResultHas("y", new[] { 1, 0, 3 });

    // ---------------------------------------------------------------
    // 19. Multiple all() guards in AND for separate arrays
    // ---------------------------------------------------------------
    [Test, Ignore("Collection narrowing: multiple all() in AND narrows all arrays")]
    public void MultipleAllGuards_InAnd_BothNarrowed() =>
        ("a:int?[] = [1, 2]\r b:int?[] = [10, 20]\r " +
         "y = if(a.all(rule it != none) and b.all(rule it != none)) " +
         "a.fold(rule it1 + it2) + b.fold(rule it1 + it2) else 0")
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled)
            .AssertResultHas("y", 33);

    // ---------------------------------------------------------------
    // 20. Chained: filter + all + map — filter result checked, then mapped
    // ---------------------------------------------------------------
    [Test, Ignore("Collection narrowing: filter result checked with all() then mapped")]
    public void Chained_FilterThenAllGuard_ThenMap() =>
        ("arr:int?[] = [1, none, 3, none, 5]\r " +
         "filtered = arr.filter(rule it != none)\r " +
         "y = if(filtered.all(rule it != none)) filtered.map(rule it * 2) else []")
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled)
            .AssertResultHas("y", new[] { 2, 6, 10 });
}
