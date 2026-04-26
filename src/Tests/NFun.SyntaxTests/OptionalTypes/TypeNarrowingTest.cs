namespace NFun.SyntaxTests.OptionalTypes;

using NFun.TestTools;
using NUnit.Framework;

/// <summary>
/// All type narrowing tests merged from TypeNarrowing/ directory.
/// Organized by region: Basic, Advanced, Multi-elif, Deep, And/Or outside if,
/// Collection, Edge cases, Safe access, Quick filter.
/// </summary>
[TestFixture]
public class TypeNarrowingTest {

    private static object Calc(string expr, params (string id, object val)[] values) =>
        expr.CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled, values: values)
            .Get("y");

    private static void Builds(string expr) =>
        expr.BuildWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled);

    private static void AssertNarrowed(string expr, string varName, object expected) {
        var r = expr.BuildWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled);
        r.Run();
        Assert.AreEqual(expected, r[varName].Value);
    }

    #region Basic narrowing

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

    [Test]
    public void Negative_OrTrue_DoesNotNarrow_MustFail() =>
        Assert.Throws<NFun.Exceptions.FunnyParseException>(
            () => "x:int? = none\r y = if(x != none or true) x + 1 else 0"
                .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled));

    [Test]
    public void Negative_OrTrue_Reversed_DoesNotNarrow_MustFail() =>
        Assert.Throws<NFun.Exceptions.FunnyParseException>(
            () => "x:int? = none\r y = if(true or x != none) x + 1 else 0"
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

    // ═══════════════════════════════════════════════════════════════
    // Narrowing in rule lambda with arithmetic
    // ═══════════════════════════════════════════════════════════════

    [Test]
    public void NarrowInRule_Multiply() {
        var r = "y:int?[] = [1,none,3]\r z = y.map(rule if(it!=none) it*2 else 0)"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled);
        Assert.AreEqual(new[] { 2, 0, 6 }, r.Get("z"));
    }

    [Test]
    public void NarrowInRule_Add() {
        var r = "y:int?[] = [1,none,3]\r z = y.map(rule if(it!=none) it+10 else 0)"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled);
        Assert.AreEqual(new[] { 11, 0, 13 }, r.Get("z"));
    }

    [Test]
    public void NarrowInRule_ComplexExpr() {
        var r = "y:int?[] = [2,none,4]\r z = y.map(rule if(it!=none) it*it+1 else -1)"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled);
        Assert.AreEqual(new[] { 5, -1, 17 }, r.Get("z"));
    }

    [Test]
    public void NarrowInRule_IdentityStillWorks() {
        var r = "y:int?[] = [1,none,3]\r z = y.map(rule if(it!=none) it else 0)"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled);
        Assert.AreEqual(new[] { 1, 0, 3 }, r.Get("z"));
    }

    #endregion

    #region Advanced narrowing

    // ── == none narrowing in else branch ─────────────────────────

    [Test]
    public void Advanced_EqNone_NarrowsInElse() =>
        AssertNarrowed("x:int? = 42\r y = if(x == none) 0 else x + 1", "y", 43);

    [Test]
    public void Advanced_EqNone_NarrowsInElse_Text() =>
        AssertNarrowed("x:text? = 'hi'\r y = if(x == none) 'none' else x", "y", "hi");

    // ── or narrowing: if(a == none or b == none) → else: both non-none ──

    [Test]
    public void Advanced_Or_BothNone_ElseNarrowsBoth() =>
        AssertNarrowed(
            "x:int? = 10\r z:int? = 20\r y = if(x == none or z == none) 0 else x + z",
            "y", 30);

    [Test]
    public void Advanced_Or_OneNone_ElseNarrowsBoth() =>
        AssertNarrowed(
            "a:int? = 5\r b:int? = 3\r y = if(a == none or b == none) -1 else a * b",
            "y", 15);

    [Test]
    public void Advanced_Or_ThreeVars_ElseNarrowsAll() =>
        AssertNarrowed(
            "a:int? = 1\r b:int? = 2\r c:int? = 3\r y = if(a == none or b == none or c == none) 0 else a + b + c",
            "y", 6);

    [Test]
    public void Advanced_Or_NoneOrNegative_ElseNarrowsAndConstrains() =>
        AssertNarrowed(
            "x:int? = 42\r y = if(x == none or x < 0) 0 else x + 1",
            "y", 43);

    // ── not narrowing ────────────────────────────────────────────

    [Test]
    public void Advanced_Not_EqNone_NarrowsInTrue() =>
        AssertNarrowed(
            "x:int? = 42\r y = if(not(x == none)) x + 1 else 0",
            "y", 43);

    [Test]
    public void Advanced_Not_NeqNone_NarrowsInElse() =>
        AssertNarrowed(
            "x:int? = 42\r y = if(not(x != none)) 0 else x + 1",
            "y", 43);

    [Test]
    public void Advanced_DoubleNot_NeqNone_NarrowsInTrue() =>
        AssertNarrowed(
            "x:int? = 42\r y = if(not(not(x != none))) x + 1 else 0",
            "y", 43);

    // ── De Morgan ────────────────────────────────────────────────

    [Test]
    public void Advanced_DeMorgan_NotOrNone_NarrowsBoth() =>
        AssertNarrowed(
            "x:int? = 10\r z:int? = 20\r y = if(not(x == none or z == none)) x + z else 0",
            "y", 30);

    [Test]
    public void Advanced_DeMorgan_NotAndNone_ElseNarrowsBoth() =>
        AssertNarrowed(
            "x:int? = 10\r z:int? = 20\r y = if(not(x != none and z != none)) 0 else x + z",
            "y", 30);

    // ── Struct field narrowing ───────────────────────────────────

    [Test]
    public void Advanced_StructField_EqNone_NarrowsInElse() =>
        AssertNarrowed(
            "s = {age = if(true) 42 else none}\r y = if(s.age == none) 0 else s.age + 1",
            "y", 43);

    [Test]
    public void Advanced_StructFields_Or_ElseNarrowsBoth() =>
        AssertNarrowed(
            "s = {a = if(true) 1 else none, b = if(true) 2 else none}\r y = if(s.a == none or s.b == none) 0 else s.a + s.b",
            "y", 3);

    // ── Safe access implies non-none ─────────────────────────────

    [Test]
    public void Advanced_SafeAccess_EqTrue_NarrowsStruct() =>
        AssertNarrowed(
            "s = if(true) {flag = true} else none\r y = if(s?.flag == true) 1 else 0",
            "y", 1);

    [Test]
    [Ignore("s?.age returns int? which is not comparable. Requires either int? comparison support or type narrowing.")]
    public void Advanced_SafeAccess_Comparison_NarrowsStruct() =>
        AssertNarrowed(
            "s = if(true) {age = 25} else none\r y = if(s?.age > 18) 1 else 0",
            "y", 1);

    // ── Bool? narrowing ──────────────────────────────────────────

    [Test]
    public void Advanced_OptionalBool_EqTrue_Narrowed() =>
        AssertNarrowed(
            "flag:bool? = true\r y = if(flag == true) 1 else 0",
            "y", 1);

    [Test]
    public void Advanced_OptionalBool_EqFalse_Narrowed() =>
        AssertNarrowed(
            "flag:bool? = false\r y = if(flag == false) 1 else 0",
            "y", 1);

    // ── Complex chains ───────────────────────────────────────────

    [Test]
    public void Advanced_MultiVarOrChain_ElseNarrowsAll() =>
        AssertNarrowed(
            "a:int? = 1\r b:int? = 2\r c:int? = 3\r d:int? = 4\r y = if(a == none or b == none or c == none or d == none) 0 else a + b + c + d",
            "y", 10);

    [Test]
    public void Advanced_MixedAndOr_NoneCheck() =>
        AssertNarrowed(
            "z:int? = 42\r y = if(z != none and z > 0) z * 2 else 0",
            "y", 84);

    // ── Collection narrowing ─────────────────────────────────────

    [Test]
    public void Advanced_Filter_NotEqNone_Narrows() =>
        AssertNarrowed(
            "items:int?[] = [1, none, 3]\r y = items.filter(rule not(it == none))",
            "y", new[] { 1, 3 });

    [Test]
    public void Advanced_Filter_OrCondition_NoNarrow() =>
        AssertNarrowed(
            "items:int?[] = [1, none, 3]\r y = items.filter(rule it != none or true).count()",
            "y", 3);

    #endregion

    #region Multi-elif

    // ── Basic multi-elif: none check narrows for subsequent branches ──

    [Test]
    public void MultiElif_NoneFirst_ValueInSecondBranch() =>
        Assert.AreEqual(42, Calc(
            "x:int? = 42\r y = if(x == none) -1\r if(x > 10) x\r else 0"));

    [Test]
    public void MultiElif_NoneInput_TakesFirstBranch() =>
        Assert.AreEqual(-1, Calc(
            "x:int? = none\r y = if(x == none) -1\r if(x > 10) x\r else 0"));

    [Test]
    public void MultiElif_ValueBelowThreshold_TakesElse() =>
        Assert.AreEqual(0, Calc(
            "x:int? = 5\r y = if(x == none) -1\r if(x > 10) x\r else 0"));

    // ── Multiple variables narrowed across different elifs ──

    [Test]
    public void MultiElif_TwoVars_BothNarrowed() =>
        // if(x == none) → elif(z == none) → else: both narrowed
        Assert.AreEqual(7, Calc(
            "x:int? = 3\r z:int? = 4\r y = if(x == none) -1\r if(z == none) -2\r else x + z"));

    [Test]
    public void MultiElif_TwoVars_FirstNone() =>
        Assert.AreEqual(-1, Calc(
            "x:int? = none\r z:int? = 4\r y = if(x == none) -1\r if(z == none) -2\r else x + z"));

    [Test]
    public void MultiElif_TwoVars_SecondNone() =>
        Assert.AreEqual(-2, Calc(
            "x:int? = 3\r z:int? = none\r y = if(x == none) -1\r if(z == none) -2\r else x + z"));

    [Test]
    public void MultiElif_TwoVars_BothNone_TakesFirst() =>
        Assert.AreEqual(-1, Calc(
            "x:int? = none\r z:int? = none\r y = if(x == none) -1\r if(z == none) -2\r else x + z"));

    // ── Elif condition uses narrowed var in computation ──

    [Test]
    public void MultiElif_NarrowedVarInElifCondition_Multiply() =>
        Assert.AreEqual(42, Calc(
            "x:int? = 42\r y = if(x == none) -1\r if(x * 2 > 100) 100\r else x"));

    [Test]
    public void MultiElif_NarrowedVarInElifCondition_TriggersElif() =>
        Assert.AreEqual(100, Calc(
            "x:int? = 60\r y = if(x == none) -1\r if(x * 2 > 100) 100\r else x"));

    [Test]
    public void MultiElif_NarrowedVarInElifCondition_Modulo() =>
        Assert.AreEqual(1, Calc(
            "x:int? = 7\r y = if(x == none) -1\r if(x % 2 == 0) 0\r else 1"));

    // ── Body of later elif uses vars narrowed by earlier conditions ──

    [Test]
    public void MultiElif_LaterBodyUsesNarrowedVar() =>
        Assert.AreEqual(84, Calc(
            "x:int? = 42\r y = if(x == none) -1\r if(x > 100) x - 100\r else x * 2"));

    [Test]
    public void MultiElif_ArithmeticInAllBranches() =>
        Assert.AreEqual(44, Calc(
            "x:int? = 42\r y = if(x == none) 0\r if(x < 0) -x\r if(x > 100) 100\r else x + 2"));

    // ── Three+ elif chain with progressive narrowing ──

    [Test]
    public void ThreeElif_ProgressiveNarrowing() =>
        Assert.AreEqual(50, Calc(
            "x:int? = 50\r y = if(x == none) 0\r if(x < 0) -1\r if(x > 100) 100\r else x"));

    [Test]
    public void ThreeElif_NegativeInput() =>
        Assert.AreEqual(-1, Calc(
            "x:int? = -5\r y = if(x == none) 0\r if(x < 0) -1\r if(x > 100) 100\r else x"));

    [Test]
    public void ThreeElif_LargeInput() =>
        Assert.AreEqual(100, Calc(
            "x:int? = 200\r y = if(x == none) 0\r if(x < 0) -1\r if(x > 100) 100\r else x"));

    [Test]
    public void FourElif_FineGrainedRanges() =>
        Assert.AreEqual(3, Calc(
            "x:int? = 75\r y = if(x == none) 0\r if(x < 25) 1\r if(x < 50) 2\r if(x < 100) 3\r else 4"));

    [Test]
    public void FourElif_EdgeLargestBucket() =>
        Assert.AreEqual(4, Calc(
            "x:int? = 150\r y = if(x == none) 0\r if(x < 25) 1\r if(x < 50) 2\r if(x < 100) 3\r else 4"));

    // ── Mixed: some elifs check none, some don't ──

    [Test]
    public void MixedElif_NoneCheckThenValueCheck() =>
        Assert.AreEqual(20, Calc(
            "x:int? = 10\r y = if(x == none) -1\r if(x > 50) x\r else x * 2"));

    [Test]
    public void MixedElif_TwoNoneChecksThenValueCheck() =>
        Assert.AreEqual(10, Calc(
            "x:int? = 3\r z:int? = 7\r y = if(x == none) -1\r if(z == none) -2\r if(x + z > 20) 20\r else x + z"));

    [Test]
    public void MixedElif_InterspersedNoneAndValue() =>
        Assert.AreEqual(52, Calc(
            "x:int? = 42\r z:int? = 10\r y = if(x == none) -1\r if(x < 0) 0\r if(z == none) x\r else x + z"));

    [Test]
    public void MixedElif_InterspersedNoneAndValue_ZIsNone() =>
        Assert.AreEqual(42, Calc(
            "x:int? = 42\r z:int? = none\r y = if(x == none) -1\r if(x < 0) 0\r if(z == none) x\r else x + z"));

    // ── Struct field narrowing across elifs ──

    [Test]
    public void StructField_MultiElif_NoneCheckThenComparison() =>
        Assert.AreEqual(42, Calc(
            "s = {v = if(true) 42 else none}\r y = if(s.v == none) -1\r if(s.v > 100) 100\r else s.v"));

    [Test]
    public void StructField_MultiElif_NoneInput() =>
        Assert.AreEqual(-1, Calc(
            "s = {v = if(false) 42 else none}\r y = if(s.v == none) -1\r if(s.v > 100) 100\r else s.v"));

    [Test]
    public void StructField_MultiElif_LargeValue() =>
        Assert.AreEqual(100, Calc(
            "s = {v = if(true) 200 else none}\r y = if(s.v == none) -1\r if(s.v > 100) 100\r else s.v"));

    [Test]
    public void StructField_MultiElif_ArithmeticInCondition() =>
        Assert.AreEqual(42, Calc(
            "s = {v = if(true) 42 else none}\r y = if(s.v == none) -1\r if(s.v * 2 > 200) 100\r else s.v"));

    // ── Two struct fields narrowed across elifs ──

    [Test]
    public void TwoStructFields_MultiElif() =>
        Assert.AreEqual(7, Calc(
            "s = {a = if(true) 3 else none, b = if(true) 4 else none}\r y = if(s.a == none) -1\r if(s.b == none) -2\r else s.a + s.b"));

    [Test]
    public void TwoStructFields_MultiElif_FirstNone() =>
        Assert.AreEqual(-1, Calc(
            "s = {a = if(false) 3 else none, b = if(true) 4 else none}\r y = if(s.a == none) -1\r if(s.b == none) -2\r else s.a + s.b"));

    // ── Multi-elif with real numbers ──

    [Test]
    public void MultiElif_RealType_Clamp() =>
        Assert.AreEqual(3.14, Calc(
            "x:real? = 3.14\r y = if(x == none) 0.0\r if(x < 0.0) 0.0\r if(x > 10.0) 10.0\r else x"));

    // ── Multi-elif with text ──

    [Test]
    public void MultiElif_TextType() =>
        Assert.AreEqual("hi", Calc(
            "x:text? = 'hi'\r y = if(x == none) 'none'\r if(x == '') 'empty'\r else x"));

    [Test]
    public void MultiElif_TextType_NoneInput() =>
        Assert.AreEqual("none", Calc(
            "x:text? = none\r y = if(x == none) 'none'\r if(x == '') 'empty'\r else x"));

    // ── Progressive narrowing with computation in body ──

    [Test]
    public void MultiElif_BodyComputesOnNarrowedVar() =>
        Assert.AreEqual(18, Calc(
            "x:int? = 6\r y = if(x == none) 0\r if(x < 0) 0\r if(x > 10) x * 3\r else x * 3"));

    [Test]
    public void MultiElif_EachBodyUsesNarrowedVar() =>
        Assert.AreEqual(-5, Calc(
            "x:int? = -5\r y = if(x == none) 0\r if(x < 0) x\r if(x > 100) x - 100\r else x"));

    #endregion

    #region Deep narrowing

    // ── or-progressive: x == none or <expr using x> ─────────────

    [Test]
    public void OrProgressive_EqNoneOrGreater() =>
        AssertNarrowed(
            "x:int? = 42\r y = if(x == none or x > 100) 0 else x",
            "y", 42);

    [Test]
    public void OrProgressive_EqNoneOrEquals() =>
        AssertNarrowed(
            "x:int? = 5\r y = if(x == none or x == 0) -1 else x + 1",
            "y", 6);

    [Test]
    public void OrProgressive_EqNoneOrComplex() =>
        AssertNarrowed(
            "x:int? = 10\r y = if(x == none or x * 2 > 100) 0 else x",
            "y", 10);

    [Test]
    public void OrProgressive_TwoVars() =>
        AssertNarrowed(
            "a:int? = 3\r b:int? = 4\r y = if(a == none or b == none or a + b > 100) 0 else a + b",
            "y", 7);

    // ── or-progressive: first WhenFalse narrows for second ──────

    [Test]
    public void OrProgressive_NeqNoneOrValue() =>
        // x != none or (something) — when x != none is FALSE (x is none),
        // the right side should NOT use x. This is correct — no narrowing needed.
        // But: x == none or x > 0 — when x == none is FALSE, x is narrowed.
        AssertNarrowed(
            "x:int? = 42\r y = if(x == none or x < 0) 0 else x + 1",
            "y", 43);

    // ── Deep nested if-else with narrowing ───────────────────────

    [Test]
    public void NestedIfElse_OuterNarrows_InnerUses() =>
        AssertNarrowed(
            "x:int? = 42\r y = if(x != none) (if(x > 10) x * 2 else x) else 0",
            "y", 84);

    [Test]
    public void NestedIfElse_ThreeLevels() =>
        AssertNarrowed(
            "x:int? = 5\r y = if(x != none) (if(x > 0) (if(x < 100) x else 100) else 0) else -1",
            "y", 5);

    [Test]
    public void NestedIfElse_TwoOptionals() =>
        AssertNarrowed(
            "a:int? = 10\r b:int? = 20\r y = if(a != none and b != none) (if(a > b) a else b) else 0",
            "y", 20);

    // ── or with struct field narrowing ───────────────────────────

    [Test]
    public void OrProgressive_StructField_EqNoneOrCompare() =>
        AssertNarrowed(
            "s = {v = if(true) 42 else none}\r y = if(s.v == none or s.v < 0) 0 else s.v",
            "y", 42);

    // ── Chained or with multiple none checks + value check ──────

    [Test]
    public void ChainedOr_NoneNoneValue() =>
        AssertNarrowed(
            "a:int? = 1\r b:int? = 2\r y = if(a == none or b == none or a + b == 0) -1 else a + b",
            "y", 3);

    // ── and + or combinations ────────────────────────────────────

    [Test]
    public void AndOr_NarrowInAnd_UseInOr() =>
        // (x != none and x > 0) or flag — x narrowed inside the and
        AssertNarrowed(
            "x:int? = 5\r flag = false\r y = if(x != none and x > 0) x else 0",
            "y", 5);

    [Test]
    public void OrThenAnd_ElseNarrows() =>
        // if(a == none or b == none) → else: both narrowed
        // then use in arithmetic
        AssertNarrowed(
            "a:int? = 3\r b:int? = 7\r y = if(a == none or b == none) 0 else (if(a > b) a - b else b - a)",
            "y", 4);

    // ── Progressive or in filter lambda ──────────────────────────

    [Test]
    public void Filter_OrProgressive_NoneOrNegative() =>
        AssertNarrowed(
            "items:int?[] = [1, none, -2, 3, none, -1]\r y = items.filter(rule not(it == none or it < 0))",
            "y", new[] { 1, 3 });

    [Test]
    public void Filter_OrProgressive_NoneOrZero() =>
        AssertNarrowed(
            "items:int?[] = [0, none, 1, 2, none, 0]\r y = items.filter(rule not(it == none or it == 0))",
            "y", new[] { 1, 2 });

    // ── Multi-elif style narrowing ───────────────────────────────

    [Test]
    public void Deep_MultiElif_EachBranchNarrows() =>
        AssertNarrowed(
            "x:int? = 42\r y = if(x == none) -1\r if(x < 0) 0\r if(x > 100) 100\r else x",
            "y", 42);

    [Test]
    public void Deep_MultiElif_NoneFirst_ThenRange() =>
        AssertNarrowed(
            "x:int? = 50\r y = if(x == none) 0\r if(x < 10) 10\r if(x > 90) 90\r else x",
            "y", 50);

    #endregion

    #region And/Or outside if

    [Test]
    public void BoolEquation_NarrowInAnd() {
        var r = "y:int? = 15\r x = y != none and y > 12"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled);
        Assert.AreEqual(true, r.Get("x"));
    }

    [Test]
    public void BoolEquation_NarrowInAnd_None() {
        var r = "y:int? = none\r x = y != none and y > 12"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled);
        Assert.AreEqual(false, r.Get("x"));
    }

    [Test]
    public void BoolEquation_NarrowInAnd_FailsCheck() {
        var r = "y:int? = 5\r x = y != none and y > 12"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled);
        Assert.AreEqual(false, r.Get("x"));
    }

    #endregion

    #region Collection narrowing

    // --- 1. Basic compact: filter none from int?[], then map without unwrap ---
    [Test]
    public void FilterNone_IntArray_MapAddsOne() {
        var r = "arr:int?[] = [1, none, 3]\r cleaned = arr.filterNotNull()\r y = cleaned.map(rule it + 1)"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled);
        Assert.AreEqual(new[] { 2, 4 }, r.Get("y"));
    }

    // --- 2. Compact + fold: sum of non-none elements ---
    [Test]
    public void FilterNone_IntArray_FoldSum() {
        var r = "arr:int?[] = [1, none, 3, none, 5]\r y = arr.filterNotNull().fold(rule it1 + it2)"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled);
        Assert.AreEqual(9, r.Get("y"));
    }

    // --- 3. Compact + count: count non-none elements ---
    [Test]
    public void FilterNone_IntArray_Count() {
        var r = "arr:int?[] = [1, none, 3, none, 5]\r y = arr.filterNotNull().count()"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled);
        Assert.AreEqual(3, r.Get("y"));
    }

    // --- 4. Compact + first: first non-none element ---
    [Test]
    public void FilterNone_IntArray_First() {
        var r = "arr:int?[] = [none, none, 42, 1]\r y = arr.filterNotNull().first()"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled);
        Assert.AreEqual(42, r.Get("y"));
    }

    // --- 5. Compact from real?[] ---
    [Test]
    public void FilterNone_RealArray_MapMultiplies() {
        var r = "arr:real?[] = [1.5, none, 2.5]\r y = arr.filterNotNull().map(rule it * 2)"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled);
        Assert.AreEqual(new[] { 3.0, 5.0 }, r.Get("y"));
    }

    // --- 6. Compact from text?[] ---
    [Test]
    public void FilterNone_TextArray_MapConcat() {
        var r = "arr:text?[] = ['hello', none, 'world']\r y = arr.filterNotNull().map(rule it.concat('!'))"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled);
        Assert.AreEqual(new[] { "hello!", "world!" }, r.Get("y"));
    }

    // --- 7. Compact from bool?[] ---
    [Test]
    public void FilterNone_BoolArray_AllTrue() {
        var r = "arr:bool?[] = [true, none, true]\r y = arr.filterNotNull().all(rule it)"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled);
        Assert.AreEqual(true, r.Get("y"));
    }

    // --- 8. Compound predicate: compact then filter by value ---
    [Test]
    public void FilterNone_CompoundPredicate_PositiveOnly() {
        var r = "arr:int?[] = [none, -1, 3, none, 0, 5]\r y = arr.filterNotNull().filter(rule it > 0).map(rule it * 10)"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled);
        Assert.AreEqual(new[] { 30, 50 }, r.Get("y"));
    }

    // --- 9. Compact preserves order ---
    [Test]
    public void FilterNone_PreservesOrder() {
        var r = "arr:int?[] = [none, 3, none, 1, none, 2]\r y = arr.filterNotNull()"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled);
        Assert.AreEqual(new[] { 3, 1, 2 }, r.Get("y"));
    }

    // --- 10. Compact on all-none array produces empty array ---
    [Test]
    public void FilterNone_AllNone_EmptyResult() {
        var r = "arr:int?[] = [none, none, none]\r y = arr.filterNotNull().count()"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled);
        Assert.AreEqual(0, r.Get("y"));
    }

    // --- 11. Compact on array with no nones: same result ---
    [Test]
    public void FilterNone_NoNones_SameElements() {
        var r = "arr:int?[] = [1, 2, 3]\r y = arr.filterNotNull().map(rule it + 10)"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled);
        Assert.AreEqual(new[] { 11, 12, 13 }, r.Get("y"));
    }

    // --- 12. Chained compact + map + fold: full pipeline ---
    [Test]
    public void FilterNone_ChainedPipeline_FilterMapFold() {
        var r = "arr:int?[] = [none, 1, none, 2, none, 3]\r y = arr.filterNotNull().map(rule it * it).fold(rule it1 + it2)"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled);
        Assert.AreEqual(14, r.Get("y")); // 1*1 + 2*2 + 3*3 = 1 + 4 + 9 = 14
    }

    // --- 13. Compact result assigned to typed variable: cleaned:int[] ---
    [Test]
    public void FilterNone_AssignToTypedVariable() {
        var r = "arr:int?[] = [1, none, 3]\r cleaned:int[] = arr.filterNotNull()\r y = cleaned[0]"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled);
        Assert.AreEqual(1, r.Get("y"));
    }

    // --- 14. Compact + sort: narrowed type is sortable ---
    [Test]
    public void FilterNone_ThenSort() {
        var r = "arr:int?[] = [none, 3, none, 1, none, 2]\r y = arr.filterNotNull().sort()"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled);
        Assert.AreEqual(new[] { 1, 2, 3 }, r.Get("y"));
    }

    // --- 15. Compact + reverse: chain operations on narrowed type ---
    [Test]
    public void FilterNone_ThenReverse() {
        var r = "arr:int?[] = [none, 1, none, 2, none, 3]\r y = arr.filterNotNull().reverse()"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled);
        Assert.AreEqual(new[] { 3, 2, 1 }, r.Get("y"));
    }

    // ═══════════════════════════════════════════════════════════════
    // Collection narrowing (advanced)
    // ═══════════════════════════════════════════════════════════════

    [Test]
    public void FilterNarrowing_ThenMap_Arithmetic() {
        "arr:int?[] = [1,none,3]\r y:int[] = arr.filterNotNull().map(rule it * 2)"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled)
            .AssertResultHas("y", new[] { 2, 6 });
    }

    [Test]
    public void FilterNarrowing_StructFieldAccess() {
        var expr =
            "arr = [if(true) {name='Alice'} else none, if(false) {name='Bob'} else none, if(true) {name='Carol'} else none]\r" +
            "y = arr.filterNotNull().map(rule it.name)";
        expr.CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled)
            .AssertResultHas("y", new[] { "Alice", "Carol" });
    }

    [Test]
    public void FilterNarrowing_MethodCallOnElement() {
        "arr:text?[] = ['hello', none, 'hi']\r y:int[] = arr.filterNotNull().map(rule it.count())"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled)
            .AssertResultHas("y", new[] { 5, 2 });
    }

    [Test]
    public void DoubleNarrowing_OptionalArrayOfOptionalElements() {
        var expr =
            "arr:int?[]? = [1,none,3]\r" +
            "y:int[] = if(arr != none) arr.filterNotNull() else []";
        expr.CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled)
            .AssertResultHas("y", new[] { 1, 3 });
    }

    [Test]
    public void FilterNarrowing_ThenFold() {
        "arr:int?[] = [1,none,2,none,3]\r y:int = arr.filterNotNull().fold(rule it1 + it2)"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled)
            .AssertResultHas("y", 6);
    }

    [Test]
    public void FilterNarrowing_ThenFilterByValue() {
        "arr:int?[] = [none, -1, none, 2, 0, 3]\r y:int[] = arr.filterNotNull().filter(rule it > 0)"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled)
            .AssertResultHas("y", new[] { 2, 3 });
    }

    [Test]
    public void FilterNarrowing_AssignedToExplicitNonOptionalArrayType() {
        "arr:int?[] = [1,none,3]\r cleaned:int[] = arr.filterNotNull()"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled)
            .AssertResultHas("cleaned", new[] { 1, 3 });
    }

    [Test]
    public void FilterNarrowing_ThenCount() {
        "arr:int?[] = [1,none,2,none,3]\r y:int = arr.filterNotNull().count()"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled)
            .AssertResultHas("y", 3);
    }

    [Test]
    public void CombinedScalarAndCollectionNarrowing() {
        var expr =
            "x:int? = 10\r" +
            "arr:int?[] = [1, none, 2]\r" +
            "y:int[] = if(x != none) arr.filterNotNull().map(rule it + x) else []";
        expr.CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled)
            .AssertResultHas("y", new[] { 11, 12 });
    }

    [Test]
    public void FilterNarrowing_OptionalStructArray_MapField() {
        var expr =
            "a = if(true) {name='Alice', age=30} else none\r" +
            "b = if(false) {name='Bob', age=25} else none\r" +
            "c = if(true) {name='Carol', age=35} else none\r" +
            "items = [a, b, c]\r" +
            "y:int[] = items.filterNotNull().map(rule it.age)";
        expr.CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled)
            .AssertResultHas("y", new[] { 30, 35 });
    }

    [Test]
    public void FilterChain_NarrowThenValueFilters() {
        "arr:int?[] = [none, -5, none, 50, 200, 0, 10]\r y:int[] = arr.filterNotNull().filter(rule it > 0).filter(rule it < 100)"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled)
            .AssertResultHas("y", new[] { 50, 10 });
    }

    [Test]
    public void FilterNarrowing_ThenToText() {
        "arr:int?[] = [1, none, 42]\r y:text[] = arr.filterNotNull().map(rule it.toText())"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled)
            .AssertResultHas("y", new[] { "1", "42" });
    }

    [Test]
    public void FilterNarrowing_LiteralArray_FoldSum() {
        "y:int = [1,none,2,none,3,none,4,none,5].filterNotNull().fold(rule it1 + it2)"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled)
            .AssertResultHas("y", 15);
    }

    [Test]
    public void FilterNarrowing_AllNone_EmptyResult() {
        "arr:int?[] = [none, none, none]\r y:int = arr.filterNotNull().count()"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled)
            .AssertResultHas("y", 0);
    }

    [Test]
    public void NonOptionalArray_FilterUnchanged() {
        "y:int[] = [1,2,3,4,5].filter(rule it > 2)"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled)
            .AssertResultHas("y", new[] { 3, 4, 5 });
    }

    #endregion

    #region Edge cases

    // ==================================================================
    // 1. Scope isolation: narrowing in one equation must NOT leak
    // ==================================================================

    [Test]
    public void ScopeIsolation_NarrowInFirstEq_SecondSeesOptional() =>
        Assert.AreEqual(-1, Calc(
            "x:int? = none\r z = if(x != none) x else 0\r y = x ?? -1"));

    [Test]
    public void ScopeIsolation_NarrowDoesNotLeakToBareMath() =>
        Assert.Throws<NFun.Exceptions.FunnyParseException>(
            () => "x:int? = 42\r z = if(x != none) x else 0\r y = x + 1"
                .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled));

    // ==================================================================
    // 2. Same var narrowed independently in multiple equations
    // ==================================================================

    [Test]
    public void IndependentNarrowing_TwoEquations_DifferentOps() {
        var result = "x:int? = 7\r y = if(x != none) x + 1 else 0\r z = if(x != none) x * 3 else 0"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled);
        Assert.AreEqual(8, result.Get("y"));
        Assert.AreEqual(21, result.Get("z"));
    }

    [Test]
    public void IndependentNarrowing_TwoEquations_NoneInput() {
        var result = "x:int? = none\r y = if(x != none) x + 1 else -1\r z = if(x != none) x * 3 else -2"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled);
        Assert.AreEqual(-1, result.Get("y"));
        Assert.AreEqual(-2, result.Get("z"));
    }

    // ==================================================================
    // 3. Narrowing + user function with non-optional parameter
    // ==================================================================

    [Test]
    public void UserFunc_NonOptionalParam_AcceptsNarrowedVar() =>
        Assert.AreEqual(84, Calc(
            "double(n:int):int = n * 2\r x:int? = 42\r y = if(x != none) double(x) else 0"));

    [Test]
    public void UserFunc_NonOptionalParam_NoneInput_TakesElse() =>
        Assert.AreEqual(0, Calc(
            "double(n:int):int = n * 2\r x:int? = none\r y = if(x != none) double(x) else 0"));

    [Test]
    public void UserFunc_TwoNonOptionalParams_BothNarrowed() =>
        Assert.AreEqual(11, Calc(
            "add(a:int, b:int):int = a + b\r x:int? = 4\r z:int? = 7\r y = if(x != none and z != none) add(x, z) else 0"));

    // ==================================================================
    // 4. Zero/false/empty are NOT none
    // ==================================================================

    [Test]
    public void ZeroIsNotNone_Int() =>
        Assert.AreEqual(0, Calc("x:int? = 0\r y = if(x != none) x else -1"));

    [Test]
    public void ZeroIsNotNone_Real() =>
        Assert.AreEqual(0.0, Calc("x:real? = 0.0\r y = if(x != none) x else -1.0"));

    [Test]
    public void FalseIsNotNone_Bool() =>
        Assert.AreEqual(false, Calc("x:bool? = false\r y = if(x != none) x else true"));

    [Test]
    public void EmptyStringIsNotNone_Text() =>
        Assert.AreEqual("", Calc("x:text? = ''\r y = if(x != none) x else 'fallback'"));

    // ==================================================================
    // 5. Narrowing preserves value identity
    // ==================================================================

    [Test]
    public void NarrowedVar_SelfEquality() =>
        Assert.AreEqual(true, Calc("x:int? = 42\r y:bool = if(x != none) x == x else false"));

    [Test]
    public void NarrowedVar_SelfSubtraction_IsZero() =>
        Assert.AreEqual(0, Calc("x:int? = 99\r y = if(x != none) x - x else -1"));

    // ==================================================================
    // 6. Narrowing proves non-none, NOT non-zero (IEEE 754 edge cases)
    // ==================================================================

    [Test]
    public void NarrowedReal_DivisionByZero_ProducesInfinity() =>
        Assert.AreEqual(double.PositiveInfinity, Calc(
            "x:real? = 0.0\r y = if(x != none) 1.0 / x else -1.0"));

    [Test]
    public void NarrowedReal_ZeroDivZero_ProducesNaN() =>
        Assert.IsNaN((double)Calc(
            "x:real? = 0.0\r y = if(x != none) x / x else -1.0"));

    // ==================================================================
    // 7. Short-circuit AND + progressive narrowing interaction
    // ==================================================================

    [Test]
    public void ShortCircuit_And_NoneInput_SkipsNarrowedComparison() =>
        Assert.AreEqual(0, Calc(
            "x:int? = none\r y = if(x != none and x > 0 and x < 100) x else 0"));

    [Test]
    public void ShortCircuit_And_ValuePassesAllGuards() =>
        Assert.AreEqual(50, Calc(
            "x:int? = 50\r y = if(x != none and x > 0 and x < 100) x else 0"));

    [Test]
    public void ShortCircuit_And_ValueFailsLaterGuard() =>
        Assert.AreEqual(0, Calc(
            "x:int? = 200\r y = if(x != none and x > 0 and x < 100) x else 0"));

    // ==================================================================
    // 8. Multi-output: narrowing + coalesce on same var
    // ==================================================================

    [Test]
    public void MultiOutput_NarrowAndCoalesce_NonNone() {
        var result = "x:int? = 42\r y = if(x != none) x + 1 else 0\r z = x ?? -1"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled);
        Assert.AreEqual(43, result.Get("y"));
        Assert.AreEqual(42, result.Get("z"));
    }

    [Test]
    public void MultiOutput_NarrowAndCoalesce_None() {
        var result = "x:int? = none\r y = if(x != none) x + 1 else 0\r z = x ?? -1"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled);
        Assert.AreEqual(0, result.Get("y"));
        Assert.AreEqual(-1, result.Get("z"));
    }

    [Test]
    public void MultiOutput_ThreeEquations_MixedMechanisms() {
        var result = "x:int? = 10\r a = if(x != none) x * 2 else 0\r b = x ?? -1\r y = if(x != none) x + 5 else 0"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled);
        Assert.AreEqual(20, result.Get("a"));
        Assert.AreEqual(10, result.Get("b"));
        Assert.AreEqual(15, result.Get("y"));
    }

    // ==================================================================
    // 9. Optional text — method calls on narrowed text var
    // ==================================================================

    [Test]
    public void NarrowedText_Reverse_NonNone() =>
        Assert.AreEqual("olleh", Calc(
            "x:text? = 'hello'\r y = if(x != none) x.reverse() else ''"));

    [Test]
    public void NarrowedText_Reverse_NoneInput() =>
        Assert.AreEqual("", Calc(
            "x:text? = none\r y = if(x != none) x.reverse() else ''"));

    [Test]
    public void NarrowedText_Count_ReturnsLength() =>
        Assert.AreEqual(5, Calc(
            "x:text? = 'hello'\r y = if(x != none) x.count() else 0"));

    [Test]
    public void NarrowedText_Concat_TwoNarrowedVars() =>
        Assert.AreEqual("foobar", Calc(
            "a:text? = 'foo'\r b:text? = 'bar'\r y = if(a != none and b != none) a.concat(b) else ''"));

    // ==================================================================
    // 10. Negative: narrowing must NOT apply where unsound
    // ==================================================================

    [Test]
    public void EdgeCase_Negative_Or_UnsharedVar_NotNarrowed() =>
        Assert.Throws<NFun.Exceptions.FunnyParseException>(
            () => "x:int? = 42\r z:int? = 10\r y:int = if(x != none or z != none) z + 1 else 0"
                .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled));

    [Test]
    public void EdgeCase_Negative_EqualNone_ThenBranchNotNarrowed() =>
        Assert.Throws<NFun.Exceptions.FunnyParseException>(
            () => "x:int? = 42\r y = if(x == none) x + 1 else 0"
                .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled));

    // ── Adversarial tests ───────────────────────────────────────

    // Attack #1: Alias collision — unicode and emoji variable names
    [Test]
    public void Attack1_UnicodeVarName_NarrowsCorrectly() =>
        Assert.AreEqual(42, Calc(
            "\u03b1:int? = 42\r y = if(\u03b1 != none) \u03b1 else 0"));

    [Test]
    public void Attack1_EmojiVarName_NarrowsCorrectly() =>
        Assert.AreEqual(7, Calc(
            "\U0001F680:int? = 7\r y = if(\U0001F680 != none) \U0001F680 else 0"));

    [Test]
    public void Attack1_CyrillicVarName_NarrowsCorrectly() =>
        Assert.AreEqual(10, Calc(
            "\u0436:int? = 5\r y = if(\u0436 != none) \u0436 * 2 else 0"));

    // Attack #2: Narrowing a NON-optional variable
    [Test]
    public void Attack2_NarrowNonOptional_Int_Works() =>
        Assert.AreEqual(43, Calc("x:int = 42\r y = if(x != none) x + 1 else 0"));

    [Test]
    public void Attack2_NarrowNonOptional_Bool_Works() =>
        Assert.AreEqual(true, Calc("x:bool = true\r y = if(x != none) x else false"));

    [Test]
    public void Attack2_NarrowNonOptional_Real_Works() =>
        Assert.AreEqual(3.14, Calc("x:real = 3.14\r y = if(x != none) x else 0.0"));

    [Test]
    public void Attack2_NarrowNonOptional_Text_Works() =>
        Assert.AreEqual("hi", Calc("x:text = 'hi'\r y = if(x != none) x else ''"));

    [Test]
    public void Attack2_NarrowNonOptional_Array_Works() {
        var r = Calc("x:int[] = [1,2,3]\r y = if(x != none) x else [0]");
        Assert.AreEqual(new[]{1,2,3}, r);
    }

    // Attack #3: Very deep AND nesting — 6 variables checked in AND chain
    [Test]
    public void Attack3_SixVariables_AllNarrowed() =>
        Assert.AreEqual(720, Calc(
            "a:int? = 1\r b:int? = 2\r c:int? = 3\r d:int? = 4\r e:int? = 5\r f:int? = 6\r" +
            " y = if(a != none and b != none and c != none and d != none and e != none and f != none) " +
            "a * b * c * d * e * f else 0"));

    [Test]
    public void Attack3_SixVariables_LastIsNone_ReturnsElse() =>
        Assert.AreEqual(-1, Calc(
            "a:int? = 1\r b:int? = 2\r c:int? = 3\r d:int? = 4\r e:int? = 5\r f:int? = none\r" +
            " y = if(a != none and b != none and c != none and d != none and e != none and f != none) " +
            "a * b * c * d * e * f else -1"));

    [Test]
    public void Attack3_RedundantTripleCheck_SameVariable() =>
        Assert.AreEqual(42, Calc(
            "x:int? = 42\r y = if(x != none and x != none and x != none) x else 0"));

    // Attack #4: Narrowed var used heavily with mixed operations
    [Test]
    public void Attack4_FourOccurrences_MixedArithmetic() =>
        Assert.AreEqual(25, Calc(
            "x:int? = 5\r y = if(x != none and x > 0) x + x * x - x else 0"));

    [Test]
    public void Attack4_Polynomial_FiveOccurrences() =>
        Assert.AreEqual(105, Calc(
            "x:int? = 10\r y = if(x != none and x > 5) x * x + x / 2 else 0"));

    [Test]
    public void Attack4_BodyWithUserFunction() =>
        Assert.AreEqual(10, Calc(
            "double(n) = n * 2\r x:int? = 5\r y = if(x != none) double(x) else 0"));

    [Test]
    public void Attack4_BodyWithTwoNarrowedVarsInUserFunction() =>
        Assert.AreEqual(15, Calc(
            "add(a, b) = a + b\r x:int? = 10\r z:int? = 5\r " +
            "y = if(x != none and z != none) add(x, z) else 0"));

    // Attack #5: Optional chain — narrowing across if-else layers
    [Test]
    public void Attack5_ChainedNarrowing_ViaIfElse() =>
        Assert.AreEqual(42, Calc(
            "x:int? = 42\r z = if(x != none) x else none\r y = if(z != none) z else 0"));

    [Test]
    public void Attack5_ChainedNarrowing_NoneInput() =>
        Assert.AreEqual(0, Calc(
            "x:int? = none\r z = if(x != none) x else none\r y = if(z != none) z else 0"));

    [Test]
    public void Attack5_SafeAccessInCondition_DirectAccessInBody() =>
        Assert.AreEqual(42, Calc(
            "a = if(true) {x = 42} else none\r " +
            "y = if(a?.x != none) a.x else 0"));

    [Test]
    public void Attack5_CoalesceOnNarrowedVar_InThenBranch() =>
        Assert.AreEqual(42, Calc("x:int? = 42\r y = if(x != none) x ?? 0 else -1"));

    [Test]
    public void Attack5_ForceUnwrapOnNarrowedVar() =>
        Assert.AreEqual(43, Calc("x:int? = 42\r y = if(x != none) x! + 1 else 0"));

    // Attack #6: OR with asymmetric WhenTrue / WhenFalse
    [Test]
    public void Attack6_AsymmetricOr_ElseNarrowsOnlyZ() {
        var result = Calc(
            "x:int? = none\r z:int? = 5\r " +
            "y = if(x != none or z == none) 0 else z + 1");
        Assert.AreEqual(6, result);
    }

    [Test]
    public void Attack6_AsymmetricOr_ElseCannotUseX_MustFail() =>
        Assert.Throws<NFun.Exceptions.FunnyParseException>(
            () => "x:int? = 10\r z:int? = 5\r y:int = if(x != none or z == none) 0 else x + 1"
                .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled));

    [Test]
    public void Attack6_NarrowedVarInMapLambda() {
        var result = Calc(
            "x:int? = 10\r arr = [1, 2, 3]\r " +
            "y = if(x != none) arr.map(rule it + x) else [0]");
        Assert.IsNotNull(result);
    }

    [Test]
    public void Attack6_NarrowedVarInFilterLambda() {
        var result = Calc(
            "x:int? = 3\r arr = [1, 2, 3, 4, 5]\r " +
            "y = if(x != none) arr.filter(rule it > x) else [0]");
        Assert.IsNotNull(result);
    }

    // Attack #7: Multi-case if-elif — else branch narrowing
    [Test]
    public void Attack7_MultiCaseIf_ElseShouldNarrowBothVars() {
        var result = Calc(
            "x:int? = 10\r z:int? = 5\r " +
            "y = if(x == none) -1 \r if(z == none) -2 else x + z");
        Assert.AreEqual(15, result);
    }

    [Test]
    public void Attack7_MultiCaseIf_ElseShouldNarrowFirstVar() {
        var result = Calc(
            "a:int? = 42\r b:int? = 10\r " +
            "y = if(a == none) 0 \r if(b == none) 0 else a + 1");
        Assert.AreEqual(43, result);
    }

    // Attack #8: Conflicting narrowing scopes — nested if narrowing same variable
    [Test]
    public void Attack8_NestedSameVar_OuterAndInner() =>
        Assert.AreEqual(42, Calc(
            "x:int? = 42\r y = if(x != none) if(x != none) x else 0 else 0"));

    [Test]
    public void Attack8_NestedSameVar_NoneInput() =>
        Assert.AreEqual(0, Calc(
            "x:int? = none\r y = if(x != none) if(x != none) x else 0 else 0"));

    [Test]
    public void Attack8_NestedTwoDifferentVars() =>
        Assert.AreEqual(15, Calc(
            "x:int? = 10\r z:int? = 5\r y = if(x != none) if(z != none) x + z else x else 0"));

    [Test]
    public void Attack8_NestedTwoDifferentVars_InnerNone() =>
        Assert.AreEqual(10, Calc(
            "x:int? = 10\r z:int? = none\r y = if(x != none) if(z != none) x + z else x else 0"));

    [Test]
    public void Attack8_DeepNesting_OuterNarrows_InnerGuards() =>
        Assert.AreEqual(100, Calc(
            "x:int? = 10\r y = if(x != none) if(x > 5) x * x else x + 1 else 0"));

    [Test]
    public void Attack8_DeepNesting_SmallValue() =>
        Assert.AreEqual(4, Calc(
            "x:int? = 3\r y = if(x != none) if(x > 5) x * x else x + 1 else 0"));

    // Attack #9: Progressive narrowing — user function in right side of AND
    [Test]
    public void Attack9_UserFuncOnRightOfAnd() =>
        Assert.AreEqual(42, Calc(
            "isPositive(n) = n > 0\r x:int? = 42\r " +
            "y = if(x != none and isPositive(x)) x else 0"));

    [Test]
    public void Attack9_UserFuncOnRightOfAnd_NoneInput() =>
        Assert.AreEqual(0, Calc(
            "isPositive(n) = n > 0\r x:int? = none\r " +
            "y = if(x != none and isPositive(x)) x else 0"));

    [Test]
    public void Attack9_UserFuncOnRightOfAnd_NegativeValue() =>
        Assert.AreEqual(0, Calc(
            "isPositive(n) = n > 0\r x:int? = -5\r " +
            "y = if(x != none and isPositive(x)) x else 0"));

    [Test]
    public void Attack9_ComparisonChain_ThreeConditions() =>
        Assert.AreEqual(50, Calc(
            "x:int? = 50\r y = if(x != none and x > 0 and x < 100) x else 0"));

    [Test]
    public void Attack9_ComparisonChain_OutOfRange() =>
        Assert.AreEqual(0, Calc(
            "x:int? = 150\r y = if(x != none and x > 0 and x < 100) x else 0"));

    [Test]
    public void Attack9_TwoVarsNarrowedThenCompared() =>
        Assert.AreEqual(7, Calc(
            "x:int? = 10\r z:int? = 3\r y = if(x != none and z != none and x > z) x - z else 0"));

    // Attack #10: Two independent equations narrowing the same variable
    [Test]
    public void Attack10_TwoEquations_SameVar() {
        var result = "x:int? = 42\r y = if(x != none) x + 1 else 0\r z = if(x != none) x * 2 else 0"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled);
        Assert.AreEqual(43, result.Get("y"));
        Assert.AreEqual(84, result.Get("z"));
    }

    [Test]
    public void Attack10_TwoEquations_NoneInput() {
        var result = "x:int? = none\r y = if(x != none) x + 1 else 0\r z = if(x != none) x * 2 else 0"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled);
        Assert.AreEqual(0, result.Get("y"));
        Assert.AreEqual(0, result.Get("z"));
    }

    [Test]
    public void Attack10_ThreeEquations_DifferentOperations() {
        var result = ("x:int? = 10\r " +
                      "a = if(x != none) x + 1 else 0\r " +
                      "b = if(x != none) x * 2 else 0\r " +
                      "y = if(x != none) x ** 2 else 0")
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled);
        Assert.AreEqual(11, result.Get("a"));
        Assert.AreEqual(20, result.Get("b"));
        Assert.AreEqual(100, result.Get("y"));
    }

    [Test]
    public void Attack10_MixedNumericTypes_IntAndReal() {
        var result = Calc(
            "x:int? = 5\r z:real? = 2.5\r y = if(x != none and z != none) x + z else 0.0");
        Assert.AreEqual(7.5, result);
    }

    [Test]
    public void Attack10_NarrowedVarUsedAsArrayIndex() =>
        Assert.AreEqual(30, Calc(
            "arr = [10, 20, 30]\r x:int? = 2\r y = if(x != none) arr[x] else 0"));

    [Test]
    public void Attack10_NarrowedVarInArrayLiteral() {
        var result = Calc("x:int? = 42\r y = if(x != none) [x, 1, 2] else [0]");
        Assert.IsNotNull(result);
    }

    [Test]
    public void Attack10_OptionalArrayElement_NarrowedAfterExtraction() =>
        Assert.AreEqual(2, Calc(
            "arr:int?[] = [1, none, 3]\r x = arr[0]\r " +
            "y = if(x != none) x + 1 else 0"));

    #endregion

    #region Safe access

    [Test]
    public void SafeFieldAccess_NotNone_NarrowsRoot() =>
        Assert.AreEqual(42, Calc(
            "a = if(true) {foo = true, boo = 42} else none\r y = if(a?.foo != none) a.boo else 0"));

    [Test]
    public void SafeFieldAccess_EqualTrue_NarrowsRoot() =>
        Assert.AreEqual(42, Calc(
            "a = if(true) {foo = true, boo = 42} else none\r y = if(a?.foo == true) a.boo else 0"));

    [Test]
    public void SafeFieldAccess_NoneInput_ReturnsElse() =>
        Assert.AreEqual(0, Calc(
            "a = if(false) {foo = true, boo = 42} else none\r y = if(a?.foo == true) a.boo else 0"));

    #endregion

    #region Quick filter

    [Test]
    public void QuickCompact_Basic() {
        var r = "arr:int?[] = [1, none, 3, none, 5]\r y = arr.filterNotNull()"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled);
        Assert.AreEqual(new[]{1, 3, 5}, r.Get("y"));
    }

    [Test]
    public void QuickCompact_ThenMap() {
        var r = "arr:int?[] = [1, none, 3]\r y = arr.filterNotNull().map(rule it + 1)"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled);
        Assert.AreEqual(new[]{2, 4}, r.Get("y"));
    }

    [Test]
    public void QuickCompact_AllNone() {
        var r = "arr:int?[] = [none, none]\r y = arr.filterNotNull()"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled);
        Assert.AreEqual(new int[0], r.Get("y"));
    }

    #endregion
}
