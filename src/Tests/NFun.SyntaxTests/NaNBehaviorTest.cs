using NFun.TestTools;
using NUnit.Framework;

namespace NFun.SyntaxTests;

/// <summary>
/// Tests for NaN (Not a Number) behavior in NFun.
/// NaN is produced by 0.0/0.0 and propagates through arithmetic/comparison.
/// </summary>
public class NaNBehaviorTest {

    private static void AssertNaN(string expr, string varName = "y") {
        var result = expr.Calc();
        var val = result.Get(varName);
        Assert.IsInstanceOf<double>(val, $"{varName} should be double");
        Assert.IsTrue(double.IsNaN((double)val), $"{varName} should be NaN but was {val}");
    }

    #region NaN production

    [Test]
    public void ZeroDivZero_ProducesNaN() => AssertNaN("y:real = 0.0 / 0.0");

    [Test]
    public void Sqrt_Negative_ProducesNaN() => AssertNaN("y = sqrt(-1.0)");

    #endregion

    #region NaN in min/max

    [Test]
    public void Max_NaN_And_Value() => AssertNaN("y = max(0.0/0.0, 1.0)");

    [Test]
    public void Max_Value_And_NaN() => AssertNaN("y = max(1.0, 0.0/0.0)");

    [Test]
    public void Max_NaN_And_NaN() => AssertNaN("y = max(0.0/0.0, 0.0/0.0)");

    [Test]
    public void Max_NaN_And_Negative() => AssertNaN("y = max(0.0/0.0, -100.0)");

    [Test]
    public void Max_NaN_And_Zero() => AssertNaN("y = max(0.0/0.0, 0.0)");

    [Test]
    public void Min_NaN_And_Value() => AssertNaN("y = min(0.0/0.0, 1.0)");

    [Test]
    public void Min_Value_And_NaN() => AssertNaN("y = min(1.0, 0.0/0.0)");

    [Test]
    public void Min_NaN_And_NaN() => AssertNaN("y = min(0.0/0.0, 0.0/0.0)");

    #endregion

    #region NaN in arithmetic

    [Test]
    public void NaN_Plus_Value() => AssertNaN("y = 0.0/0.0 + 1.0");

    [Test]
    public void NaN_Multiply_Value() => AssertNaN("y = (0.0/0.0) * 5.0");

    #endregion

    #region Normal min/max (sanity)

    [Test]
    public void Max_Normal() => "y = max(3.0, 7.0)".AssertReturns("y", 7.0);

    [Test]
    public void Min_Normal() => "y = min(3.0, 7.0)".AssertReturns("y", 3.0);

    [Test]
    public void Max_Negative() => "y = max(-5.0, -1.0)".AssertReturns("y", -1.0);

    [Test]
    public void Min_Negative() => "y = min(-5.0, -1.0)".AssertReturns("y", -5.0);

    #endregion
}
