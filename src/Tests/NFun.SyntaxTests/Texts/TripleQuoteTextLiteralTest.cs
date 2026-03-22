using NFun.TestTools;
using NUnit.Framework;

namespace NFun.SyntaxTests.Texts;

[TestFixture]
public class TripleQuoteTextLiteralTest {

    // ── Basic multiline ───────────────────────────────────────────────────────

    [TestCase("y = '''\n'''", "")]
    [TestCase("y = '''\nhello\n'''", "hello")]
    [TestCase("y = '''\nhello\nworld\n'''", "hello\nworld")]
    [TestCase("y = '''\nline1\nline2\nline3\n'''", "line1\nline2\nline3")]
    [TestCase("y = \"\"\"\nhello\n\"\"\"", "hello")]
    [TestCase("y = \"\"\"\nhello\nworld\n\"\"\"", "hello\nworld")]
    public void BasicMultiline(string expr, string expected)
        => expr.AssertReturns(expected);

    // ── Auto-dedent ───────────────────────────────────────────────────────────

    [TestCase("y = '''\n    hello\n    '''", "hello")]
    [TestCase("y = '''\n    hello\n    world\n    '''", "hello\nworld")]
    [TestCase("y = '''\n    hello\n      world\n    '''", "hello\n  world")]
    [TestCase("y = '''\n  a\n    b\n  '''", "a\n  b")]
    [TestCase("y = '''\n\thello\n\t'''", "hello")]
    public void AutoDedent(string expr, string expected)
        => expr.AssertReturns(expected);

    [Test]
    public void AutoDedent_BlankLineExempted() {
        var expr = "y = '''\n    hello\n\n    world\n    '''";
        expr.AssertReturns("hello\n\nworld");
    }

    [Test]
    public void AutoDedent_BlankLineWithSomeSpaces() {
        // blank line with spaces less than baseline — still exempted
        var expr = "y = '''\n    hello\n  \n    world\n    '''";
        expr.AssertReturns("hello\n\nworld");
    }

    [Test]
    public void AutoDedent_ZeroBaseline() {
        var expr = "y = '''\nhello\nworld\n'''";
        expr.AssertReturns("hello\nworld");
    }

    // ── Interpolation ─────────────────────────────────────────────────────────

    [TestCase("y = '''\nhello {42}\n'''", "hello 42")]
    [TestCase("y = '''\n{42} hello\n'''", "42 hello")]
    [TestCase("y = '''\nhello {42} world {7}\n'''", "hello 42 world 7")]
    [TestCase("y = '''\n    hello {42}\n    '''", "hello 42")]
    public void Interpolation(string expr, string expected)
        => expr.AssertReturns(expected);

    [Test]
    public void Interpolation_WithVariable() {
        var expr = "y = '''\nhello {x}\n'''";
        expr.Calc("x", 42).AssertReturns("hello 42");
    }

    [Test]
    public void Interpolation_MultiLine() {
        var expr = "y = '''\n    first {1}\n    second {2}\n    '''";
        expr.AssertReturns("first 1\nsecond 2");
    }

    [Test]
    public void Interpolation_OnlyInterpolation() {
        var expr = "y = '''\n{42}\n'''";
        expr.AssertReturns("42");
    }

    [Test]
    public void Interpolation_NestedString() {
        // interpolation contains a regular string with its own interpolation
        var expr = "y = '''\nhello {'world {1}'}\n'''";
        expr.AssertReturns("hello world 1");
    }

    [Test]
    public void Interpolation_WithBracesInExpression() {
        // array inside interpolation — braces in expression context
        var expr = "y = '''\nlen={[1,2,3].count()}\n'''";
        expr.AssertReturns("len=3");
    }

    [Test]
    public void Interpolation_AtEndOfLine() {
        var expr = "y = '''\nhello {42}\n'''";
        expr.AssertReturns("hello 42");
    }

    [Test]
    public void Interpolation_AtStartOfLine() {
        var expr = "y = '''\n{42} hello\n'''";
        expr.AssertReturns("42 hello");
    }

    [Test]
    public void Interpolation_EmptyTextBetween() {
        var expr = "y = '''\n{1}{2}{3}\n'''";
        expr.AssertReturns("123");
    }

    [Test]
    public void Interpolation_AcrossMultipleLinesWithDedent() {
        var expr = "y = '''\n    a {1}\n    b {2}\n    c {3}\n    '''";
        expr.AssertReturns("a 1\nb 2\nc 3");
    }

    // ── Escape sequences ──────────────────────────────────────────────────────

    [TestCase("y = '''\n\\n\n'''", "\n")]                                       // \n alone
    [TestCase("y = '''\n\\r\n'''", "\r")]                                       // \r alone
    [TestCase("y = '''\n\\t\n'''", "\t")]                                       // \t alone
    [TestCase("y = '''\n\\\\\n'''", "\\")]                                      // \\ alone
    [TestCase("y = '''\n\\'\n'''", "'")]                                        // \' alone
    [TestCase("y = '''\n\\\"\n'''", "\"")]                                      // \" alone
    [TestCase("y = '''\n\\{\n'''", "{")]                                        // \{ alone
    [TestCase("y = '''\n\\}\n'''", "}")]                                        // \} alone
    [TestCase("y = '''\nhello\\nworld\n'''", "hello\nworld")]                   // \n in middle
    [TestCase("y = '''\nhello\\rworld\n'''", "hello\rworld")]                   // \r in middle
    [TestCase("y = '''\nhello\\tworld\n'''", "hello\tworld")]                   // \t in middle
    [TestCase("y = '''\nhello\\\\world\n'''", "hello\\world")]                  // \\ in middle
    [TestCase("y = '''\n\\{not interpolation\\}\n'''", "{not interpolation}")]  // \{ and \} together
    [TestCase("y = '''\n\\{alone\n'''", "{alone")]                              // \{ at start
    [TestCase("y = '''\nalone\\}\n'''", "alone}")]                              // \} at end
    [TestCase("y = '''\na\\\\b\\tc\\nd\n'''", "a\\b\tc\nd")]                   // multiple escapes on one line
    [TestCase("y = '''\n\\n\\r\\t\n'''", "\n\r\t")]                            // consecutive escapes
    [TestCase("y = '''\n  \\\\  \n'''", "  \\  ")]                             // escape with surrounding spaces
    public void EscapeSequences(string expr, string expected)
        => expr.AssertReturns(expected);

    [TestCase("y = '''\n    \\n\n    '''", "\n")]                              // escape with dedent
    [TestCase("y = '''\n    \\t\n    '''", "\t")]                              // escape with dedent
    [TestCase("y = '''\n    hello\\rworld\n    '''", "hello\rworld")]          // \r with dedent
    [TestCase("y = '''\n    a\\\\b\n    '''", "a\\b")]                         // \\ with dedent
    public void EscapeSequencesWithDedent(string expr, string expected)
        => expr.AssertReturns(expected);

    // ── Quotes inside ─────────────────────────────────────────────────────────

    [Test]
    public void SingleQuotesInsideDoubleTriple() {
        var expr = "\"\"\"\nit's here\n\"\"\"";
        expr.AssertReturns("out", "it's here");
    }

    [Test]
    public void DoubleQuotesInsideSingleTriple() {
        var expr = "y = '''\nsay \"hi\"\n'''";
        expr.AssertReturns("say \"hi\"");
    }

    [Test]
    public void TwoConsecutiveSingleQuotesInsideTriple() {
        var expr = "y = '''\nhe said '' yes\n'''";
        expr.AssertReturns("he said '' yes");
    }

    [Test]
    public void TwoConsecutiveDoubleQuotesInsideDoubleTriple() {
        var expr = "\"\"\"\nhe said \"\" yes\n\"\"\"";
        expr.AssertReturns("out", "he said \"\" yes");
    }

    [Test]
    public void EscapedQuoteInSingleTriple() {
        // \' inside ''' — escaped even though single quotes don't close triple
        var expr = "y = '''\nit\\'s\n'''";
        expr.AssertReturns("it's");
    }

    [Test]
    public void EscapedDoubleQuoteInDoubleTriple() {
        // \" inside """ — escaped
        var expr = "\"\"\"\nsay \\\"hi\\\"\n\"\"\"";
        expr.AssertReturns("out", "say \"hi\"");
    }

    // ── Equality with single-line strings ───────────────────────────────────────

    [TestCase("y = '''\nhello\n''' == 'hello'", true)]
    [TestCase("y = '''\nhello\n''' == 'world'", false)]
    [TestCase("y = '''\n\n''' == ''", true)]                               // empty triple == empty single
    [TestCase("y = '''\nhello\nworld\n''' == 'hello\\nworld'", true)]      // multiline == single with \n
    [TestCase("y = '''\nhello\\tworld\n''' == 'hello\\tworld'", true)]     // escape in both
    [TestCase("y = '''\nhello\\rworld\n''' == 'hello\\rworld'", true)]     // \r escape in both
    [TestCase("y = '''\nhello\n''' != 'world'", true)]
    [TestCase("y = '''\nhello\n''' != 'hello'", false)]
    public void EqualityWithSingleLineString(string expr, bool expected)
        => expr.AssertReturns(expected);

    [Test]
    public void EqualityTripleDoubleQuoteWithSingleQuote() {
        // """ variant equals '' variant
        var expr = "\"\"\"\nhello\n\"\"\" == 'hello'";
        expr.AssertReturns("out", true);
    }

    [Test]
    public void EqualityTripleSingleWithTripleDouble() {
        var expr = "a = '''\nhello\n'''\nb = \"\"\"\nhello\n\"\"\"\ny = a == b";
        expr.Build().Run();
    }

    [Test]
    public void EqualityWithDedent() {
        // dedented triple should equal plain string
        var expr = "y = '''\n    hello\n    ''' == 'hello'";
        expr.AssertReturns(true);
    }

    [Test]
    public void EqualityMultilineWithDedent() {
        var expr = "y = '''\n    hello\n    world\n    ''' == 'hello\\nworld'";
        expr.AssertReturns(true);
    }

    // ── Integration ───────────────────────────────────────────────────────────

    [Test]
    public void WithToUpper() {
        var expr = "y = '''\nhello\n'''.toUpper()";
        expr.AssertReturns("HELLO");
    }

    [Test]
    public void WithConcat() {
        var expr = "y = '''\nhello\n'''.concat(' world')";
        expr.AssertReturns("hello world");
    }

    [Test]
    public void WithReverse() {
        var expr = "y = '''\nabc\n'''.reverse()";
        expr.AssertReturns("cba");
    }

    [Test]
    public void MultipleTripleQuotedStringsInScript() {
        var expr = "a = '''\nhello\n'''\nb = '''\nworld\n'''";
        var runtime = expr.Build();
        runtime.Run();
        Assert.AreEqual("hello", runtime["a"].Value);
        Assert.AreEqual("world", runtime["b"].Value);
    }

    [Test]
    public void TripleQuotedInArray() {
        var expr = "y = ['''\nhello\n''', '''\nworld\n''']";
        expr.AssertReturns(new[] { "hello", "world" });
    }

    [Test]
    public void TypeAnnotation() {
        var expr = "y:text = '''\nhello\n'''";
        expr.AssertReturns("hello");
    }

    [Test]
    public void WithSplit() {
        var expr = "y = '''\nhello world\n'''.split(' ')";
        expr.AssertReturns(new[] { "hello", "world" });
    }

    [Test]
    public void WithTrim() {
        var expr = "y = '''\n  hello  \n'''.trim()";
        expr.AssertReturns("hello");
    }

    [Test]
    public void IndexIntoTripleQuoted() {
        var expr = "y = '''\nhello\n'''[0]";
        expr.AssertReturns('h');
    }

    [Test]
    public void LengthOfTripleQuoted() {
        var expr = "y = '''\nhello\n'''.count()";
        expr.AssertReturns(5);
    }

    // ── Error cases ───────────────────────────────────────────────────────────

    [TestCase("y = '''hello'''")]                       // no newline after opening
    [TestCase("y = '''hello\n'''")]                     // no newline after opening (content on same line)
    [TestCase("y = '''\nhello")]                        // unclosed
    [TestCase("y = '''\nhello\nworld")]                 // unclosed
    [TestCase("y = '''\nhello'''")]                     // closing not on own line
    [TestCase("y = '''\n  hello\n world\n  '''")]       // less indent than baseline
    public void ObviousFails(string expr) => expr.AssertObviousFailsOnParse();

    [Test]
    public void MixedTabsAndSpaces_Fails() {
        // baseline is spaces (from closing), content line has tab
        var expr = "y = '''\n\t hello\n    world\n    '''";
        expr.AssertObviousFailsOnParse();
    }

    [Test]
    public void MixedIndentation_SpaceBaselineTabContent() {
        // baseline = 4 spaces, second content line starts with tab
        var expr = "y = '''\n    hello\n\tworld\n    '''";
        expr.AssertObviousFailsOnParse();
    }

    [Test]
    public void MixedIndentation_TabBaselineSpaceContent() {
        // baseline = tab (from closing), content line starts with spaces
        var expr = "y = '''\n\thello\n    world\n\t'''";
        expr.AssertObviousFailsOnParse();
    }

    [Test]
    public void MixedIndentation_TabThenSpaceOnSameLine() {
        // baseline is spaces, content starts with tab+spaces
        var expr = "y = '''\n\t   hello\n    world\n    '''";
        expr.AssertObviousFailsOnParse();
    }

    [Test]
    public void InsufficientIndent_OnFirstContentLine() {
        var expr = "y = '''\n  hello\n    world\n    '''";
        expr.AssertObviousFailsOnParse();
    }

    [Test]
    public void InsufficientIndent_OnMiddleLine() {
        var expr = "y = '''\n    hello\n  middle\n    world\n    '''";
        expr.AssertObviousFailsOnParse();
    }

    [Test]
    public void InsufficientIndent_OnLastContentLine() {
        var expr = "y = '''\n    hello\n    world\n  last\n    '''";
        expr.AssertObviousFailsOnParse();
    }

    [TestCase("y = '''\nhello'''")]                             // closing not on own line (no whitespace before)
    [TestCase("y = '''\n    hello    '''")]                     // closing not on own line (content before)
    [TestCase("y = '''\\nhello\n'''")]                          // backslash-n is not a real newline after opening
    [TestCase("y = '''\n\\q\n'''")]                             // unknown escape sequence
    [TestCase("y = '''\n\\z\n'''")]                             // unknown escape sequence
    public void MoreObviousFails(string expr) => expr.AssertObviousFailsOnParse();

    // ── Edge cases ─────────────────────────────────────────────────────────────

    [Test]
    public void EmptyWithIndentedClosing() {
        // closing has indent but no content lines → empty
        var expr = "y = '''\n    '''";
        expr.AssertReturns("");
    }

    [Test]
    public void TrailingSpacesPreserved() {
        var expr = "y = '''\nhello   \n'''";
        expr.AssertReturns("hello   ");
    }

    [Test]
    public void OnlyWhitespaceContent() {
        // single blank line between open and close — the \n before closing is the delimiter newline, not content
        var expr = "y = '''\n\n'''";
        expr.AssertReturns("");
    }

    [Test]
    public void InParentheses() {
        var expr = "y = ('''\nhello\n''')";
        expr.AssertReturns("hello");
    }

    [Test]
    public void EscapedSingleQuoteInSingleTriple() {
        var expr = "y = '''\nit\\'s here\n'''";
        expr.AssertReturns("it's here");
    }

    [Test]
    public void InterpolationWithExpression() {
        var expr = "y = '''\nresult: {2 + 3}\n'''";
        expr.AssertReturns("result: 5");
    }

    [Test]
    public void InterpolationWithArray() {
        var expr = "y = '''\n{[1,2,3].count()}\n'''";
        expr.AssertReturns("3");
    }

    [Test]
    public void MultipleBlankLines() {
        var expr = "y = '''\nhello\n\n\nworld\n'''";
        expr.AssertReturns("hello\n\n\nworld");
    }

    [Test]
    public void DedentWithBlankLinesBetween() {
        // blank lines between indented content, with dedent
        var expr = "y = '''\n    hello\n\n\n    world\n    '''";
        expr.AssertReturns("hello\n\n\nworld");
    }

    [Test]
    public void ZeroBaseline_MixedTabsAndSpacesAcrossLines() {
        // baseline is empty — no stripping, mixed indentation across lines is fine
        var expr = "y = '''\n\thello\n    world\n'''";
        expr.AssertReturns("\thello\n    world");
    }

    [Test]
    public void ZeroBaseline_TabThenSpaceLines() {
        var expr = "y = '''\n\t\ta\n        b\n'''";
        expr.AssertReturns("\t\ta\n        b");
    }

    [Test]
    public void ConsistentTabPlusSpace_NotAnError() {
        // every line uses tab+space — consistent, should work
        var expr = "y = '''\n\t hello\n\t world\n\t '''";
        expr.AssertReturns("hello\nworld");
    }

    [Test]
    public void ConsistentTabPlusSpaces_ExtraIndent() {
        // baseline = tab+space, content has tab+space+extra
        var expr = "y = '''\n\t hello\n\t   world\n\t '''";
        expr.AssertReturns("hello\n  world");
    }

    [Test]
    public void SingleCharContent() {
        var expr = "y = '''\na\n'''";
        expr.AssertReturns("a");
    }

    [Test]
    public void ContentWithOnlyEscapes() {
        var expr = "y = '''\n\\n\\r\\t\\\\\n'''";
        expr.AssertReturns("\n\r\t\\");
    }

    [Test]
    public void FourSingleQuotesInContent_InsideDoubleTriple() {
        // '''' inside """ — just four single quotes, not a closing
        var expr = "\"\"\"\na''''b\n\"\"\"";
        expr.AssertReturns("out", "a''''b");
    }

    [Test]
    public void TwoDoubleQuotesInContent_InsideDoubleTriple() {
        // "" inside """ — not a closing (need three)
        var expr = "\"\"\"\na\"\"b\n\"\"\"";
        expr.AssertReturns("out", "a\"\"b");
    }

    [Test]
    public void TabDedentMultipleLines() {
        var expr = "y = '''\n\thello\n\tworld\n\t'''";
        expr.AssertReturns("hello\nworld");
    }

    [Test]
    public void TabDedentWithExtraTabIndent() {
        var expr = "y = '''\n\thello\n\t\tworld\n\t'''";
        expr.AssertReturns("hello\n\tworld");
    }

    [Test]
    public void TripleQuotedInFunctionCall() {
        var expr = "y = '''\nhello world\n'''.split(' ').count()";
        expr.AssertReturns(2);
    }

    // ── Double-quote triple (""") ──────────────────────────────────────────────

    [TestCase("\"\"\"\n\"\"\"\n", "")]
    [TestCase("\"\"\"\n    hello\n    \"\"\"\n", "hello")]
    [TestCase("\"\"\"\n    hello\n    world\n    \"\"\"\n", "hello\nworld")]
    [TestCase("\"\"\"\n    hello\n      world\n    \"\"\"\n", "hello\n  world")]
    public void DoubleTriple_Dedent(string expr, string expected)
        => expr.AssertReturns("out", expected);

    [Test]
    public void DoubleTriple_Interpolation() {
        var expr = "\"\"\"\nhello {42}\n\"\"\"";
        expr.AssertReturns("out", "hello 42");
    }

    [Test]
    public void DoubleTriple_InterpolationWithDedent() {
        var expr = "\"\"\"\n    hello {42}\n    world {7}\n    \"\"\"";
        expr.AssertReturns("out", "hello 42\nworld 7");
    }

    [Test]
    public void DoubleTriple_EscapeSequences() {
        var expr = "\"\"\"\nhello\\nworld\\t!\n\"\"\"";
        expr.AssertReturns("out", "hello\nworld\t!");
    }

    [TestCase("y = \"\"\"hello\"\"\"")]               // no newline after opening
    [TestCase("y = \"\"\"hello\n\"\"\"")]              // no newline after opening (content on same line)
    [TestCase("y = \"\"\"\nhello")]                    // unclosed
    [TestCase("y = \"\"\"\nhello\"\"\"")]              // closing not on own line
    [TestCase("y = \"\"\"\n  hello\n world\n  \"\"\"")]  // less indent than baseline
    public void DoubleTriple_ObviousFails(string expr) => expr.AssertObviousFailsOnParse();

    // ── Windows line endings ──────────────────────────────────────────────────

    [Test]
    public void WindowsLineEndings() {
        var expr = "y = '''\r\nhello\r\nworld\r\n'''";
        expr.AssertReturns("hello\nworld");
    }

    [Test]
    public void WindowsLineEndings_WithDedent() {
        var expr = "y = '''\r\n    hello\r\n    world\r\n    '''";
        expr.AssertReturns("hello\nworld");
    }

    [Test]
    public void WindowsLineEndings_WithInterpolation() {
        var expr = "y = '''\r\nhello {42}\r\nworld\r\n'''";
        expr.AssertReturns("hello 42\nworld");
    }

    [Test]
    public void MixedLineEndings() {
        // first line \r\n, second line \n
        var expr = "y = '''\r\nhello\nworld\r\n'''";
        expr.AssertReturns("hello\nworld");
    }

    // ── More edge cases ─────────────────────────────────────────────────────────

    [Test]
    public void FirstContentLineBlank_ThenContent() {
        // blank line, then content, then closing
        var expr = "y = '''\n\nhello\n'''";
        expr.AssertReturns("\nhello");
    }

    [Test]
    public void FirstContentLineBlank_WithDedent() {
        var expr = "y = '''\n\n    hello\n    '''";
        expr.AssertReturns("\nhello");
    }

    [Test]
    public void ContentLineBecomesEmptyAfterDedent() {
        // line with only baseline whitespace → empty line after stripping
        var expr = "y = '''\n    hello\n    \n    world\n    '''";
        expr.AssertReturns("hello\n\nworld");
    }

    [Test]
    public void EscapedBraceNextToRealInterpolation() {
        // \{ then real {expr} on same line
        var expr = "y = '''\n\\{not} {42}\n'''";
        expr.AssertReturns("{not} 42");
    }

    [Test]
    public void RealInterpolationThenEscapedBrace() {
        var expr = "y = '''\n{42} \\{not\\}\n'''";
        expr.AssertReturns("42 {not}");
    }

    [Test]
    public void StandaloneExpression() {
        // no variable assignment — triple-quoted string as the whole expression
        var expr = "'''\nhello\n'''";
        expr.AssertReturns("out", "hello");
    }

    [Test]
    public void StandaloneDoubleTriple() {
        var expr = "\"\"\"\nhello\n\"\"\"";
        expr.AssertReturns("out", "hello");
    }

    [Test]
    public void FourQuotes_Fails() {
        // '''' is not valid — interpreted as ''' + lone '
        var expr = "y = ''''\nhello\n'''";
        expr.AssertObviousFailsOnParse();
    }

    [Test]
    public void SixQuotes_Fails() {
        // '''''' — two triple quotes with nothing between
        var expr = "y = ''''''\nhello\n'''";
        expr.AssertObviousFailsOnParse();
    }

    // ── Dollar-prefix $'''...''' ────────────────────────────────────────────────

    [TestCase("y = $'''\nhello\n'''", "hello")]
    [TestCase("y = $'''\n'''", "")]
    public void DollarTriple_PlainText(string expr, string expected)
        => expr.AssertReturns(expected);

    [Test]
    public void DollarTriple_JsonTemplate()
        => "y = $'''\n{\"name\": \"${name}\"}\n'''".Calc("name", "Alice")
            .AssertReturns("{\"name\": \"Alice\"}");

    [Test]
    public void DollarTriple_BracesAreLiteral() {
        var expr = "y = $'''\n{\"key\": \"value\"}\n'''";
        expr.AssertReturns("{\"key\": \"value\"}");
    }

    [Test]
    public void DollarTriple_WithDedent() {
        var expr = "y = $'''\n    {\"key\": \"value\"}\n    '''";
        expr.AssertReturns("{\"key\": \"value\"}");
    }

    [Test]
    public void DollarTriple_InterpolationWithDedent()
        => "y = $'''\n    value: ${x}\n    '''".Calc("x", 42).AssertReturns("value: 42");

    [Test]
    public void DollarTriple_MultipleInterpolations()
        => "y = $'''\n${a}:${b}\n'''".Calc(("a", "host"), ("b", 8080)).AssertReturns("host:8080");

    [Test]
    public void DollarTriple_EscapedDollar()
        => "y = $'''\n\\${not}\n'''".AssertReturns("${not}");

    [Test]
    public void DollarTriple_EscapedDollarThenRealInterpolation()
        => "y = $'''\n\\$b${x}c\n'''".Calc("x", 1).AssertReturns("$b1c");

    [Test]
    public void DollarTriple_AllEscapes()
        => "y = $'''\n\\n\\t\\\\\\{\\}\\$\n'''".AssertReturns("\n\t\\{}$");

    [TestCase("y = $'''\n    '''", "")]
    [TestCase("y = $$'''\n'''", "")]
    [TestCase("y = $$'''\n    '''", "")]
    public void DollarTriple_EmptyWithDedent(string expr, string expected)
        => expr.AssertReturns(expected);

    [Test]
    public void DollarTriple_ExcessDollarInterpolation()
        => "y = $'''\n$${x}\n'''".Calc("x", 42).AssertReturns("$42");

    [Test]
    public void DollarTriple_WindowsLineEndings()
        => "y = $'''\r\nhello\r\nworld\r\n'''".AssertReturns("hello\nworld");

    [Test]
    public void DollarTriple_WindowsLineEndingsWithInterpolation()
        => "y = $'''\r\nhello ${x}\r\n'''".Calc("x", 42).AssertReturns("hello 42");

    [Test]
    public void DollarTriple_BlankLineBetweenInterpolations()
        => "y = $'''\n${x}\n\n${z}\n'''".Calc(("x", "a"), ("z", "b")).AssertReturns("a\n\nb");

    [Test]
    public void DollarTriple_MultilineWithDedentAndInterpolation()
        => "y = $'''\n    line1 ${x}\n    line2 ${z}\n    '''".Calc(("x", 1), ("z", 2))
            .AssertReturns("line1 1\nline2 2");

    [Test]
    public void DollarTriple_EqualsRegularTriple() {
        var expr = "y = $'''\nhello\n''' == '''\nhello\n'''";
        expr.AssertReturns(true);
    }

    [Test]
    public void DollarTriple_MethodCall() {
        var expr = "y = $'''\nhello\n'''.reverse()";
        expr.AssertReturns("olleh");
    }

    [Test]
    public void DollarTriple_Indexing() {
        var expr = "y = $'''\nhello\n'''[0]";
        expr.AssertReturns('h');
    }

    [Test]
    public void DollarTriple_Slicing()
        => "y = $'''\nhello\n'''[0:2]".AssertReturns("hel");

    // ── Dollar-prefix $"""...""" ────────────────────────────────────────────────

    [TestCase("y = $\"\"\"\nhello\n\"\"\"", "hello")]
    [TestCase("y = $\"\"\"\n{literal}\n\"\"\"", "{literal}")]
    public void DollarTripleDouble_PlainText(string expr, string expected)
        => expr.AssertReturns(expected);

    [Test]
    public void DollarTripleDouble_Interpolation()
        => "y = $\"\"\"\nvalue: ${x}\n\"\"\"".Calc("x", 42).AssertReturns("value: 42");

    // ── Double dollar $$'''...''' ───────────────────────────────────────────────

    [Test]
    public void DoubleDollarTriple_Config()
        => "y = $$'''\nPrice: $100\nCustomer: $${customer}\n'''".Calc("customer", "John")
            .AssertReturns("Price: $100\nCustomer: John");

    [Test]
    public void DoubleDollarTriple_BracesAndSingleDollarAreLiteral() {
        var expr = "y = $$'''\n{json} and ${template}\n'''";
        expr.AssertReturns("{json} and ${template}");
    }

    [Test]
    public void DoubleDollarTriple_WithDedent()
        => "y = $$'''\n    Price: $100\n    Value: $${x}\n    '''".Calc("x", 42)
            .AssertReturns("Price: $100\nValue: 42");

    [Test]
    public void DoubleDollarTriple_EscapedDollar()
        => "y = $$'''\n\\$100\n'''".AssertReturns("$100");

    [Test]
    public void DoubleDollarTriple_ExcessDollarInterpolation()
        => "y = $$'''\n$$${x}\n'''".Calc("x", 42).AssertReturns("$42");

    // ── Double dollar $$"""...""" ───────────────────────────────────────────────

    [Test]
    public void DoubleDollarTripleDouble_WithDedent()
        => "y = $$\"\"\"\n    {literal} $${x}\n    \"\"\"".Calc("x", 42)
            .AssertReturns("{literal} 42");

    [Test]
    public void DoubleDollarTripleDouble_BracesAndDollarLiteral()
        => "y = $$\"\"\"\n{a} ${b} $${x}\n\"\"\"".Calc("x", 1)
            .AssertReturns("{a} ${b} 1");

    // ── Dollar triple-quote error cases ─────────────────────────────────────────

    [TestCase("y = $'''hello'''")]
    [TestCase("y = $$'''hello'''")]
    public void DollarTriple_NoNewlineAfterOpening_IsError(string expr)
        => expr.AssertObviousFailsOnParse();

    [TestCase("y = $'''\nhello")]
    [TestCase("y = $$'''\nhello")]
    public void DollarTriple_Unclosed_IsError(string expr)
        => expr.AssertObviousFailsOnParse();

    [Test]
    public void DollarTriple_InsufficientIndent_IsError()
        => "y = $'''\n    hello\n  world\n    '''".AssertObviousFailsOnParse();

    [Test]
    public void DollarTriple_MixedIndent_IsError()
        => "y = $'''\n    hello\n\tworld\n    '''".AssertObviousFailsOnParse();

    [Test]
    public void DoubleDollarTriple_Unclosed_IsError()
        => "y = $$'''\nhello".AssertObviousFailsOnParse();
}
