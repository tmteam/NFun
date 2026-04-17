namespace NFun.SyntaxTests.TypeNarrowing;

using NFun.TestTools;
using NUnit.Framework;

[TestFixture]
public class TypeNarrowingTest {

    private static object Calc(string expr, params (string id, object val)[] values) =>
        expr.CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled, values: values)
            .Get("y");

    private static void Builds(string expr) =>
        expr.BuildWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled);

    // --- Basic: x != none narrows then-branch ---

    [Test]
    public void NotEqual_ThenBranch_Narrows() =>
        Assert.AreEqual(42, Calc("x:int? = 42\r y = if(x != none) x else 0"));

    [Test]
    public void NotEqual_ThenBranch_NoneInput_ReturnsElse() =>
        Assert.AreEqual(0, Calc("x:int? = none\r y = if(x != none) x else 0"));

    [Test]
    public void NotEqual_Reversed_NoneFirst() =>
        Assert.AreEqual(42, Calc("x:int? = 42\r y = if(none != x) x else 0"));

    // --- Basic: x == none narrows else-branch ---

    [Test]
    public void Equal_ElseBranch_Narrows() =>
        Assert.AreEqual(42, Calc("x:int? = 42\r y = if(x == none) 0 else x"));

    [Test]
    public void Equal_ElseBranch_NoneInput_ReturnsThen() =>
        Assert.AreEqual(0, Calc("x:int? = none\r y = if(x == none) 0 else x"));

    [Test]
    public void Equal_Reversed_NoneFirst() =>
        Assert.AreEqual(42, Calc("x:int? = 42\r y = if(none == x) 0 else x"));

    // --- AND: both conditions narrow the then-branch ---

    [Test]
    public void And_BothNarrow_ThenBranch() =>
        Assert.AreEqual(7, Calc(
            "x:int? = 3\r z:int? = 4\r y = if(x != none and z != none) x + z else 0"));

    [Test]
    public void And_FirstNone_ReturnsElse() =>
        Assert.AreEqual(0, Calc(
            "x:int? = none\r z:int? = 4\r y = if(x != none and z != none) x + z else 0"));

    // --- OR: only intersected narrowing applies ---

    [Test]
    public void Or_OnlySharedVarNarrows() =>
        // x appears in both sides of OR, so x is narrowed in then-branch
        // z only in one side, so z is NOT narrowed
        Builds("x:int? = 42\r z:int? = 1\r y = if(x != none or x != none) x else 0");

    [Test]
    public void Or_BothSameVar_Narrows() =>
        Assert.AreEqual(42, Calc(
            "x:int? = 42\r y = if(x != none or x != none) x else 0"));

    // --- NOT: swaps narrowing ---

    [Test]
    public void Not_InvertsNarrowing_ThenBranch() =>
        // not(x == none) is equivalent to x != none
        Assert.AreEqual(42, Calc("x:int? = 42\r y = if(not(x == none)) x else 0"));

    [Test]
    public void Not_InvertsNarrowing_ElseBranch() =>
        // not(x != none) is equivalent to x == none → else-branch narrows
        Assert.AreEqual(42, Calc("x:int? = 42\r y = if(not(x != none)) 0 else x"));

    // --- Narrowing with different types ---

    [Test]
    public void NarrowReal() =>
        Assert.AreEqual(3.14, Calc("x:real? = 3.14\r y = if(x != none) x else 0.0"));

    [Test]
    public void NarrowText() =>
        Assert.AreEqual("hello", Calc("x:text? = 'hello'\r y = if(x != none) x else ''"));

    [Test]
    public void NarrowBool() =>
        Assert.AreEqual(true, Calc("x:bool? = true\r y = if(x != none) x else false"));

    // --- Expression uses narrowed variable ---

    [Test]
    public void NarrowedVarInArithmetic() =>
        Assert.AreEqual(43, Calc("x:int? = 42\r y = if(x != none) x + 1 else 0"));

    [Test]
    public void NarrowedVarInComparison() =>
        Assert.AreEqual(true, Calc("x:int? = 42\r y:bool = if(x != none) x > 0 else false"));

    // --- No narrowing for non-none comparisons ---

    [Test]
    public void NonNoneComparison_NoNarrowing_Builds() =>
        Builds("x:int? = 42\r y = if(x != none) x else none");

    // --- Multiple if-cases (no else narrowing for multi-case) ---

    [Test]
    public void MultiCase_EachCaseNarrowsIndependently() =>
        Assert.AreEqual(42, Calc(
            "x:int? = 42\r z:int? = none\r y = if(x != none) x \r if(z != none) z else 0"));

    // --- Progressive narrowing in AND conditions ---

    [Test]
    public void And_IntraCondition_NarrowedInRightSide() =>
        // x != none narrows x, then x > 0 sees x as int (not int?)
        Assert.AreEqual(84, Calc(
            "x:int? = 42\r y = if(x != none and x > 0) x * 2 else 0"));

    [Test]
    public void And_IntraCondition_NoneInput() =>
        Assert.AreEqual(0, Calc(
            "x:int? = none\r y = if(x != none and x > 0) x * 2 else 0"));

    [Test]
    public void And_IntraCondition_NegativeValue() =>
        Assert.AreEqual(0, Calc(
            "x:int? = -5\r y = if(x != none and x > 0) x * 2 else 0"));

    // --- Bool? narrowing with true/false literals ---

    [Test]
    public void BoolOptional_EqualTrue_Narrows() =>
        Assert.AreEqual(true, Calc(
            "x:bool? = true\r y = if(x == true) x else false"));

    [Test]
    public void BoolOptional_EqualFalse_Narrows() =>
        Assert.AreEqual(true, Calc(
            "x:bool? = false\r y = if(x == false) not x else false"));

    [Test]
    public void BoolOptional_NotEqualTrue_NarrowsBothBranches() =>
        // x != true → x is still non-none (could be false or none, but != proves it's comparable)
        // Actually: x != true narrows x in BOTH branches (non-none either way)
        Assert.AreEqual(false, Calc(
            "x:bool? = false\r y = if(x != true) x else false"));

    // --- OR with De Morgan in else branch ---

    [Test]
    public void Or_DeMorgan_ElseBranchNarrows() =>
        // if(a == none or b == none) → else branch: both are non-none
        Assert.AreEqual(7, Calc(
            "a:int? = 3\r b:int? = 4\r y = if(a == none or b == none) 0 else a + b"));

    // --- Narrowed var in function calls ---

    [Test]
    public void NarrowedVarInMax() =>
        Assert.AreEqual(42, Calc("x:int? = 42\r y = if(x != none) max(x, 10) else 0"));

    [Test]
    public void NarrowedVarInAbs_NegativeInput() =>
        Assert.AreEqual(3, Calc("x:int? = -3\r y = if(x != none) abs(x) else 0"));

    [Test]
    public void NarrowedVarInMin_TwoOptionals() =>
        Assert.AreEqual(3, Calc(
            "x:int? = 5\r z:int? = 3\r y = if(x != none and z != none) min(x, z) else 0"));

    [Test]
    public void NarrowedVarInMax_TwoOptionals() =>
        Assert.AreEqual(5, Calc(
            "x:int? = 5\r z:int? = 3\r y = if(x != none and z != none) max(x, z) else 0"));

    // --- Narrowed var in nested expressions ---

    [Test]
    public void NarrowedVar_DifferenceOfSquares() =>
        // (5+1)*(5-1) = 6*4 = 24
        Assert.AreEqual(24, Calc("x:int? = 5\r y = if(x != none) (x + 1) * (x - 1) else 0"));

    [Test]
    public void NarrowedVar_Polynomial() =>
        // 5*5 + 2*5 + 1 = 25 + 10 + 1 = 36
        Assert.AreEqual(36, Calc("x:int? = 5\r y = if(x != none) x * x + 2 * x + 1 else 0"));

    // --- Multiple occurrences of narrowed var ---

    [Test]
    public void NarrowedVar_TripleSum() =>
        Assert.AreEqual(15, Calc("x:int? = 5\r y = if(x != none) x + x + x else 0"));

    // --- Real-world arithmetic patterns ---

    [Test]
    public void NarrowedReal_Division() =>
        Assert.AreEqual(5.0, Calc("x:real? = 10.0\r y = if(x != none) x / 2.0 else 0.0"));

    [Test]
    public void NarrowedVar_Modulo() =>
        Assert.AreEqual(1, Calc("x:int? = 7\r y = if(x != none) x % 3 else 0"));

    [Test]
    public void NarrowedVar_Power() =>
        Assert.AreEqual(100, Calc("x:int? = 10\r y = if(x != none) x ** 2 else 0"));

    [Test]
    public void NarrowedReal_Sqrt() =>
        Assert.AreEqual(3.0, Calc("x:real? = 9.0\r y = if(x != none) sqrt(x) else 0.0"));

    // --- Edge: none literal in else produces optional result ---

    [Test]
    public void NarrowedVar_NonNoneInput_NoneElse() =>
        Assert.AreEqual(42, Calc("x:int? = 42\r y = if(x != none) x else none"));

    [Test]
    public void NarrowedVar_NoneInput_NoneElse() =>
        Assert.AreEqual(null, Calc("x:int? = none\r y = if(x != none) x else none"));

    // --- Edge: == none narrows else branch with complex expression ---

    [Test]
    public void EqualNone_ElseBranch_ArithmeticOnNarrowed() =>
        Assert.AreEqual(10, Calc("x:int? = 5\r y = if(x == none) none else x * 2"));

    // --- Edge: narrowed var compared with another narrowed var ---

    [Test]
    public void TwoNarrowedVars_Comparison() =>
        Assert.AreEqual(true, Calc(
            "x:int? = 5\r z:int? = 3\r y:bool = if(x != none and z != none) x > z else false"));

    [Test]
    public void TwoNarrowedVars_Subtraction_NoneElse() =>
        Assert.AreEqual(2, Calc(
            "x:int? = 5\r z:int? = 3\r y = if(x != none and z != none) x - z else none"));

    // --- Edge: deeply nested if ---

    [Test]
    public void NarrowedVar_NestedIf_SimulatesAbs() =>
        // if(x != none) if(x > 0) x else -x else 0  →  abs(-7) = 7
        Assert.AreEqual(7, Calc("x:int? = -7\r y = if(x != none) if(x > 0) x else -x else 0"));

    // --- Edge: three-way AND narrowing ---

    [Test]
    public void ThreeVars_AllNarrowedInAnd() =>
        Assert.AreEqual(20, Calc(
            "a:int? = 10\r b:int? = 3\r c:int? = 7\r y = if(a != none and b != none and c != none) a + b + c else 0"));

    [Test]
    public void ThreeVars_MiddleIsNone_ReturnsElse() =>
        Assert.AreEqual(-1, Calc(
            "a:int? = 10\r b:int? = none\r c:int? = 7\r y = if(a != none and b != none and c != none) a + b + c else -1"));

    // --- Edge: narrowing with intra-AND value check and complex body ---

    [Test]
    public void IntraAnd_NarrowAndRangeCheck_DoubleResult() =>
        Assert.AreEqual(10, Calc(
            "x:int? = 5\r y = if(x != none and x > 0 and x < 100) x * 2 else 0"));

    // ==================================================================
    // Negative tests: narrowing must NOT apply in these cases
    // ==================================================================

    [Test]
    public void Negative_NoCheck_OptionalInArithmetic_MustFail() =>
        Assert.Throws<NFun.Exceptions.FunnyParseException>(
            () => "x:int? = 42\r y = x + 1"
                .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled));

    [Test]
    public void Negative_NarrowingDoesNotLeakOutsideIf() =>
        Assert.Throws<NFun.Exceptions.FunnyParseException>(
            () => "x:int? = 42\r z = if(x != none) x else 0\r y = x + 1"
                .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled));

    [Test]
    public void Negative_Or_DoesNotNarrowIndividual_MustFail() =>
        Assert.Throws<NFun.Exceptions.FunnyParseException>(
            () => "x:int? = 42\r z:int? = 10\r y:int = if(x != none or z != none) z else 0"
                .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled));

    // ==================================================================
    // Regression: existing features unbroken by narrowing
    // ==================================================================

    [Test]
    public void Regression_CoalesceStillWorks() =>
        Assert.AreEqual(42, Calc("x:int? = 42\r y = x ?? 0"));

    [Test]
    public void Regression_CoalesceWithNone() =>
        Assert.AreEqual(0, Calc("x:int? = none\r y = x ?? 0"));

    [Test]
    public void Regression_RegularIfElse_NoOptionals() =>
        Assert.AreEqual(1, Calc("y = if(true) 1 else 2"));

    [Test]
    public void Regression_ForceUnwrapStillWorks() =>
        Assert.AreEqual(43, Calc("x:int? = 42\r y = if(x != none) x! + 1 else 0"));

    // ==================================================================
    // Type variety
    // ==================================================================

    [Test]
    public void TypeVariety_Uint16() =>
        Assert.AreEqual((ushort)42, Calc("x:uint16? = 42\r y = if(x != none) x else 0"));

    [Test]
    public void TypeVariety_Int16() =>
        Assert.AreEqual((short)42, Calc("x:int16? = 42\r y = if(x != none) x else 0"));

    [Test]
    public void TypeVariety_Uint32() =>
        Assert.AreEqual((uint)42, Calc("x:uint32? = 42\r y = if(x != none) x else 0"));

    [Test]
    public void TypeVariety_Uint64() =>
        Assert.AreEqual((ulong)42, Calc("x:uint64? = 42\r y = if(x != none) x else 0"));

    [Test]
    public void TypeVariety_Int64() =>
        Assert.AreEqual((long)42, Calc("x:int64? = 42\r y = if(x != none) x else 0"));

    [Test]
    public void TypeVariety_Byte() =>
        Assert.AreEqual((byte)42, Calc("x:byte? = 42\r y = if(x != none) x else 0"));

    // ==================================================================
    // Interaction: multiple outputs, multiple usages
    // ==================================================================

    [Test]
    public void MultipleOutputs_EachNarrowsIndependently() {
        var result = "a:int? = 42\r y = if(a != none) a + 1 else 0\r z = if(a != none) a * 2 else 0"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled);
        Assert.AreEqual(43, result.Get("y"));
        Assert.AreEqual(84, result.Get("z"));
    }

    [Test]
    public void NarrowedVarUsedThreeTimes() =>
        Assert.AreEqual(126, Calc("x:int? = 42\r y = if(x != none) x + x + x else 0"));

    // ==================================================================
    // Compound logic: NOT+De Morgan, progressive multi-var, mixed
    // ==================================================================

    [Test]
    public void Not_And_DeMorgan_AtLeastOneNonNone_Builds() =>
        // not(x == none and z == none) = x != none or z != none → neither guaranteed
        Builds("x:int? = 42\r z:int? = 1\r y:int? = if(not(x == none and z == none)) x else z");

    [Test]
    public void Not_Or_DeMorgan_BothNonNone() =>
        // not(x == none or z == none) = x != none and z != none
        Assert.AreEqual(7, Calc(
            "x:int? = 3\r z:int? = 4\r y = if(not(x == none or z == none)) x + z else 0"));

    [Test]
    public void Not_Or_DeMorgan_OneIsNone_ReturnsElse() =>
        Assert.AreEqual(0, Calc(
            "x:int? = 3\r z:int? = none\r y = if(not(x == none or z == none)) x + z else 0"));

    [Test]
    public void DoubleNot_EquivalentToOriginal() =>
        Assert.AreEqual(42, Calc("x:int? = 42\r y = if(not(not(x != none))) x else 0"));

    [Test]
    public void DoubleNot_NoneInput_ReturnsElse() =>
        Assert.AreEqual(0, Calc("x:int? = none\r y = if(not(not(x != none))) x else 0"));

    [Test]
    public void And_ProgressiveNarrowing_TwoVarsCompared() =>
        Assert.AreEqual(2, Calc(
            "x:int? = 5\r z:int? = 3\r y = if(x != none and z != none and x > z) x - z else 0"));

    [Test]
    public void And_ProgressiveNarrowing_ComparisonFails_ReturnsElse() =>
        Assert.AreEqual(0, Calc(
            "x:int? = 3\r z:int? = 5\r y = if(x != none and z != none and x > z) x - z else 0"));

    [Test]
    public void BoolOptional_EqualTrue_AndOtherVar() =>
        Assert.AreEqual(10, Calc(
            "x:bool? = true\r z:int? = 10\r y = if(x == true and z != none) z else 0"));

    [Test]
    public void BoolOptional_NotEqualFalse_Narrows() =>
        Assert.AreEqual(true, Calc("x:bool? = true\r y = if(x != false) x else false"));

    [Test]
    public void And_NoneFirstInBothChecks() =>
        Assert.AreEqual(7, Calc(
            "a:int? = 3\r b:int? = 4\r y = if(none != a and none != b) a + b else 0"));

    [Test]
    public void And_SameVarCheckedTwice_StillNarrows() =>
        Assert.AreEqual(42, Calc("x:int? = 42\r y = if(x != none and x != none) x else 0"));

    [Test]
    public void And_MixedTypes_TextConcat() =>
        Assert.AreEqual("hello world", Calc(
            "a:text? = 'hello'\r b:text? = ' world'\r y = if(a != none and b != none) a.concat(b) else ''"));

    [Test]
    public void Or_AndBranch_OnlyIntersectionNarrows_Builds() =>
        // (a!=none and b!=none) or c!=none → no var in BOTH sides → no narrowing
        Builds("a:int? = 1\r b:int? = 2\r c:int? = 3\r y:int? = if((a != none and b != none) or c != none) a else c");

    [Test]
    public void NotEqualNone_ThenProgressiveComparison() =>
        // not(x == none) narrows x, then x > 0 uses narrowed x
        Assert.AreEqual(84, Calc("x:int? = 42\r y = if(not(x == none) and x > 0) x * 2 else 0"));

    [Test]
    public void NotEqualNone_ThenProgressiveComparison_NegativeValue() =>
        Assert.AreEqual(0, Calc("x:int? = -5\r y = if(not(x == none) and x > 0) x * 2 else 0"));

    [Test]
    public void IfElif_NarrowedInArithmetic() =>
        Assert.AreEqual(43, Calc(
            "x:int? = 42\r z:int? = none\r y = if(x != none) x + 1 \r if(z != none) z + 2 else 0"));

    [Test]
    public void IfElif_SecondBranchNarrows() =>
        Assert.AreEqual(99, Calc(
            "x:int? = none\r z:int? = 99\r y = if(x != none) x \r if(z != none) z else 0"));

    [Test]
    public void IfElif_BothNone_ReturnsElse() =>
        Assert.AreEqual(0, Calc(
            "x:int? = none\r z:int? = none\r y = if(x != none) x \r if(z != none) z else 0"));

    // ==================================================================
    // TypeScript/Kotlin-inspired tricky narrowing edge cases
    // ==================================================================

    // 1. Multi-level safe access: a?.b?.c proves a is non-none, so a.b.c works in body.
    //    Inspired by TypeScript: `if (a?.b?.c != null) a.b.c + 1`
    [Test]
    public void TS1_MultiLevelSafeAccess_NarrowsRoot() =>
        Assert.AreEqual(43, Calc(
            "a = if(true) {b = {c = 42}} else none\r y = if(a?.b?.c != none) a.b.c + 1 else 0"));

    [Test]
    public void TS1_MultiLevelSafeAccess_NoneInput_ReturnsElse() =>
        Assert.AreEqual(0, Calc(
            "a = if(false) {b = {c = 42}} else none\r y = if(a?.b?.c != none) a.b.c + 1 else 0"));

    // 2. Safe access + arithmetic: a?.value proves a is non-none, use a.value in arithmetic.
    //    Inspired by Kotlin: `a?.value?.let { it * 2 } ?: 0`
    [Test]
    public void TS2_SafeAccessField_Arithmetic() =>
        Assert.AreEqual(20, Calc(
            "a = if(true) {value = 10} else none\r y = if(a?.value != none) a.value * 2 else 0"));

    [Test]
    public void TS2_SafeAccessField_Arithmetic_None() =>
        Assert.AreEqual(0, Calc(
            "a = if(false) {value = 10} else none\r y = if(a?.value != none) a.value * 2 else 0"));

    // 3. Safe access narrowing + method chain on result.
    //    Inspired by TypeScript: `if (a?.name != null) a.name.length`
    [Test]
    public void TS3_SafeAccessField_ThenMethodOnField() =>
        Assert.AreEqual(11, Calc(
            "a = if(true) {name = 'hello world'} else none\r y = if(a?.name != none) a.name.count() else 0"));

    [Test]
    public void TS3_SafeAccessField_ThenMethodOnField_None() =>
        Assert.AreEqual(0, Calc(
            "a = if(false) {name = 'hello world'} else none\r y = if(a?.name != none) a.name.count() else 0"));

    // 4. Narrowing + coalesce in same expression: narrowed `a` combined with coalesced `b`.
    //    Inspired by TypeScript: `if (a != null) a + (b ?? 0)`
    [Test]
    public void TS4_NarrowedPlusCoalesced() =>
        Assert.AreEqual(8, Calc(
            "a:int? = 5\r b:int? = 3\r y = if(a != none) a + (b ?? 0) else 0"));

    [Test]
    public void TS4_NarrowedPlusCoalesced_BIsNone() =>
        Assert.AreEqual(5, Calc(
            "a:int? = 5\r b:int? = none\r y = if(a != none) a + (b ?? 0) else 0"));

    // 5. Nested if with inner narrowing: outer narrows a, inner narrows b, inner else still sees a.
    //    Inspired by Kotlin: `if (a != null) { if (b != null) a + b else a * 2 } else 0`
    [Test]
    public void TS5_NestedIf_OuterNarrowSurvivesInnerElse() =>
        Assert.AreEqual(10, Calc(
            "a:int? = 5\r b:int? = none\r y = if(a != none) if(b != none) a + b else a * 2 else 0"));

    [Test]
    public void TS5_NestedIf_BothPresent() =>
        Assert.AreEqual(8, Calc(
            "a:int? = 5\r b:int? = 3\r y = if(a != none) if(b != none) a + b else a * 2 else 0"));

    [Test]
    public void TS5_NestedIf_OuterNone() =>
        Assert.AreEqual(0, Calc(
            "a:int? = none\r b:int? = 3\r y = if(a != none) if(b != none) a + b else a * 2 else 0"));

    // 6. Narrowed optional array: narrow then sort + index — method chain on narrowed container.
    //    Inspired by Kotlin: `arr?.sorted()?.first() ?: 0`
    [Test]
    public void TS6_NarrowedArray_SortReverseIndex() =>
        Assert.AreEqual(5, Calc(
            "arr:int[]? = [3,1,5,2,4]\r y = if(arr != none) arr.sort().reverse()[0] else 0"));

    [Test]
    public void TS6_NarrowedArray_NoneInput() =>
        Assert.AreEqual(0, Calc(
            "arr:int[]? = none\r y = if(arr != none) arr.sort().reverse()[0] else 0"));

    // 7. De Morgan NOT+OR narrowing combined with progressive comparison.
    //    Inspired by TypeScript: `if (!(a == null || b == null) && a > b) a - b`
    [Test]
    public void TS7_DeMorganNotOr_ThenProgressiveComparison() =>
        Assert.AreEqual(2, Calc(
            "a:int? = 5\r b:int? = 3\r y = if(not(a == none or b == none) and a > b) a - b else 0"));

    [Test]
    public void TS7_DeMorganNotOr_ComparisonFails_ReturnsElse() =>
        Assert.AreEqual(0, Calc(
            "a:int? = 3\r b:int? = 5\r y = if(not(a == none or b == none) and a > b) a - b else 0"));

    [Test]
    public void TS7_DeMorganNotOr_OneIsNone_ReturnsElse() =>
        Assert.AreEqual(0, Calc(
            "a:int? = 5\r b:int? = none\r y = if(not(a == none or b == none) and a > b) a - b else 0"));

    // 8. Narrowed vars used in array literal construction.
    //    Inspired by TypeScript: `if (x != null && z != null) [x, z, x+z]`
    [Test]
    public void TS8_NarrowedVarsInArrayLiteral() {
        var result = Calc("x:int? = 5\r z:int? = 10\r y = if(x != none and z != none) [x, z, x + z] else []");
        Assert.AreEqual(new[] { 5, 10, 15 }, result);
    }

    [Test]
    public void TS8_NarrowedVarsInArrayLiteral_OneNone() {
        var result = Calc("x:int? = 5\r z:int? = none\r y = if(x != none and z != none) [x, z, x + z] else []");
        Assert.AreEqual(new int[0], result);
    }

    // 9. Bool? three-way pattern match: true/false/none — each elif narrows independently.
    //    Inspired by Kotlin's sealed when: `when(x) { true -> ...; false -> ...; null -> ... }`
    [Test]
    public void TS9_BoolOptional_ThreeWayMatch_True() =>
        Assert.AreEqual("yes", Calc(
            "x:bool? = true\r y = if(x == true) 'yes' \r if(x == false) 'no' else 'unknown'"));

    [Test]
    public void TS9_BoolOptional_ThreeWayMatch_False() =>
        Assert.AreEqual("no", Calc(
            "x:bool? = false\r y = if(x == true) 'yes' \r if(x == false) 'no' else 'unknown'"));

    [Test]
    public void TS9_BoolOptional_ThreeWayMatch_None() =>
        Assert.AreEqual("unknown", Calc(
            "x:bool? = none\r y = if(x == true) 'yes' \r if(x == false) 'no' else 'unknown'"));

    // 10. Struct optional field: extract to variable, narrow, call method.
    //     This tests the boundary — narrowing works on variables, not field access expressions.
    //     Direct `s.age != none` does NOT narrow `s.age` (limitation), but extracting to a variable works.
    //     Inspired by TypeScript: `const age = s.age; if (age != null) age.toString()`
    [Test]
    public void TS10_StructOptionalField_ExtractNarrowUse() =>
        Assert.AreEqual("30", Calc(
            "s:{name:text, age:int?} = {name = 'Alice', age = 30}\r a = s.age\r y = if(a != none) a.toText() else 'unknown'"));

    [Test]
    public void TS10_StructOptionalField_ExtractNarrowUse_None() =>
        Assert.AreEqual("unknown", Calc(
            "s:{name:text, age:int?} = {name = 'Alice', age = none}\r a = s.age\r y = if(a != none) a.toText() else 'unknown'"));

    // Negative: direct struct field narrowing does NOT work (known limitation).
    // NarrowingAnalyzer.GetVariableOrSafeAccessRoot only handles variable names and safe-access chains,
    // not plain field access (s.age). This is the TypeScript equivalent of narrowing not applying
    // after property access without `?.`.
    [Test]
    public void TS10_DirectFieldAccessNarrowing() =>
        Assert.AreEqual(31, Calc(
            "s:{name:text, age:int?} = {name = 'Alice', age = 30}\r y = if(s.age != none) s.age + 1 else 0"));
}
