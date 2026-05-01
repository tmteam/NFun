namespace NFun.SyntaxTests.OptionalTypes;

using NFun.Exceptions;
using NFun.TestTools;
using NUnit.Framework;

/// <summary>
/// Tests for two thin coverage areas:
/// 1. Nested optionals (int?? and indirect construction via if-else/functions)
/// 2. Optional values inside string interpolation and toText
///
/// Key design fact: NFun FLATTENS nested optionals. opt(opt(T)) -> opt(T).
/// This is by design in the TIC solver (see OptionalCompositeTests.NestedOptional_Flattens).
///
/// Syntax fact: `int??` cannot be parsed because `??` is tokenized as
/// TokType.NullCoalesce (a single two-char token), not two Question tokens.
/// The type parser in TokenHelper.ReadTypeSyntax only handles TokType.Question.
/// So nested optional types must be constructed indirectly.
/// </summary>
[TestFixture]
public class NestedOptionalAndInterpolationTest {

    // ══════════════════════════════════════════════════════════════════════════
    // Area 1: Nested Optionals — 10 tests
    // ══════════════════════════════════════════════════════════════════════════

    // --- Test 1: int?? syntax cannot be parsed (tokenizer reads ?? as NullCoalesce) ---

    [Test]
    public void NestedOptional_DirectSyntax_IntQuestionQuestion_FailsOnParse() {
        // `x:int??` tokenizes as: Id("x"), Colon, Int32Type, NullCoalesce
        // The type parser sees `int` (no Question follows), then `??` is parsed
        // as an operator, leading to a parse error.
        Assert.Throws<FunnyParseException>(
            () => "x:int?? = 42".BuildWithDialect(
                optionalTypesSupport: OptionalTypesSupport.Enabled));
    }

    // --- Test 2: Nested optional flattens via if-else wrapping ---

    [Test]
    public void NestedOptional_IfElse_OptionalOrNone_Flattens_HasValue() {
        // x is int?, so `if(true) x else none` should be int? (not int??).
        // NFun flattens: LCA(opt(int), None) = opt(int), not opt(opt(int)).
        var result = "x:int? = 42\r y = if(true) x else none"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.Enabled);
        Assert.AreEqual(42, result.Get("y"));
    }

    // --- Test 3: Nested optional flattens via if-else, none case ---

    [Test]
    public void NestedOptional_IfElse_OptionalOrNone_Flattens_None() {
        // if(false) x else none => none (the outer layer, but type is int? not int??)
        var result = "x:int? = 42\r y = if(false) x else none"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.Enabled);
        Assert.IsNull(result.Get("y"));
    }

    // --- Test 4: Double coalesce chain ---

    [Test]
    public void NestedOptional_DoubleCoalesceChain() {
        // a ?? b ?? 0: first coalesce unwraps a, if none then b, if none then 0
        "a:int? = none\r b:int? = none\r y = a ?? b ?? 0"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.Enabled)
            .AssertResultHas("y", 0);
    }

    // --- Test 5: Unwrap then coalesce ---

    [Test]
    public void NestedOptional_UnwrapThenCoalesce() {
        // x is int?. x! unwraps to int. No coalesce needed, but we can still use it.
        "x:int? = 42\r y = x! + 0"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.Enabled)
            .AssertResultHas("y", 42);
    }

    // --- Test 6: Function returning optional, wrapped in if-else with none ---

    [Test]
    public void NestedOptional_FunctionReturningOptional_IfElseWithNone_Flattens() {
        // f returns int?. `if(true) f(5) else none` should still be int? (flattened).
        "f(x:int):int? = if(x > 0) x else none\r y = if(true) f(5) else none"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.Enabled)
            .AssertResultHas("y", 5);
    }

    // --- Test 7: Function returning optional, none path ---

    [Test]
    public void NestedOptional_FunctionReturningOptional_IfElseWithNone_NoneResult() {
        var result = "f(x:int):int? = if(x > 0) x else none\r y = if(false) f(5) else none"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.Enabled);
        Assert.IsNull(result.Get("y"));
    }

    // --- Test 8: Coalesce on if-else result that wraps optional ---

    [Test]
    public void NestedOptional_CoalesceOnIfElseWrappingOptional() {
        // x is int?. `if(false) x else none` is int? (flattened). Coalesce produces int.
        "x:int? = none\r y = (if(true) x else none) ?? 99"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.Enabled)
            .AssertResultHas("y", 99);
    }

    // --- Test 9: Array of optional ints, element access returns optional, coalesce ---

    [Test]
    public void NestedOptional_ArrayOfOptionalInts_ElementCoalesce() {
        // arr is int?[]. arr[1] is int? (none). Coalesce gives int.
        "arr:int?[] = [10, none, 30]\r y = arr[1] ?? -1"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.Enabled)
            .AssertResultHas("y", -1);
    }

    // --- Test 10: Chained optional assignment stays flat ---

    [Test]
    public void NestedOptional_ChainedOptionalAssignment_StaysFlat() {
        // a is int?, b:int? = a should stay int? (not int??).
        // Then coalesce on b.
        "a:int? = 42\r b:int? = a\r y = b ?? 0"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.Enabled)
            .AssertResultHas("y", 42);
    }


    // ══════════════════════════════════════════════════════════════════════════
    // Area 2: Optional + String Interpolation and toText — 10 tests
    // ══════════════════════════════════════════════════════════════════════════

    // --- Test 11: Optional int with value in interpolation ---

    [Test]
    public void Interpolation_OptionalInt_HasValue() {
        "x:int? = 42\r y = '{x}'"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.Enabled)
            .AssertResultHas("y", "42");
    }

    // --- Test 12: Optional int none in interpolation ---

    [Test]
    public void Interpolation_OptionalInt_None() {
        "x:int? = none\r y = '{x}'"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.Enabled)
            .AssertResultHas("y", "none");
    }

    // --- Test 13: Optional in sentence interpolation ---

    [Test]
    public void Interpolation_OptionalInSentence_HasValue() {
        "x:int? = 42\r y = 'value is {x}'"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.Enabled)
            .AssertResultHas("y", "value is 42");
    }

    // --- Test 14: Coalesce inside interpolation ---

    [Test]
    public void Interpolation_CoalesceInsideInterpolation() {
        "x:int? = none\r y = '{x ?? 0}'"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.Enabled)
            .AssertResultHas("y", "0");
    }

    // --- Test 15: Force unwrap inside interpolation ---

    [Test]
    public void Interpolation_ForceUnwrapInsideInterpolation() {
        "x:int? = 42\r y = '{x!}'"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.Enabled)
            .AssertResultHas("y", "42");
    }

    // --- Test 16: toText on optional with value ---

    [Test]
    public void ToText_OptionalInt_HasValue() {
        "x:int? = 42\r y = toText(x)"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.Enabled)
            .AssertResultHas("y", "42");
    }

    // --- Test 17: toText on optional none ---

    [Test]
    public void ToText_OptionalInt_None() {
        "x:int?\r y = toText(x)"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.Enabled)
            .AssertReturns("y", "none");
    }

    // --- Test 18: toText after coalesce ---

    [Test]
    public void ToText_AfterCoalesce() {
        "x:int? = none\r y = toText(x ?? 0)"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.Enabled)
            .AssertResultHas("y", "0");
    }

    // --- Test 19: Multiple optionals in interpolation ---

    [Test]
    public void Interpolation_MultipleOptionals() {
        "a:int? = 1\r b:int? = none\r y = '{a} and {b}'"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.Enabled)
            .AssertResultHas("y", "1 and none");
    }

    // --- Test 20: Optional real in interpolation ---

    [Test]
    public void Interpolation_OptionalReal_HasValue() {
        "x:real? = 1.5\r y = 'val={x}'"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.Enabled)
            .AssertResultHas("y", "val=1.5");
    }
}
