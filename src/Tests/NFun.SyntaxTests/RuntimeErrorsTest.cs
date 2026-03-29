using NFun.Exceptions;
using NFun.TestTools;
using NUnit.Framework;

namespace NFun.SyntaxTests;

[TestFixture]
public class RuntimeErrorsTest {

    // ── Integer overflow → FunnyRuntimeException ────────────────────

    [Test]
    public void IntegerOverflow_ThrowsFunnyRuntimeException() {
        var runtime = "y = 2147483647 + 1".Build();
        Assert.Throws<FunnyRuntimeException>(() => runtime.Calc());
    }

    [Test]
    public void IntegerUnderflow_ThrowsFunnyRuntimeException() {
        var runtime = "y = -2147483648 - 1".Build();
        Assert.Throws<FunnyRuntimeException>(() => runtime.Calc());
    }

    [Test]
    public void PowerOverflow_ThrowsFunnyRuntimeException() {
        var runtime = "y = 2 ** 32".Build();
        Assert.Throws<FunnyRuntimeException>(() => runtime.Calc());
    }

    [Test]
    public void Int64Overflow_ThrowsFunnyRuntimeException() {
        var runtime = "y:int64 = 9223372036854775807 + 1".Build();
        Assert.Throws<FunnyRuntimeException>(() => runtime.Calc());
    }

    // ── Division by zero → FunnyRuntimeException ────────────────────

    [Test]
    public void IntDivisionByZero_ThrowsFunnyRuntimeException() {
        var runtime = "y = 1 // 0".Build();
        Assert.Throws<FunnyRuntimeException>(() => runtime.Calc());
    }

    [Test]
    public void ModuloByZero_ThrowsFunnyRuntimeException() {
        var runtime = "y = 5 % 0".Build();
        Assert.Throws<FunnyRuntimeException>(() => runtime.Calc());
    }

    // ── Empty array operations → FunnyRuntimeException ──────────────

    [Test]
    public void LastOfEmptyArray_ThrowsFunnyRuntimeException() {
        var runtime = "y:int[] = []\r z = y.last()".Build();
        Assert.Throws<FunnyRuntimeException>(() => runtime.Calc());
    }

    [Test]
    public void MedianOfEmptyArray_ThrowsFunnyRuntimeException() {
        var runtime = "y:int[] = []\r z = y.median()".Build();
        Assert.Throws<FunnyRuntimeException>(() => runtime.Calc());
    }

    [Test]
    public void AvgOfEmptyArray_ThrowsFunnyRuntimeException() {
        var runtime = "y:real[] = []\r z = y.avg()".Build();
        Assert.Throws<FunnyRuntimeException>(() => runtime.Calc());
    }

    [Test]
    public void MaxOfEmptyArray_ThrowsFunnyRuntimeException() {
        var runtime = "y:int[] = []\r z = y.max()".Build();
        Assert.Throws<FunnyRuntimeException>(() => runtime.Calc());
    }

    [Test]
    public void MinOfEmptyArray_ThrowsFunnyRuntimeException() {
        var runtime = "y:int[] = []\r z = y.min()".Build();
        Assert.Throws<FunnyRuntimeException>(() => runtime.Calc());
    }

    // ── Other runtime errors ────────────────────────────────────────

    [Test]
    public void RepeatNegativeCount_ThrowsFunnyRuntimeException() {
        var runtime = "y = repeat(1, -1)".Build();
        Assert.Throws<FunnyRuntimeException>(() => runtime.Calc());
    }

    [Test]
    public void BackwardsSlice_ThrowsFunnyRuntimeException() {
        var runtime = "y = [1,2,3,4,5][3:1]".Build();
        Assert.Throws<FunnyRuntimeException>(() => runtime.Calc());
    }
}
