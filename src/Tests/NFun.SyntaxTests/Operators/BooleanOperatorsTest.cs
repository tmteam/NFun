using NFun.TestTools;
using NUnit.Framework;

namespace NFun.SyntaxTests.Operators;

public class BooleanOperatorsTest {
    [TestCase("y = true", true)]
    [TestCase("y = false", false)]
    [TestCase("y = true and true", true)]
    [TestCase("y = true and false", false)]
    [TestCase("y = false and true", false)]
    [TestCase("y = false and false", false)]
    [TestCase("y = true or true", true)]
    [TestCase("y = true or false", true)]
    [TestCase("y = false or true", true)]
    [TestCase("y = false or false", false)]
    [TestCase("y = true xor true", false)]
    [TestCase("y = true xor false", true)]
    [TestCase("y = false xor true", true)]
    [TestCase("y = false xor false", false)]
    [TestCase("y = not true", false)]
    [TestCase("y = not false", true)]
    [TestCase("y = not not false", false)]
    [TestCase("y = not not true", true)]
    [TestCase("y = false or not false", true)]
    public void ConstantBoolCalc(string expression, bool expected)
        => expression.AssertReturns("y", expected);

    [TestCase("y = not π")]
    [TestCase("y = not ∞")]
    [TestCase("y = not 3.14")]
    [TestCase("y = not 42")]
    [TestCase("y = π and true")]
    [TestCase("y = ∞ or false")]
    public void BooleanOperatorsRejectNonBoolConstants(string expr)
        => expr.AssertObviousFailsOnParse();

    // ═══════════════════════════════════════════════════════════════
    // Boolean operators on numeric types — type error
    // ═══════════════════════════════════════════════════════════════

    [TestCase("y = if(42) 1 else 2")]
    [TestCase("y = if(0) 'a' else 'b'")]
    public void BoolOperatorOnNumeric_GivesTypeError(string expr) =>
        expr.AssertObviousFailsOnParse();

    [Test]
    public void LogicalOnInt_TypeError() {
        Assert.Throws<NFun.Exceptions.FunnyParseException>(() => "y = 1 and 2".Calc());
    }

    [Test]
    public void IntLiteralInIfCondition_TypeError() {
        Assert.Throws<NFun.Exceptions.FunnyParseException>(
            () => "y = if(42) true else false".Calc());
    }

    [Test]
    public void IncompatibleAnnotation_TypeError() {
        Assert.Throws<NFun.Exceptions.FunnyParseException>(() => "y:bool = 42".Calc());
    }
}
