using System;
using NFun.Exceptions;
using NFun.TestTools;
using NUnit.Framework;

namespace NFun.SyntaxTests;

[TestFixture]
public class MathSurgarTest {

    // ── π constant ──────────────────────────────────────────────────────────────

    [Test]
    public void Pi_Constant()
        => "y = π".AssertReturns(Math.PI);

    [Test]
    public void Pi_InExpression()
        => "y = 2*π".AssertReturns(2 * Math.PI);

    [Test]
    public void Pi_ImplicitMultiplication()
        => "y = 2π".AssertReturns(2 * Math.PI);

    [Test]
    public void Pi_Comparison()
        => "y = π > 3 and π < 4".AssertReturns(true);

    [Test]
    public void Pi_InFunctionCall()
        => "y = round(π, 0)".AssertReturns(3.0);

    // ── ∞ constant ──────────────────────────────────────────────────────────────

    [Test]
    public void Infinity_Constant() {
        var runtime = "y = ∞".Build();
        runtime.Run();
        Assert.AreEqual(double.PositiveInfinity, runtime["y"].Value);
    }

    [Test]
    public void NegativeInfinity() {
        var runtime = "y = -∞".Build();
        runtime.Run();
        Assert.AreEqual(double.NegativeInfinity, runtime["y"].Value);
    }

    [Test]
    public void Infinity_GreaterThanAnyNumber()
        => "y = ∞ > 1000000".AssertReturns(true);

    [Test]
    public void NegativeInfinity_LessThanAnyNumber()
        => "y = -∞ < -1000000".AssertReturns(true);

    [Test]
    public void Infinity_ImplicitMultiplication() {
        var runtime = "y = 2∞".Build();
        runtime.Run();
        Assert.AreEqual(double.PositiveInfinity, runtime["y"].Value);
    }

    [Test]
    public void Infinity_InArithmetic()
        => "y = ∞ + 1 == ∞".AssertReturns(true);

    // ── ≤ ≥ ≠ operator aliases ──────────────────────────────────────────────────

    [TestCase("y = 3 ≤ 5", true)]
    [TestCase("y = 5 ≤ 3", false)]
    [TestCase("y = 3 ≤ 3", true)]
    public void LessOrEqual(string expr, bool expected)
        => expr.AssertReturns(expected);

    [TestCase("y = 5 ≥ 3", true)]
    [TestCase("y = 3 ≥ 5", false)]
    [TestCase("y = 3 ≥ 3", true)]
    public void GreaterOrEqual(string expr, bool expected)
        => expr.AssertReturns(expected);

    [TestCase("y = 3 ≠ 5", true)]
    [TestCase("y = 3 ≠ 3", false)]
    public void NotEqual(string expr, bool expected)
        => expr.AssertReturns(expected);

    [Test]
    public void UnicodeOperators_MixedWithRegular()
        => "y = 3 ≤ x and x ≤ 10".Calc("x", 5.0).AssertReturns(true);

    [Test]
    public void UnicodeOperators_WithPi()
        => "y = π ≤ 4".AssertReturns(true);

    [Test]
    public void UnicodeOperators_InfNotEqualZero()
        => "y = ∞ ≠ 0".AssertReturns(true);

    // ── Superscript power ²³⁴⁵⁶⁷⁸⁹ ─────────────────────────────────────────────

    [TestCase("y = 3²", 9)]
    [TestCase("y = 2³", 8)]
    [TestCase("y = 10²", 100)]
    [TestCase("y = 2⁴", 16)]
    [TestCase("y = 2⁵", 32)]
    [TestCase("y = 2⁶", 64)]
    [TestCase("y = 2⁷", 128)]
    [TestCase("y = 2⁸", 256)]
    [TestCase("y = 2⁹", 512)]
    public void Superscript_IntegerBase(string expr, int expected)
        => expr.AssertReturns(expected);

    [Test]
    public void Superscript_RealBase()
        => "y = 2.5²".AssertReturns(6.25);

    [Test]
    public void Superscript_Variable()
        => "y = x²".Calc("x", 5).AssertReturns(25);

    [Test]
    public void Superscript_VariableCubed()
        => "y = x³".Calc("x", 3).AssertReturns(27);

    [Test]
    public void Superscript_InPolynomial()
        => "y = x² + x + 1".Calc("x", 3).AssertReturns(13);

    [Test]
    public void Superscript_WithImplicitMultiplication()
        => "y = 2x²".Calc("x", 3).AssertReturns(18);

    [Test]
    public void Superscript_ConsecutiveDigits_Error()
        => Assert.Throws<FunnyParseException>(() => "y = 2²³".Build());

    [Test]
    public void Superscript_HigherPriorityThanMultiply()
        // x² * 2 should be (x²) * 2, not x^(2*2)
        => "y = x² * 2".Calc("x", 3).AssertReturns(18);

    [Test]
    public void Superscript_HigherPriorityThanAddition()
        => "y = 1 + x²".Calc("x", 3).AssertReturns(10);

    [Test]
    public void Superscript_OnParenthesizedExpr()
        => "y = (x + 1)²".Calc("x", 2).AssertReturns(9);

    // ── Combined usage ──────────────────────────────────────────────────────────

    [Test]
    public void CircleArea()
        // A = π*r²
        => "y = π * r²".Calc("r", 1.0).AssertReturns(Math.PI);

    [Test]
    public void CircleArea_ImplicitMult()
        // 2π works (number + identifier), but πr is one identifier
        => "y = 2π * r²".Calc("r", 1.0).AssertReturns(2 * Math.PI);

    [Test]
    public void QuadraticFormula()
        // discriminant = b² - 4ac
        => "y = b² - 4*a*c".Calc(("a", 1), ("b", 5), ("c", 6)).AssertReturns(1);

      [TestCase("10x", 2, 20)]
    [TestCase("1x", 2, 2)]
    [TestCase("1_0x", 2, 20)]
    [TestCase("-1_0x", 2, -20)]
    [TestCase("0.1x", 2, 0.2)]
    [TestCase("-0.1x", 2.0, -0.2)]
    [TestCase("-1x", 2, -2)]
    [TestCase("1.0x", 2.0, 2.0)]
    [TestCase("1.5x", 2.0, 3.0)]
    [TestCase("-1.0x", 2.0, -2.0)]

    [TestCase("10(x)", 2, 20)]
    [TestCase("1(x)", 2, 2)]
    [TestCase("1_0(x)", 2, 20)]
    [TestCase("-1_0(x)", 2, -20)]
    [TestCase("0(x)", 2, 0)]
    [TestCase("0.1(x)", 2.0, 0.2)]
    [TestCase("-0.1(x)", 2.0, -0.2)]
    [TestCase("-1(x)", 2, -2)]
    [TestCase("1.0(x)", 2.0, 2.0)]
    [TestCase("1.5(x)", 2.0, 3.0)]
    [TestCase("-1.0(x)", 2.0, -2.0)]

    [TestCase("10(x+1)", 2, 30)]
    [TestCase("1(x+1)", 2, 3)]
    [TestCase("1_0(x+1)", 2, 30)]
    [TestCase("-1_0(x+1)", 2, -30)]
    [TestCase("0(x+1)", 2, 0)]
    [TestCase("0.1(x+1)", 2.0, 0.3)]
    [TestCase("-0.1(x+1)", 2.0, -0.3)]
    [TestCase("-1(x+1)", 2, -3)]
    [TestCase("1.0(x+1)", 2.0, 3.0)]
    [TestCase("1.5(x+1)", 2.0, 4.5)]
    [TestCase("-1.0(x+1)", 2.0, -3.0)]
    public void SingleXVariableEquation(string expr, object arg, object expected) =>
        expr.Calc("x", arg).AssertReturns("out", expected);

    [TestCase("10b", 2, 20)]
    [TestCase("1b", 2, 2)]
    [TestCase("1_0b", 2, 20)]
    [TestCase("-1_0b", 2, -20)]
    [TestCase("0.1b", 2, 0.2)]
    [TestCase("-0.1b", 2.0, -0.2)]
    [TestCase("-1b", 2, -2)]
    [TestCase("1.0b", 2.0, 2.0)]
    [TestCase("1.5b", 2.0, 3.0)]
    [TestCase("-1.0b", 2.0, -2.0)]

    [TestCase("10(b)", 2, 20)]
    [TestCase("1(b)", 2, 2)]
    [TestCase("1_0(b)", 2, 20)]
    [TestCase("-1_0(b)", 2, -20)]
    [TestCase("0(b)", 2, 0)]
    [TestCase("0.1(b)", 2.0, 0.2)]
    [TestCase("-0.1(b)", 2.0, -0.2)]
    [TestCase("-1(b)", 2, -2)]
    [TestCase("1.0(b)", 2.0, 2.0)]
    [TestCase("1.5(b)", 2.0, 3.0)]
    [TestCase("-1.0(b)", 2.0, -2.0)]

    [TestCase("10(b+1)", 2, 30)]
    [TestCase("1(b+1)", 2, 3)]
    [TestCase("1_0(b+1)", 2, 30)]
    [TestCase("-1_0(b+1)", 2, -30)]
    [TestCase("0(b+1)", 2, 0)]
    [TestCase("0.1(b+1)", 2.0, 0.3)]
    [TestCase("-0.1(b+1)", 2.0, -0.3)]
    [TestCase("-1(b+1)", 2, -3)]
    [TestCase("1.0(b+1)", 2.0, 3.0)]
    [TestCase("1.5(b+1)", 2.0, 4.5)]
    [TestCase("-1.0(b+1)", 2.0, -3.0)]
    public void SingleBVariableEquation(string expr, object arg, object expected) =>
        expr.Calc("b", arg).AssertReturns("out", expected);

    [TestCase("10z", 2, 20)]
    [TestCase("1z", 2, 2)]
    [TestCase("1_0z", 2, 20)]
    [TestCase("-1_0z", 2, -20)]
    [TestCase("0z", 2, 0)]
    [TestCase("0.1z", 2, 0.2)]
    [TestCase("-0.1z", 2.0, -0.2)]
    [TestCase("-1z", 2, -2)]
    [TestCase("1.0z", 2.0, 2.0)]
    [TestCase("1.5z", 2.0, 3.0)]
    [TestCase("-1.0z", 2.0, -2.0)]

    [TestCase("10(z)", 2, 20)]
    [TestCase("1(z)", 2, 2)]
    [TestCase("1_0(z)", 2, 20)]
    [TestCase("-1_0(z)", 2, -20)]
    [TestCase("0(z)", 2, 0)]
    [TestCase("0.1(z)", 2.0, 0.2)]
    [TestCase("-0.1(z)", 2.0, -0.2)]
    [TestCase("-1(z)", 2, -2)]
    [TestCase("1.0(z)", 2.0, 2.0)]
    [TestCase("1.5(z)", 2.0, 3.0)]
    [TestCase("-1.0(z)", 2.0, -2.0)]

    [TestCase("10(z+1)", 2, 30)]
    [TestCase("1(z+1)", 2, 3)]
    [TestCase("1_0(z+1)", 2, 30)]
    [TestCase("-1_0(z+1)", 2, -30)]
    [TestCase("0(z+1)", 2, 0)]
    [TestCase("0.1(z+1)", 2.0, 0.3)]
    [TestCase("-0.1(z+1)", 2.0, -0.3)]
    [TestCase("-1(z+1)", 2, -3)]
    [TestCase("1.0(z+1)", 2.0, 3.0)]
    [TestCase("1.5(z+1)", 2.0, 4.5)]
    [TestCase("-1.0(z+1)", 2.0, -3.0)]

    [TestCase("1+ 10z", 2, 21)]
    [TestCase("1+ 1z", 2, 3)]
    [TestCase("1+ 1_0z", 2, 21)]
    [TestCase("1 -1_0z", 2, -19)]
    [TestCase("1+ 0z", 2, 1)]
    [TestCase("1+ 0.1z", 2, 1.2)]
    [TestCase("1+ -0.1z", 2.0, 0.8)]
    [TestCase("1-1z", 2, -1)]
    [TestCase("1+ 1.0z", 2.0, 3.0)]
    [TestCase("1+ 1.5z", 2.0, 4.0)]
    [TestCase("1 -1.0z", 2.0, -1.0)]
    public void SinglezVariableEquation(string expr, object arg, object expected) =>
        expr.Calc("z", arg).AssertReturns("out", expected);

    // Scientific notation + implicit multiplication interaction
    [TestCase("1e2x", 3.0, 300.0)]   // 1e2 * x = 100 * 3
    [TestCase("1.5e2x", 2.0, 300.0)] // 1.5e2 * x = 150 * 2
    [TestCase("1e2(x)", 3.0, 300.0)]
    [TestCase("1e2(x+1)", 3.0, 400.0)]
    public void ScientificNotationWithImplicitMultiplication(string expr, object arg, object expected) =>
        expr.Calc("x", arg).AssertReturns("out", expected);

    // Parenthesized function call is OK (not direct implicit mult before func)
    [TestCase("2(sin(1.0))", 2 * 0.8414709848078965)]
    [TestCase("2.0(max(3,5))", 10.0)]
    public void ParenthesizedFunctionCallIsAllowed(string expr, object expected) =>
        expr.Calc().AssertReturns("out", expected);

    // Multiple implicit multiplications in one expression
    [TestCase("y = 2x + 3x", 5, 25)]
    public void MultipleImplicitMultiplications(string expr, object arg, object expected) =>
        expr.Calc("x", arg).AssertReturns("y", expected);

    [TestCase("12.0.0.1x")]
    [TestCase("x = 0x10y10")]
    [TestCase("x = 0x10y")]
    [TestCase("x = 0b10y10")]
    [TestCase("x = 0b10b")]
    [TestCase("0b")]
    [TestCase("0x")]
    // Whitespace prevents implicit multiplication
    [TestCase("2 x")]
    [TestCase("2 10")]
    [TestCase("2 (10)")]
    [TestCase("2 (max(1,2))")]
    // Implicit multiplication before function call is forbidden
    [TestCase("2sin(1.0)")]
    [TestCase("2max(1,2)")]
    [TestCase("10abs(-5)")]
    [TestCase("1.5sin(1.0)")]
    [TestCase("1e2sin(1.0)")]
    [TestCase("-2sin(1.0)")]
    public void ObviouslyFails(string expr) => expr.AssertObviousFailsOnParse();
}
