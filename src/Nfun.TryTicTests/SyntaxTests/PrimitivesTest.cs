using NFun;
using NFun.Types;
using NUnit.Framework;

namespace Nfun.TryTicTests.SyntaxTests
{
    public class PrimitivesTest
    {
        [TestCase("[1,2,3]", new []{1.0,2.0,3.0})]
        [TestCase("1", 1.0)]
        [TestCase("0x1", 1)]
        [TestCase("true", true)]
        [TestCase("2*3", 6.0)]
        [TestCase("true == false", false)]
        [TestCase("if (2<3) true else false", true)]
        public void AnonymousExpressionConstantEquatation(string expr, object expected)
        {
            var runtime = TestTools.Build(expr);
            runtime.Calculate().AssertReturns(VarVal.New("out", expected));
        }

        [Ignore("generics")]
        [TestCase("y(x) = x*2 \r y(3.0) * y(4.0)", 48.0)]
        [TestCase("y(x) = x \r y(3.0)", 3.0)]
        [TestCase("y(x) = x*2 \r y(3.0)  \r z(j) = j*j", 6.0)]
        public void AnonymousExpressionConstantEquatationWithUserFunctions(string expr, object expected)
        {
            var runtime = TestTools.Build(expr);
            runtime.Calculate().AssertReturns(VarVal.New("out", expected));
        }

    }
}