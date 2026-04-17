namespace NFun.SyntaxTests.TypeNarrowing;

using NFun.TestTools;
using NUnit.Framework;

/// <summary>
/// 10 tricky type narrowing correctness edge cases inspired by Rust/Swift optional handling.
///
/// Each group targets a specific correctness property that real-world optional type systems
/// must uphold. The tests are ordered from foundational (scope isolation) to advanced
/// (user functions, IEEE 754, multi-output mixing).
/// </summary>
[TestFixture]
public class TypeNarrowingEdgeCaseTest {

    private static object Calc(string expr) =>
        expr.CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled)
            .Get("y");

    // ==================================================================
    // 1. Scope isolation: narrowing in one equation must NOT leak
    //    Rust: narrowing in one `if let` block is invisible outside.
    //    Swift: `guard let` narrowing ends at the scope boundary.
    //    Here: narrowing x in equation z must not affect equation y.
    // ==================================================================

    [Test]
    public void ScopeIsolation_NarrowInFirstEq_SecondSeesOptional() =>
        // z's if-branch narrows x. But y is a separate equation: x is still int?.
        // Coalesce (??) works on int?, proving y sees the original optional type.
        Assert.AreEqual(-1, Calc(
            "x:int? = none\r z = if(x != none) x else 0\r y = x ?? -1"));

    [Test]
    public void ScopeIsolation_NarrowDoesNotLeakToBareMath() =>
        // Critical negative test: even though z narrows x, y = x + 1 must FAIL
        // because y is outside any narrowing scope.
        Assert.Throws<NFun.Exceptions.FunnyParseException>(
            () => "x:int? = 42\r z = if(x != none) x else 0\r y = x + 1"
                .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled));

    // ==================================================================
    // 2. Same var narrowed independently in multiple equations
    //    Rust: calling .unwrap() in two different functions on the same Option.
    //    Each narrowing scope is a fresh "if let" — they must not interfere.
    // ==================================================================

    [Test]
    public void IndependentNarrowing_TwoEquations_DifferentOps() {
        // y and z both narrow the same x, but use different operations.
        // Both must produce correct results from the same input.
        var result = "x:int? = 7\r y = if(x != none) x + 1 else 0\r z = if(x != none) x * 3 else 0"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled);
        Assert.AreEqual(8, result.Get("y"));
        Assert.AreEqual(21, result.Get("z"));
    }

    [Test]
    public void IndependentNarrowing_TwoEquations_NoneInput() {
        // Both equations see none — both take else branch independently.
        // Different fallback values prove each scope is independent.
        var result = "x:int? = none\r y = if(x != none) x + 1 else -1\r z = if(x != none) x * 3 else -2"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled);
        Assert.AreEqual(-1, result.Get("y"));
        Assert.AreEqual(-2, result.Get("z"));
    }

    // ==================================================================
    // 3. Narrowing + user function with non-optional parameter
    //    Rust: fn double(n: i32) -> i32. After `if let Some(x) = opt`, x: i32.
    //    Swift: func double(_ n: Int) -> Int. After guard-let, n is Int.
    //    The narrowed var must be accepted by a function expecting non-optional.
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
    // 4. Zero/false/empty are NOT none (Rust: Some(0) != None)
    //    A classic confusion in nullable systems. NFun optional is a proper
    //    Maybe/Option monad — zero, false, "" are valid wrapped values.
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
    // 5. Narrowing preserves value identity (x == x, x - x == 0)
    //    Ensures the narrowing transformation doesn't copy, box, or convert
    //    the underlying value. The same runtime object must be used.
    // ==================================================================

    [Test]
    public void NarrowedVar_SelfEquality() =>
        Assert.AreEqual(true, Calc("x:int? = 42\r y:bool = if(x != none) x == x else false"));

    [Test]
    public void NarrowedVar_SelfSubtraction_IsZero() =>
        Assert.AreEqual(0, Calc("x:int? = 99\r y = if(x != none) x - x else -1"));

    // ==================================================================
    // 6. Narrowing proves non-none, NOT non-zero (IEEE 754 edge cases)
    //    Rust: Some(0.0) / Some(0.0) = NaN.  Swift: 0.0 / 0.0 = nan.
    //    Narrowing strips optionality but must not validate the value.
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
    //    Rust: `if let Some(x) = opt && x > 0` — left-to-right, short-circuit.
    //    If first conjunct is false, the rest must not evaluate.
    //    Progressive narrowing: x != none narrows x, then x > 0 sees int.
    //    With none input, short-circuit must prevent x > 0 from running on none.
    // ==================================================================

    [Test]
    public void ShortCircuit_And_NoneInput_SkipsNarrowedComparison() =>
        // x is none. x != none = false. AND short-circuits. x > 0 never runs.
        // If short-circuit were broken, comparing none > 0 would crash.
        Assert.AreEqual(0, Calc(
            "x:int? = none\r y = if(x != none and x > 0 and x < 100) x else 0"));

    [Test]
    public void ShortCircuit_And_ValuePassesAllGuards() =>
        // x=50 passes all three: != none, > 0, < 100
        Assert.AreEqual(50, Calc(
            "x:int? = 50\r y = if(x != none and x > 0 and x < 100) x else 0"));

    [Test]
    public void ShortCircuit_And_ValueFailsLaterGuard() =>
        // x=200 passes != none and > 0, but fails < 100 — takes else
        Assert.AreEqual(0, Calc(
            "x:int? = 200\r y = if(x != none and x > 0 and x < 100) x else 0"));

    // ==================================================================
    // 8. Multi-output: narrowing + coalesce on same var — independent mechanisms
    //    Rust: match + unwrap_or_else in different let bindings.
    //    y uses if-narrowing, z uses ?? coalesce — both operate on same x.
    //    Neither should interfere with the other.
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
        // a: narrowing+arithmetic, b: coalesce, y: narrowing+arithmetic
        // All three operate on the same x independently
        var result = "x:int? = 10\r a = if(x != none) x * 2 else 0\r b = x ?? -1\r y = if(x != none) x + 5 else 0"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled);
        Assert.AreEqual(20, result.Get("a"));
        Assert.AreEqual(10, result.Get("b"));
        Assert.AreEqual(15, result.Get("y"));
    }

    // ==================================================================
    // 9. Optional text — method calls on narrowed text var
    //    Swift: if let s = optionalString { s.reversed() }
    //    After narrowing, text? becomes text, so .reverse() .count() etc.
    //    must work without safe-access (?.) syntax.
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
    //     These document that the analyzer correctly rejects unsafe patterns.
    // ==================================================================

    [Test]
    public void Negative_Or_UnsharedVar_NotNarrowed() =>
        // x != none OR z != none: in then-branch, z might still be none
        // (only x appears if z's check was the one that succeeded).
        // Using z + 1 (requiring int) must fail.
        Assert.Throws<NFun.Exceptions.FunnyParseException>(
            () => "x:int? = 42\r z:int? = 10\r y:int = if(x != none or z != none) z + 1 else 0"
                .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled));

    [Test]
    public void Negative_EqualNone_ThenBranchNotNarrowed() =>
        // if(x == none) → then-branch: we know x IS none, not non-none.
        // Attempting x + 1 in then-branch must fail.
        Assert.Throws<NFun.Exceptions.FunnyParseException>(
            () => "x:int? = 42\r y = if(x == none) x + 1 else 0"
                .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled));
}
