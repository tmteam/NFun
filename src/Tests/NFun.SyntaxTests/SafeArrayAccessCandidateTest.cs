using NFun.TestTools;
using NUnit.Framework;

namespace NFun.SyntaxTests;

/// <summary>
/// Candidate tests for ?[ (safe array indexing) operator.
/// The ?[ syntax is reserved but not yet enabled at the parser level.
/// These tests document the intended behavior for when the feature is enabled.
/// TIC-level support is already implemented.
/// </summary>
[TestFixture]
[Ignore("?[ syntax is not yet supported at the parser level")]
public class SafeArrayAccessCandidateTest {

    // ═══════════════════════════════════════════════════════════════
    // Basic ?[ safe array indexing
    // ═══════════════════════════════════════════════════════════════

    [Test]
    public void SafeArrayIndex_Int_HasValue() =>
        "x:int[]? = [10,20,30]\r y = x?[0]".AssertResultHas("y", 10);

    [Test]
    public void SafeArrayIndex_Int_SecondElement() =>
        "x:int[]? = [10,20,30]\r y = x?[1]".AssertResultHas("y", 20);

    [Test]
    public void SafeArrayIndex_Int_None() {
        var result = "x:int[]? = none\r y = x?[0]".Calc();
        Assert.IsNull(result.Get("y"));
    }

    [Test]
    public void SafeArrayIndex_Real_HasValue() =>
        "x:real[]? = [1.1, 2.2]\r y = x?[0]".AssertResultHas("y", 1.1);

    [Test]
    public void SafeArrayIndex_Text_HasValue() =>
        "x:text[]? = ['hello', 'world']\r y = x?[0]".AssertResultHas("y", "hello");

    [Test]
    public void SafeArrayIndex_Text_None() {
        var result = "x:text[]? = none\r y = x?[0]".Calc();
        Assert.IsNull(result.Get("y"));
    }


    // ═══════════════════════════════════════════════════════════════
    // ?[ with ?? coalesce
    // ═══════════════════════════════════════════════════════════════

    [Test]
    public void SafeArrayIndex_WithCoalesce_HasValue() =>
        "x:int[]? = [10,20,30]\r y = x?[1] ?? -1".AssertResultHas("y", 20);

    [Test]
    public void SafeArrayIndex_WithCoalesce_None() =>
        "x:int[]? = none\r y = x?[1] ?? -1".AssertResultHas("y", -1);


    // ═══════════════════════════════════════════════════════════════
    // ?. then ?[ chaining
    // ═══════════════════════════════════════════════════════════════

    [Test]
    public void SafeFieldAccess_ThenSafeArrayIndex() =>
        "x = if(true) {items = [10,20,30]} else none\r y:int = (x?.items)?[1] ?? -1"
            .AssertResultHas("y", 20);

    [Test]
    public void SafeFieldAccess_ThenSafeArrayIndex_None() =>
        "x = if(false) {items = [10,20,30]} else none\r y:int = (x?.items)?[1] ?? -1"
            .AssertResultHas("y", -1);

    [Test]
    public void SafeFieldAccess_ThenSafeArrayIndex_NoParens() =>
        "x = if(true) {data = [10,20,30]} else none\r y:int = x?.data?[1] ?? -1"
            .AssertResultHas("y", 20);


    // ═══════════════════════════════════════════════════════════════
    // Chained ?[]?[] on nested optional arrays (Bug #58)
    // ═══════════════════════════════════════════════════════════════

    [Test]
    public void ChainedSafeIndex_NestedOptionalArrays() =>
        "x:int[]?[]? = [[1,2], none, [3]]\r y = (x?[0])?[1]"
            .AssertResultHas("y", 2);

    [Test]
    public void ChainedSafeIndex_NestedOptionalArrays_None() {
        var result = "x:int[]?[]? = [[1,2], none, [3]]\r y = (x?[1])?[0]".Calc();
        Assert.IsNull(result.Get("y"));
    }


    // ═══════════════════════════════════════════════════════════════
    // Bug #92: ?. on optional field + chained ?[]
    // ═══════════════════════════════════════════════════════════════

    [Test]
    public void SafeFieldAccess_OptionalArrayField_ThenSafeIndex() =>
        "x:int[]? = [1,2]\r s = if(true) {arr = x} else none\r y:int = s?.arr?[0] ?? -1"
            .AssertResultHas("y", 1);


    // ═══════════════════════════════════════════════════════════════
    // From OptionalChainingTest: ?[ tests with {field:type} struct syntax
    // ═══════════════════════════════════════════════════════════════

    [Test]
    public void OptionalChaining_ArrayIndex_HasValue() =>
        "x:int[]? = [10,20,30]\r y = x?[0]".AssertResultHas("y", 10);

    [Test]
    public void OptionalChaining_ArrayIndex_Second() =>
        "x:int[]? = [10,20,30]\r y = x?[1]".AssertResultHas("y", 20);

    [Test]
    public void OptionalChaining_ArrayIndex_None() {
        var result = "x:int[]? = none\r y = x?[0]".Calc();
        Assert.IsNull(result.Get("y"));
    }

    [Test]
    public void OptionalChaining_RealArrayIndex_HasValue() =>
        "x:real[]? = [1.1, 2.2]\r y = x?[0]".AssertResultHas("y", 1.1);

    [Test]
    public void OptionalChaining_TextArrayIndex_HasValue() =>
        "x:text[]? = ['hello', 'world']\r y = x?[0]".AssertResultHas("y", "hello");

    [Test]
    public void OptionalChaining_TextArrayIndex_None() {
        var result = "x:text[]? = none\r y = x?[0]".Calc();
        Assert.IsNull(result.Get("y"));
    }

    [Test]
    public void Combo_OptionalArrayIndexCoalesce() =>
        "x:int[]? = [10,20,30]\r y = (x?[1]) ?? -1".AssertResultHas("y", 20);

    [Test]
    public void Combo_OptionalArrayIndexCoalesce_None() =>
        "x:int[]? = none\r y = (x?[1]) ?? -1".AssertResultHas("y", -1);
}
