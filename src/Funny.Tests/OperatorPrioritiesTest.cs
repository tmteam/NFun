using System.Collections.Generic;
using Funny.Interpritation;
using Funny.Runtime;
using NUnit.Framework;

namespace Funny.Tests
{
    [TestFixture]
    public class OperatorPrioritiesTest
    {
        [TestCase("y=x1 and x2 ==  x3","y=x1 and (x2==x3)")]
        [TestCase("y=x1 ==  x2 and x3","y=(x1==x2) and x3")]

        [TestCase("y=x1 and x2 or  x3","y=(x1 and x2) or x3")]
        [TestCase("y=x1 or  x2 and x3","y=x1 or (x2 and x3)")]
        
        [TestCase("y=x1  <> x2  or x3","y=(x1<>x2) or x3")]
        [TestCase("y=x1  or x2  <> x3","y=x1 or (x2<>x3)")]

        [TestCase("y=x1  <> x2 and x3","y=(x1<>x2) and x3")]
        [TestCase("y=x1 and x2  <> x3","y=x1 and (x2<>x3)")]

        [TestCase("y=x1  == x2 or  x3","y=(x1==x2) or  x3")]
        [TestCase("y=x1  or x2  == x3","y=x1  or (x2==x3)")]
        
        [TestCase("y=x1  == x2 <>  x3","y=(x1==x2) <>  x3")]
        [TestCase("y=x1  <> x2  == x3","y=(x1<>x2)  == x3")]

        public void DiscreetePriorities(string actualExpr, string expectedExpr)
        {
            var allCombinations = new List<Var[]>();
            foreach (var x1 in new[] {0, 1})
            foreach (var x2 in new[] {0, 1})
            foreach (var x3 in new[] {0, 1})
                allCombinations.Add(new[]{
                    Var.New("x1",x1),
                    Var.New("x2",x2),
                    Var.New("x3",x3)});

            Assert.Multiple(()=>{
                foreach (var inputs in allCombinations)
                {
                    var actual = Interpriter
                        .BuildOrThrow(actualExpr)
                        .Calculate(inputs)
                        .GetResultOf("y");

                    var expected = Interpriter
                        .BuildOrThrow(expectedExpr)
                        .Calculate(inputs)
                        .GetResultOf("y");

                    if (actual != expected)
                        Assert.Fail($"On x1={inputs[0].Value} x2={inputs[1].Value} x3={inputs[2].Value}\r" +
                                    $"Eq: {actualExpr}\r" +
                                    $"Expected: {expected}\r" +
                                    $"But was: {actual} ");
                }
            });
        }
       
        [TestCase("y = 1+2*3",  "y = 1+(2*3)")]
        [TestCase("y = 2*3+1",  "y = (2*3)+1")]
        [TestCase("y = 1+4/2",  "y = 1+(4/2)")]
        [TestCase("y = 4/2+1",  "y = (4/2)+1")]
        [TestCase("y = 5*4/2",  "y = (5*4)/2")]
        [TestCase("y = 4/2*5",  "y = (4/2)*5")]
        [TestCase("y = 2^3*4",  "y = (2^3)*4")]
        [TestCase("y = 4*2^3",  "y = 4*(2^3)")]
        [TestCase("y = 2^3/4",  "y = (2^3)/4")]
        [TestCase("y = 4/2^3",  "y = 4/(2^3)")]
        [TestCase("y = 2^3+4",  "y = (2^3)+4")]
        [TestCase("y = 4+2^3",  "y = 4+(2^3)")]
        public void ArithmeticPriorities(string actualExpr, string expectedExpr)
        {
            var expected = Interpriter.BuildOrThrow(expectedExpr).Calculate().GetResultOf("y");
            
            Interpriter
                .BuildOrThrow(actualExpr)
                .Calculate()
                .AssertReturns(Var.New("y", expected));
        }
        
    }
}