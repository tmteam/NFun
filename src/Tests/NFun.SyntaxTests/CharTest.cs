using NFun.TestTools;
using NUnit.Framework;

namespace NFun.SyntaxTests; 

[TestFixture]
public class CharTest {
    [TestCase("out:char = 'abc'[0]", 'a')]
    [TestCase("out:char[] = 'test'", "test")]
    [TestCase("out:text = ['a'[0],'ab'[1],'abc'[2]]", "abc")]
    
    [TestCase("'a'[0] < 'z'[0]", true)]
    [TestCase("'b'[0] > 'z'[0]", false)]
    [TestCase("'a'[0] <= 'a'[0]", true)]
    [TestCase("'b'[0] <= 'z'[0]", true)]
    [TestCase("'A'[0] >= 'B'[0]", false)]
    [TestCase("'B'[0] > 'C'[0]", false)]

    
    [TestCase("'A'[0] <= 'z'[0]", true)]
    [TestCase("'B'[0] > 'y'[0]", false)]
    [TestCase("'C'[0] >= 'x'[0]", false)]
    [TestCase("'D'[0] < 'w'[0]", true)]
    [TestCase("'E'[0] <= 'v'[0]", true)]
    [TestCase("'F'[0] <= '1'[0]", false)]
    [TestCase("'1'[0] <= 'a'[0]", true)]
    [TestCase("cmp(a,b) = a>b; 'a'[0].cmp('b'[0])", false)]

    [TestCase("out:byte = 'a'[0].convert()", (byte)97)]
    [TestCase("out:byte = 'z'[0].convert()", (byte)122)]
    [TestCase("out:byte = '0'[0].convert()", (byte)48)]
    [TestCase("out:byte = 'A'[0].convert()", (byte)65)]
    [TestCase("out:byte = ' '[0].convert()", (byte)32)]
    
    [TestCase("x:byte = 97;   out:char = x.convert()", 'a')]
    [TestCase("x:byte = 122;  out:char = x.convert()", 'z')]
    [TestCase("x:byte = 48;   out:char = x.convert()", '0')]
    [TestCase("x:byte = 65;   out:char = x.convert()", 'A')]
    [TestCase("x:byte = 32;   out:char = x.convert()", ' ')]
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

    //todo. Convert function, Range function (?)
    public void SingleVariableEquation(object input, string expr, object expected) =>
        expr.Calc("x", input).AssertReturns(expected);

    [TestCase("out:byte = 'ы'[0].convert()")]
    public void ObviousFailsWithRuntimeException(string expr) => 
        expr.AssertObviousFailsOnRuntime();
}