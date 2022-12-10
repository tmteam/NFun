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
}
