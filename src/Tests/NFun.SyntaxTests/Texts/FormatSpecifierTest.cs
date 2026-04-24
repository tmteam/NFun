namespace NFun.SyntaxTests.Texts;

using NFun.Exceptions;
using NFun.TestTools;
using NUnit.Framework;

[TestFixture]
public class FormatSpecifierTest {

    // ═══════════════════════════════════════════════════════════════
    // Custom numeric masks
    // ═══════════════════════════════════════════════════════════════

    [TestCase("'{3.14159:0.00}'", "3.14")]
    [TestCase("'{3.1:0.00}'", "3.10")]
    [TestCase("'{3.1:#.##}'", "3.1")]
    [TestCase("'{3.0:#.##}'", "3")]
    [TestCase("'{1234567:#,##0}'", "1,234,567")]
    [TestCase("'{42:0000}'", "0042")]
    [TestCase("'{9.99:0.0}'", "10.0")]
    [TestCase("'{0.001:0.####}'", "0.001")]
    [TestCase("'{42:0.00}'", "42.00")]
    [TestCase("'{1000000:#,##0}'", "1,000,000")]
    [TestCase("'{0:#,##0}'", "0")]
    [TestCase("'{-3.14:0.0}'", "-3.1")]
    [TestCase("'{100:#}'", "100")]
    [TestCase("'{0.5:0}'", "1")]                 // rounds
    [TestCase("'{99.999:0.00}'", "100.00")]       // rounds up
    public void CustomMask(string expr, string expected) =>
        $"y = {expr}".AssertReturns("y", expected);

    // ═══════════════════════════════════════════════════════════════
    // Named specifiers
    // ═══════════════════════════════════════════════════════════════

    [TestCase("'{255:hex}'", "FF")]
    [TestCase("'{255:HEX}'", "FF")]
    [TestCase("'{0:hex}'", "0")]
    [TestCase("'{16:hex}'", "10")]
    [TestCase("'{42:bin}'", "101010")]
    [TestCase("'{0:bin}'", "0")]
    [TestCase("'{1:bin}'", "1")]
    [TestCase("'{255:bin}'", "11111111")]
    [TestCase("'{314.159:sci}'", "3.141590e+002")]
    [TestCase("'{0.001:sci}'", "1.000000e-003")]
    [TestCase("'{314.159:SCI}'", "3.141590E+002")]
    [TestCase("'{0.001:SCI}'", "1.000000E-003")]
    public void NamedSpecifier(string expr, string expected) =>
        $"y = {expr}".AssertReturns("y", expected);

    // ═══════════════════════════════════════════════════════════════
    // Alignment only
    // ═══════════════════════════════════════════════════════════════

    // Right align
    [TestCase("'[{42:>8}]'", "[      42]")]
    [TestCase("'[{42:>3}]'", "[ 42]")]
    [TestCase("'[{42:>2}]'", "[42]")]           // exact width = no padding
    [TestCase("'[{42:>1}]'", "[42]")]           // narrower = unchanged

    // Left align
    [TestCase("'[{42:<8}]'", "[42      ]")]
    [TestCase("'[{42:<2}]'", "[42]")]

    // Center align
    [TestCase("'[{42:^8}]'", "[   42   ]")]
    [TestCase("'[{42:^7}]'", "[  42   ]")]      // odd padding: extra right
    [TestCase("'[{42:^2}]'", "[42]")]

    // Align on text
    [TestCase("'[{'hello':^15}]'", "[     hello     ]")]
    [TestCase("'[{'hi':<6}]'", "[hi    ]")]
    [TestCase("'[{'hi':>6}]'", "[    hi]")]

    // Align on negative numbers
    [TestCase("'[{-42:>6}]'", "[   -42]")]
    [TestCase("'[{-42:<6}]'", "[-42   ]")]
    public void AlignOnly(string expr, string expected) =>
        $"y = {expr}".AssertReturns("y", expected);

    // ═══════════════════════════════════════════════════════════════
    // Format + alignment (format first, then alignment)
    // ═══════════════════════════════════════════════════════════════

    // mask:align
    [TestCase("'[{3.14:0.00:>10}]'", "[      3.14]")]
    [TestCase("'[{3.14:0.00:<10}]'", "[3.14      ]")]
    [TestCase("'[{3.14:0.00:^10}]'", "[   3.14   ]")]
    [TestCase("'[{1234:#,##0:>12}]'", "[       1,234]")]

    // named:align
    [TestCase("'[{255:hex:>6}]'", "[    FF]")]
    [TestCase("'[{42:bin:>10}]'", "[    101010]")]
    [TestCase("'[{42:bin:^12}]'", "[   101010   ]")]
    [TestCase("'[{255:hex:<6}]'", "[FF    ]")]
    public void FormatPlusAlign(string expr, string expected) =>
        $"y = {expr}".AssertReturns("y", expected);

    // ═══════════════════════════════════════════════════════════════
    // Expressions inside interpolation with format
    // ═══════════════════════════════════════════════════════════════

    // Arithmetic
    [TestCase("'{1+2:0.00}'", "3.00")]
    [TestCase("'{3.14159 * 2:0.000}'", "6.283")]
    [TestCase("'{100 / 3.0:0.00}'", "33.33")]
    [TestCase("'{2 ** 10.0:0}'", "1024")]

    // Function calls
    [TestCase("'{max(3,5):000}'", "005")]
    [TestCase("'{min(10,20):0.0}'", "10.0")]
    [TestCase("'{round(3.14159, 2):0.00}'", "3.14")]

    // Conditional
    [TestCase("'{if(true) 42 else 0:hex}'", "2A")]
    [TestCase("'{if(1>0) 255 else 0:hex}'", "FF")]

    // Parenthesized
    [TestCase("'{(1+2)*10:0.0}'", "30.0")]
    [TestCase("'{(255):hex}'", "FF")]

    // Complex expression + alignment
    [TestCase("'[{max(3,5):>6}]'", "[     5]")]
    [TestCase("'[{1+2:0.00:>10}]'", "[      3.00]")]

    // Operator precedence
    [TestCase("'{1+2*3:0.00}'", "7.00")]         // * before +
    [TestCase("'{(1+2)*3:0.00}'", "9.00")]        // parens override

    // Negative values
    [TestCase("'{-42:0.00}'", "-42.00")]
    [TestCase("'{-3.14:0.0}'", "-3.1")]

    // Pipe-forward
    [TestCase("'{3.14159.round(2):0.00}'", "3.14")]

    // Comparison → bool → toText
    [TestCase("'{1 > 0:>5}'", " true")]
    [TestCase("'{1 < 0:>5}'", "false")]

    // Boolean
    [TestCase("'{not false:>6}'", "  true")]
    [TestCase("'{true and true:>6}'", "  true")]
    public void ExpressionWithFormat(string expr, string expected) =>
        $"y = {expr}".AssertReturns("y", expected);

    // ═══════════════════════════════════════════════════════════════
    // Array/collection expressions with format
    // ═══════════════════════════════════════════════════════════════

    [Test] public void ArrayElementWithFormat() =>
        "y = '{[10,20,30][1]:0.0}'".AssertReturns("y", "20.0");

    [Test] public void ArraySumWithFormat() =>
        "y = '{[1,2,3].sum():#,##0}'".AssertReturns("y", "6");

    [Test] public void ArrayCountWithAlign() =>
        "y = '[{[1,2,3].count():>4}]'".AssertReturns("y", "[   3]");

    [Test] public void ArrayMapSumWithFormat() =>
        "y = '{[1,2,3].map(rule it*10).sum():0.00}'".AssertReturns("y", "60.00");

    [Test] public void ArrayFilterCountWithAlign() =>
        "y = '[{[1,2,3,4,5].filter(rule it>2).count():>4}]'".AssertReturns("y", "[   3]");

    // ═══════════════════════════════════════════════════════════════
    // String operations with alignment
    // ═══════════════════════════════════════════════════════════════

    [Test] public void StringReverseWithAlign() =>
        "y = '[{'hello'.reverse():>10}]'".AssertReturns("y", "[     olleh]");

    [Test] public void StringConcatWithAlign() =>
        "y = '[{'a'.concat('b'):^6}]'".AssertReturns("y", "[  ab  ]");

    [Test] public void StringToUpperWithAlign() =>
        "y = '[{'hello'.toUpper():<10}]'".AssertReturns("y", "[HELLO     ]");

    // ═══════════════════════════════════════════════════════════════
    // Struct field access with format
    // ═══════════════════════════════════════════════════════════════

    [Test] public void StructFieldWithFormat() =>
        "y = '{{age = 25.5; score = 100}.age:0.0}'".AssertReturns("y", "25.5");

    [Test] public void StructFieldWithAlign() =>
        "y = '[{{x = 42}.x:>6}]'".AssertReturns("y", "[    42]");

    // ═══════════════════════════════════════════════════════════════
    // If-else expressions with format
    // ═══════════════════════════════════════════════════════════════

    [Test] public void IfElseWithMask() =>
        "y = '{if(true) 3.14 else 0:0.00}'".AssertReturns("y", "3.14");

    [Test] public void IfElseWithAlign() =>
        "y = '[{if(true) 42 else 0:>6}]'".AssertReturns("y", "[    42]");

    [Test] public void IfElseWithFormatAndAlign() =>
        "y = '[{if(true) 3.14 else 0:0.00:>10}]'".AssertReturns("y", "[      3.14]");

    // ═══════════════════════════════════════════════════════════════
    // Implicit multiplication with format
    // ═══════════════════════════════════════════════════════════════

    [Test] public void ImplicitMult_WithMask() =>
        "x:int \r y = '{2x:0.00}'".Calc(("x", 5)).AssertReturns(("y", "10.00"));

    [Test] public void ImplicitMult_WithAlign() =>
        "x:int \r y = '[{2x:>6}]'".Calc(("x", 5)).AssertReturns(("y", "[    10]"));

    [Test] public void ImplicitMult_Real() =>
        "x:int \r y = '{2.5x:0.0}'".Calc(("x", 4)).AssertReturns(("y", "10.0"));

    [Test] public void ImplicitMult_Complex() =>
        "x:int \r y = '{2x + 1:000}'".Calc(("x", 3)).AssertReturns(("y", "007"));

    [Test] public void ImplicitMult_WithHex() =>
        "x:int \r y = '{2x:hex}'".Calc(("x", 8)).AssertReturns(("y", "10"));

    [Test] public void ImplicitMult_Superscript() =>
        "x:int \r y = '{x²:0.00}'".Calc(("x", 3)).AssertReturns(("y", "9.00"));

    // ═══════════════════════════════════════════════════════════════
    // Multiple interpolations in one string
    // ═══════════════════════════════════════════════════════════════

    [Test] public void MultipleFormats() =>
        "y = '{3.14159:0.00} and {42:000}'".AssertReturns("y", "3.14 and 042");

    [Test] public void MixedFormatAndNoFormat() =>
        "y = '{3.14159:0.00} is {42}'".AssertReturns("y", "3.14 is 42");

    [Test] public void ThreeWithFormats() =>
        "y = '{1:000}-{2:000}-{3:000}'".AssertReturns("y", "001-002-003");

    [Test] public void AlignedTableRow() =>
        "y = '{42:>6} | {3.14:0.00:>8}'".AssertReturns("y", "    42 |     3.14");

    [Test] public void MultipleAligned() =>
        "y = '[{'A':^5}|{'B':^5}|{'C':^5}]'".AssertReturns("y", "[  A  |  B  |  C  ]");

    [Test] public void FormatAndAlignMixed() =>
        "y = '{255:hex:>6} {42:bin:<10}'".AssertReturns("y", "    FF 101010    ");

    // ═══════════════════════════════════════════════════════════════
    // Dollar-prefixed strings
    // ═══════════════════════════════════════════════════════════════

    [Test] public void DollarString_WithFormat() =>
        "y = $'pi = ${3.14:0.0}'".AssertReturns("y", "pi = 3.1");

    [Test] public void DollarString_WithAlign() =>
        "y = $'[${42:>6}]'".AssertReturns("y", "[    42]");

    [Test] public void DollarString_FormatAndAlign() =>
        "y = $'[${3.14:0.00:>10}]'".AssertReturns("y", "[      3.14]");

    // ═══════════════════════════════════════════════════════════════
    // Input variables with format
    // ═══════════════════════════════════════════════════════════════

    [Test] public void VariableWithFormat() =>
        "x:int \r y = '{x:0.00}'".Calc(("x", 42)).AssertReturns(("y", "42.00"));

    [Test] public void VariableWithAlign() =>
        "x:int \r y = '[{x:>6}]'".Calc(("x", 42)).AssertReturns(("y", "[    42]"));

    [Test] public void VariableWithFormatAndAlign() =>
        "x:real \r y = '[{x:0.00:>10}]'".Calc(("x", 3.14)).AssertReturns(("y", "[      3.14]"));

    // ═══════════════════════════════════════════════════════════════
    // Empty format / edge cases
    // ═══════════════════════════════════════════════════════════════

    [Test] public void EmptyFormat_ConvertsToText() =>
        "y = '{42:}'".AssertReturns("y", "42");

    [Test] public void OnlyWhitespace_ConvertsToText() =>
        "y = '{42: }'".AssertReturns("y", "42");

    // ═══════════════════════════════════════════════════════════════
    // Regression: no format
    // ═══════════════════════════════════════════════════════════════

    [Test] public void Regression_NoFormat() =>
        "y = 'answer is {42}!'".AssertReturns("y", "answer is 42!");

    [Test] public void Regression_MultipleNoFormat() =>
        "y = '{1} + {2} = {1+2}'".AssertReturns("y", "1 + 2 = 3");

    [Test] public void Regression_PlainText() =>
        "y = 'hello world'".AssertReturns("y", "hello world");

    [Test] public void Regression_VariableInterpolation() =>
        "x:int \r y = 'val={x}'".Calc(("x", 42)).AssertReturns(("y", "val=42"));

    [Test] public void Regression_NestedParensInExpression() =>
        "y = '{(1+2)*(3+4)}'".AssertReturns("y", "21");

    [Test] public void Regression_ArrayInInterpolation() =>
        "y = '{[1,2,3]}'".AssertReturns("y", "[1,2,3]");

    // ═══════════════════════════════════════════════════════════════
    // Error: invalid format
    // ═══════════════════════════════════════════════════════════════

    [TestCase("'{42:foo}'")]
    [TestCase("'{42:abc}'")]
    [TestCase("'{42:hello}'")]
    [TestCase("'{42:X}'")]          // reserved for future named
    [TestCase("'{42:F2}'")]         // reserved for future named
    public void Error_InvalidFormat_ParseTime(string expr) =>
        Assert.Throws<FunnyParseException>(() => $"y = {expr}".Build());

    // ═══════════════════════════════════════════════════════════════
    // Error: format on wrong type (TIC catches at compile time)
    // ═══════════════════════════════════════════════════════════════

    [Test] public void Error_MaskOnText() =>
        Assert.Throws<FunnyParseException>(() => "y = '{'hello':0.00}'".Build());

    [Test] public void Error_MaskOnBool() =>
        Assert.Throws<FunnyParseException>(() => "y = '{true:0.00}'".Build());

    [Test] public void Error_BinOnReal() =>
        Assert.Throws<FunnyParseException>(() => "y = '{3.14:bin}'".Build());

    [Test] public void Error_HexOnReal() =>
        Assert.Throws<FunnyParseException>(() => "y = '{3.14:hex}'".Build());

    // ═══════════════════════════════════════════════════════════════
    // Error: invalid alignment (parse time)
    // ═══════════════════════════════════════════════════════════════

    [Test] public void Error_AlignNoWidth() =>
        Assert.Throws<FunnyParseException>(() => "y = '{42:>}'".Build());

    // ═══════════════════════════════════════════════════════════════
    // Dynamic alignment width (expressions)
    // ═══════════════════════════════════════════════════════════════

    [Test] public void AlignDynamic_Variable() =>
        "w:int \r y = '[{42:>w}]'".Calc(("w", 8)).AssertReturns(("y", "[      42]"));

    [Test] public void AlignDynamic_Expression() =>
        "w:int \r y = '[{42:>(w*2)}]'".Calc(("w", 4)).AssertReturns(("y", "[      42]"));

    [Test] public void AlignDynamic_FunctionCall() =>
        "y = '[{42:>(max(6,10))}]'".AssertReturns("y", "[        42]");

    [Test] public void AlignDynamic_FormatPlusVariable() =>
        "w:int \r y = '[{3.14:0.00:>w}]'".Calc(("w", 10)).AssertReturns(("y", "[      3.14]"));

    [Test] public void AlignDynamic_NamedPlusVariable() =>
        "w:int \r y = '[{255:hex:>w}]'".Calc(("w", 6)).AssertReturns(("y", "[    FF]"));

    [Test] public void AlignDynamic_CenterVariable() =>
        "w:int \r y = '[{42:^w}]'".Calc(("w", 8)).AssertReturns(("y", "[   42   ]"));

    [Test] public void AlignDynamic_LeftVariable() =>
        "w:int \r y = '[{42:<w}]'".Calc(("w", 8)).AssertReturns(("y", "[42      ]"));

    [Test] public void Error_AlignInvalidWidth() =>
        Assert.Throws<FunnyParseException>(() => "y = '{42:>-5}'".Build());

    [Test] public void Error_AlignExpressionWithoutParens() =>
        Assert.That(() => "y = '{42:>w*2}'".Build(), Throws.Exception);
}
