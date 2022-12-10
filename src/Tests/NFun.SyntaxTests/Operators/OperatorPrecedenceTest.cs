using System;
using System.Collections.Generic;
using System.Linq;
using NFun.TestTools;
using NUnit.Framework;

namespace NFun.SyntaxTests.Operators;

[TestFixture]
public class OperatorPrecedenceTest {
    [TestCase("not x1 and x2 ", "(not x1) and x2")]
    [TestCase("not x1 or x2 ", "(not x1) or  x2")]
    [TestCase("not x1 xor x2 ", "(not x1) xor x2")]
    [TestCase("not x1 == x2 ", " not (x1 == x2)")]
    [TestCase("not x1 != x2 ", " not (x1 != x2)")]
    [TestCase("not x1 and not x2 ", "(not x1) and (not x2)")]
    [TestCase("not x1 or  not x2 ", "(not x1) or  (not x2)")]
    [TestCase("not x1 xor not x2 ", "(not x1) xor (not x2)")]
    [TestCase("not x1 ==  not x2 ", "not (x1  ==  (not x2))")]
    [TestCase("not x1 !=  not x2 ", "not (x1  !=  (not x2))")]
    [TestCase("not not x1 and not not x2 ", " (not (not x1)) and (not (not x2))")]
    [TestCase("not not x1 or  not not x2 ", " (not (not x1)) or (not (not x2))")]
    [TestCase("not not x1 xor not not x2 ", " (not (not x1)) xor (not (not x2))")]
    [TestCase("not not x1 ==  not not x2 ", " (not (not (x1 == (not (not x2)))))")]
    [TestCase("not not x1 !=  not not x2 ", " (not (not (x1 != (not (not x2)))))")]
    public void DiscreetePrecedenceForTwoInputs(string actualExpr, string expectedExpr) {
        var allCombinations = GetAllCombinationsForTwoInputs(true, false);
        AssertExpressionAreEqualForAllCombinationOfInputs(actualExpr, expectedExpr, allCombinations);
    }

    [TestCase("x1 and x2 and  x3", "(x1 and x2) and x3")]
    [TestCase("x1 and x2 or  x3", "(x1 and x2) or  x3")]
    [TestCase("x1 and x2 xor  x3", "(x1 and x2) xor x3")]
    [TestCase("x1 and x2 ==  x3", "x1 and (x2==x3)")]
    [TestCase("x1 and x2  != x3", "x1 and (x2!=x3)")]
    [TestCase("x1  or x2 or x3", "(x1 or x2) or x3")]
    [TestCase("x1  or x2 and x3", "x1 or (x2 and x3)")]
    [TestCase("x1  or x2  xor x3", "x1 or (x2 xor x3)")]
    [TestCase("x1  or x2  != x3", "x1 or (x2!=x3)")]
    [TestCase("x1  or x2  == x3", "x1  or (x2==x3)")]
    [TestCase("x1 xor x2 xor x3", "(x1 xor x2) xor x3")]
    [TestCase("x1 xor x2 and x3", "x1 xor (x2 and x3)")]
    [TestCase("x1 xor x2  or x3", "(x1 xor x2) or x3")]
    [TestCase("x1 xor x2  == x3", "x1 xor (x2 == x3)")]
    [TestCase("x1 xor x2  != x3", "x1 xor (x2 != x3)")]
    [TestCase("x1  == x2 ==  x3", "(x1==x2) == x3")]
    [TestCase("x1  == x2 !=  x3", "(x1==x2) !=  x3")]
    [TestCase("x1  == x2 and x3", "(x1==x2) and x3")]
    [TestCase("x1  == x2 or  x3", "(x1==x2) or  x3")]
    [TestCase("x1  == x2 xor x3", "(x1==x2) xor x3")]
    [TestCase("x1  != x2 !=  x3", "(x1!=x2) !=  x3")]
    [TestCase("x1  != x2 ==  x3", "(x1!=x2) == x3")]
    [TestCase("x1  != x2 and x3", "(x1!=x2) and x3")]
    [TestCase("x1  != x2 or  x3", "(x1!=x2) or  x3")]
    [TestCase("x1  != x2 xor x3", "(x1!=x2) xor x3")]
    [TestCase("not x1 and x2 and x3", "((not x1) and x2) and x3")]
    [TestCase("not x1 and not x2 or x3", "((not x1) and (not x2)) or x3")]
    [TestCase("not x1 and not x2 or not x3 xor x1", "((not x1) and (not x2)) or ((not x3) xor x1)")]
    public void DiscreetePrecedenceForThreeInputs(string actualExpr, string expectedExpr) {
        var allCombinations = GetAllCombinationsForThreeInputs(true, false);
        AssertExpressionAreEqualForAllCombinationOfInputs(actualExpr, expectedExpr, allCombinations);
    }

    [TestCase("x1 & x2 << 2", "x1 & (x2 << 2)")]
    [TestCase("x1 | x2 << 2", "x1 | (x2 << 2)")]
    [TestCase("x1 ^ x2 << 2", "x1 ^ (x2 << 2)")]
    [TestCase("x1 & x2 << 2", "x1 & (x2 << 2)")]
    [TestCase("x1 | x2 << 2", "x1 | (x2 << 2)")]
    [TestCase("x1 ^ x2 << 2", "x1 ^ (x2 << 2)")]
    [TestCase("~x1 & x2", "(~x1) & x2")]
    [TestCase("~x1 | x2", "(~x1) | x2")]
    [TestCase("~x1 ^ x2", "(~x1) ^ x2")]
    [TestCase("~x1 + x2", "(~x1) + x2")]
    [TestCase("~x1 - x2", "(~x1) - x2")]
    [TestCase("~x1 * x2", "(~x1) * x2")]
    [TestCase("-~x1 + x2", "(-(~x1)) + x2")]
    [TestCase("-~x1 - x2", "(-(~x1)) - x2")]
    [TestCase("-~x1 * x2", "(-(~x1)) * x2")]
    [TestCase("~-x1 + x2", "(~(-x1)) + x2")]
    [TestCase("~-x1 - x2", "(~(-x1)) - x2")]
    [TestCase("~-x1 * x2", "(~(-x1)) * x2")]
    public void IntegerVariablePrecedenceForTwoInputs(string actualExpr, string expectedExpr) {
        var allCombinations = GetAllCombinationsForTwoInputs(-1, 0, 42, 112);
        AssertExpressionAreEqualForAllCombinationOfInputs(actualExpr, expectedExpr, allCombinations);
    }

    [TestCase("x1 & x2 & x3", "(x1 & x2) & x3")]
    [TestCase("x1 & x2 | x3", "(x1 & x2) | x3")]
    [TestCase("x1 & x2 ^ x3", "(x1 & x2) ^ x3")]
    [TestCase("x1 & x2 * x3", "x1 & (x2 * x3)")]
    [TestCase("x1 & x2 + x3", "x1 & (x2 + x3)")]
    [TestCase("x1 & x2 - x3", "x1 & (x2 - x3)")]
    [TestCase("x1 & x2 % x3", "x1 & (x2 % x3)")]
    [TestCase("x1 & x2 // x3", "x1 & (x2 // x3)")]
    [TestCase("x1 & x2 == x3", "(x1 & x2) == x3")]
    [TestCase("x1 & x2 != x3", "(x1 & x2) != x3")]
    [TestCase("x1 | x2 & x3", "x1 | (x2 & x3)")]
    [TestCase("x1 | x2 | x3", "(x1 | x2) | x3")]
    [TestCase("x1 | x2 ^ x3", "x1 | (x2 ^ x3)")]
    [TestCase("x1 | x2 * x3", "x1 | (x2 * x3)")]
    [TestCase("x1 | x2 + x3", "x1 | (x2 + x3)")]
    [TestCase("x1 | x2 - x3", "x1 | (x2 - x3)")]
    [TestCase("x1 | x2 %  x3", "x1 | (x2 % x3)")]
    [TestCase("x1 | x2 // x3", "x1 | (x2 // x3)")]
    [TestCase("x1 | x2 == x3", "(x1 | x2) == x3")]
    [TestCase("x1 | x2 != x3", "(x1 | x2) != x3")]
    [TestCase("x1 ^ x2 & x3", "x1 ^ (x2 & x3)")]
    [TestCase("x1 ^ x2 | x3", "(x1 ^ x2) | x3")]
    [TestCase("x1 ^ x2 ^ x3", "(x1 ^ x2) ^ x3")]
    [TestCase("x1 ^ x2 * x3", "x1 ^ (x2 * x3)")]
    [TestCase("x1 ^ x2 + x3", "x1 ^ (x2 + x3)")]
    [TestCase("x1 ^ x2 - x3", "x1 ^ (x2 - x3)")]
    [TestCase("x1 ^ x2 // x3", "x1 ^ (x2 // x3)")]
    [TestCase("x1 ^ x2 %  x3", "x1 ^ (x2 %  x3)")]
    [TestCase("x1 ^ x2 == x3", "(x1 ^ x2) == x3")]
    [TestCase("x1 ^ x2 != x3", "(x1 ^ x2) != x3")]
    [TestCase("x1 * x2 & x3", "(x1 * x2) & x3")]
    [TestCase("x1 * x2 | x3", "(x1 * x2) | x3")]
    [TestCase("x1 * x2 ^ x3", "(x1 * x2) ^ x3")]
    [TestCase("x1 * x2 * x3", "(x1 * x2) * x3")]
    [TestCase("x1 * x2 + x3", "(x1 * x2) + x3")]
    [TestCase("x1 * x2 - x3", "(x1 * x2) - x3")]
    [TestCase("x1 * x2 == x3", "(x1 * x2) == x3")]
    [TestCase("x1 * x2 != x3", "(x1 * x2) != x3")]
    [TestCase("x1 * x2 // x3", "(x1 * x2) // x3")]
    [TestCase("x1 * x2 %  x3", "(x1 * x2) %  x3")]
    [TestCase("x1 + x2 & x3", "(x1 + x2) & x3")]
    [TestCase("x1 + x2 | x3", "(x1 + x2) | x3")]
    [TestCase("x1 + x2 ^ x3", "(x1 + x2) ^ x3")]
    [TestCase("x1 + x2 + x3", "(x1 + x2) + x3")]
    [TestCase("x1 + x2 - x3", "(x1 + x2) - x3")]
    [TestCase("x1 + x2 == x3", "(x1 + x2) == x3")]
    [TestCase("x1 + x2 != x3", "(x1 + x2) != x3")]
    [TestCase("x1 + x2 * x3", "x1 + (x2 * x3)")]
    [TestCase("x1 + x2 // x3", "x1 + (x2 // x3)")]
    [TestCase("x1 + x2 %  x3", "x1 + (x2 %  x3)")]
    [TestCase("x1 - x2 & x3", "(x1 - x2) & x3")]
    [TestCase("x1 - x2 | x3", "(x1 - x2) | x3")]
    [TestCase("x1 - x2 ^ x3", "(x1 - x2) ^ x3")]
    [TestCase("x1 - x2 + x3", "(x1 - x2) + x3")]
    [TestCase("x1 - x2 - x3", "(x1 - x2) - x3")]
    [TestCase("x1 - x2 == x3", "(x1 - x2) == x3")]
    [TestCase("x1 - x2 != x3", "(x1 - x2) != x3")]
    [TestCase("x1 - x2 * x3", "x1 - (x2  * x3)")]
    [TestCase("x1 - x2 // x3", "x1 - (x2 // x3)")]
    [TestCase("x1 - x2 %  x3", "x1 - (x2 %  x3)")]
    [TestCase("x1 == x2 == x3", "(x1 == x2) == x3")]
    [TestCase("x1 == x2 != x3", "(x1 == x2) != x3")]
    [TestCase("x1 == x2 & x3", "x1 == (x2 & x3)")]
    [TestCase("x1 == x2 | x3", "x1 == (x2 | x3)")]
    [TestCase("x1 == x2 ^ x3", "x1 == (x2 ^ x3)")]
    [TestCase("x1 == x2 * x3", "x1 == (x2 * x3)")]
    [TestCase("x1 == x2 + x3", "x1 == (x2 + x3)")]
    [TestCase("x1 == x2 - x3", "x1 == (x2 - x3)")]
    [TestCase("x1 == x2 // x3", "x1 == (x2 // x3)")]
    [TestCase("x1 == x2 %  x3", "x1 == (x2 %  x3)")]
    [TestCase("x1 != x2 == x3", "(x1 != x2) == x3")]
    [TestCase("x1 != x2 != x3", "(x1 != x2) != x3")]
    [TestCase("x1 != x2 & x3", "x1 != (x2 &  x3)")]
    [TestCase("x1 != x2 | x3", "x1 != (x2 |  x3)")]
    [TestCase("x1 != x2 ^ x3", "x1 != (x2 ^  x3)")]
    [TestCase("x1 != x2 * x3", "x1 != (x2 *  x3)")]
    [TestCase("x1 != x2 + x3", "x1 != (x2 +  x3)")]
    [TestCase("x1 != x2 - x3", "x1 != (x2 -  x3)")]
    [TestCase("x1 != x2 // x3", "x1 != (x2 // x3)")]
    [TestCase("x1 != x2 %  x3", "x1 != (x2 %  x3)")]
    [TestCase("x1 // x2 & x3", "(x1 // x2) & x3")]
    [TestCase("x1 // x2 | x3", "(x1 // x2) | x3")]
    [TestCase("x1 // x2 ^ x3", "(x1 // x2) ^ x3")]
    [TestCase("x1 // x2 * x3", "(x1 // x2) * x3")]
    [TestCase("x1 // x2 + x3", "(x1 // x2) + x3")]
    [TestCase("x1 // x2 - x3", "(x1 // x2) - x3")]
    [TestCase("x1 // x2 == x3", "(x1 // x2) == x3")]
    [TestCase("x1 // x2 != x3", "(x1 // x2) != x3")]
    [TestCase("x1 // x2 // x3", "(x1 // x2) // x3")]
    [TestCase("x1 // x2 %  x3", "(x1 // x2) %  x3")]
    [TestCase("x1 % x2 & x3", "(x1 % x2) & x3")]
    [TestCase("x1 % x2 | x3", "(x1 % x2) | x3")]
    [TestCase("x1 % x2 ^ x3", "(x1 % x2) ^ x3")]
    [TestCase("x1 % x2 * x3", "(x1 % x2) * x3")]
    [TestCase("x1 % x2 + x3", "(x1 % x2) + x3")]
    [TestCase("x1 % x2 - x3", "(x1 % x2) - x3")]
    [TestCase("x1 % x2 == x3", "(x1 % x2) == x3")]
    [TestCase("x1 % x2 != x3", "(x1 % x2) != x3")]
    [TestCase("x1 % x2 // x3", "(x1 % x2) // x3")]
    [TestCase("x1 % x2 %  x3", "(x1 % x2) %  x3")]
    public void IntegerVariablePrecedenceForThreeInputs(string actualExpr, string expectedExpr) {
        var allCombinations = GetAllCombinationsForThreeInputs(-1, 1, 42, 112);
        AssertExpressionAreEqualForAllCombinationOfInputs(actualExpr, expectedExpr, allCombinations);
    }

    [TestCase("x1 * x2 > x3 * 2 ", "(x1 * x2 )> (x3 * 2)")]
    [TestCase("x1 + x2 * 1  > 2 * x2+x3", "(x1 + (x2 * 1))  > ((2 * x2)+x3)")]
    [TestCase("not x1*x2>x3", "not ((x1*x2)>x3)")]
    [TestCase("x1 %  x2 ** x3", "x1 % (x2 ** x3)")]
    [TestCase("x1 -  x2 ** x3", "x1 - (x2 ** x3)")]
    [TestCase("x1 +  x2 ** x3", "x1 + (x2 ** x3)")]
    [TestCase("x1 *  x2 ** x3", "x1 * (x2 ** x3)")]
    [TestCase("x1 != x2 ** x3", "x1 !=(x2 ** x3)")]
    [TestCase("x1 == x2 ** x3", "x1 ==(x2 ** x3)")]
    [TestCase("x1 ** x2 * x3", "(x1 ** x2) * x3")]
    [TestCase("x1 ** x2 + x3", "(x1 ** x2) + x3")]
    [TestCase("x1 ** x2 - x3", "(x1 ** x2) - x3")]
    [TestCase("x1 ** x2 ** x3", "(x1 ** x2) ** x3")]
    [TestCase("x1 ** x2 == x3", "(x1 ** x2) == x3")]
    [TestCase("x1 ** x2 != x3", "(x1 ** x2) != x3")]
    [TestCase("x1 ** x2 / x3", "(x1 ** x2) / x3")]
    [TestCase("x1 ** x2 % x3", "(x1 ** x2) % x3")]
    [TestCase("x1 / x2 * x3", "(x1 / x2) * x3")]
    [TestCase("x1 / x2 + x3", "(x1 / x2) + x3")]
    [TestCase("x1 / x2 - x3", "(x1 / x2) - x3")]
    [TestCase("x1 / x2 ** x3", "x1 / (x2 ** x3)")]
    [TestCase("x1 / x2 == x3", "(x1 / x2) == x3")]
    [TestCase("x1 / x2 != x3", "(x1 / x2) != x3")]
    [TestCase("x1 / x2 / x3", "(x1 / x2) / x3")]
    [TestCase("x1 / x2 % x3", "(x1 / x2) %  x3")]
    public void RealVariablePrecedenceForThreeInputs(string actualExpr, string expectedExpr) {
        var allCombinations = GetAllCombinationsForThreeInputs(-1.0, 0.0, 1, 2);
        AssertExpressionAreEqualForAllCombinationOfInputs(actualExpr, expectedExpr, allCombinations);
    }

    private static void AssertExpressionAreEqualForAllCombinationOfInputs(string actualExpr, string expectedExpr,
        List<(string, object)[]> allCombinations)
        => Assert.Multiple(
            () => {
                foreach (var inputs in allCombinations)
                {
                    var actual = actualExpr.CalcAnonymousOut(inputs);
                    var expected = expectedExpr.CalcAnonymousOut(inputs);
                    Assert.AreEqual(actual.GetType(), expected.GetType(),
                        $"Type of actual:{actual.GetType().Name} and expected:{expected.GetType().Name} differs");
                    if (!actual.Equals(expected))
                        Assert.Fail(
                            $"On {String.Join(" ", inputs.Select((v, i) => $"x{i + 1}={v.Item2}"))}\r" +
                            $"Eq: {actualExpr}\r" +
                            $"Expected: {expected}\r" +
                            $"But was: {actual} ");
                }
            });

    [TestCase("1+2*3", "1+(2*3)")]
    [TestCase("-1*-2", "(-1)*(-2)")]
    [TestCase("2*3+1", "(2*3)+1")]
    [TestCase("1+4/2", "1+(4/2)")]
    [TestCase("4/2+1", "(4/2)+1")]
    [TestCase("5*4/2", "(5*4)/2")]
    [TestCase("4/2*5", "(4/2)*5")]
    [TestCase("2**3*4", "(2**3)*4")]
    [TestCase("4*2**3", "4*(2**3)")]
    [TestCase("2**3/4", "(2**3)/4")]
    [TestCase("4/2**3", "4/(2**3)")]
    [TestCase("2**3+4", "(2**3)+4")]
    [TestCase("4+2**3", "4+(2**3)")]
    [TestCase("8 * 3 > 2 * 100", "(8 * 3 )> (2 * 100)")]
    [TestCase("1 + 8 * 3  > 2 * 100+4", "(8 * 3 )> (2 * 100)")]
    [TestCase("not 3*3>8", "not (3*3>8)")]
    [TestCase("[[1,2,3],[4,5,6]] [1] [1:1]", "([[1,2,3],[4,5,6]] [1])[1:1]")]
    [TestCase("not 3*3>8", "not (3*3>8)")]
    [TestCase("not 1 in [1,2,3]", "not (1 in [1,2,3])")]
    [TestCase("0xFAFA & 128 >> 7", "0xFAFA & (128 >> 7)")]
    [TestCase("0xFAFB & 128 >> 7", "0xFAFB & (128 >> 7)")]
    [TestCase("0xFAFA & 128 << 7", "0xFAFA & (128 << 7)")]
    [TestCase("0xFAFB & 128 << 7", "0xFAFB & (128 << 7)")]
    public void ConstantCalculationPrecedence(string actualExpr, string expectedExpr) {
        var expected = expectedExpr.CalcAnonymousOut();
        actualExpr.AssertAnonymousOut(expected);
    }

    private static List<(string, object)[]> GetAllCombinationsForThreeInputs<T>(params T[] possibleValues)
        => (from x1 in possibleValues
            from x2 in possibleValues
            from x3 in possibleValues
            select new (string, object)[] { ("x1", x1), ("x2", x2), ("x3", x3) }).ToList();

    private static List<(string, object)[]> GetAllCombinationsForTwoInputs<T>(params T[] possibleValues)
        => (from x1 in possibleValues
            from x2 in possibleValues
            select new (string, object)[] { ("x1", x1), ("x2", x2), }).ToList();
}
