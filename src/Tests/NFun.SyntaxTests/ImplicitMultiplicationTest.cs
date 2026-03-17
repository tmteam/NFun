namespace NFun.SyntaxTests;

using NUnit.Framework;
using TestTools;

public class ImplicitMultiplicationTest {
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
