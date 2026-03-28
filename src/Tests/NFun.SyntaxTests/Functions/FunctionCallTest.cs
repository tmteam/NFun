namespace NFun.SyntaxTests.Functions;

using NFun.TestTools;
using NUnit.Framework;

public class FunctionCallTest {
    [TestCase("f(x)= x; (f(42))", 42)]
    [TestCase("f(x)= x; (f(42.0))", 42.0)]
    [TestCase("f(x)= (x); (f(42))", 42)]
    [TestCase("f()= (2); (1)", 1)]
    public void ConstantEquation(string expr, object expected) => expr.AssertAnonymousOut(expected);

}
