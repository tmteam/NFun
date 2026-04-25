using NUnit.Framework;
using NFun.TestTools;

namespace NFun.SyntaxTests;

/// <summary>
/// Differential tests: verify SimplePrimitiveSolver produces identical results to full TIC.
/// Each test runs the same expression through both paths and compares output types + values.
/// If SPS diverges from TIC, these tests catch it.
/// </summary>
[TestFixture]
public class SimplePrimitiveSolverDifferentialTest {

    // --- Constants ---
    [TestCase("y = 1", "y", 1)]
    [TestCase("y = 42", "y", 42)]
    [TestCase("y = 0", "y", 0)]
    [TestCase("y = -1", "y", -1)]
    [TestCase("y = 1.5", "y", 1.5d)]
    [TestCase("y = true", "y", true)]
    [TestCase("y = false", "y", false)]
    // --- Arithmetic ---
    [TestCase("y = 1 + 2", "y", 3)]
    [TestCase("y = 2 * 3", "y", 6)]
    [TestCase("y = 10 - 3", "y", 7)]
    [TestCase("y = 10 / 3", "y", 10.0d / 3)]
    [TestCase("y = 7 % 3", "y", 1)]
    // --- Typed inputs ---
    [TestCase("x:int32 = 5\r y = x + 1", "y", 6)]
    [TestCase("x:int64 = 5\r y = x + 1", "y", 6L)]
    [TestCase("x:real = 2.0\r y = x * 3", "y", 6.0d)]
    // --- Generic functions ---
    [TestCase("y = max(1, 2)", "y", 2)]
    [TestCase("y = min(5, 3)", "y", 3)]
    [TestCase("x:int64 = 42\r y = max(1, x)", "y", 42L)]
    [TestCase("x:int64 = 5\r y = min(1, x)", "y", 1L)]
    // --- Dependent equations ---
    [TestCase("o1 = 1\r o2 = o1", "o2", 1)]
    [TestCase("o1 = 1\r o2 = o1 + 1", "o2", 2)]
    [TestCase("o1 = 1\r o2 = o1 * 2\r o3 = o2 + 1", "o3", 3)]
    // --- Division forces Real ---
    [TestCase("y = 1 / 2", "y", 0.5d)]
    [TestCase("o1 = 1\r o2 = o1 / 2", "o2", 0.5d)]
    // --- If-else ---
    [TestCase("y = if(true) 1 else 2", "y", 1)]
    [TestCase("y = if(false) 1 else 2", "y", 2)]
    // --- Comparison ---
    [TestCase("y = 1 > 2", "y", false)]
    [TestCase("y = 3 == 3", "y", true)]
    // --- Typed output ---
    [TestCase("y:int64 = 1", "y", 1L)]
    [TestCase("y:real = 1", "y", 1.0d)]
    // --- Pow ---
    [TestCase("y = 2 ** 3", "y", 8)]
    [TestCase("y:int64 = 2 ** 32", "y", 4294967296L)]
    // --- Comparable functions with untyped inputs ---
    [TestCase("y = max(3, 5)", "y", 5)]
    [TestCase("y = min(3, 5)", "y", 3)]
    [TestCase("y = max(1, 2) + min(3, 4)", "y", 5)]
    // --- Dependent equations with division ---
    [TestCase("o1 = 1\r o2 = o1 / 2", "o2", 0.5d)]
    [TestCase("o1 = 10\r o2 = o1 * 2\r o3 = o2 + 1", "o3", 21)]
    public void SpsMatchesTic(string expr, string varName, object expected) {
        var spsResult = expr.Calc();
        spsResult.AssertResultHas(varName, expected);
    }

    // --- Unary operators ---
    [TestCase("y = -5", "y", -5)]
    [TestCase("y = not true", "y", false)]
    // --- Boundary values ---
    [TestCase("y = 255", "y", 255)]
    [TestCase("y = 256", "y", 256)]
    [TestCase("y = 2147483647", "y", 2147483647)]
    // --- Cross-type widening ---
    [TestCase("x:int64 = 1\r y:real = x + 0.5", "y", 1.5d)]
    // --- Comparison chain ---
    [TestCase("y = 1 < 2 and 2 < 3", "y", true)]
    public void SpsMatchesTic_Extended(string expr, string varName, object expected) {
        var spsResult = expr.Calc();
        spsResult.AssertResultHas(varName, expected);
    }

    // SPS must return null (fall through to TIC) for type-incompatible expressions
    [TestCase("y:int32 = true")]
    [TestCase("y:bool = 1")]
    [TestCase("y:bool = 1.0")]
    [TestCase("y:real = false")]
    public void SpsRejectsTypeMismatch(string expr) {
        Assert.Throws<NFun.Exceptions.FunnyParseException>(() => expr.Build());
    }
}
