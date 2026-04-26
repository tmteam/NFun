namespace NFun.SyntaxTests.OptionalTypes;

using NFun.TestTools;
using NUnit.Framework;

/// <summary>
/// Tests for the ?[i] safe array access operator.
/// arr?[i] — safe indexing on an optional array.
/// If arr is none, returns none. If arr has a value, returns the element (as optional).
/// </summary>
[TestFixture]
public class SafeArrayAccessTest {

    // 1. Basic has-value: int array with value, access first element
    [Test]
    public void IntArray_HasValue_FirstElement() =>
        "arr:int[]? = [10,20,30]\r y = arr?[0] ?? -1"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled)
            .AssertResultHas("y", 10);

    // 2. Basic none: int array is none, coalesce to default
    [Test]
    public void IntArray_None_CoalescesToDefault() =>
        "arr:int[]? = none\r y = arr?[0] ?? -1"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled)
            .AssertResultHas("y", -1);

    // 3. Middle index
    [Test]
    public void IntArray_HasValue_MiddleIndex() =>
        "arr:int[]? = [10,20,30]\r y = arr?[1] ?? -1"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled)
            .AssertResultHas("y", 20);

    // 4. Last index
    [Test]
    public void IntArray_HasValue_LastIndex() =>
        "arr:int[]? = [10,20,30]\r y = arr?[2] ?? -1"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled)
            .AssertResultHas("y", 30);

    // 5. Real array
    [Test]
    public void RealArray_HasValue() =>
        "arr:real[]? = [1.5, 2.5]\r y = arr?[0] ?? 0.0"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled)
            .AssertResultHas("y", 1.5);

    // 6. Text array
    [Test]
    public void TextArray_HasValue() =>
        "arr:text[]? = ['hello', 'world']\r y = arr?[1] ?? ''"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled)
            .AssertResultHas("y", "world");

    // 7. Bool array
    [Test]
    public void BoolArray_HasValue() =>
        "arr:bool[]? = [true, false]\r y = arr?[0] ?? false"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled)
            .AssertResultHas("y", true);

    // 8. With force unwrap
    [Test]
    public void IntArray_HasValue_ForceUnwrap() =>
        "arr:int[]? = [42]\r y = arr?[0]!"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled)
            .AssertResultHas("y", 42);

    // 9. Chained with safe field access: struct?.field?[i]
    [Test]
    public void ChainedSafeFieldAccess_ThenSafeArrayAccess_HasValue() =>
        "s = if(true) {items = [1,2,3]} else none\r y = s?.items?[0] ?? -1"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled)
            .AssertResultHas("y", 1);

    // 10. Result type is optional when no coalesce
    [Test]
    public void IntArray_HasValue_ResultIsOptional() {
        var result = "arr:int[]? = [10]\r y = arr?[0]"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled);
        // y should be int? = 10, which converts to boxed int (not null)
        Assert.AreEqual(10, result.Get("y"));
    }

    // 11. None result without coalesce
    [Test]
    public void IntArray_None_ResultIsNone() {
        var result = "arr:int[]? = none\r y = arr?[0]"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled);
        Assert.IsNull(result.Get("y"));
    }

    // 12. Array of optionals: safe access on int?[]?, none element coalesces
    [Test]
    public void ArrayOfOptionals_NoneElement_Coalesces() =>
        "arr:int?[]? = [1, none, 3]\r y = arr?[1] ?? -1"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled)
            .AssertResultHas("y", -1);

    // 13. Used in arithmetic with coalesce
    [Test]
    public void SafeAccess_InArithmeticWithCoalesce() =>
        "arr:int[]? = [10]\r y = (arr?[0] ?? 0) + 5"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled)
            .AssertResultHas("y", 15);

    // 14. Used in if condition
    [Test]
    public void SafeAccess_InIfCondition_HasValue() =>
        "arr:int[]? = [42]\r y = if(arr?[0] != none) arr?[0]! else 0"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled)
            .AssertResultHas("y", 42);

    // 15. Multiple safe accesses from different arrays
    [Test]
    public void MultipleSafeAccesses_BothHaveValue() =>
        "a:int[]? = [1]\r b:int[]? = [2]\r y = (a?[0] ?? 0) + (b?[0] ?? 0)"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled)
            .AssertResultHas("y", 3);

    // 16. Variable index
    [Test]
    public void SafeAccess_VariableIndex() =>
        "arr:int[]? = [10,20,30]\r i:int = 1\r y = arr?[i] ?? -1"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled)
            .AssertResultHas("y", 20);

    // 17. ?[] on non-optional array — should still work (lifts result to optional)
    [Test]
    public void NonOptionalArray_SafeAccess_ReturnsOptional() {
        var result = "arr:int[] = [1,2,3]\r y = arr?[0]"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled);
        // arr is not optional, but ?[] returns T? anyway
        Assert.AreEqual(1, result.Get("y"));
    }

    // 18. Chained: struct is none, safe field + safe array access returns none
    [Test]
    public void ChainedSafeFieldAccess_StructIsNone_CoalescesToDefault() =>
        "s = if(false) {items = [1,2,3]} else none\r y = s?.items?[0] ?? -1"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled)
            .AssertResultHas("y", -1);

    // 19. Safe access then method on coalesced text
    [Test]
    public void SafeAccess_TextArray_CoalesceThenCount_HasValue() =>
        "arr:text[]? = ['hello']\r y = (arr?[0] ?? '').count()"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled)
            .AssertResultHas("y", 5);

    // 20. Nested array: int[][]? — safe access returns int[]?
    [Test]
    public void NestedArray_SafeAccess_ReturnsOptionalInnerArray() {
        var result = "arr:int[][]? = [[1,2],[3,4]]\r y = arr?[0]"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled);
        var inner = (int[])result.Get("y");
        Assert.AreEqual(new[] { 1, 2 }, inner);
    }

    // ═══════════════════════════════════════════════════════════════
    // Safe array access: bounds and type variations
    // ═══════════════════════════════════════════════════════════════

    [Test]
    public void SafeArrayAccess_ValidIndex_ReturnsValue() =>
        "arr = [10,20,30]\r out:int = arr?[0] ?? -1"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled)
            .AssertResultHas("out", 10);

    [Test]
    public void SafeArrayAccess_OutOfBounds_ReturnsNone_CoalescesToZero() =>
        "arr = [10,20,30]\r out:int = arr?[99] ?? 0"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled)
            .AssertResultHas("out", 0);

    [Test]
    public void SafeArrayAccess_NegativeIndex_ReturnsNone() =>
        "arr = [10,20,30]\r out:int = arr?[-1] ?? -99"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled)
            .AssertResultHas("out", -99);

    [Test]
    public void SafeArrayAccess_NoneArray_ReturnsNone() {
        var result = "arr:int[]? = none\r out = arr?[0]"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled);
        Assert.IsNull(result.Get("out"));
    }

    [Test]
    public void SafeArrayAccess_LastElement_CoalescesToDefault() =>
        "arr = [10,20,30]\r out:int = arr?[2] ?? -1"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled)
            .AssertResultHas("out", 30);

    [Test]
    public void SafeArrayAccess_RealArray_ValidIndex() =>
        "arr:real[]? = [1.5, 2.5, 3.5]\r out = arr?[1] ?? 0.0"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled)
            .AssertResultHas("out", 2.5);

    [Test]
    public void SafeArrayAccess_TextArray_OutOfBounds() =>
        "arr:text[]? = ['hello', 'world']\r out = arr?[5] ?? 'empty'"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled)
            .AssertResultHas("out", "empty");

    [Test]
    public void SafeArrayAccess_ChainedWithSafeFieldAccess_HasValue() =>
        "s = if(true) {items = [1,2,3]} else none\r out = s?.items?[1] ?? -1"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled)
            .AssertResultHas("out", 2);

    [Test]
    public void SafeArrayAccess_ChainedWithSafeFieldAccess_NoneStruct() =>
        "s = if(false) {items = [1,2,3]} else none\r out = s?.items?[0] ?? -1"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled)
            .AssertResultHas("out", -1);

    [Test]
    public void SafeArrayAccess_InArithmeticExpression() =>
        "arr = [10,20,30]\r out:int = (arr?[0] ?? 0) + (arr?[1] ?? 0)"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled)
            .AssertResultHas("out", 30);

    [Test]
    public void SafeArrayAccessOnNonOptVar_CoalesceTypeCorrect() {
        // a?[0] ?? 0 should be Int32, not Int32?
        "a = [1,2,3]; z = a?[0] ?? 0"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled)
            .AssertResultHas("z", 1);
    }

    [Test]
    public void ChainedSafeArrayAccess_NoError() {
        Assert.DoesNotThrow(() =>
            "a = [1]; b = [2]; y = a?[0] ?? b?[0] ?? 0"
                .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled));
    }
}
