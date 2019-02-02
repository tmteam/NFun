using Funny.Interpritation;
using Funny.Runtime;
using Funny.Tokenization;
using NUnit.Framework;

namespace Funny.Tests
{
    [TestFixture]
    public class EquatationsFormattingTest
    {
        [Test]
        public void SeveralLinesBeforeSingleEquatation_Calculates()
        {
            var runtime = Interpriter.BuildOrThrow(
                @"
                
                y = 1");
            runtime.Calculate().AssertReturns(Var.New("y",1));
        }
        
        [Test]
        public void SeveralLinesAfterуEqual_Calculates()
        {
            var runtime = Interpriter.BuildOrThrow("y =\r\r 1");
            runtime.Calculate().AssertReturns(Var.New("y",1));
        }
        
        [TestCase("y = \r1",1)]
        [TestCase("y = 2\r*3",6)]
        [TestCase("y = 2*\r3",6)]
        [TestCase("y = 2\r+3",5)]
        [TestCase("y = 2+\r3",5)]
        [TestCase("y = 2\r+3",5)]
        [TestCase("y = 2+3\r",5)]
        [TestCase("y = \r(2+3)",5)]
        [TestCase("y = (\r2+3)",5)]
        [TestCase("y = (2+3\r)",5)]
        [TestCase("y = \r(\r2\r+\r3\r)*(\r3\r)",15)]

        public void SeveralLinesBetweenNodes_Calculates(string expr, double expected)
        {
            var runtime = Interpriter.BuildOrThrow(expr);
            runtime.Calculate().AssertReturns(Var.New("y",expected));
        }
        
        [Test]
        public void SeveralLinesAfterSingleEquatation_Calculates()
        {
            var runtime = Interpriter.BuildOrThrow(
                @"y = 1

                ");
            runtime.Calculate().AssertReturns(Var.New("y",1));
        }
        
        [Test]
        public void SeveralLinesBetweenEveryStatement_Calculates()
        {
            var runtime = Interpriter.BuildOrThrow(
                @"
                    y 
                    
                    = 

                    1

                ");
            runtime.Calculate().AssertReturns(Var.New("y",1));
        }
        
        [Test]
        public void TabulationEverywhere_Calculates()
        {
            var runtime = Interpriter.BuildOrThrow("\t\ty\t\t=\t\t1\t\t");
            runtime.Calculate().AssertReturns(Var.New("y",1));
        }
        
        /* TODO: Is it an error?
        [Test]
        public void TwoEquatationsOnOneLineFails()
        {
            Assert.Throws<ParseException>(()=> Interpriter.BuildOrThrow("y=1 z=5"));
        }*/
    }
}