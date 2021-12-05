using NFun.TestTools;
using NUnit.Framework;

namespace NFun.SyntaxTests.BuiltInFunctions {

class TextFunctionsTest {
    [TestCase("y = ' hi world '.trim()", "hi world")]
    [TestCase("y = ' hi world'.trim()", "hi world")]
    [TestCase("y = 'hi world  '.trim()", "hi world")]
    [TestCase("y = '  hi world  '.trim()", "hi world")]
    [TestCase("y = 'hi world'.trim()", "hi world")]
    [TestCase("y = ' hi world '.trimStart()", "hi world ")]
    [TestCase("y = ' hi world'.trimStart()", "hi world")]
    [TestCase("y = 'hi world  '.trimStart()", "hi world  ")]
    [TestCase("y = '  hi world  '.trimStart()", "hi world  ")]
    [TestCase("y = 'hi world'.trim()", "hi world")]
    [TestCase("y = ' hi world '.trimEnd()", " hi world")]
    [TestCase("y = ' hi world'.trimEnd()", " hi world")]
    [TestCase("y = 'hi world  '.trimEnd()", "hi world")]
    [TestCase("y = ' hi world '.trimStart().trimEnd().split(' ')", new[] { "hi", "world" })]
    [TestCase("y = '  hi world  '.trimEnd()", "  hi world")]
    [TestCase("y = 'hi world'.trim()", "hi world")]
    [TestCase("y = 'HI WoRld'.toUpper()", "HI WORLD")]
    [TestCase("y = 'HI WoRld'.toLower()", "hi world")]
    public void ConstantEquationWithGenericPredefinedFunction(string expr, object expected) =>
        expr.AssertReturns("y", expected);
}

}