using NFun.TestTools;
using NUnit.Framework;

namespace NFun.SyntaxTests.Texts;

[TestFixture]
public class TextTypeTest {

    // ── Operations ────────────────────────────────────────────────────────────────────

    [TestCase("y = 'foo'.concat('bar')", "foobar")]
    [TestCase("y = 'foo'.reverse()", "oof")]
    [TestCase("y = 'pre'.concat((1+2).toText())", "pre3")]
    [TestCase("y = 'ab'.concat(toText(1+2))", "ab3")]
    [TestCase("y = 'a b '.concat(toText(1+2)).split(' ')", new[] { "a", "b", "3" })]
    [TestCase("y = 'a b '.concat((1+2).toText()).split(' ')", new[] { "a", "b", "3" })]
    [TestCase("y = ''.toUpper()", "")]
    [TestCase("y = 'abcDEF'.toUpper()", "ABCDEF")]
    [TestCase("y = ''.toLower()", "")]
    [TestCase("y = 'abcDEF'.toLower()", "abcdef")]

    [TestCase("y = '  padded  '.trim()", "padded")]
    [TestCase("y = '  padded  '.trimStart()", "padded  ")]
    [TestCase("y = '  padded  '.trimEnd()", "  padded")]
    public void TextOperations(string expr, object expected) => expr.AssertReturns(expected);

    // ── Indexing and slicing ──────────────────────────────────────────────────────────

    [TestCase("y = ['bar'[1]]", "a")]
    [TestCase("y = 'bar'[1]", 'a')]
    [TestCase("y = '01234'[:]", "01234")]
    [TestCase("y = '01234'[2:4]", "234")]
    [TestCase("y = '01234'[1:1]", "1")]
    [TestCase("y = ''[0:0]", "")]
    [TestCase("y = ''[1:]", "")]
    [TestCase("y = ''[:1]", "")]
    [TestCase("y = ''[::1]", "")]
    [TestCase("y = ''[::3]", "")]
    [TestCase("y = '01234'[1:2]", "12")]
    [TestCase("y = '01234'[2:]", "234")]
    [TestCase("y = '01234'[::2]", "024")]
    [TestCase("y = '01234'[::1]", "01234")]
    [TestCase("y = '01234'[::]", "01234")]
    [TestCase("y = '0123456789'[2:9:3]", "258")]
    [TestCase("y = '0123456789'[1:8:2]", "1357")]
    [TestCase("y = '0123456789'[1:8:]", "12345678")]
    public void IndexingAndSlicing(string expr, object expected) => expr.AssertReturns(expected);

    // ── Equality and comparison ───────────────────────────────────────────────────────

    [TestCase("y = '0123456789'[1:8:] == '12345678'", true)]
    [TestCase("y = '0123456789'[1:8:] != '12345678'", false)]
    [TestCase("y = 'abc' == 'abc'", true)]
    [TestCase("y = 'abc' == 'cba'", false)]
    [TestCase("y = 'abc' == 'cba'.reverse()", true)]
    [TestCase("y = 'abc' == 'abc'.reverse()", false)]
    [TestCase("y = 'abc'.concat('def') == 'abcdef'", true)]
    [TestCase("y = 'abc'.concat('de') == 'abcdef'", false)]
    public void EqualityAndComparison(string expr, object expected) => expr.AssertReturns(expected);

    [TestCase("'avatar'.reverse() >  reverse('avatar') ", false)]
    [TestCase("('avatar'.reverse()) >  reverse('avatar') ", false)]
    [TestCase("'avatar'.reverse() <  'avatar'", false)]
    public void ConstantEquation(string expr, bool expected)
        => expr.AssertReturns("out", expected);

    // ── Split and join ────────────────────────────────────────────────────────────────

    [TestCase("y = 'a b c'.split(' ')", new[] { "a", "b", "c" })]
    [TestCase("y = 'a b '.concat('c').split(' ')", new[] { "a", "b", "c" })]
    [TestCase("y = 'a b '.split('')", new[] { "a", " ", "b", " "})]
    [TestCase("y = ['a','b','c'].join(',')", "a,b,c")]
    [TestCase("y = ['a','b','c'].join('')", "abc")]
    [TestCase("y = [].join(',')", "")]
    [TestCase("y = [''].join(',')", "")]

    [TestCase("y = 'a,b,c'.split(',')", new[] { "a", "b", "c" })]
    [TestCase("y = 'a,,c'.split(',')", new[] { "a", "c" })]  // removeEmptyEntries
    [TestCase("y = 'a b c'.split(' ')", new[] { "a", "b", "c" })]
    [TestCase("y = 'abc'.split('')", new[] { "a", "b", "c" })]
    [TestCase("y = ''.split(',')", new string[0])]
    public void SplitAndJoin(string expr, object expected) => expr.AssertReturns(expected);

    // ── Variable equations ───────────────────────────────────────────────────────────

    [TestCase(42, "y = x.toText().concat('lalala')", "42lalala")]
    [TestCase("abc", "y:text = concat(x,x)", "abcabc")]
    public void SingleVariableEquation(object input, string expr, object expected) =>
        expr.Calc("x", input).AssertReturns(expected);

    [Test]
    public void RepeatConcatTest() {
        var expression = "out:text = name.repeat(count).flat()";
        Funny.Hardcore.Build(expression).Calc(("count", 3), ("name", "foo")).AssertReturns("foofoofoo");
    }

    [Test]
    public void RepeatConcatTest2() {
        var expression = "if (count>0) name.repeat(count).flat() else 'none'";
        Funny.Hardcore.Build(expression).Calc(("count", 3), ("name", "foo")).AssertReturns("foofoofoo");
    }

    // ── Type errors ───────────────────────────────────────────────────────────────────

    [TestCase("y = 'hi'+5")]
    [TestCase("y = ''+10")]
    [TestCase("y = ''+true")]
    [TestCase("y = 'hi'+5+true")]
    [TestCase("y = 'hi'+' '+'world'")]
    [TestCase("y = 'arr: '+ [1,2,3]")]
    [TestCase("y = 'arr: '+ [[1,2],[3]]")]
    public void PlusOperatorFails(string expr) => expr.AssertObviousFailsOnParse();
}
