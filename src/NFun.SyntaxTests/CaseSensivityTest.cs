using NFun.TestTools;
using NFun.Types;
using NUnit.Framework;

namespace NFun.SyntaxTests
{
    [TestFixture]
    public class CaseSensivityTest
    {
        [TestCase("Y(x) = x*2 \r Y(3.0) * Y(4.0)", 48.0)]
        [TestCase("y(X) = X \r y(3.0)", 3.0)]
        [TestCase("teastyVar(x) = x \r  teastyVAR(x,y) =x+y\r teastyVAR(3.0,4.0)", 7.0)]
        [TestCase("testFun(x) = x \r testFun(3.0)", 3.0)]
        [TestCase("y(x) = x*2 \r y(3.0)  \r z(jamboJet) = jamboJet*jamboJet", 6.0)]
        public void ConstantEquatation(string expr, object expected) 
            => FunBuilder.Build(expr).Calculate().AssertOutEquals(expected);

        [Test]
        public void DependentVariableEquations()
        {
            var runtime = FunBuilder.Build("yPub = 2\r y2 = 3 +yPub");
            runtime.Calculate()
                .AssertReturns(
                    VarVal.New("yPub", 2.0),
                    VarVal.New("y2", 5.0));
        }

        [TestCase("[1.0].fold((X,x)->x)")]
        [TestCase("test = 2.0\r tESt = 3.0")]
        [TestCase("test = Sin(0.5)")]
        [TestCase("test = x * X")]
        [TestCase("test = xin * xIn")]
        [TestCase("x = X")]
        [TestCase("x = X+ y")]
        [TestCase("test = Test + tEst")]

        public void ObviouslyFails(string expr) => TestHelper.AssertObviousFailsOnParse(expr);
    }
}