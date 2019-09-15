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
        [TestCase("y = 'foo'.concat('bar')", "foobar")]
        [TestCase("y = 'foo'.reverse()", "oof")]
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
        [TestCase("y = '0123456789'[1:8:] == '12345678'", true)]
        [TestCase("y = '0123456789'[1:8:] != '12345678'", false)]
        [TestCase("y = 'abc' == 'abc'", true)]
        [TestCase("y = 'abc' == 'cba'", false)]
        [TestCase("y = 'abc' == 'cba'.reverse()", true)]
        [TestCase("y = 'abc' == 'abc'.reverse()", false)]
        [TestCase("y = 'abc'.concat('def') == 'abcdef'", true)]
        [TestCase("y = 'abc'.concat('de') == 'abcdef'", false)]
        public void TextConstantEquation(string expr, object expected)
        {
            FunBuilder
                .BuildDefault(expr)
                .Calculate()
                .AssertReturns(VarVal.New("y", expected));
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
                .AssertReturns(VarVal.New("y", expected));
        }
        [TestCase("y='hell")]
        [TestCase("y=hell'")]
        [TestCase("y='")]
        [TestCase("y = '")]
        [TestCase("'\\'")]
        [TestCase("'some\\'")]
        [TestCase("'\\GGG'")]
        [TestCase("'\\q'")]
        [TestCase("y = 'hi'+5")]
        [TestCase("y = ''+10")]
        [TestCase("y = ''+true")]
        [TestCase("y = 'hi'+5+true")]
        [TestCase("y = 'hi'+' '+'world'")]
        [TestCase("y = 'arr: '+ [1,2,3]")]
        [TestCase("y = 'arr: '+ [[1,2],[3]]")]
        public void ObviousFails(string expr)
        {
            Assert.Throws<FunParseException>(()=> FunBuilder.BuildDefault(expr));
        }
    }
}