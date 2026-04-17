namespace NFun.SyntaxTests.TypeNarrowing;

using NFun.TestTools;
using NUnit.Framework;

/// <summary>
/// Adversarial tests designed to probe edge cases in the type narrowing implementation.
/// Each test targets a specific attack vector that could break the narrowing analysis,
/// TIC graph setup, or runtime execution.
///
/// Results summary (10 attack vectors):
///   PASS: #1 (alias collision), #3 (deep AND), #4 (heavy body),
///         #5 (optional chain), #6 (OR asymmetry), #8 (nested scopes),
///         #9 (progressive + user func), #10 (multi-equation merge)
///   LIMITATION: #7 (multi-case if-elif else narrowing)
///   BUG: #2 (non-optional narrowing: inconsistent behavior for primitives vs composites)
/// </summary>
[TestFixture]
public class AdversarialNarrowingTest {

    private static object Calc(string expr, params (string id, object val)[] values) =>
        expr.CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled, values: values)
            .Get("y");

    private static void Builds(string expr) =>
        expr.BuildWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled);

    // ==========================================================================
    // Attack #1: Alias collision — unicode and emoji variable names
    //
    // RESULT: PASS
    //
    // The narrowing alias format is `scopeId~varName`.  The `~` character is not
    // a valid identifier character in NFun, so alias collision is impossible at
    // the syntax level.  Unicode and emoji identifiers work correctly.
    // ==========================================================================

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

    // ==========================================================================
    // Attack #2: Narrowing a NON-optional variable
    //
    // RESULT: BUG — inconsistent behavior across types.
    //
    // When narrowing is applied to a non-optional variable (e.g. `x:int = 42`
    // with `if(x != none)`), SetNarrowedVariable creates opt(T) and merges
    // with the original node.  For primitive types (int, bool, real), this
    // merge FAILS with FunnyParseException.  For composite types (text=char[],
    // int[]), it SUCCEEDS silently.
    //
    // This is inconsistent: either ALL non-optional narrowing should be rejected
    // (with a clear error) or ALL should be accepted (as a no-op).
    // The inconsistency comes from MergeInplace handling of composite vs primitive
    // states differently when merged with StateOptional.
    // ==========================================================================

    // Non-optional vars: narrowing is a no-op (skipped), expression works normally.
    // != none is vacuously true for non-optional types.
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

    // ==========================================================================
    // Attack #3: Very deep AND nesting — 6 variables checked in AND chain
    //
    // RESULT: PASS
    //
    // Union accumulation in CombineAnd and SetNarrowedVariable + MergeInplace
    // handle 6 independent narrowing constraints correctly.
    // ==========================================================================

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

    // ==========================================================================
    // Attack #4: Narrowed var used heavily with mixed operations
    //
    // RESULT: PASS
    //
    // The narrowed alias is consistently resolved for all occurrences in the body.
    // Works with 4+ occurrences, mixed arithmetic, and condition+body usage.
    // ==========================================================================

    [Test]
    public void Attack4_FourOccurrences_MixedArithmetic() =>
        // x=5: x + x*x - x = 5 + 25 - 5 = 25
        Assert.AreEqual(25, Calc(
            "x:int? = 5\r y = if(x != none and x > 0) x + x * x - x else 0"));

    [Test]
    public void Attack4_Polynomial_FiveOccurrences() =>
        // x=10: x*x + 2*x + 1 = 100 + 20 + 1 = 121. But x / 2 = 5 → total 105
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

    // ==========================================================================
    // Attack #5: Optional chain — narrowing across if-else layers
    //
    // RESULT: PASS
    //
    // Narrowing works correctly when optional values are produced by one
    // if-else and consumed by another, even with chained safe-access operators.
    // ==========================================================================

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
        // x is narrowed to int in then-branch; x ?? 0 applies coalesce to non-optional
        // This works because TIC allows coalesce on any type (coalesce is T? -> T)
        Assert.AreEqual(42, Calc("x:int? = 42\r y = if(x != none) x ?? 0 else -1"));

    [Test]
    public void Attack5_ForceUnwrapOnNarrowedVar() =>
        Assert.AreEqual(43, Calc("x:int? = 42\r y = if(x != none) x! + 1 else 0"));

    // ==========================================================================
    // Attack #6: OR with asymmetric WhenTrue / WhenFalse
    //
    // RESULT: PASS
    //
    // `if(x != none or z == none)`:
    //   Left:  WhenTrue={x}, WhenFalse={}
    //   Right: WhenTrue={}, WhenFalse={z}
    //   OR:    WhenTrue=intersect({x},{})={}, WhenFalse=union({},{z})={z}
    // Else branch correctly narrows z but not x.
    // ==========================================================================

    [Test]
    public void Attack6_AsymmetricOr_ElseNarrowsOnlyZ() {
        // Else executes when: x IS none AND z IS NOT none
        var result = Calc(
            "x:int? = none\r z:int? = 5\r " +
            "y = if(x != none or z == none) 0 else z + 1");
        Assert.AreEqual(6, result);
    }

    [Test]
    public void Attack6_AsymmetricOr_ElseCannotUseX_MustFail() =>
        // x is NOT narrowed in else branch — using x+1 must fail
        Assert.Throws<NFun.Exceptions.FunnyParseException>(
            () => "x:int? = 10\r z:int? = 5\r y:int = if(x != none or z == none) 0 else x + 1"
                .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled));

    [Test]
    public void Attack6_NarrowedVarInMapLambda() {
        // Narrowed variable captured in a map lambda
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

    // ==========================================================================
    // Attack #7: Multi-case if-elif — else branch narrowing SKIPPED
    //
    // RESULT: LIMITATION
    //
    // Code: `if (node.Ifs.Length == 1 && !lastNarrowing.IsEmpty)` — else-branch
    // narrowing is only applied for single if-case.  For if-elif, even when
    // the else branch logically guarantees non-none-ness, no narrowing happens.
    // ==========================================================================

    [Test]
    public void Attack7_MultiCaseIf_ElseShouldNarrowBothVars() {
        // if(x == none) -1  if(z == none) -2  else x + z
        // Else means: x != none AND z != none — both should be narrowed
        var result = Calc(
            "x:int? = 10\r z:int? = 5\r " +
            "y = if(x == none) -1 \r if(z == none) -2 else x + z");
        Assert.AreEqual(15, result);
    }

    [Test]
    public void Attack7_MultiCaseIf_ElseShouldNarrowFirstVar() {
        // if(a == none) 0  if(b == none) 0  else a + 1
        var result = Calc(
            "a:int? = 42\r b:int? = 10\r " +
            "y = if(a == none) 0 \r if(b == none) 0 else a + 1");
        Assert.AreEqual(43, result);
    }

    // ==========================================================================
    // Attack #8: Conflicting narrowing scopes — nested if narrowing same variable
    //
    // RESULT: PASS
    //
    // Outer if narrows x via `outerScopeId~x`, inner if narrows x again via
    // `innerScopeId~x`.  The VariableScopeAliasTable correctly resolves the
    // innermost alias.  SetNarrowedVariable on already-narrowed aliases works
    // because MergeInplace of opt(T) with an existing narrowed node (which is
    // already non-optional T) effectively creates opt(T) where T=T, a tautology.
    // ==========================================================================

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
        // Outer: x narrowed.  Inner: x > 5 guard.  x=10 → 100, x=3 → 4
        Assert.AreEqual(100, Calc(
            "x:int? = 10\r y = if(x != none) if(x > 5) x * x else x + 1 else 0"));

    [Test]
    public void Attack8_DeepNesting_SmallValue() =>
        Assert.AreEqual(4, Calc(
            "x:int? = 3\r y = if(x != none) if(x > 5) x * x else x + 1 else 0"));

    // ==========================================================================
    // Attack #9: Progressive narrowing — user function in right side of AND
    //
    // RESULT: PASS
    //
    // Left side of AND narrows x from int? to int.  Right side calls a
    // user-defined function with x as argument — progressive narrowing makes
    // x available as int for the function call.
    // ==========================================================================

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

    // ==========================================================================
    // Attack #10: Two independent equations narrowing the same variable
    //
    // RESULT: PASS
    //
    // Each equation gets a different scopeId, creating different aliases.
    // SetNarrowedVariable is called twice on the same original node, creating
    // TWO opt(T) constraints merged into it.  The double-merge works because
    // opt(T1) and opt(T2) share the same original node structure.
    // ==========================================================================

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
        // x:int? and z:real? narrowed together, then combined
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
}
