using NFun.TestTools;
using NUnit.Framework;

namespace NFun.SyntaxTests {

[TestFixture]
public class EquationsFormattingTest {
    [Test]
    public void SeveralLinesBeforeSingleEquation_Calculates() =>
        @"
                
                y = 1.0".AssertReturns("y", 1.0);

    [Test]
    public void SeveralLinesAfterуEqual_Calculates() => "y =\r\r 1.0".AssertReturns("y", 1.0);

    [TestCase("y:int = \r1", 1)]
    [TestCase("y:int = 2\r*3", 6)]
    [TestCase("y:int = 2*\r3", 6)]
    [TestCase("y:int = 2\r+3", 5)]
    [TestCase("y:int = 2+\r3", 5)]
    [TestCase("y:int = 2\r+3", 5)]
    [TestCase("y:int = 2+3\r", 5)]
    [TestCase("y:int = \r(2+3)", 5)]
    [TestCase("y:int = (\r2+3)", 5)]
    [TestCase("y:int = (2+3\r)", 5)]
    [TestCase("y:int = \r(\r2\r+\r3\r)*(\r3\r)", 15)]
    [TestCase("y:int = ;1", 1)]
    [TestCase("y:int = 2;*3", 6)]
    [TestCase("y:int = 2*;3", 6)]
    [TestCase("y:int = 2;+3", 5)]
    [TestCase("y:int = 2+;3", 5)]
    [TestCase("y:int = 2;+3", 5)]
    [TestCase("y:int = 2+3;", 5)]
    [TestCase("y:int = ;(2+3)", 5)]
    [TestCase("y:int = (;2+3)", 5)]
    [TestCase("y:int = (2+3;)", 5)]
    [TestCase("y:int = ;(;2;+;3;)*(;3;)", 15)]
    public void SeveralLinesBetweenNodes_Calculates(string expr, int expected) => expr.AssertReturns("y", expected);

    [TestCase(
        @"y:int = 1

                ")]
    [TestCase(@"y:int = 1;;;;")]
    public void SeveralLinesAfterSingleEquation_Calculates(string expr) => expr.AssertReturns("y", 1);

    [Test]
    public void SeveralLinesBetweenEveryStatement_Calculates() =>
        @"
                    y :int
                    
                    = 

                    1

                ".AssertReturns("y", 1);

    [TestCase("\t\ty\t\t=\t\t0x1\t\t")]
    [TestCase("\t\ty\t:int\t=\t\t1\t\t")]
    [TestCase("\ty\t:int\t=\t1  ")]
    [TestCase(";;y:int;=;;1;;")]
    public void TabulationEverywhere_Calculates(string expr) => expr.AssertReturns("y", 1);

    [Test]
    public void TwoEquationsOnOneLineFails() =>
        Assert.DoesNotThrow(() => "y=1; z=5".Build());
}

}