using System;
using NFun.TestTools;
using NUnit.Framework;

namespace NFun.SyntaxTests;

/// <summary>
/// IEEE 754 compliance tests for floating-point operations.
///
/// Key IEEE 754 rules:
/// - NaN compared to anything (including NaN) returns false, except != which returns true
/// - Any arithmetic with NaN produces NaN
/// - Division by zero produces +/-Infinity
/// - Infinity arithmetic follows standard rules
///
/// In NFun: NaN = 0.0/0.0, Infinity = 1.0/0.0, -Infinity = -1.0/0.0
/// </summary>
[TestFixture]
public class IEEE754ComplianceTest {

    // ═══════════════════════════════════════════════════════════════
    // NaN comparisons: ALL must return false (except !=)
    //
    // IEEE 754: NaN is "unordered" — it is not less than, equal to,
    // or greater than any value, including itself.
    // ═══════════════════════════════════════════════════════════════

    [TestCase("(0.0/0.0) == (0.0/0.0)", false)] // NaN == NaN
    [TestCase("(0.0/0.0) == 0.0",       false)] // NaN == 0.0
    [TestCase("(0.0/0.0) == 1.0",       false)] // NaN == 1.0
    [TestCase("(0.0/0.0) == -1.0",      false)] // NaN == -1.0
    public void NaN_Equality_ReturnsFalse(string expr, bool expected)
        => expr.AssertAnonymousOut(expected);

    [TestCase("(0.0/0.0) != (0.0/0.0)", true)] // NaN != NaN
    [TestCase("(0.0/0.0) != 0.0",       true)] // NaN != 0.0
    [TestCase("(0.0/0.0) != 1.0",       true)] // NaN != 1.0
    public void NaN_Inequality_ReturnsTrue(string expr, bool expected)
        => expr.AssertAnonymousOut(expected);

    [TestCase("(0.0/0.0) < 1.0",           false)] // NaN < 1.0
    [TestCase("(0.0/0.0) <= 1.0",          false)] // NaN <= 1.0
    [TestCase("(0.0/0.0) > 1.0",           false)] // NaN > 1.0
    [TestCase("(0.0/0.0) >= 1.0",          false)] // NaN >= 1.0
    [TestCase("(0.0/0.0) < -1.0",          false)] // NaN < -1.0
    [TestCase("(0.0/0.0) > -1.0",          false)] // NaN > -1.0
    public void NaN_LessThanGreaterThan_ReturnsFalse(string expr, bool expected)
        => expr.AssertAnonymousOut(expected);

    [TestCase("1.0 < (0.0/0.0)",           false)] // 1.0 < NaN
    [TestCase("1.0 <= (0.0/0.0)",          false)] // 1.0 <= NaN
    [TestCase("1.0 > (0.0/0.0)",           false)] // 1.0 > NaN
    [TestCase("1.0 >= (0.0/0.0)",          false)] // 1.0 >= NaN
    [TestCase("-1.0 < (0.0/0.0)",          false)] // -1.0 < NaN
    [TestCase("-1.0 > (0.0/0.0)",          false)] // -1.0 > NaN
    public void Value_ComparedToNaN_ReturnsFalse(string expr, bool expected)
        => expr.AssertAnonymousOut(expected);

    [TestCase("(0.0/0.0) < (0.0/0.0)",    false)] // NaN < NaN
    [TestCase("(0.0/0.0) <= (0.0/0.0)",   false)] // NaN <= NaN
    [TestCase("(0.0/0.0) > (0.0/0.0)",    false)] // NaN > NaN
    [TestCase("(0.0/0.0) >= (0.0/0.0)",   false)] // NaN >= NaN
    public void NaN_ComparedToNaN_ReturnsFalse(string expr, bool expected)
        => expr.AssertAnonymousOut(expected);

    // NaN with Infinity
    [TestCase("(0.0/0.0) < (1.0/0.0)",    false)] // NaN < Infinity
    [TestCase("(0.0/0.0) > (1.0/0.0)",    false)] // NaN > Infinity
    [TestCase("(0.0/0.0) < (-1.0/0.0)",   false)] // NaN < -Infinity
    [TestCase("(0.0/0.0) > (-1.0/0.0)",   false)] // NaN > -Infinity
    [TestCase("(1.0/0.0) > (0.0/0.0)",    false)] // Infinity > NaN
    [TestCase("(-1.0/0.0) < (0.0/0.0)",   false)] // -Infinity < NaN
    public void NaN_ComparedToInfinity_ReturnsFalse(string expr, bool expected)
        => expr.AssertAnonymousOut(expected);

    // ═══════════════════════════════════════════════════════════════
    // NaN arithmetic: any operation with NaN produces NaN
    // ═══════════════════════════════════════════════════════════════

    [Test]
    public void NaN_Plus_One_IsNaN() {
        var result = "y:real = (0.0/0.0) + 1.0".Calc().Get("y");
        Assert.IsTrue(double.IsNaN((double)result), $"Expected NaN but got {result}");
    }

    [Test]
    public void NaN_Times_Zero_IsNaN() {
        var result = "y:real = (0.0/0.0) * 0.0".Calc().Get("y");
        Assert.IsTrue(double.IsNaN((double)result), $"Expected NaN but got {result}");
    }

    [Test]
    public void Zero_Times_NaN_IsNaN() {
        var result = "y:real = 0.0 * (0.0/0.0)".Calc().Get("y");
        Assert.IsTrue(double.IsNaN((double)result), $"Expected NaN but got {result}");
    }

    [Test]
    public void NaN_Minus_NaN_IsNaN() {
        var result = "y:real = (0.0/0.0) - (0.0/0.0)".Calc().Get("y");
        Assert.IsTrue(double.IsNaN((double)result), $"Expected NaN but got {result}");
    }

    [Test]
    public void ZeroDivZero_IsNaN() {
        var result = "y:real = 0.0/0.0".Calc().Get("y");
        Assert.IsTrue(double.IsNaN((double)result), $"Expected NaN but got {result}");
    }

    // ═══════════════════════════════════════════════════════════════
    // Infinity arithmetic
    // ═══════════════════════════════════════════════════════════════

    [Test]
    public void OneDivZero_IsPositiveInfinity() {
        var result = "y:real = 1.0/0.0".Calc().Get("y");
        Assert.IsTrue(double.IsPositiveInfinity((double)result), $"Expected +Infinity but got {result}");
    }

    [Test]
    public void NegOneDivZero_IsNegativeInfinity() {
        var result = "y:real = -1.0/0.0".Calc().Get("y");
        Assert.IsTrue(double.IsNegativeInfinity((double)result), $"Expected -Infinity but got {result}");
    }

    [Test]
    public void Infinity_Plus_Infinity_IsInfinity() {
        var result = "y:real = (1.0/0.0) + (1.0/0.0)".Calc().Get("y");
        Assert.IsTrue(double.IsPositiveInfinity((double)result), $"Expected +Infinity but got {result}");
    }

    [Test]
    public void Infinity_Minus_Infinity_IsNaN() {
        var result = "y:real = (1.0/0.0) - (1.0/0.0)".Calc().Get("y");
        Assert.IsTrue(double.IsNaN((double)result), $"Expected NaN but got {result}");
    }

    [Test]
    public void Infinity_Times_Zero_IsNaN() {
        var result = "y:real = (1.0/0.0) * 0.0".Calc().Get("y");
        Assert.IsTrue(double.IsNaN((double)result), $"Expected NaN but got {result}");
    }

    // ═══════════════════════════════════════════════════════════════
    // Infinity comparisons
    // ═══════════════════════════════════════════════════════════════

    [TestCase("(1.0/0.0) > 1000000.0",        true)]  // Infinity > large number
    [TestCase("(-1.0/0.0) < -1000000.0",      true)]  // -Infinity < large negative
    [TestCase("(1.0/0.0) == (1.0/0.0)",        true)]  // Infinity == Infinity
    [TestCase("(-1.0/0.0) == (-1.0/0.0)",     true)]  // -Infinity == -Infinity
    [TestCase("(1.0/0.0) != (-1.0/0.0)",      true)]  // Infinity != -Infinity
    [TestCase("(1.0/0.0) > (-1.0/0.0)",       true)]  // Infinity > -Infinity
    [TestCase("(-1.0/0.0) < (1.0/0.0)",       true)]  // -Infinity < Infinity
    public void Infinity_Comparisons(string expr, bool expected)
        => expr.AssertAnonymousOut(expected);

    // ═══════════════════════════════════════════════════════════════
    // Special values in functions
    // ═══════════════════════════════════════════════════════════════

    [Test]
    public void Abs_NaN_IsNaN() {
        var result = "y:real = abs(0.0/0.0)".Calc().Get("y");
        Assert.IsTrue(double.IsNaN((double)result), $"Expected NaN but got {result}");
    }

    // .NET max/min with NaN: Math.Max(NaN, 1.0) = NaN, but
    // NFun's max/min use IComparable.CompareTo which treats NaN as smallest.
    // After our fix, NaN comparisons return false, so max(NaN, x) returns x
    // and min(NaN, x) returns NaN. This matches Math.Max/Min behavior only
    // partially. We test the actual behavior here.
    [Test]
    public void Max_NaN_And_Value() {
        // NaN propagates through max: max(NaN, x) = NaN (consistent with NaNBehaviorTest)
        var result = "y:real = max(0.0/0.0, 1.0)".Calc().Get("y");
        var d = (double)result;
        Console.WriteLine($"max(NaN, 1.0) = {d} (IsNaN: {double.IsNaN(d)})");
        Assert.IsTrue(double.IsNaN(d), $"max(NaN, 1.0) should return NaN, got {d}");
    }

    [Test]
    public void Min_NaN_And_Value() {
        var result = "y:real = min(0.0/0.0, 1.0)".Calc().Get("y");
        var d = (double)result;
        Console.WriteLine($"min(NaN, 1.0) = {d} (IsNaN: {double.IsNaN(d)})");
        // min: left.CompareTo(right) > 0 ? b : a
        // NaN.CompareTo(1.0) = -1, !(−1 > 0), returns a = NaN
        Assert.IsTrue(double.IsNaN(d), $"min(NaN, 1.0) should return NaN (NaN is treated as smallest by IComparable), got {d}");
    }

    // ═══════════════════════════════════════════════════════════════
    // Comparison chains with NaN
    // ═══════════════════════════════════════════════════════════════

    [TestCase("1.0 < 2.0 < (0.0/0.0)",        false)] // chain breaks at NaN
    [TestCase("(0.0/0.0) < 1.0 < 2.0",        false)] // chain starts with NaN
    [TestCase("1.0 < (0.0/0.0) < 3.0",        false)] // NaN in middle of chain
    public void NaN_InComparisonChain_ReturnsFalse(string expr, bool expected)
        => expr.AssertAnonymousOut(expected);

    // ═══════════════════════════════════════════════════════════════
    // NaN in conditional expressions
    // ═══════════════════════════════════════════════════════════════

    [TestCase("if ((0.0/0.0) > 0.0) 1 else 2", 2)]    // NaN > 0 is false
    [TestCase("if ((0.0/0.0) < 0.0) 1 else 2", 2)]    // NaN < 0 is false
    [TestCase("if ((0.0/0.0) == 0.0) 1 else 2", 2)]   // NaN == 0 is false
    public void NaN_InConditional_TakesElseBranch(string expr, int expected)
        => expr.AssertAnonymousOut(expected);

    // ═══════════════════════════════════════════════════════════════
    // NaN equality and comparison
    // ═══════════════════════════════════════════════════════════════

    [Test]
    public void NaN_EqualsSelf_ReturnsFalse() {
        "out = (0.0/0.0) == (0.0/0.0)".AssertReturns("out", false);
    }

    [Test]
    public void NaN_LessThan_ReturnsFalse() {
        // IComparable.CompareTo treats NaN as smallest, but IEEE 754 says false
        "out = (0.0/0.0) < 1.0".AssertReturns("out", false);
    }

    [Test]
    public void MaxNaN_ShouldPropagateNaN() {
        "out = max(0.0/0.0, 1.0)".Calc().AssertResultHas("out", double.NaN);
    }
}
