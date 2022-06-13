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

    
}