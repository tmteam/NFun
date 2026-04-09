using NFun.TestTools;
using NUnit.Framework;

namespace NFun.SyntaxTests.NamedTypes;

/// <summary>
/// Tests for inline named-type constructor combined with safe field access (?.).
/// Bug: `a{x=1, b=b{y=99}}.b?.y ?? -1` fails with FU829
/// while `v = a{...}; out = v.b?.y ?? -1` works fine.
/// </summary>
[TestFixture]
public class InlineConstructorSafeAccessTest {
    static CalculationResult CalcBoth(string expr) =>
        expr.CalcWithDialect(
            optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled,
            namedTypesSupport: NamedTypesSupport.ExperimentalEnabled);

    static CalculationResult CalcOptional(string expr) =>
        expr.CalcWithDialect(
            optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled);

    // 1. Anonymous struct (no named types) — does inline safe access work at all?
    [Test]
    public void AnonymousStruct_InlineSafeAccess_WithCoalesce() =>
        CalcOptional("out = {x=1, b=none}.b?.y ?? -1")
            .AssertResultHas("out", -1);

    // 2. Named type with simple optional field (not nested struct)
    [Test]
    public void NamedType_SimpleOptionalField_DefaultNone() =>
        CalcBoth("type a = {x:int, b:int? = none}\r out = a{x=1}.b ?? -1")
            .AssertResultHas("out", -1);

    [Test]
    public void NamedType_SimpleOptionalField_Provided() =>
        CalcBoth("type a = {x:int, b:int? = none}\r out = a{x=1, b=42}.b ?? -1")
            .AssertResultHas("out", 42);

    // 3. Non-safe access on inline constructor (no optional involved)
    [Test]
    public void InlineConstructor_NonSafeAccess_NestedField() =>
        CalcBoth("type b = {y:int}\r type a = {x:int, b:b}\r out = a{x=1, b=b{y=99}}.b.y")
            .AssertResultHas("out", 99);

    // 4. Inline constructor — non-optional nested struct, direct field access
    [Test]
    public void InlineConstructor_NonOptionalNested_DirectAccess() =>
        CalcBoth("type b = {y:int}\r type a = {x:int, b:b}\r out = a{x=1, b=b{y=99}}.x")
            .AssertResultHas("out", 1);

    // 5. Inline with ?? but no ?. — coalesce on optional field, then access result
    [Test]
    public void InlineConstructor_CoalesceWithoutSafeAccess() =>
        CalcBoth("type b = {y:int}\r type a = {x:int, b:b? = none}\r out = (a{x=1, b=b{y=99}}.b ?? b{y=0}).y")
            .AssertResultHas("out", 99);

    // 6. Two-step (variable) — this WORKS per bug report
    [Test]
    public void TwoStep_Variable_SafeAccess_HasValue() =>
        CalcBoth("type b = {y:int}\r type a = {x:int, b:b? = none}\r v = a{x=1, b=b{y=99}}\r out = v.b?.y ?? -1")
            .AssertResultHas("out", 99);

    [Test]
    public void TwoStep_Variable_SafeAccess_None() =>
        CalcBoth("type b = {y:int}\r type a = {x:int, b:b? = none}\r v = a{x=1}\r out = v.b?.y ?? -1")
            .AssertResultHas("out", -1);

    // 7. Inline constructor — non-optional field access (no ?. at all)
    [Test]
    public void InlineConstructor_NonOptionalField() =>
        CalcBoth("type a = {x:int, b:int? = none}\r out = a{x=42}.x")
            .AssertResultHas("out", 42);

    // 8. THE BUG: inline constructor + safe field access chain
    [Test]
    public void InlineConstructor_SafeAccess_Chain_HasValue() =>
        CalcBoth("type b = {y:int}\r type a = {x:int, b:b? = none}\r out = a{x=1, b=b{y=99}}.b?.y ?? -1")
            .AssertResultHas("out", 99);

    [Test]
    public void InlineConstructor_SafeAccess_Chain_WithParens() =>
        CalcBoth("type b = {y:int}\r type a = {x:int, b:b? = none}\r out = (a{x=1, b=b{y=99}}).b?.y ?? -1")
            .AssertResultHas("out", 99);
}
