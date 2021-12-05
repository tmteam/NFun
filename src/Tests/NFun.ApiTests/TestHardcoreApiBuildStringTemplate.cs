using NUnit.Framework;

namespace NFun.ApiTests {

public class TestHardcoreApiBuildStringTemplate {
    [TestCase(42, "{x.toText().concat('lalala')}", "42lalala")]
    [TestCase(42.0, "pre{x-1*2}mid{x*x/x}fin", "pre40mid42fin")]
    [TestCase(42, "pre{x-1*2}mid{x*x/x}fin", "pre40mid42fin")]
    [TestCase("abc", "{concat(x,x)}", "abcabc")]
    [TestCase("abc", "pre {'inner = {x.concat('test of \\{')}'} outer {x}", "pre inner = abctest of { outer abc")]
    public void SingleVariableTemplate(object input, string expr, string expected) {
        var calculator = Funny.Hardcore.BuildStringTemplate(expr);
        calculator["x"].Value = input;
        var res = calculator.Calculate();
        Assert.AreEqual(expected, res);
    }

    [TestCase("'vasa' is \\{good\\}", "'vasa' is \\{good\\}")]
    [TestCase("{0}", "0")]
    [TestCase("hi {42}", "hi 42")]
    [TestCase("{42}hi", "42hi")]
    [TestCase("hello {42} world", "hello 42 world")]
    [TestCase("hello {42+1} world", "hello 43 world")]
    [TestCase("{''}'", "'")]
    [TestCase("{'{'{'{12}'}'}'}", "12")]
    [TestCase("hi {42} and {21}", "hi 42 and 21")]
    [TestCase("hi {42+13} and {21-1}", "hi 55 and 20")]
    [TestCase("{0+1} {1+2} {2+3}", "1 3 5")]
    [TestCase("pre {'p{42-1*2}m{21-1+10*3}a'} mid {'p{42-2}m{21-1}a'} fin", "pre p40m50a mid p40m20a fin")]
    [TestCase("pre1{'pre2{2-2}after2'}after1", "pre1pre20after2after1")]
    [TestCase("pre1 {'inside'} after1", "pre1 inside after1")]
    public void ConstantTemplate(string expr, string expected) =>
        Assert.AreEqual(expected, Funny.Hardcore.BuildStringTemplate(expr).Calculate());
}

}