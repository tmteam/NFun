using System;
using NFun.TestTools;
using NUnit.Framework;

namespace NFun.SyntaxTests; 

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

    [TestCase("[1,]", new []{1})]
    [TestCase("sqrt(4)", 2.0)]
    [TestCase("foo(a,) = -a; foo(1)", -1)]
    [TestCase("foo(a) = -a; foo(1,)", -1)]
    [TestCase("{a=42,}.a", 42)]
    [TestCase("foo() = rule it; foo()(2,)",2)]
    
    [TestCase("[1,2,3,]", new []{1,2,3})]
    [TestCase("max(4,2,)", 4)]
    [TestCase("foo(a,b,) = a+b; foo(1,2)", 3)]
    [TestCase("foo(a,b) = a+b; foo(1,2,)", 3)]
    [TestCase("{a=42,b=12,}.a", 42)]
    [TestCase("foo() = rule it1*it2; foo()(2,4,)",8)]
    [TestCase("foo() = rule(a,b)= a*b; foo()(2,4,)",8)]
    [TestCase("[0,1,2,3].fold(rule(a,b,) =a+b)",6)]
    public void TrailingComaInTheList(string expr, object expected) => expr.AssertAnonymousOut(expected);

    [TestCase("{,}")]
    [TestCase("foo(,) = 42; foo()")]
    [TestCase("foo() = 42; foo(,)")]
    [TestCase("[1,2,3,,]")]
    [TestCase("max(4,2,,)")]
    [TestCase("foo(a,b,,) = a+b; foo(1,2)")]
    [TestCase("foo(a,b) = a+b; foo(1,2,,)")]
    [TestCase("{a=42,b=12,,}.a")]
    [TestCase("foo() = rule it1*it2; foo()(2,4,,)")]
    [TestCase("y = [0,1,2,3].fold(rule(,) =a+b)")]
    public void TrailingComaInTheListFails(string expr) => expr.AssertObviousFailsOnParse();

    
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
    public void TwoEquationsOnOneLine() =>
        Assert.DoesNotThrow(() => "y=1; z=5".Build());
    
    [TestCase("y=1 z=5")]
    public void ObviousFails(String expr) =>
        expr.AssertObviousFailsOnParse();
}