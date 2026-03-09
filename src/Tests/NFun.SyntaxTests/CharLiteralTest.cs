using NFun.TestTools;
using NUnit.Framework;

namespace NFun.SyntaxTests;

[TestFixture]
public class CharLiteralTest {

    // === Basic char literals ===
    [TestCase("y = /'a'", 'a')]
    [TestCase("y = /'z'", 'z')]
    [TestCase("y = /'A'", 'A')]
    [TestCase("y = /'Z'", 'Z')]
    [TestCase("y = /'0'", '0')]
    [TestCase("y = /'9'", '9')]
    [TestCase("y = /' '", ' ')]
    [TestCase("y = /'!'", '!')]
    [TestCase("y = /'@'", '@')]
    [TestCase("y = /'#'", '#')]
    [TestCase("y = /'+'", '+')]
    [TestCase("y = /'-'", '-')]
    [TestCase("y = /'.'", '.')]
    [TestCase("y = /'/'", '/')]       // slash inside char literal
    [TestCase("y = /'*'", '*')]
    [TestCase("y = /'('", '(')]
    [TestCase("y = /')'", ')')]
    [TestCase("y = /'['", '[')]
    [TestCase("y = /']'", ']')]
    [TestCase("y = /'{'", '{')]       // brace — no interpolation in char literals
    [TestCase("y = /'}'", '}')]
    public void BasicCharLiteral(string expr, char expected) => expr.AssertReturns(expected);

    // === Escape sequences in char literals ===
    [TestCase("y = /'\\t'", '\t')]
    [TestCase("y = /'\\n'", '\n')]
    [TestCase("y = /'\\r'", '\r')]
    [TestCase("y = /'\\\\'", '\\')]
    [TestCase("y = /'\\\"'", '"')]
    [TestCase("y = /'\\''", '\'')]
    public void EscapedCharLiteral(string expr, char expected) => expr.AssertReturns(expected);

    // === Char type inference ===
    [TestCase("y:char = /'a'", 'a')]
    [TestCase("y = /'a'", 'a')]
    public void CharTypeInference(string expr, char expected) => expr.AssertReturns(expected);

    // === Char in expressions ===
    [TestCase("y = /'a' == 'abc'[0]", true)]
    [TestCase("y = /'b' == 'abc'[0]", false)]
    [TestCase("y = /'a' == /'a'", true)]
    [TestCase("y = /'a' == /'b'", false)]
    [TestCase("y = /'a' != /'b'", true)]
    [TestCase("y = /'a' != /'a'", false)]
    public void CharInExpressions(string expr, object expected) => expr.AssertReturns(expected);

    // === Char arrays ===
    [TestCase("y:text = [/'a', /'b', /'c']", "abc")]
    [TestCase("y:char[] = [/'a', /'b', /'c']", "abc")]
    [TestCase("y = [/'a', /'b', /'c']", "abc")]
    public void CharArrays(string expr, object expected) => expr.AssertReturns(expected);

    // === Char with variables ===
    [TestCase('a', "y = x == /'a'", true)]
    [TestCase('a', "y = x == /'b'", false)]
    [TestCase('b', "y = x == /'b'", true)]
    public void CharWithVariable(object input, string expr, object expected) =>
        expr.Calc("x", input).AssertReturns(expected);

    // === Char comparison (IsComparable) ===
    [TestCase("y = /'a' > /'b'", false)]
    [TestCase("y = /'b' > /'a'", true)]
    [TestCase("y = /'a' < /'b'", true)]
    [TestCase("y = /'a' >= /'a'", true)]
    [TestCase("y = /'a' <= /'a'", true)]
    public void CharComparison(string expr, bool expected) => expr.AssertReturns(expected);

    // === Char in if/else (LCA) ===
    [TestCase("y = if (true) /'a' else /'b'", 'a')]
    [TestCase("y = if (false) /'a' else /'b'", 'b')]
    public void CharInIfElse(string expr, char expected) => expr.AssertReturns(expected);

    // === Char in string interpolation ===
    [TestCase("y = 'hi {/'x'} bye'", "hi x bye")]
    [TestCase("y = '{/'a'}{/'b'}'", "ab")]
    public void CharInInterpolation(string expr, object expected) => expr.AssertReturns(expected);

    // === Char in functions ===
    [TestCase("y = /'a'.toText()", "a")]
    public void CharInFunctions(string expr, object expected) => expr.AssertReturns(expected);

    // === Char repeat ===
    [TestCase("y:text = /'x'.repeat(3)", "xxx")]
    [TestCase("y:char[] = /'x'.repeat(3)", "xxx")]
    public void CharRepeat(string expr, object expected) => expr.AssertReturns(expected);

    // === Non-ASCII chars ===
    [TestCase("y = /'я'", 'я')]
    [TestCase("y = /'ü'", 'ü')]
    public void NonAsciiCharLiteral(string expr, char expected) => expr.AssertReturns(expected);

    // === Division not broken ===
    [TestCase("y = 10.0 / 5.0", 2.0)]
    [TestCase("y = 10.0/5.0", 2.0)]
    public void DivisionStillWorks(string expr, double expected) => expr.AssertReturns(expected);

    // === Error cases ===
    [TestCase("y = /''")]          // empty char literal
    [TestCase("y = /'ab'")]        // multiple chars
    [TestCase("y = /'abc'")]       // multiple chars
    [TestCase("y = /'")]           // unclosed
    [TestCase("y = /'a")]          // missing closing quote
    [TestCase("y = /'\\q'")]       // invalid escape
    [TestCase("y = /'\\GGG'")]     // invalid escape
    public void ObviousFails(string expr) => expr.AssertObviousFailsOnParse();
}
