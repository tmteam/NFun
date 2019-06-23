using NFun;
using NFun.ParseErrors;
using NFun.Types;
using NUnit.Framework;

namespace Funny.Tests
{
    [TestFixture]
    public class CaseSensivityTest
    {
        [TestCase("Y(x) = x*2.0 \r Y(3.0) * Y(4.0)", 48.0)]
        [TestCase("y(X) = X \r y(3.0)",3.0)]
        [TestCase("teastyVar(x) = x \r  teastyVAR(x,y) =x+y\r teastyVAR(3.0,4.0)",7.0)]
        [TestCase("testFun(x) = x \r testFun(3.0)",3.0)]
        [TestCase("y(x) = x*2.0 \r y(3.0)  \r z(jamboJet) = jamboJet*jamboJet",6.0)]
        public void ConstantEquatation(string expr, object expected)
        {
            var runtime = FunBuilder.BuildDefault(expr);
            runtime.Calculate().AssertReturns(Var.New("out", expected));
        }
        [Test]
        public void DependentVariableEquations()
        {
            var runtime = FunBuilder.BuildDefault("yPub = 2\r y2 = 3 +yPub");
            runtime.Calculate()
                .AssertReturns(
                    Var.New("yPub", 2),
                    Var.New("y2", 5));
        }
        [TestCase("y(X) = x \r y(3.0)")]
        [TestCase("y(X,x) = x \r y(3.0,4.0)")]
        [TestCase("y(z,x) = x+X \r y(3.0,4.0)")]
        [TestCase("[1.0].fold((X,x)->x)")]
        [TestCase("test = 2.0\r tESt = 3.0")]
        [TestCase("test = Sin(0.5)")]
        [TestCase("test = x * X")]
        [TestCase("test = xin * xIn")]
        [TestCase("x = X")]
        [TestCase("x = X+ y")]
        [TestCase("test = Test + tEst")]

        public void ObviouslyFails(string expr) =>
            Assert.Throws<FunParseException>(
                ()=> FunBuilder.BuildDefault(expr));
    }
}