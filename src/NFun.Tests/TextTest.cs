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
        [TestCase("y='  \\\\'","  \\")]
        [TestCase("y='\\t'","\t")]
        [TestCase("y='\\n'","\n")]
        [TestCase("y='\\''","'")]
        [TestCase("y='\\r'","\r")]
        [TestCase("y='\\v'","\v")]
        [TestCase("y='\\f'","\f")]
        [TestCase("y='\\\"'","\"")]
        [TestCase("y='\\\\'","\\")]
        [TestCase("y='e\\''","e'")]
        [TestCase("y='#\\r'","#\r")]
        [TestCase("y=' \\r\r'"," \r\r")]
        [TestCase("y='\\r\r'","\r\r")]
        [TestCase("y='  \\\\  '","  \\  ")]
        [TestCase("y='John: \\'fuck you!\\', he stops.'", "John: 'fuck you!', he stops.") ]
        [TestCase("y='w\\t'","w\t")]
        [TestCase("y='w\\\\\\t'","w\\\t")]
        [TestCase("y='q\\t'","q\t")]
        [TestCase("y='w\\\"'","w\"")]
        [TestCase("y=' \\r'"," \r")]
        [TestCase("y='\t \\n'","\t \n")]
        [TestCase("y='q\\tg'","q\tg")]
        [TestCase("y='e\\\\mm\\''","e\\mm'")]
        [TestCase("y=' \\r\r'"," \r\r")]
        [TestCase("y='\t \\n\n'","\t \n\n")]
        [TestCase("y='pre \\{lalala\\} after'","pre {lalala} after")]

        public void EscapedTest(string expr,string expected)
        {
            FunBuilder
                .BuildDefault(expr)
                .Calculate()
                .AssertReturns(Var.New("y", expected));
        }
        [TestCase("y='hell")]
        [TestCase("y=hell'")]
        [TestCase("y='")]
        [TestCase("y = '")]
        [TestCase("'\\'")]
        [TestCase("'some\\'")]
        [TestCase("'\\GGG'")]
        [TestCase("'\\q'")]
        public void ObviousFails(string expr)
        {
            Assert.Throws<FunParseException>(()=> FunBuilder.BuildDefault(expr));
        }
    }
}