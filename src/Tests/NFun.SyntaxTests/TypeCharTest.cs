using NFun.TestTools;
using NUnit.Framework;

namespace NFun.SyntaxTests;

[TestFixture]
public class CharTest {
    [TestCase("out:char = 'abc'[0]", 'a')]
    [TestCase("out:char[] = 'test'", "test")]
    [TestCase("out:text = ['a'[0],'ab'[1],'abc'[2]]", "abc")]
    public void ConstantEquation(string expr, object expected) => expr.AssertResultHas("out", expected);

    [TestCase('a', "y:char = x", 'a')]
    [TestCase('a', "y = x == 'a'[0]", true)]
    [TestCase('a', "y = x == 'b'[0]", false)]
    [TestCase('a', "x:char; y = x", 'a')]
    [TestCase('a', "y = x", 'a')]
    [TestCase('a', "x:char; y = [x,x,x]", "aaa")]
    [TestCase('a', "y:char[] = [x,x,x]", "aaa")]
    [TestCase('a', "y:text = [x,x,x]", "aaa")]
    [TestCase('a', "y:text = x.repeat(4)", "aaaa")]
    [TestCase('a', "y:char[] = x.repeat(4)", "aaaa")]
    [TestCase('a', "x:char; y = x.repeat(4)", "aaaa")]
    public void SingleVariableEquation(object input, string expr, object expected) =>
        expr.Calc("x", input).AssertReturns(expected);

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
    [TestCase("y = /'/'", '/')]
    [TestCase("y = /'*'", '*')]
    [TestCase("y = /'('", '(')]
    [TestCase("y = /')'", ')')]
    [TestCase("y = /'['", '[')]
    [TestCase("y = /']'", ']')]
    [TestCase("y = /'{'", '{')]
    [TestCase("y = /'}'", '}')]
    public void BasicCharLiteral(string expr, char expected) => expr.AssertReturns(expected);

    [TestCase("y = /'\\t'", '\t')]
    [TestCase("y = /'\\n'", '\n')]
    [TestCase("y = /'\\r'", '\r')]
    [TestCase("y = /'\\\\'", '\\')]
    [TestCase("y = /'\\\"'", '"')]
    [TestCase("y = /'\\''", '\'')]
    public void EscapedCharLiteral(string expr, char expected) => expr.AssertReturns(expected);

    [TestCase("y:char = /'a'", 'a')]
    [TestCase("y = /'a'", 'a')]
    public void CharTypeInference(string expr, char expected) => expr.AssertReturns(expected);

    [TestCase("y = /'a' == 'abc'[0]", true)]
    [TestCase("y = /'b' == 'abc'[0]", false)]
    [TestCase("y = /'a' == /'a'", true)]
    [TestCase("y = /'a' == /'b'", false)]
    [TestCase("y = /'a' != /'b'", true)]
    [TestCase("y = /'a' != /'a'", false)]
    public void CharInExpressions(string expr, object expected) => expr.AssertReturns(expected);

    [TestCase("y:text = [/'a', /'b', /'c']", "abc")]
    [TestCase("y:char[] = [/'a', /'b', /'c']", "abc")]
    [TestCase("y = [/'a', /'b', /'c']", "abc")]
    public void CharArrays(string expr, object expected) => expr.AssertReturns(expected);

    [TestCase('a', "y = x == /'a'", true)]
    [TestCase('a', "y = x == /'b'", false)]
    [TestCase('b', "y = x == /'b'", true)]
    public void CharWithVariable(object input, string expr, object expected) =>
        expr.Calc("x", input).AssertReturns(expected);

    [TestCase("y = /'a' > /'b'", false)]
    [TestCase("y = /'b' > /'a'", true)]
    [TestCase("y = /'a' < /'b'", true)]
    [TestCase("y = /'a' >= /'a'", true)]
    [TestCase("y = /'a' <= /'a'", true)]
    public void CharComparison(string expr, bool expected) => expr.AssertReturns(expected);

    [TestCase("y = if (true) /'a' else /'b'", 'a')]
    [TestCase("y = if (false) /'a' else /'b'", 'b')]
    public void CharInIfElse(string expr, char expected) => expr.AssertReturns(expected);

    [TestCase("y = 'hi {/'x'} bye'", "hi x bye")]
    [TestCase("y = '{/'a'}{/'b'}'", "ab")]
    public void CharInInterpolation(string expr, object expected) => expr.AssertReturns(expected);

    [TestCase("y = /'a'.toText()", "a")]
    public void CharInFunctions(string expr, object expected) => expr.AssertReturns(expected);

    [TestCase("y:text = /'x'.repeat(3)", "xxx")]
    [TestCase("y:char[] = /'x'.repeat(3)", "xxx")]
    public void CharRepeat(string expr, object expected) => expr.AssertReturns(expected);

    [TestCase("y = /'я'", 'я')]
    [TestCase("y = /'ü'", 'ü')]
    public void NonAsciiCharLiteral(string expr, char expected) => expr.AssertReturns(expected);

    [TestCase("y = /''")]          // empty char literal
    [TestCase("y = /'ab'")]        // multiple chars
    [TestCase("y = /'abc'")]       // multiple chars
    [TestCase("y = /'")]           // unclosed
    [TestCase("y = /'a")]          // missing closing quote
    [TestCase("y = /'\\q'")]       // invalid escape
    [TestCase("y = /'\\GGG'")]     // invalid escape
    public void ObviousFails(string expr) => expr.AssertObviousFailsOnParse();
}
