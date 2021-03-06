using System.Collections.Generic;
using NFun.TestTools;
using NUnit.Framework;

namespace NFun.SyntaxTests.Operators
{
    [TestFixture]
    public class OperatorPrioritiesTest
    {
        [TestCase("y=(x1!=0) and (x2!=0) ==  (x3!=0)", "y=(x1!=0) and ((x2!=0)==(x3!=0))")]
        [TestCase("y=(x1!=0) ==  (x2!=0) and (x3!=0)", "y=((x1!=0)==(x2!=0)) and (x3!=0)")]

        [TestCase("y=(x1!=0) and (x2!=0) or  (x3!=0)", "y=((x1!=0) and (x2!=0)) or (x3!=0)")]
        [TestCase("y=(x1!=0) or  (x2!=0) and (x3!=0)", "y=(x1!=0) or ((x2!=0) and (x3!=0))")]

        [TestCase("y=(x1!=0)  != (x2!=0)  or (x3!=0)", "y=((x1!=0)!=(x2!=0)) or (x3!=0)")]
        [TestCase("y=(x1!=0)  or (x2!=0)  != (x3!=0)", "y=(x1!=0) or ((x2!=0)!=(x3!=0))")]

        [TestCase("y=(x1!=0)  != (x2!=0) and (x3!=0)", "y=((x1!=0)!=(x2!=0)) and (x3!=0)")]
        [TestCase("y=(x1!=0) and (x2!=0)  != (x3!=0)", "y=(x1!=0) and ((x2!=0)!=(x3!=0))")]

        [TestCase("y=(x1!=0)  == (x2!=0) or  (x3!=0)", "y=((x1!=0)==(x2!=0)) or  (x3!=0)")]
        [TestCase("y=(x1!=0)  or (x2!=0)  == (x3!=0)", "y=(x1!=0)  or ((x2!=0)==(x3!=0))")]

        [TestCase("y=(x1!=0)  == (x2!=0) !=  (x3!=0)", "y=((x1!=0)==(x2!=0)) !=  (x3!=0)")]
        [TestCase("y=(x1!=0)  != (x2!=0)  == (x3!=0)", "y=((x1!=0)!=(x2!=0))  == (x3!=0)")]
        public void DiscreetePriorities(string actualExpr, string expectedExpr)
        {
            var allCombinations = new List<(string, object)[]>();
            foreach (var x1 in new[] {0, 1})
            foreach (var x2 in new[] {0, 1})
            foreach (var x3 in new[] {0, 1})
                allCombinations.Add(new (string, object)[]
                {
                    ("x1", x1),
                    ("x2", x2),
                    ("x3", x3)
                });

            Assert.Multiple(() =>
            {
                foreach (var inputs in allCombinations)
                {
                    var actual = Funny.Hardcore
                        .Build(actualExpr)
                        .Calc(inputs)
                        .Get("y");

                    var expected = Funny.Hardcore
                        .Build(expectedExpr)
                        .Calc(inputs)
                        .Get("y");

                    if (!actual.Equals(expected))
                        Assert.Fail($"On x1={inputs[0].Item2} x2={inputs[1].Item2} x3={inputs[2].Item2}\r" +
                                    $"Eq: {actualExpr}\r" +
                                    $"Expected: {expected}\r" +
                                    $"But was: {actual} ");
                }
            });
        }

        [TestCase("y = x1 * x2 > x3 * 2 ", "y = (x1 * x2 )> (x3 * 2)")]
        [TestCase("y = x1 + x2 * 1  > 2 * x2+x3", "y = (x1 + (x2 * 1))  > ((2 * x2)+x3)")]
        [TestCase("y = not x1*x2>x3", "y = not (x1*x2>x3)")]
        public void ArithmeticVariablePriorities(string actualExpr, string expectedExpr)
        {
            var allCombinations = new List<(string, object)[]>();
            foreach (var x1 in new[] {0.0, 1, 2})
            foreach (var x2 in new[] {0.0, 1, 2})
            foreach (var x3 in new[] {0.0, 1, 2})
                allCombinations.Add(new (string, object)[]
                {
                    ("x1", x1),
                    ("x2", x2),
                    ("x3", x3)
                });

            Assert.Multiple(() =>
            {
                foreach (var inputs in allCombinations)
                {
                    var actual = Funny.Hardcore
                        .Build(actualExpr)
                        .Calc(inputs)
                        .Get("y");

                    var expected = Funny.Hardcore
                        .Build(expectedExpr)
                        .Calc(inputs)
                        .Get("y");

                    if (!actual.Equals(expected))
                        Assert.Fail($"On x1={inputs[0].Item2} x2={inputs[1].Item2} x3={inputs[2].Item2}\r" +
                                    $"Eq: {actualExpr}\r" +
                                    $"Expected: {expected}\r" +
                                    $"But was: {actual} ");
                }
            });
        }

        [TestCase("y = 1+2*3", "y = 1+(2*3)")]
        [TestCase("y = 2*3+1", "y = (2*3)+1")]
        [TestCase("y = 1+4/2", "y = 1+(4/2)")]
        [TestCase("y = 4/2+1", "y = (4/2)+1")]
        [TestCase("y = 5*4/2", "y = (5*4)/2")]
        [TestCase("y = 4/2*5", "y = (4/2)*5")]
        [TestCase("y = 2**3*4", "y = (2**3)*4")]
        [TestCase("y = 4*2**3", "y = 4*(2**3)")]
        [TestCase("y = 2**3/4", "y = (2**3)/4")]
        [TestCase("y = 4/2**3", "y = 4/(2**3)")]
        [TestCase("y = 2**3+4", "y = (2**3)+4")]
        [TestCase("y = 4+2**3", "y = 4+(2**3)")]

        [TestCase("y = 8 * 3 > 2 * 100", "y = (8 * 3 )> (2 * 100)")]
        [TestCase("y = 1 + 8 * 3  > 2 * 100+4", "y = (8 * 3 )> (2 * 100)")]

        [TestCase("y = not 3*3>8", "y = not (3*3>8)")]
        [TestCase("y= [[1,2,3],[4,5,6]] [1] [1:1]", "y = ([[1,2,3],[4,5,6]] [1])[1:1]")]
        [TestCase("y = not 3*3>8", "y = not (3*3>8)")]
        [TestCase("y = 0xFAFA & 128 >> 7", "y = 0xFAFA & (128 >> 7)")]
        [TestCase("y = 0xFAFB & 128 >> 7", "y = 0xFAFB & (128 >> 7)")]
        [TestCase("y = 0xFAFA & 128 << 7", "y = 0xFAFA & (128 << 7)")]
        [TestCase("y = 0xFAFB & 128 << 7", "y = 0xFAFB & (128 << 7)")]

        public void ConstantCalculationPriorities(string actualExpr, string expectedExpr)
        {
            var expected = Funny.Hardcore.Build(expectedExpr).Calc().Get("y");
            actualExpr.AssertReturns("y", expected);
        }
    }
}