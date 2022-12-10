using NFun.TestTools;
using NUnit.Framework;

namespace NFun.SyntaxTests;

public class ParenthesesTest {
    [TestCase("(((2)))", 2)]
    [TestCase("(((2)))*(((3)))", 6)]
    public void ConstantEquation(string expr, object expected) {
        var runtime = expr.Build();
        runtime.AssertInputsCount(0, "Unexpected inputs on constant equations");
        runtime.Calc().AssertAnonymousOut(expected);
    }

    [TestCase("x = 12; y = x()")]
    [TestCase("y = x()")]
    [TestCase("y = ()")]
    [TestCase("y = ()2")]
    [TestCase("y = 2()")]
    [TestCase("y = 2*()")]
    [TestCase("y = ()*2")]
    [TestCase("y = )")]
    [TestCase("y = )2")]
    [TestCase("y = (")]
    [TestCase("y = (2")]
    [TestCase("y = ((2)")]
    [TestCase("y = 2)")]
    [TestCase("y = x*((2)")]
    [TestCase("y = 2*x)")]
    [TestCase("y = )*2")]
    [TestCase("y = (*2")]
    [TestCase("y = (")]
    public void ObviouslyFails(string expr) => expr.AssertObviousFailsOnParse();
}
