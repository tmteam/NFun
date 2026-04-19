namespace NFun.SyntaxTests.OptionalTypes;

using NFun.TestTools;
using NUnit.Framework;

/// <summary>
/// Tests for `none` inside struct literals combined with safe access (?.) chains.
/// Probes the boundary between typed and untyped none in nested structs.
/// Bug: untyped `none` in nested struct + safe access chain causes FU769.
/// </summary>
[TestFixture]
public class NoneInStructSafeAccessTest {
    // ===== Working cases: typed none at single level =====

    [Test]
    public void TypedNone_SingleLevel_SafeAccess_WithCoalesce() =>
        "a:{x:int, inner:{x:int}?} = {x = 1, inner = none}\r out = a.inner?.x ?? -1"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled)
            .AssertResultHas("out", -1);

    [Test]
    public void TypedNone_SingleLevel_SafeAccess_HasValue() =>
        "a:{x:int, inner:{x:int}?} = {x = 1, inner = {x = 2}}\r out = a.inner?.x ?? -1"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled)
            .AssertResultHas("out", 2);

    [Test]
    public void TypedNone_SingleLevel_SafeAccess_NoCoalesce() {
        var result = "a:{x:int, inner:{x:int}?} = {x = 1, inner = none}\r out = a.inner?.x"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled);
        Assert.IsNull(result.Get("out"));
    }

    // ===== Working case: if-else produces optional struct (none not inside struct literal) =====

    [Test]
    public void IfElseNone_NestedSafeAccess_HasValue() =>
        "a = if(true) {x = 1, inner = {x = 2}} else none\r out = a?.inner.x"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled)
            .AssertResultHas("out", 2);

    [Test]
    public void IfElseNone_NestedSafeAccess_None() {
        var result = "a = if(false) {x = 1, inner = {x = 2}} else none\r out = a?.inner.x"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled);
        Assert.IsNull(result.Get("out"));
    }

    // ===== BUG cases: untyped none inside struct literal + deep safe access =====

    [Test]
    public void UntypedNone_NestedStruct_DeepSafeAccess_WithCoalesce() =>
        // a.inner is a concrete struct, use '.' not '?.'. inner.inner is None, use '?.'.
        "a = {x = 1, inner = {x = 2, inner = none}}\r out = a.inner.inner?.x ?? -1"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled)
            .AssertResultHas("out", -1);

    [Test]
    public void UntypedNone_NestedStruct_SafeAccess_WithCoalesce() =>
        // Simpler: none as a struct field value, no type annotation
        "a = {x = 1, inner = none}\r out = a.inner?.x ?? -1"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled)
            .AssertResultHas("out", -1);

    [Test]
    public void UntypedNone_NestedStruct_SafeAccess_NoCoalesce() {
        // Same but without coalesce -- result should be null
        var result = "a = {x = 1, inner = none}\r out = a.inner?.x"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled);
        Assert.IsNull(result.Get("out"));
    }

    // ===== Variations: different field names, multiple none fields =====

    [Test]
    public void UntypedNone_DifferentFieldNames() =>
        "rec = {name = 'Alice', child = none}\r out = rec.child?.name ?? 'nobody'"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled)
            .AssertResultHas("out", "nobody");

    [Test]
    public void UntypedNone_MultipleNoneFields() =>
        "a = {first = none, second = none}\r out = a.first?.x ?? a.second?.x ?? -1"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled)
            .AssertResultHas("out", -1);

    // ===== Variation: none at different nesting levels =====

    [Test]
    public void UntypedNone_ThreeLevelsDeep() =>
        "a = {l1 = {l2 = {l3 = none}}}\r out = a.l1.l2.l3?.value ?? -1"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled)
            .AssertResultHas("out", -1);

    // ===== Variation: intermediate variables vs inline =====

    [Test]
    public void UntypedNone_IntermediateVariable() =>
        "inner = {x = 2, child = none}\r a = {x = 1, inner = inner}\r out = a.inner.child?.x ?? -1"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled)
            .AssertResultHas("out", -1);

    [Test]
    public void TypedNone_IntermediateVariable_HasValue() =>
        "inner:{x:int, child:{x:int}?} = {x = 2, child = {x = 42}}\r a = {x = 1, inner = inner}\r out = a.inner.child?.x ?? -1"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled)
            .AssertResultHas("out", 42);

    // ===== BUG FIX: if-else with none as struct field + chained safe access =====
    // Bug: y?.a?.b fails when both optional levels are from inline if(cond) struct else none.
    // Root cause: PushConstraintsFunctions.Apply(StateStruct, ConstraintsState) called
    // TransformToStructOrNull which stripped the IsOptional flag from the inner if-else result.

    [Test]
    public void IfElseNone_AsStructField_ChainedSafeAccess_HasValue() =>
        "y = if(true) {a = if(true) {b=42} else none} else none\r out = y?.a?.b ?? -1"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled)
            .AssertResultHas("out", 42);

    [Test]
    public void IfElseNone_AsStructField_ChainedSafeAccess_OuterNone() =>
        "y = if(false) {a = if(true) {b=42} else none} else none\r out = y?.a?.b ?? -1"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled)
            .AssertResultHas("out", -1);

    [Test]
    public void IfElseNone_AsStructField_ChainedSafeAccess_InnerNone() =>
        "y = if(true) {a = if(false) {b=42} else none} else none\r out = y?.a?.b ?? -1"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled)
            .AssertResultHas("out", -1);

    [Test]
    public void IfElseNone_AsStructField_Decomposed_Works() =>
        // Decomposed version always worked; this is the regression guard.
        "y = if(true) {a = if(true) {b=42} else none} else none\r w = y?.a\r out = w?.b ?? -1"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled)
            .AssertResultHas("out", 42);
}
