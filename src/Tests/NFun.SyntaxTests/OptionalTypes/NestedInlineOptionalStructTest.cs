namespace NFun.SyntaxTests.OptionalTypes;

using NFun.TestTools;
using NUnit.Framework;

/// <summary>
/// Tests for inline nested optional structs created via if/else none at multiple nesting levels.
/// Bug: struct with optional field (via if/else none) + ?. access causes FU777.
/// The workaround (pre-declaring intermediate variables) works; inline should too.
/// </summary>
[TestFixture]
public class NestedInlineOptionalStructTest {

    // === Build-only tests: inline nesting without ?. access (baseline, already works) ===

    [Test]
    public void InlineNested_2Levels_Builds() =>
        Assert.DoesNotThrow(() =>
            "a = if(true) {b = if(true) {d = 99} else none} else none"
                .BuildWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled));

    [Test]
    public void InlineNested_3Levels_Builds() =>
        Assert.DoesNotThrow(() =>
            "a = if(true) {b = if(true) {c = if(true) {d = 99} else none} else none} else none"
                .BuildWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled));

    [Test]
    public void InlineNested_4Levels_Builds() =>
        Assert.DoesNotThrow(() =>
            "a = if(true) {b = if(true) {c = if(true) {d = if(true) {e = 42} else none} else none} else none} else none"
                .BuildWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled));

    // === The core bug: inline optional field + ?. access ===

    [Test]
    public void InlineOptionalField_SafeAccess_2Levels_Builds() =>
        Assert.DoesNotThrow(() =>
            ("x = if(true) {inner = if(true) {d = 99} else none} else none\r" +
             "y = x?.inner")
                .BuildWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled));

    [Test]
    public void InlineOptionalField_SafeAccess_3Levels_Builds() =>
        Assert.DoesNotThrow(() =>
            ("x = if(true) {b = if(true) {c = if(true) {d = 99} else none} else none} else none\r" +
             "y = x?.b")
                .BuildWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled));

    [Test]
    public void InlineOptionalField_SafeAccess_4Levels_Builds() =>
        Assert.DoesNotThrow(() =>
            ("x = if(true) {b = if(true) {c = if(true) {d = if(true) {e = 42} else none} else none} else none} else none\r" +
             "y = x?.b")
                .BuildWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled));

    // === Runtime: none propagation at various nesting levels ===

    [Test]
    public void InlineNested_3Levels_OuterNone_ReturnsNull() {
        var result = "a = if(false) {b = if(true) {c = if(true) {d = 99} else none} else none} else none"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled);
        Assert.IsNull(result.Get("a"));
    }

    [Test]
    public void InlineNested_4Levels_OuterNone_ReturnsNull() {
        var result = "a = if(false) {b = if(true) {c = if(true) {d = if(true) {e = 42} else none} else none} else none} else none"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled);
        Assert.IsNull(result.Get("a"));
    }

    [Test]
    public void InlineOptionalField_SafeAccess_OuterNone_ReturnsNull() {
        var result =
            ("x = if(false) {b = if(true) {c = if(true) {d = 99} else none} else none} else none\r" +
             "y = x?.b")
                .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled);
        Assert.IsNull(result.Get("y"));
    }

    // === Predeclared workaround (regression baseline — always works) ===

    [Test]
    public void InlineNested_3Levels_Predeclared_Equivalent() =>
        Assert.DoesNotThrow(() =>
            ("inner = if(true) {d=99} else none\r" +
             "mid = if(true) {c=inner} else none\r" +
             "outer = if(true) {b=mid} else none")
                .BuildWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled));
}
