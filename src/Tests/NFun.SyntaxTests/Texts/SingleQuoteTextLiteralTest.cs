using NFun.TestTools;
using NUnit.Framework;

namespace NFun.SyntaxTests.Texts;

[TestFixture]
public class SingleQuoteTextLiteralTest {

    // ── Basic literals ──────────────────────────────────────────────────────────────────

    [TestCase("y = ''", "")]
    [TestCase("y = 'hi'", "hi")]
    [TestCase("y = 'World'", "World")]
    public void BasicLiteral(string expr, string expected)
        => expr.AssertReturns(expected);

    // ── Interpolation ─────────────────────────────────────────────────────────────────

    [TestCase("y = '{0}'", "0")]
    [TestCase("y = 'hi {42}'", "hi 42")]
    [TestCase("y = '{42}hi'", "42hi")]
    [TestCase("y = 'hello {42} world'", "hello 42 world")]
    [TestCase("y = 'hello {42+1} world'", "hello 43 world")]
    [TestCase("y = '{''}'", "")]
    [TestCase("y = 'hi {42} and {21}'", "hi 42 and 21")]
    [TestCase("y = 'hi {42+13} and {21-1}'", "hi 55 and 20")]
    [TestCase("y = '{0+1} {1+2} {2+3}'", "1 3 5")]
    public void Interpolation(string expr, string expected)
        => expr.AssertReturns(expected);

    [TestCase("y = 'pre {'p{42-1*2}m{21-1+10*3}a'} mid {'p{42-2}m{21-1}a'} fin'", "pre p40m50a mid p40m20a fin")]
    [TestCase("y = 'pre \"{'p\"{42-1*2}\"m{21-1+10*3}a\"'} mid\" {'p{42-2}\"m{21-1}a'}\"fin\"'",
        "pre \"p\"40\"m50a\" mid\" p40\"m20a\"fin\"")]
    [TestCase("y = \"pre {\"p{42-1*2}m{21-1+10*3}a\"} mid {\"p{42-2}m{21-1}a\"} fin\"", "pre p40m50a mid p40m20a fin")]
    [TestCase("y = \"pre4' '{\"'p'{42-1*2}'m'{21-1+10*3}'a'\"}'m'i'd'{\"'p'{42-2}'m'{21-1}'a'\"}''fin'\"",
        "pre4' ''p'40'm'50'a''m'i'd''p'40'm'20'a'''fin'")]
    [TestCase("y = 'pre1{'pre2{2-2}after2'}after1'", "pre1pre20after2after1")]
    [TestCase("y = 'pre1 {'inside'} after1'", "pre1 inside after1")]
    public void NestedInterpolation(string expr, string expected)
        => expr.AssertReturns(expected);

    [TestCase(42.0, "y = 'pre{x-1*2}mid{x*x/x}fin'", "pre40mid42fin")]
    [TestCase(42, "x:int; y = 'pre{x-1*2}mid{x*x/x}fin'", "pre40mid42fin")]
    public void InterpolationWithVariable(object input, string expr, object expected) =>
        expr.Calc("x", input).AssertReturns(expected);

    // ── Quotes inside ─────────────────────────────────────────────────────────────────

    [TestCase("y =  ‘some \"figure\" quotes‘", "some \"figure\" quotes")]
    [TestCase("y =  “some \"figure\" quotes“", "some \"figure\" quotes")]
    [TestCase("y =  ‘some 'figure' quotes‘", "some 'figure' quotes")]
    [TestCase("y =  “some 'figure' quotes“", "some 'figure' quotes")]
    public void QuotesInside(string expr, string expected)
        => expr.AssertReturns(expected);

    [TestCase("y='hello1\"{0}world'", "hello1\"0world")]
    [TestCase("y='hello2{0}\"world'", "hello20\"world")]
    [TestCase("y='hello3{0}\"world{0}'", "hello30\"world0")]
    [TestCase("y=\"hello4'{0}world\"", "hello4'0world")]
    [TestCase("y=\"hello5{0}'world\"", "hello50'world")]
    [TestCase("y=\"hello6{0}'world{0}\"", "hello60'world0")]
    public void QuotesInInterpolation(string expr, string expected)
        => expr.AssertReturns(expected);

    // ── Escape sequences ────────────────────────────────────────────────────────────────

    [TestCase("y='  \\\\'", "  \\")]
    [TestCase("y='\\t'", "\t")]
    [TestCase("y='\\n'", "\n")]
    [TestCase("y='\\''", "'")]
    [TestCase("y='\\r'", "\r")]
    [TestCase("y='\\\"'", "\"")]
    [TestCase("y='\\\\'", "\\")]
    [TestCase("y='e\\''", "e'")]
    [TestCase("y='#\\r'", "#\r")]
    [TestCase("y=' \\r\r'", " \r\r")]
    [TestCase("y='\\r\r'", "\r\r")]
    [TestCase("y='  \\\\  '", "  \\  ")]
    [TestCase("y='John: \\'fuck you!\\', he stops.'", "John: 'fuck you!', he stops.")]
    [TestCase("y='w\\t'", "w\t")]
    [TestCase("y='w\\\\\\t'", "w\\\t")]
    [TestCase("y='q\\t'", "q\t")]
    [TestCase("y='w\\\"'", "w\"")]
    [TestCase("y=' \\r'", " \r")]
    [TestCase("y='\t \\n'", "\t \n")]
    [TestCase("y='q\\tg'", "q\tg")]
    [TestCase("y='e\\\\mm\\''", "e\\mm'")]
    [TestCase("y=' \\r\r'", " \r\r")]
    [TestCase("y='\t \\n\n'", "\t \n\n")]
    [TestCase("y='pre \\{lalala\\} after'", "pre {lalala} after")]
    public void EscapeSequences(string expr, string expected)
        => expr.AssertReturns(expected);

    // ── Error cases ───────────────────────────────────────────────────────────────────

    [TestCase("y='hell")]
    [TestCase("y=hell'")]
    [TestCase("y='")]
    [TestCase("y = '")]
    [TestCase("'\\'")]
    [TestCase("'some\\'")]
    [TestCase("'\\GGG'")]
    [TestCase("'\\q'")]
    [TestCase("y='hello'world'")]
    [TestCase("y='hello''world'")]
    [TestCase("y='hello{}world'")]
    [TestCase("y='hello'{0}world'")]
    [TestCase("y='hello{0}'world'")]
    [TestCase("y='hello{0}world'{0}'")]
    [TestCase("y='hello{0}world{0}''")]
    [TestCase("y='hello{0}world{0}")]
    [TestCase("y='hello{{0}world{0}'")]
    [TestCase("y='hello{0{}world{0}'")]
    [TestCase("y='hello{0}world{{0}'")]
    [TestCase("y='hello{0}world{0{}'")]
    [TestCase("y='hello{'i'{0}}world{{0}'")]
    [TestCase("y='hello{{0}'i'}world{{0}'")]
    [TestCase("y={0}'hello'world'")]
    [TestCase("y='pre {0}''fin'")]
    [TestCase("y='pre {0}''mid{1}fin'")]
    [TestCase("y='hello3{0}\"world{0}\"")]
    [TestCase("y='hello3{0}\"world{0'")]
    [TestCase("y =  ‘some \"figure\" quotes\"")]
    [TestCase("y =  “some \"figure\" quotes'")]
    [TestCase("y =  ‘some \'figure\' quotes“")]
    [TestCase("y =  “some \'figure\' quotes‘")]
    public void ObviousFails(string expr) => expr.AssertObviousFailsOnParse();

    // ── Dollar-prefix $'...' ────────────────────────────────────────────────────────────────

    [TestCase("y = $'hello world'", "hello world")]
    [TestCase("y = $'plain text'", "plain text")]
    [TestCase("y = $''", "")]
    [TestCase("y = $\"hello\"", "hello")]
    public void DollarPrefix_PlainText(string expr, string expected)
        => expr.AssertReturns(expected);

    [TestCase("y = $'{x} and {y}'", "{x} and {y}")]
    [TestCase("y = $'{not interpolation}'", "{not interpolation}")]
    [TestCase("y = $'{ }'", "{ }")]
    [TestCase("y = $'a { b } c'", "a { b } c")]
    public void DollarPrefix_BracesAreLiteral(string expr, string expected)
        => expr.AssertReturns(expected);

    [Test]
    public void DollarPrefix_BasicInterpolation()
        => "y = $'value: ${x}'".Calc("x", 42).AssertReturns("value: 42");

    [Test]
    public void DollarPrefix_ExpressionInterpolation()
        => "y = $'sum: ${1+2}'".AssertReturns("sum: 3");

    [Test]
    public void DollarPrefix_MultipleInterpolations()
        => "y = $'${x} and ${z}'".Calc(("x", 1), ("z", 2)).AssertReturns("1 and 2");

    [TestCase("y = $'price: $5'", "price: $5")]
    [TestCase("y = $'$$'", "$$")]
    [TestCase("y = $'$'", "$")]
    [TestCase("y = $'100$'", "100$")]
    public void DollarPrefix_DollarNotBeforeBraceIsLiteral(string expr, string expected)
        => expr.AssertReturns(expected);

    [Test]
    public void DollarPrefix_ExcessDollar()
        // $$ before { in $'...' → 1 excess dollar + interpolation
        => "y = $'$${x}'".Calc("x", 42).AssertReturns("$42");

    [Test]
    public void DollarPrefix_EscapedDollar() {
        // \$ prevents interpolation trigger
        var expr = @"y = $'\${not}'";
        expr.AssertReturns("${not}");
    }

    [Test]
    public void DollarPrefix_EscapedDollarBeforeBrace() {
        var expr = @"y = $'\${ }'";
        expr.AssertReturns("${ }");
    }

    [TestCase(@"y = $'hello\nworld'", "hello\nworld")]
    [TestCase(@"y = $'tab\there'", "tab\there")]
    [TestCase(@"y = $'back\\slash'", "back\\slash")]
    [TestCase(@"y = $'\{literal}'", "{literal}")]
    [TestCase(@"y = $'\}literal'", "}literal")]
    public void DollarPrefix_EscapeSequences(string expr, string expected)
        => expr.AssertReturns(expected);

    [TestCase(@"y = 'hello \$ world'", "hello $ world")]
    [TestCase(@"y = $'hello \$ world'", "hello $ world")]
    [TestCase(@"y = $$'hello \$ world'", "hello $ world")]
    public void DollarEscape_InAllStringTypes(string expr, string expected)
        => expr.AssertReturns(expected);

    [Test]
    public void DollarEscape_BreaksInterpolationTrigger() {
        // In $'...', \${ should NOT trigger interpolation
        var expr = @"y = $'\${not interpolation}'";
        expr.AssertReturns("${not interpolation}");
    }

    [Test]
    public void DollarPrefix_AdjacentInterpolations()
        => "y = $'${x}${z}'".Calc(("x", "a"), ("z", "b")).AssertReturns("ab");

    [Test]
    public void DollarPrefix_AdjacentWithTextBetween()
        => "y = $'${x}-${z}'".Calc(("x", "a"), ("z", "b")).AssertReturns("a-b");

    [Test]
    public void DollarPrefix_OnlyInterpolation()
        => "y = $'${x}'".Calc("x", "hello").AssertReturns("hello");

    [Test]
    public void DollarPrefix_InterpolationAtStart()
        => "y = $'${x} after'".Calc("x", "hi").AssertReturns("hi after");

    [Test]
    public void DollarPrefix_InterpolationAtEnd()
        => "y = $'before ${x}'".Calc("x", "hi").AssertReturns("before hi");

    [Test]
    public void DollarPrefix_NestedRegularString()
        => "y = $'outer ${'inner {x}'}'".Calc("x", 42).AssertReturns("outer inner 42");

    [Test]
    public void DollarPrefix_NestedDollarString()
        // Inside ${}, normal NFun expression — $'...' starts a new dollar-prefix string
        => "y = $'a${$'b${x}c'}d'".Calc("x", 1).AssertReturns("ab1cd");

    [TestCase("y = $'hello' == 'hello'", true)]
    [TestCase("y = $$'hello' == 'hello'", true)]
    [TestCase("y = $'{literal}' == '\\{literal\\}'", true)]
    public void DollarPrefix_Equality(string expr, bool expected)
        => expr.AssertReturns(expected);

    [Test]
    public void DollarPrefix_EqualsRegularInterpolation()
        => "y = $'value ${x}' == 'value {x}'".Calc("x", 42).AssertReturns(true);

    [Test]
    public void DollarPrefix_InArray() {
        var expr = "y = [$'a', $'b', $'c']";
        expr.AssertReturns(new[] { "a", "b", "c" });
    }

    [Test]
    public void DollarPrefix_InFunctionCall() {
        var expr = "y = $'hello'.reverse()";
        expr.AssertReturns("olleh");
    }

    [Test]
    public void DollarPrefix_TypeAnnotation() {
        var expr = "y:text = $'hello'";
        expr.AssertReturns("hello");
    }

    [TestCase("y = $'abc' < $'xyz'", true)]
    [TestCase("y = $'xyz' > $'abc'", true)]
    [TestCase("y = $'abc' == $$'abc'", true)]
    public void DollarPrefix_Comparison(string expr, bool expected)
        => expr.AssertReturns(expected);

    [Test]
    public void DollarPrefix_Slicing()
        => "y = $'hello'[0:2]".AssertReturns("hel");

    [Test]
    public void DollarPrefix_ToUpper()
        => "y = $'hello'.toUpper()".AssertReturns("HELLO");

    [Test]
    public void DollarPrefix_Length()
        => "y = $'hello'.count()".AssertReturns((int)5);

    [Test]
    public void DollarPrefix_MultipleDollarStringsInScript()
        => "y = $'hello' == 'hello' and $$'world' == 'world'"
            .AssertReturns(true);

    [Test]
    public void DollarPrefix_DollarAndRegularStringsTogether()
        => "y = $'{literal}' == '\\{literal\\}'"
            .AssertReturns(true);

    // ── Double dollar $$'...' ───────────────────────────────────────────────────────────────

    [TestCase("y = $$'hello'", "hello")]
    [TestCase("y = $$''", "")]
    [TestCase("y = $$\"hello\"", "hello")]
    public void DoubleDollar_PlainText(string expr, string expected)
        => expr.AssertReturns(expected);

    [TestCase("y = $$'{not}'", "{not}")]
    [TestCase("y = $$'${not}'", "${not}")]
    [TestCase("y = $$'a { b } c'", "a { b } c")]
    public void DoubleDollar_BracesAndSingleDollarBraceAreLiteral(string expr, string expected)
        => expr.AssertReturns(expected);

    [Test]
    public void DoubleDollar_BasicInterpolation()
        => "y = $$'value: $${x}'".Calc("x", 42).AssertReturns("value: 42");

    [Test]
    public void DoubleDollar_ExpressionInterpolation()
        => "y = $$'sum: $${1+2}'".AssertReturns("sum: 3");

    [TestCase("y = $$'$100'", "$100")]
    [TestCase("y = $$'$$'", "$$")]
    [TestCase("y = $$'$'", "$")]
    public void DoubleDollar_DollarsAreLiteral(string expr, string expected)
        => expr.AssertReturns(expected);

    [Test]
    public void DoubleDollar_ExcessDollar()
        // $$$ before { in $$'...' → 1 excess dollar + interpolation
        => "y = $$'$$${x}'".Calc("x", 42).AssertReturns("$42");

    [Test]
    public void DoubleDollar_AdjacentInterpolations()
        => "y = $$'$${x}$${z}'".Calc(("x", "a"), ("z", "b")).AssertReturns("ab");

    [TestCase(@"y = $$'hello\nworld'", "hello\nworld")]
    [TestCase(@"y = $$'tab\there'", "tab\there")]
    [TestCase(@"y = $$'back\\slash'", "back\\slash")]
    [TestCase(@"y = $$'\''", "'")]
    [TestCase(@"y = $$'\""'", "\"")]
    [TestCase(@"y = $$'\r'", "\r")]
    [TestCase(@"y = $$'\{'", "{")]
    [TestCase(@"y = $$'\}'", "}")]
    [TestCase(@"y = $$'\$'", "$")]
    public void DoubleDollar_AllEscapeSequences(string expr, string expected)
        => expr.AssertReturns(expected);

    // ── Triple dollar $$$'...' ──────────────────────────────────────────────────────────────

    [TestCase("y = $$$'hello'", "hello")]
    [TestCase("y = $$$'{a}'", "{a}")]
    [TestCase("y = $$$'${a}'", "${a}")]
    [TestCase("y = $$$'$${a}'", "$${a}")]
    public void TripleDollar_LowerLevelsAreLiteral(string expr, string expected)
        => expr.AssertReturns(expected);

    [Test]
    public void TripleDollar_Interpolation()
        => "y = $$$'val: $$${x}'".Calc("x", 42).AssertReturns("val: 42");

    [Test]
    public void TripleDollar_ExcessDollar()
        => "y = $$$'$$$${x}'".Calc("x", 42).AssertReturns("$42");

    // ── Dollar at boundaries ────────────────────────────────────────────────────────────────

    [TestCase("y = $'text$'", "text$")]
    [TestCase("y = $'$text$'", "$text$")]
    [TestCase("y = $$'text$$'", "text$$")]
    public void DollarBeforeClosingQuote(string expr, string expected)
        => expr.AssertReturns(expected);

    [TestCase("y = $'$$$$$'", "$$$$$")]
    [TestCase("y = $$'$$$$$'", "$$$$$")]
    [TestCase("y = $'$a$b$c'", "$a$b$c")]
    public void ManyDollarsInContent(string expr, string expected)
        => expr.AssertReturns(expected);

    [Test]
    public void MixedDollarTriggersAndLiterals()
        => "y = $'$a ${x} $b ${z} $c'".Calc(("x", 1), ("z", 2)).AssertReturns("$a 1 $b 2 $c");

    [TestCase("y = $'text$}'", "text$}")]
    [TestCase("y = $'a}b'", "a}b")]
    [TestCase("y = $$'$$}'", "$$}")]
    public void DollarAndClosingBrace(string expr, string expected)
        => expr.AssertReturns(expected);

    // ── Dollar-prefix error cases ───────────────────────────────────────────────────────────

    [TestCase("y = $")]
    [TestCase("y = $$")]
    [TestCase("y = $$$")]
    public void DollarWithoutQuote_IsError(string expr)
        => expr.AssertObviousFailsOnParse();

    [TestCase("y = $'unclosed")]
    [TestCase("y = $$'unclosed")]
    [TestCase("y = $\"unclosed")]
    public void DollarPrefix_Unclosed_IsError(string expr)
        => expr.AssertObviousFailsOnParse();

    [TestCase(@"y = $'\a'")]
    [TestCase(@"y = $$'\q'")]
    public void DollarPrefix_UnknownEscape_IsError(string expr)
        => expr.AssertObviousFailsOnParse();
}
