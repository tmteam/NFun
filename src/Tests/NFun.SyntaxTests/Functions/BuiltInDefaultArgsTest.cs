namespace NFun.SyntaxTests.Functions;

using Exceptions;
using TestTools;
using NUnit.Framework;

/// <summary>
/// Tests for built-in functions with default argument values.
/// Both positional (partial arity) and named arg patterns.
/// </summary>
[TestFixture]
public class BuiltInDefaultArgsTest {

    // ═══════════════════════════════════════════════════════════════
    // toNumText — 5 args, 4 with defaults
    // ═══════════════════════════════════════════════════════════════

    // All defaults
    [Test] public void ToNumText_AllDefaults() =>
        "y = toNumText(3.14)".AssertReturns("y", "3.14");

    [Test] public void ToNumText_AllDefaults_Integer() =>
        "y = toNumText(42)".AssertReturns("y", "42.00");

    // Positional override
    [Test] public void ToNumText_Decimals_Positional() =>
        "y = toNumText(3.14159, 4)".AssertReturns("y", "3.1416");

    [Test] public void ToNumText_Decimals_Zero() =>
        "y = toNumText(3.14, 0)".AssertReturns("y", "3");

    [Test] public void ToNumText_MinDigits_Positional() =>
        "y = toNumText(42, 0, 6)".AssertReturns("y", "000042");

    [Test] public void ToNumText_Thousands_Positional() =>
        "y = toNumText(1234567, 0, 0, true)".AssertReturns("y", "1,234,567");

    [Test] public void ToNumText_AllPositional() =>
        "y = toNumText(3.1, 2, 0, false, false)".AssertReturns("y", "3.1");

    // Named args
    [Test] public void ToNumText_Decimals_Named() =>
        "y = toNumText(3.14159, decimals=4)".AssertReturns("y", "3.1416");

    [Test] public void ToNumText_Thousands_Named() =>
        "y = toNumText(1234567, thousands=true)".AssertReturns("y", "1,234,567.00");

    [Test] public void ToNumText_ForceZeros_Named() =>
        "y = toNumText(3.1, forceZeros=false)".AssertReturns("y", "3.1");

    [Test] public void ToNumText_Mixed_Positional_Named() =>
        "y = toNumText(1234567.89, 2, thousands=true)".AssertReturns("y", "1,234,567.89");

    // ═══════════════════════════════════════════════════════════════
    // toSciText — 2 args, 1 with default
    // ═══════════════════════════════════════════════════════════════

    [Test] public void ToSciText_Default() =>
        "y = toSciText(3.14)".AssertReturns("y", "3.140000E+000");

    [Test] public void ToSciText_Lowercase() =>
        "y = toSciText(3.14, false)".AssertReturns("y", "3.140000e+000");

    [Test] public void ToSciText_Uppercase_Named() =>
        "y = toSciText(3.14, uppercase=false)".AssertReturns("y", "3.140000e+000");

    // ═══════════════════════════════════════════════════════════════
    // Functions without defaults — exact arity still works
    // ═══════════════════════════════════════════════════════════════

    [Test] public void ToHexText_ExactArity() =>
        "y = toHexText(255)".AssertReturns("y", "FF");

    [Test] public void ToBinText_ExactArity() =>
        "y = toBinText(42)".AssertReturns("y", "101010");

    [Test] public void PadLeftText_ExactArity() =>
        "y = padLeftText('hi', 10)".AssertReturns("y", "        hi");

    [Test] public void PadRightText_ExactArity() =>
        "y = padRightText('hi', 10)".AssertReturns("y", "hi        ");

    [Test] public void PadCenterText_ExactArity() =>
        "y = padCenterText('hi', 10)".AssertReturns("y", "    hi    ");

    // ═══════════════════════════════════════════════════════════════
    // Pipe-forward with defaults
    // ═══════════════════════════════════════════════════════════════

    [Test] public void ToNumText_PipeForward() =>
        "y = 3.14.toNumText()".AssertReturns("y", "3.14");

    [Test] public void ToNumText_PipeForward_WithDecimals() =>
        "y = 3.14159.toNumText(4)".AssertReturns("y", "3.1416");

    [Test] public void ToHexText_PipeForward() =>
        "y = 255.toHexText()".AssertReturns("y", "FF");

    [Test] public void PadLeftText_PipeForward() =>
        "y = 'hi'.padLeftText(10)".AssertReturns("y", "        hi");

    // ═══════════════════════════════════════════════════════════════
    // Error: wrong type
    // ═══════════════════════════════════════════════════════════════

    [Test] public void Error_ToNumText_OnText() =>
        Assert.Throws<FunnyParseException>(() => "y = toNumText('hello')".Build());

    [Test] public void Error_ToHexText_OnReal() =>
        Assert.Throws<FunnyParseException>(() => "y = toHexText(3.14)".Build());

    [Test] public void Error_ToBinText_OnText() =>
        Assert.Throws<FunnyParseException>(() => "y = toBinText('hello')".Build());

    // Width-aware hex/bin formatting per operand's declared bit width.
    // Without this, the value was widened to int64 before formatting and a
    // negative typed-narrower input got 64 hex chars: `int -1` →
    // 'FFFFFFFFFFFFFFFF' instead of 'FFFFFFFF'. Now signed values are cast
    // to unsigned of the same width to capture two's-complement at the
    // declared width, then formatted without forced padding.
    [TestCase("y:int=-1\r o=toHexText(y)", "FFFFFFFF")]
    [TestCase("y:int16=-1\r o=toHexText(y)", "FFFF")]
    [TestCase("y:int8=-1\r o=toHexText(y)", "FF")]
    [TestCase("y:int64=-1\r o=toHexText(y)", "FFFFFFFFFFFFFFFF")]
    [TestCase("y:byte=255\r o=toHexText(y)", "FF")]
    [TestCase("y:int=-1\r o=toBinText(y)", "11111111111111111111111111111111")]
    [TestCase("y:int16=-1\r o=toBinText(y)", "1111111111111111")]
    [TestCase("y:int8=-1\r o=toBinText(y)", "11111111")]
    public void ToHexBin_PreservesOperandBitWidth(string expr, string expected) =>
        expr.AssertResultHas("o", expected);

    [Test] public void Error_PadLeftText_WrongWidthType() =>
        Assert.Throws<FunnyParseException>(() => "y = padLeftText('hi', 'ten')".Build());

    // ═══════════════════════════════════════════════════════════════
    // Error: too many / too few args
    // ═══════════════════════════════════════════════════════════════

    [Test] public void Error_ToHexText_NoArgs() =>
        Assert.Throws<FunnyParseException>(() => "y = toHexText()".Build());

    [Test] public void Error_ToHexText_TooMany() =>
        Assert.Throws<FunnyParseException>(() => "y = toHexText(255, 10)".Build());
}
