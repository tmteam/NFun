using NFun;
using NFun.ParseErrors;
using NFun.Types;
using NUnit.Framework;

namespace Funny.Tests
{
    public class TextTest
    {
        [TestCase("y = ''", "")]
        [TestCase("y = 'hi'", "hi")]
        [TestCase("y = 'World'", "World")]
        [TestCase("y = 'hi'+5", "hi5")]
        [TestCase("y = ''+10", "10")]
        [TestCase("y = ''+true", "True")]
        [TestCase("y = 'hi'+5+true", "hi5True")]
        [TestCase("y = 'hi'+' '+'world'", "hi world")]
        [TestCase("y = 'arr: '+ [1,2,3]", "arr: [1,2,3]")]
        [TestCase("y = 'arr: '+ [[1,2],[3]]", "arr: [[1,2],[3]]")]
        public void TextConstantEquation(string expr, string expected)
        {
            FunBuilder
                .BuildDefault(expr)
                .Calculate()
                .AssertReturns(Var.New("y", expected));
        }
        [TestCase("'\\t'","\t")]
        [TestCase("'\\\"'","\"")]
        [TestCase("'\\''","'")]
        [TestCase("'\\r'","\r")]
        [TestCase("'\\n'","\n")]
        [TestCase("'\\\\'","\\")]
        [TestCase("'q\\t'","q\t")]
        [TestCase("'w\\\"'","w\"")]
        [TestCase("'e\\''","e'")]
        [TestCase("' \\r'"," \r")]
        [TestCase("'\t \\n'","\t \n")]
        [TestCase("'  \\\\'","  \\")]
        [TestCase("'q\\tg'","q\tg")]
        [TestCase("'w\\\\t'","w\t")]
        [TestCase("'e\\mm''","emm'")]
        [TestCase("' \\r\r'"," \r\r")]
        [TestCase("'\t \\n\n'","\t \n\n")]
        [TestCase("'  \\\\  '","  \\  ")]
        public void EscapedTest(string expr,string expected)
        {
            FunBuilder
                .BuildDefault(expr)
                .Calculate()
                .AssertReturns(Var.New("out", expected));
        }
        [TestCase("y='hell")]
        [TestCase("y=hell'")]
        [TestCase("y='")]
        [TestCase("y = '")]
        [TestCase("'\\'")]
        [TestCase("'\\q'")]
        public void ObviousFails(string expr)
        {
            Assert.Throws<FunParseException>(()=> FunBuilder.BuildDefault(expr));
        }
    }
}