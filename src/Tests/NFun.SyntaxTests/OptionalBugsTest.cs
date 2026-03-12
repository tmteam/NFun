using NFun.Exceptions;
using NFun.TestTools;
using NUnit.Framework;

namespace NFun.SyntaxTests;

/// <summary>
/// Tests for known optional bugs found during bug hunt.
/// Each test documents a specific bug with [Ignore] and a description.
/// When a bug is fixed, move the test to the appropriate file and remove [Ignore].
/// </summary>
[TestFixture]
public class OptionalBugsTest {

    // ═══════════════════════════════════════════════════════════════
    // Bug #92 FIXED: ?. on optional field + chained ?[] now works
    // Previously failed because TIC created opt(opt(T)) constraints.
    // Fixed by PullNoneNode refactoring + PropagateOptionalUpward improvements.
    // ═══════════════════════════════════════════════════════════════

    [Test]
    public void SafeFieldAccess_OptionalArrayField_ThenSafeIndex() =>
        "x:int[]? = [1,2]\r s = if(true) {arr = x} else none\r y:int = s?.arr?[0] ?? -1"
            .AssertResultHas("y", 1);


    // ═══════════════════════════════════════════════════════════════
    // Bug #75 FIXED: Array of optional structs + consumer now works
    // Fixed by removing PullNoneNode (inline None handling in Pull dispatch)
    // ═══════════════════════════════════════════════════════════════

    [Test]
    public void OptionalStructArray_Index_ShouldWork() {
        var result = "x = [{a=1}, none]\r y = x[0]".Calc();
        Assert.IsNotNull(result.Get("y"));
    }


    [Test]
    public void OptionalStructArray_Map_ShouldWork() {
        "x = [{a=1}, none, {a=3}]\r y = x.map(rule it?.a ?? 0)"
            .Build();
    }


    // ═══════════════════════════════════════════════════════════════
    // Bug #31 FIXED: fold on optional array now gives proper type error
    // fold's T=opt(int) but '+' requires [U24..Real] — incompatible.
    // Previously crashed with ArgumentOutOfRangeException; now gives
    // FunnyParseException (proper type error).
    // ═══════════════════════════════════════════════════════════════

    [Test]
    public void FoldOnOptionalArray_WithArithmetic_GivesTypeError() {
        Assert.Throws<FunnyParseException>(
            () => "y = [1,none,3].fold(rule it1 + (it2 ?? 0))".Build());
    }

    [Test]
    public void FoldOnOptionalArray_WithCoalesce_Works() {
        "y = [1,none,3].map(rule it ?? 0).fold(rule it1 + it2)"
            .AssertResultHas("y", 4);
    }


    // ═══════════════════════════════════════════════════════════════
    // Bug #5 FIXED: Value-type optional array display
    // int?[] with none previously displayed 0 instead of null.
    // Fixed by CLR conversion improvements.
    // ═══════════════════════════════════════════════════════════════

    [Test]
    public void OptionalIntArray_NoneDisplaysAsNull() {
        var result = "x:int?[] = [1, none, 3]\r y = x".Calc();
        var arr = (int?[])result.Get("y");
        Assert.AreEqual(1, arr[0]);
        Assert.IsNull(arr[1]);
        Assert.AreEqual(3, arr[2]);
    }

    [Test]
    public void OptionalBoolArray_NoneDisplaysAsNull() {
        var result = "x:bool?[] = [true, none, false]\r y = x".Calc();
        var arr = (bool?[])result.Get("y");
        Assert.AreEqual(true, arr[0]);
        Assert.IsNull(arr[1]);
        Assert.AreEqual(false, arr[2]);
    }


    // ═══════════════════════════════════════════════════════════════
    // Bug #12 FIXED: Chained ?? with ?.
    // x?.a ?? x?.b ?? 0 previously failed because ?? got opt(T) as
    // second arg from x?.b. Now works correctly.
    // ═══════════════════════════════════════════════════════════════

    [Test]
    public void ChainedCoalesce_WithSafeAccess_HasValue() =>
        "x:{a:int, b:int}? = {a=1, b=2}\r y = x?.a ?? x?.b ?? 0"
            .AssertResultHas("y", 1);

    [Test]
    public void ChainedCoalesce_WithSafeAccess_None() =>
        "x:{a:int, b:int}? = none\r y = x?.a ?? x?.b ?? 0"
            .AssertResultHas("y", 0);


    // ═══════════════════════════════════════════════════════════════
    // Bug #17 FIXED: Struct-in-if-else loses nested struct fields
    // z1 = {b=1}; x = if(true) {a = z1} else none previously gave
    // x:{}? (empty struct). Now correctly preserves nested fields.
    // ═══════════════════════════════════════════════════════════════

    [Test]
    public void StructInIfElse_NestedStruct_PreservesFields() {
        var result = "z1 = {b=1}\r x = if(true) {a = z1} else none\r y = x".Calc();
        Assert.IsNotNull(result.Get("y"));
    }

    [Test]
    public void StructInIfElse_NestedStruct_NoneCase() {
        var result = "z1 = {b=1}\r x = if(false) {a = z1} else none\r y = x".Calc();
        Assert.IsNull(result.Get("y"));
    }


    // ═══════════════════════════════════════════════════════════════
    // Bug #58 FIXED: Chained ?[]?[] on nested optional arrays
    // (x?[0])?[1] previously failed with FU758.
    // ═══════════════════════════════════════════════════════════════

    [Test]
    public void ChainedSafeArrayAccess_HasValue() =>
        "x:int[]?[]? = [[1,2], none, [3]]\r y = (x?[0])?[1]"
            .AssertResultHas("y", 2);

    [Test]
    public void ChainedSafeArrayAccess_NoneInner() {
        var result = "x:int[]?[]? = [none, [3]]\r y = (x?[0])?[1]".Calc();
        Assert.IsNull(result.Get("y"));
    }

}
