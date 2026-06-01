namespace NFun.SyntaxTests.OptionalTypes;

using Exceptions;
using TestTools;
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
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.Enabled)
            .AssertResultHas("y", 10);

    // 2. Basic none: int array is none, coalesce to default
    [Test]
    public void IntArray_None_CoalescesToDefault() =>
        "arr:int[]? = none\r y = arr?[0] ?? -1"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.Enabled)
            .AssertResultHas("y", -1);

    // 3. Middle index
    [Test]
    public void IntArray_HasValue_MiddleIndex() =>
        "arr:int[]? = [10,20,30]\r y = arr?[1] ?? -1"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.Enabled)
            .AssertResultHas("y", 20);

    // 4. Last index
    [Test]
    public void IntArray_HasValue_LastIndex() =>
        "arr:int[]? = [10,20,30]\r y = arr?[2] ?? -1"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.Enabled)
            .AssertResultHas("y", 30);

    // 5. Real array
    [Test]
    public void RealArray_HasValue() =>
        "arr:real[]? = [1.5, 2.5]\r y = arr?[0] ?? 0.0"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.Enabled)
            .AssertResultHas("y", 1.5);

    // 6. Text array
    [Test]
    public void TextArray_HasValue() =>
        "arr:text[]? = ['hello', 'world']\r y = arr?[1] ?? ''"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.Enabled)
            .AssertResultHas("y", "world");

    // 7. Bool array
    [Test]
    public void BoolArray_HasValue() =>
        "arr:bool[]? = [true, false]\r y = arr?[0] ?? false"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.Enabled)
            .AssertResultHas("y", true);

    // 8. With force unwrap
    [Test]
    public void IntArray_HasValue_ForceUnwrap() =>
        "arr:int[]? = [42]\r y = arr?[0]!"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.Enabled)
            .AssertResultHas("y", 42);

    // 9. Chained with safe field access: struct?.field?[i]
    [Test]
    public void ChainedSafeFieldAccess_ThenSafeArrayAccess_HasValue() =>
        "s = if(true) {items = [1,2,3]} else none\r y = s?.items?[0] ?? -1"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.Enabled)
            .AssertResultHas("y", 1);

    // 10. Result type is optional when no coalesce
    [Test]
    public void IntArray_HasValue_ResultIsOptional() {
        var result = "arr:int[]? = [10]\r y = arr?[0]"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.Enabled);
        // y should be int? = 10, which converts to boxed int (not null)
        Assert.AreEqual(10, result.Get("y"));
    }

    // 11. None result without coalesce
    [Test]
    public void IntArray_None_ResultIsNone() {
        var result = "arr:int[]? = none\r y = arr?[0]"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.Enabled);
        Assert.IsNull(result.Get("y"));
    }

    // 12. Array of optionals: safe access on int?[]?, none element coalesces
    [Test]
    public void ArrayOfOptionals_NoneElement_Coalesces() =>
        "arr:int?[]? = [1, none, 3]\r y = arr?[1] ?? -1"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.Enabled)
            .AssertResultHas("y", -1);

    // 13. Used in arithmetic with coalesce
    [Test]
    public void SafeAccess_InArithmeticWithCoalesce() =>
        "arr:int[]? = [10]\r y = (arr?[0] ?? 0) + 5"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.Enabled)
            .AssertResultHas("y", 15);

    // 14. Used in if condition
    [Test]
    public void SafeAccess_InIfCondition_HasValue() =>
        "arr:int[]? = [42]\r y = if(arr?[0] != none) arr?[0]! else 0"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.Enabled)
            .AssertResultHas("y", 42);

    // 15. Multiple safe accesses from different arrays
    [Test]
    public void MultipleSafeAccesses_BothHaveValue() =>
        "a:int[]? = [1]\r b:int[]? = [2]\r y = (a?[0] ?? 0) + (b?[0] ?? 0)"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.Enabled)
            .AssertResultHas("y", 3);

    // 16. Variable index
    [Test]
    public void SafeAccess_VariableIndex() =>
        "arr:int[]? = [10,20,30]\r i:int = 1\r y = arr?[i] ?? -1"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.Enabled)
            .AssertResultHas("y", 20);

    // 17. ?[] on non-optional array — should still work (lifts result to optional)
    [Test]
    public void NonOptionalArray_SafeAccess_ReturnsOptional() {
        var result = "arr:int[] = [1,2,3]\r y = arr?[0]"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.Enabled);
        // arr is not optional, but ?[] returns T? anyway
        Assert.AreEqual(1, result.Get("y"));
    }

    // 18. Chained: struct is none, safe field + safe array access returns none
    [Test]
    public void ChainedSafeFieldAccess_StructIsNone_CoalescesToDefault() =>
        "s = if(false) {items = [1,2,3]} else none\r y = s?.items?[0] ?? -1"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.Enabled)
            .AssertResultHas("y", -1);

    // 19. Safe access then method on coalesced text
    [Test]
    public void SafeAccess_TextArray_CoalesceThenCount_HasValue() =>
        "arr:text[]? = ['hello']\r y = (arr?[0] ?? '').count()"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.Enabled)
            .AssertResultHas("y", 5);

    // 20. Nested array: int[][]? — safe access returns int[]?
    [Test]
    public void NestedArray_SafeAccess_ReturnsOptionalInnerArray() {
        var result = "arr:int[][]? = [[1,2],[3,4]]\r y = arr?[0]"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.Enabled);
        var inner = (int[])result.Get("y");
        Assert.AreEqual(new[] { 1, 2 }, inner);
    }

    // ═══════════════════════════════════════════════════════════════
    // Safe array access: bounds and type variations
    // ═══════════════════════════════════════════════════════════════

    [Test]
    public void SafeArrayAccess_ValidIndex_ReturnsValue() =>
        "arr = [10,20,30]\r out:int = arr?[0] ?? -1"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.Enabled)
            .AssertResultHas("out", 10);

    [Test]
    public void SafeArrayAccess_OutOfBounds_ReturnsNone_CoalescesToZero() =>
        "arr = [10,20,30]\r out:int = arr?[99] ?? 0"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.Enabled)
            .AssertResultHas("out", 0);

    [Test]
    public void SafeArrayAccess_NegativeIndex_ReturnsNone() =>
        "arr = [10,20,30]\r out:int = arr?[-1] ?? -99"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.Enabled)
            .AssertResultHas("out", -99);

    [Test]
    public void SafeArrayAccess_NoneArray_ReturnsNone() {
        var result = "arr:int[]? = none\r out = arr?[0]"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.Enabled);
        Assert.IsNull(result.Get("out"));
    }

    [Test]
    public void SafeArrayAccess_LastElement_CoalescesToDefault() =>
        "arr = [10,20,30]\r out:int = arr?[2] ?? -1"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.Enabled)
            .AssertResultHas("out", 30);

    [Test]
    public void SafeArrayAccess_RealArray_ValidIndex() =>
        "arr:real[]? = [1.5, 2.5, 3.5]\r out = arr?[1] ?? 0.0"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.Enabled)
            .AssertResultHas("out", 2.5);

    [Test]
    public void SafeArrayAccess_TextArray_OutOfBounds() =>
        "arr:text[]? = ['hello', 'world']\r out = arr?[5] ?? 'empty'"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.Enabled)
            .AssertResultHas("out", "empty");

    [Test]
    public void SafeArrayAccess_ChainedWithSafeFieldAccess_HasValue() =>
        "s = if(true) {items = [1,2,3]} else none\r out = s?.items?[1] ?? -1"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.Enabled)
            .AssertResultHas("out", 2);

    [Test]
    public void SafeArrayAccess_ChainedWithSafeFieldAccess_NoneStruct() =>
        "s = if(false) {items = [1,2,3]} else none\r out = s?.items?[0] ?? -1"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.Enabled)
            .AssertResultHas("out", -1);

    [Test]
    public void SafeArrayAccess_InArithmeticExpression() =>
        "arr = [10,20,30]\r out:int = (arr?[0] ?? 0) + (arr?[1] ?? 0)"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.Enabled)
            .AssertResultHas("out", 30);

    [Test]
    public void SafeArrayAccessOnNonOptVar_CoalesceTypeCorrect() {
        // a?[0] ?? 0 should be Int32, not Int32?
        "a = [1,2,3]; z = a?[0] ?? 0"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.Enabled)
            .AssertResultHas("z", 1);
    }

    [Test]
    public void ChainedSafeArrayAccess_NoError() {
        Assert.DoesNotThrow(() =>
            "a = [1]; b = [2]; y = a?[0] ?? b?[0] ?? 0"
                .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.Enabled));
    }

    // ───────────────────────────────────────────────────────────────
    // MR6Bug2 — Safe-array-access `?[` on an opt array of composite
    //   element type loses the Optional propagation through subsequent
    //   operations:
    //
    //     arr:int[][]? = none
    //     out = arr?[0].count()
    //
    //   Compiles successfully (treating `arr?[0]` as non-opt `int[]`),
    //   then crashes at runtime: "Unable to cast FunnyNone to IFunnyArray".
    //
    //   Direct equivalent IS rejected at compile time:
    //     b:int[]? = none; out = b.count()        # FU783
    //
    //   Bug specific to `?[` on opt-array-of-composite (lost Optional
    //   through nested composite). Doesn't manifest for primitive
    //   element types (`int[]?` → `?[0]` correctly returns `int?`).
    // ───────────────────────────────────────────────────────────────
    [Test]
    public void MR6Bug2_SafeArrayAccessLosesOptThroughComposite_Crash() {
        Assert.Throws<FunnyParseException>(() =>
            "arr:int[][]? = none\rout = arr?[0].count()"
                .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.Enabled));
    }

    // ===============================================================
    // MR6Bug2 BOUNDARY PROBES — `?[` on opt-array, behavior by element shape.
    //
    // Hypothesis (Professor preliminary): SetSafeArrayAccess (GraphBuilderExtensions:254)
    // uses an LCA-with-None pattern (elemNode→result, None→result) instead of
    // directly wrapping `result = StateOptional.Of(elemNode)` like SetSafeFieldAccess /
    // SetSafeMethodCall do. For primitive elem the LCA resolves correctly (opt(int)),
    // but for composite elem (arr/struct/inner-fn) the optional layer is lost — the
    // result is treated as the bare composite, allowing later operations to crash on
    // None at runtime. The expected fix is to drop the LCA pattern in favor of direct
    // `result.State = StateOptional.Of(elemNode)` (mirroring the field/method paths).
    //
    // After the fix:
    //   • Primitive-elem probes still pass (already correct).
    //   • Composite-elem probes that today compile-then-crash should be rejected at
    //     COMPILE time with FU783 — matching the directly-declared `b:int[]? = none;
    //     out = b.count()` rejection.
    //   • Workarounds (`?.`, `??`) continue to work.
    //   • Struct-elem `arr?[0].v` is already FU761 today (stricter form of the same
    //     check applied earlier in struct field path) — kept here as control.
    // ===============================================================

    // 2-1a. Primitive elem control: `arr?[0]` is correctly opt(int) — value is none. PASSES on master.
    [Test]
    public void MR6Bug2_Boundary_PrimitiveElem_Works() {
        var rt = "arr:int[]? = none\rout = arr?[0]"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.Enabled);
        Assert.IsNull(rt.Get("out"));
    }

    // 2-1b. Primitive elem control: arithmetic on opt-result correctly rejected (FU767). PASSES on master.
    [Test]
    public void MR6Bug2_Boundary_PrimitiveElem_ArithmeticRejected_FU767() {
        Assert.Throws<FunnyParseException>(() =>
            "arr:int[]? = none\rout = arr?[0] + 1"
                .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.Enabled));
    }

    // 2-2a. Array-elem variation: `.sum()` (different fn than `.count()`).
    //   Today: compiles → runtime FunnyNone → IFunnyArray cast crash.
    //   After fix: should be FunnyParseException (FU783) at compile.
    [Test]
    public void MR6Bug2_Boundary_ArrayElem_Sum_ShouldCompileReject() {
        Assert.Throws<FunnyParseException>(() =>
            "arr:int[][]? = none\rout = arr?[0].sum()"
                .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.Enabled));
    }

    // 2-2b. Array-elem variation: `.first()`.
    //   Today: compiles → runtime cast crash.
    //   After fix: FU783 at compile.
    [Test]
    public void MR6Bug2_Boundary_ArrayElem_First_ShouldCompileReject() {
        Assert.Throws<FunnyParseException>(() =>
            "arr:int[][]? = none\rout = arr?[0].first()"
                .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.Enabled));
    }

    // 2-2c. Array-elem variation: nested indexing `arr?[0][0]`.
    //   Today: compiles → runtime cast crash.
    //   After fix: should be rejected at compile (treating outer `arr?[0]` as opt(int[]),
    //   inner `[0]` cannot index an optional).
    [Test]
    public void MR6Bug2_Boundary_ArrayElem_ChainedIndex_ShouldCompileReject() {
        Assert.Throws<FunnyParseException>(() =>
            "arr:int[][]? = none\rout = arr?[0][0]"
                .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.Enabled));
    }

    // 2-2d. Array-elem variation: slicing `arr?[0][:2]`.
    //   Today: compiles → runtime cast crash.
    //   After fix: FU783 (slicing an optional is not legal).
    [Test]
    public void MR6Bug2_Boundary_ArrayElem_Slice_ShouldCompileReject() {
        Assert.Throws<FunnyParseException>(() =>
            "arr:int[][]? = none\rout = arr?[0][:2]"
                .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.Enabled));
    }

    // 2-3. 3-deep nested array: does the bug compound?
    //   Today: `arr?[0]` returns int[][]? (bug shifts inward) but `.first()` proceeds —
    //   we get the same kind of runtime cast crash.
    //   After fix: still rejected at compile because the optional propagation should
    //   keep `arr?[0]` as opt(int[][]) and `.first()` on opt(arr) is illegal.
    [Test]
    public void MR6Bug2_Boundary_3DeepArray_BugCompounds_ShouldCompileReject() {
        Assert.Throws<FunnyParseException>(() =>
            "arr:int[][][]? = none\rout = arr?[0].first().count()"
                .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.Enabled));
    }

    // 2-4a. Workaround: full `?.` chain — PASSES on master.
    //   `arr?[0]?.count()` propagates optional via the `?.` operator,
    //   producing opt(int) for the result. None input → none output.
    [Test]
    public void MR6Bug2_Boundary_Workaround_SafeMethodChain_Works() {
        var rt = "arr:int[][]? = none\rout = arr?[0]?.count()"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.Enabled);
        Assert.IsNull(rt.Get("out"));
    }

    // 2-4b. Workaround: `?.` chain + `??` default — PASSES on master.
    [Test]
    public void MR6Bug2_Boundary_Workaround_SafeMethodChainWithDefault_Works() {
        "arr:int[][]? = none\rout = arr?[0]?.count() ?? -1"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.Enabled)
            .AssertResultHas("out", -1);
    }

    // 2-4c. Workaround: explicit-default via `?? []` — PASSES on master.
    //   `arr?[0] ?? []` forces optional resolution to bare int[] (empty array).
    [Test]
    public void MR6Bug2_Boundary_Workaround_ExplicitDefaultArray_Works() {
        "arr:int[][]? = none\rout = (arr?[0] ?? []).count()"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.Enabled)
            .AssertResultHas("out", 0);
    }

    // 2-5. Struct-elem control: already rejected at compile with FU761 today.
    //   This is the "correct" baseline — opt-array of struct correctly fails
    //   `arr?[0].v` because the struct field access can't traverse opt.
    [Test]
    public void MR6Bug2_Boundary_StructElem_AlreadyRejected() {
        Assert.Throws<FunnyParseException>(() =>
            "arr:{v:int}[]? = none\rout = arr?[0].v"
                .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.Enabled));
    }

    // 2-6. Direct equivalent control: declared `int[]?` then call `.count()` is FU783.
    //   This is the comparator — the `?[]` form should reach the same outcome.
    [Test]
    public void MR6Bug2_Boundary_DirectOptArray_FU783_Control() {
        Assert.Throws<FunnyParseException>(() =>
            "b:int[]? = none\rout = b.count()"
                .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.Enabled));
    }
}
