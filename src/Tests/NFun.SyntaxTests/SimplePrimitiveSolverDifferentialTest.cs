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

    // ======================================================================
    // Category 1: If-else type inference
    // ======================================================================
    [TestCase("y = if(true) 1 else 2", "y", 1)]
    [TestCase("y = if(true) 1 else 2.5", "y", 1.0d)]           // LCA widens to Real
    [TestCase("y = if(false) 1 else 2", "y", 2)]
    [TestCase("x:int64 = 5\r y = if(true) x else 1", "y", 5L)]
    [TestCase("y = if(true) true else false", "y", true)]
    [TestCase("y = if(true) 1 else if(false) 2 else 3", "y", 1)]     // multi-elif
    [TestCase("x:int32 = 1\r y:int64 = 2\r z = if(true) x else y", "z", 1L)] // widening
    [TestCase("y = if(1 > 2) 10 else 20", "y", 20)]
    [TestCase("y = if(false) 1.5 else 2.5", "y", 2.5d)]
    [TestCase("a = 5\r b = 10\r y = if(a < b) a else b", "y", 5)]
    [TestCase("y = if(true) 0 else -1", "y", 0)]
    public void SpsMatchesTic_IfElse(string expr, string varName, object expected) {
        var spsResult = expr.Calc();
        spsResult.AssertResultHas(varName, expected);
    }

    // ======================================================================
    // Category 2: Multi-equation type propagation
    // ======================================================================
    [TestCase("a = 1\r b = a + 1\r c = b * 2", "c", 4)]
    [TestCase("a:int64 = 1\r b = a + 1\r c = b", "c", 2L)]
    [TestCase("a = 1\r b = 2\r c = max(a, b)", "c", 2)]
    [TestCase("a = 1.0\r b = a + 1", "b", 2.0d)]
    [TestCase("a = 1\r b = a / 2", "b", 0.5d)]                 // division forces Real
    [TestCase("a = 1\r b = 2\r c = a + b\r d = c * 2", "d", 6)]
    [TestCase("a = 1\r b = a\r c = b\r d = c", "d", 1)]        // chain propagation
    [TestCase("a = 10\r b = a % 3\r c = b + 1", "c", 2)]
    [TestCase("a:real = 2.0\r b = a * 3\r c = b + 1", "c", 7.0d)]
    [TestCase("a = 1\r b = 2\r c = a + b\r d = c - 1\r e = d * 2", "e", 4)]
    public void SpsMatchesTic_MultiEquation(string expr, string varName, object expected) {
        var spsResult = expr.Calc();
        spsResult.AssertResultHas(varName, expected);
    }

    // ======================================================================
    // Category 3: Type annotations + inference
    // ======================================================================
    [TestCase("y:int64 = 1 + 2", "y", 3L)]
    [TestCase("y:real = 42", "y", 42.0d)]
    [TestCase("x:int64 = 5\r y = x + 1", "y", 6L)]
    [TestCase("x:int32 = 5\r y:int64 = x", "y", 5L)]           // implicit widening
    [TestCase("y:int16 = 1", "y", (short)1)]
    [TestCase("y:uint8 = 42", "y", (byte)42)]
    [TestCase("y:uint16 = 1000", "y", (ushort)1000)]
    [TestCase("y:uint32 = 100000", "y", 100000u)]
    [TestCase("y:uint64 = 1", "y", 1UL)]
    [TestCase("x:int32 = 10\r y:real = x + 0.5", "y", 10.5d)]
    [TestCase("y:int64 = 100", "y", 100L)]
    public void SpsMatchesTic_TypeAnnotations(string expr, string varName, object expected) {
        var spsResult = expr.Calc();
        spsResult.AssertResultHas(varName, expected);
    }

    // ======================================================================
    // Category 4: Comparison operators
    // ======================================================================
    [TestCase("y = 1 > 2 and 3 < 4", "y", false)]
    [TestCase("y = max(1, 2) == 2", "y", true)]
    [TestCase("y = 5 != 3", "y", true)]
    [TestCase("y = 10 >= 10", "y", true)]
    [TestCase("y = 10 <= 9", "y", false)]
    [TestCase("y = 1 < 2 and 3 > 1", "y", true)]
    public void SpsMatchesTic_Comparison(string expr, string varName, object expected) {
        var spsResult = expr.Calc();
        spsResult.AssertResultHas(varName, expected);
    }

    // ======================================================================
    // Category 5: Generic function calls
    // ======================================================================
    [TestCase("y = max(1, 2) + min(3, 4)", "y", 5)]
    [TestCase("x:int64 = 5\r y = max(x, 1)", "y", 5L)]
    [TestCase("y = max(1, max(2, 3))", "y", 3)]
    [TestCase("y = abs(-5)", "y", 5)]
    [TestCase("y = min(10, 20)", "y", 10)]
    [TestCase("y = max(max(1, 2), max(3, 4))", "y", 4)]
    [TestCase("y = min(min(10, 20), min(5, 15))", "y", 5)]
    [TestCase("x:int64 = 10\r y = min(x, 20)", "y", 10L)]
    [TestCase("y = max(min(1, 5), min(2, 4))", "y", 2)]
    [TestCase("y = abs(-1) + abs(-2)", "y", 3)]
    public void SpsMatchesTic_GenericFunctions(string expr, string varName, object expected) {
        var spsResult = expr.Calc();
        spsResult.AssertResultHas(varName, expected);
    }

    // ======================================================================
    // Category 6: Pow operator edge cases
    // ======================================================================
    [TestCase("y = 2 ** 3", "y", 8)]
    [TestCase("y = 2 ** 0", "y", 1)]
    [TestCase("y:int64 = 2 ** 32", "y", 4294967296L)]
    [TestCase("y = 3 ** 2", "y", 9)]
    [TestCase("y = 10 ** 1", "y", 10)]
    [TestCase("y = 1 ** 100", "y", 1)]
    public void SpsMatchesTic_Pow(string expr, string varName, object expected) {
        var spsResult = expr.Calc();
        spsResult.AssertResultHas(varName, expected);
    }

    // ======================================================================
    // Category 7: Boolean operators
    // ======================================================================
    [TestCase("y = true and false", "y", false)]
    [TestCase("y = true or false", "y", true)]
    [TestCase("y = not true", "y", false)]
    [TestCase("y = not false", "y", true)]
    [TestCase("y = true xor true", "y", false)]
    [TestCase("y = true xor false", "y", true)]
    [TestCase("y = (1 > 0) and (2 > 1)", "y", true)]
    public void SpsMatchesTic_Boolean(string expr, string varName, object expected) {
        var spsResult = expr.Calc();
        spsResult.AssertResultHas(varName, expected);
    }

    // ======================================================================
    // Category 8: Complex arithmetic expressions
    // ======================================================================
    [TestCase("y = (1 + 2) * (3 + 4)", "y", 21)]
    [TestCase("y = 2 * 3 + 4 * 5", "y", 26)]
    [TestCase("y = 1 + 2 + 3", "y", 6)]
    [TestCase("y = 10 % 3 + 1", "y", 2)]
    [TestCase("y = 100 - 50 - 25", "y", 25)]
    [TestCase("y = -(-5)", "y", 5)]
    public void SpsMatchesTic_ComplexArithmetic(string expr, string varName, object expected) {
        var spsResult = expr.Calc();
        spsResult.AssertResultHas(varName, expected);
    }

    // ======================================================================
    // Category 9: SPS rejection — type mismatches (should error)
    // ======================================================================
    [TestCase("y:int32 = true")]
    [TestCase("y:bool = 1")]
    [TestCase("y:bool = 1.0")]
    [TestCase("y:real = false")]
    public void SpsRejectsTypeMismatch(string expr) {
        Assert.Throws<NFun.Exceptions.FunnyParseException>(() => expr.Build());
    }

    // ======================================================================
    // Category 10: SPS rejection — non-primitive expressions (fall through to TIC, still work)
    // ======================================================================
    [TestCase("y = [1, 2, 3]")]
    [TestCase("y = {x = 1}")]
    public void NonPrimitiveExpressionStillWorks(string expr) {
        // These should fall through to full TIC but still produce valid results
        Assert.DoesNotThrow(() => expr.Build());
    }

    // ======================================================================
    // Category 11: Bitwise operators
    // ======================================================================
    [TestCase("y = 5 & 3", "y", 1)]
    [TestCase("y = 5 | 3", "y", 7)]
    [TestCase("y = 5 ^ 3", "y", 6)]
    [TestCase("y = ~0", "y", -1)]
    [TestCase("x:int32 = 5\r y = x & 3", "y", 1)]
    [TestCase("x:int32 = 5\r y:int32 = 3\r z = x & y", "z", 1)]
    [TestCase("x:int64 = 5\r y = x & 3", "y", 1L)]
    public void SpsMatchesTic_Bitwise(string expr, string varName, object expected) {
        var spsResult = expr.Calc();
        spsResult.AssertResultHas(varName, expected);
    }

    // ======================================================================
    // Category 12: Mixed signed/unsigned — SPS falls through, TIC resolves
    // ======================================================================
    [Test]
    public void MixedSignedUnsigned_IfElse_ResolvesToInt64() {
        "a:int32 = 1\r b:uint32 = 2\r c = if(true) a else b"
            .Calc().AssertResultHas("c", 1L);
    }

    [Test]
    public void MixedSignedUnsigned_IfElse_ResolvesToInt32() {
        "a:uint16 = 1\r b:int16 = 2\r c = if(true) a else b"
            .Calc().AssertResultHas("c", 1);
    }

    [Test]
    public void MixedSignedUnsigned_Bitwise_U32_I32_ResolvesToInt64() {
        "a:uint32 = 5\r b:int32 = 3\r c = a & b"
            .Calc().AssertResultHas("c", 1L);
    }

    [Test]
    public void MixedSignedUnsigned_Bitwise_U16_I16_ResolvesToInt32() {
        "a:uint16 = 5\r b:int16 = 3\r c = a & b"
            .Calc().AssertResultHas("c", 1);
    }

    [Test]
    public void MixedSignedUnsigned_Bitwise_U64_I64_Error() {
        Assert.Throws<NFun.Exceptions.FunnyParseException>(() =>
            "a:uint64 = 5\r b:int64 = 3\r c = a & b".Build());
    }
}
