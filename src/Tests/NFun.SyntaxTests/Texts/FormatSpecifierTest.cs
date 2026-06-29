namespace NFun.SyntaxTests.Texts;

using Exceptions;
using TestTools;
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

    // Width-preserving hex/bin on int8 (matches ToHexText/ToBinText behavior).
    [TestCase("x:int8=-1\r y='{x:hex}'", "FF")]
    [TestCase("x:int8=-1\r y='{x:bin}'", "11111111")]
    [TestCase("x:int8=5\r  y='{x}'",     "5")]
    public void NamedSpecifier_NarrowSignedOperand(string expr, string expected) =>
        expr.AssertResultHas("y", expected);

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
    // Array, struct, if-else, multiple interpolation, dollar-strings,
    // implicit-mult, regressions — all assertable via TestCase
    // ═══════════════════════════════════════════════════════════════

    // Array / collection
    [TestCase("'{[10,20,30][1]:0.0}'", "20.0")]
    [TestCase("'{[1,2,3].sum():#,##0}'", "6")]
    [TestCase("'[{[1,2,3].count():>4}]'", "[   3]")]
    [TestCase("'{[1,2,3].map(rule it*10).sum():0.00}'", "60.00")]
    [TestCase("'[{[1,2,3,4,5].filter(rule it>2).count():>4}]'", "[   3]")]
    // String operations with alignment
    [TestCase("'[{'hello'.reverse():>10}]'", "[     olleh]")]
    [TestCase("'[{'a'.concat('b'):^6}]'", "[  ab  ]")]
    [TestCase("'[{'hello'.toUpper():<10}]'", "[HELLO     ]")]
    // Struct field access
    [TestCase("'{{age = 25.5; score = 100}.age:0.0}'", "25.5")]
    [TestCase("'[{{x = 42}.x:>6}]'", "[    42]")]
    // If-else
    [TestCase("'{if(true) 3.14 else 0:0.00}'", "3.14")]
    [TestCase("'[{if(true) 42 else 0:>6}]'", "[    42]")]
    [TestCase("'[{if(true) 3.14 else 0:0.00:>10}]'", "[      3.14]")]
    // Multiple interpolations in one string
    [TestCase("'{3.14159:0.00} and {42:000}'", "3.14 and 042")]
    [TestCase("'{3.14159:0.00} is {42}'", "3.14 is 42")]
    [TestCase("'{1:000}-{2:000}-{3:000}'", "001-002-003")]
    [TestCase("'{42:>6} | {3.14:0.00:>8}'", "    42 |     3.14")]
    [TestCase("'[{'A':^5}|{'B':^5}|{'C':^5}]'", "[  A  |  B  |  C  ]")]
    [TestCase("'{255:hex:>6} {42:bin:<10}'", "    FF 101010    ")]
    // Dollar-prefixed strings
    [TestCase("$'pi = ${3.14:0.0}'", "pi = 3.1")]
    [TestCase("$'[${42:>6}]'", "[    42]")]
    [TestCase("$'[${3.14:0.00:>10}]'", "[      3.14]")]
    // Empty format / whitespace-only edge cases
    [TestCase("'{42:}'", "42")]
    [TestCase("'{42: }'", "42")]
    // Regression: no-format interpolations + plain text
    [TestCase("'answer is {42}!'", "answer is 42!")]
    [TestCase("'{1} + {2} = {1+2}'", "1 + 2 = 3")]
    [TestCase("'hello world'", "hello world")]
    [TestCase("'{(1+2)*(3+4)}'", "21")]
    [TestCase("'{[1,2,3]}'", "[1,2,3]")]
    public void ConstantInterpolationWithFormat(string expr, string expected) =>
        $"y = {expr}".AssertReturns("y", expected);

    // ── Input variables / implicit multiplication: need Calc(...) ─────

    [TestCase("'{x:0.00}'", "x", 42, "42.00")]
    [TestCase("'[{x:>6}]'", "x", 42, "[    42]")]
    [TestCase("'{2x:0.00}'", "x", 5, "10.00")]
    [TestCase("'[{2x:>6}]'", "x", 5, "[    10]")]
    [TestCase("'{2x + 1:000}'", "x", 3, "007")]
    [TestCase("'{2x:hex}'", "x", 8, "10")]
    [TestCase("'{x²:0.00}'", "x", 3, "9.00")]
    [TestCase("'val={x}'", "x", 42, "val=42")]
    public void IntVarInterpolationWithFormat(string expr, string id, int x, string expected) =>
        $"x:int \r y = {expr}".Calc((id, x)).AssertReturns(("y", expected));

    [TestCase("'{2.5x:0.0}'", "x", 4, "10.0")]
    [TestCase("'[{x:0.00:>10}]'", "x", 3.14, "[      3.14]")]
    public void RealVarInterpolationWithFormat(string expr, string id, double x, string expected) =>
        $"x:real \r y = {expr}".Calc((id, x)).AssertReturns(("y", expected));

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
