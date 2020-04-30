using NFun;
using NFun.ParseErrors;
using Nfun.TryTicTests.SyntaxTests;
using NFun.Types;
using NUnit.Framework;

namespace Funny.Tests
{
    [TestFixture]
    public class CaseSensivityTest
    {
        
        [Test]
        public void DependentVariableEquations()
        {
            var runtime = FunBuilder.BuildDefault("yPub = 2\r y2 = 3 +yPub");
            runtime.Calculate()
                .AssertReturns(
                    VarVal.New("yPub", 2),
                    VarVal.New("y2", 5));
        }
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