using System;
using NFun.ParseErrors;
using NFun.Tokenization;
using NUnit.Framework;

namespace Funny.Tests.UnitTests
{
    [TestOf(typeof(QuotationHelper))]
    public class QuotationHelperTest
    {
        [TestCase("  \\\\","  \\")]
        [TestCase("","")]
        [TestCase("test","test")]
        [TestCase("\\t","\t")]
        [TestCase("\\n","\n")]
        [TestCase("\\'","'")]
        [TestCase("\\r","\r")]
        [TestCase("\\{", "{")]
        [TestCase("\\}", "}")]

        [TestCase("\\v","\v")]
        [TestCase("\\f","\f")]
        [TestCase("\\\"","\"")]
        [TestCase("\\\\","\\")]
        [TestCase("e\\'","e'")]
        [TestCase("#\\r","#\r")]
        [TestCase(" \\r\r"," \r\r")]
        [TestCase("\\r\r","\r\r")]
        [TestCase("  \\\\  ","  \\  ")]
        [TestCase("John: \\'fuck you!\\', he stops.", "John: 'fuck you!', he stops.") ]
        [TestCase("w\\t","w\t")]
        [TestCase("w\\\\\\t","w\\\t")]
        [TestCase("q\\t","q\t")]
        [TestCase("w\\\"","w\"")]
        [TestCase(" \\r"," \r")]
        [TestCase("\t \\n","\t \n")]
        [TestCase("q\\tg","q\tg")]
        [TestCase("e\\\\mm\\'","e\\mm'")]
        [TestCase(" \\r\r"," \r\r")]
        [TestCase("\t \\n\n","\t \n\n")]
        public void TextIsCorrect_EscapedReplacedWell(string origin, string expected)
        {
            AssertStringParsed("", "", origin, expected);
            AssertStringParsed("some prefix", "", origin, expected);
            AssertStringParsed("", "some postfix", origin, expected);
            AssertStringParsed("prefix", "postfix", origin, expected);
        }

        [TestCase("  \\\\")]
        [TestCase("")]
        [TestCase("test")]
        [TestCase("\\t")]
        [TestCase("\\n")]
        [TestCase("\\'")]
        [TestCase("\\r")]
        [TestCase("\\v")]
        [TestCase("\\f")]
        [TestCase("\\}")]
        [TestCase("\\{")]
        [TestCase("\\\"")]
        [TestCase("\\\\")]
        [TestCase("e\\'")]
        [TestCase("#\\r")]
        [TestCase(" \\r\\r")]
        [TestCase("\\r\\r ")]
        [TestCase("  \\\\  ")]
        [TestCase("John: \\'fuck you!\\', he stops.")]
        [TestCase("w\\t")]
        [TestCase("w\\\\\\t")]
        [TestCase("q\\t")]
        [TestCase("w\\\"")]
        [TestCase(" \\r")]
        [TestCase("\\t \\n")]
        [TestCase("q\\tg")]
        [TestCase("e\\\\mm\\'")]
        [TestCase("\\t \\n\\n")]
        public void TextIsCorrect_EscapedConvertBack_ConvertedEqualToOrigin(string quoted)
        {
            var str = $"'{quoted}'";
            var (parsed, end) = QuotationHelper.ReadQuotation(str, 0);
            var restored = QuotationHelper.ToEscaped(parsed);

            Assert.AreEqual(quoted, restored);
        }

        private void AssertStringParsed(string prefix, string postfix, string quoted, string expected)
        {
            var str = $"'{quoted}'";
            var (parsed, end) =  QuotationHelper.ReadQuotation(
                prefix+str+postfix, prefix.Length);
            
            Assert.AreEqual(expected, parsed);
            Assert.AreEqual(str.Length-1+ prefix.Length, end);
        }


        [TestCase("'something", "\\ ", "else' some postfix")]
        [TestCase("'something", "\\e", "lse' some postfix")]
        [TestCase("'something \\\\", "\\e", "lse' some postfix")]
        [TestCase("'", "\\e", "lse' some postfix")]
        [TestCase("'", "\\G", "' some postfix")]
        [TestCase("'", "\\(", "' some postfix")]
        [TestCase("'\\\\", "\\(", "hi' some postfix")]
        [TestCase("'some text ","\\", "")]
        public void TextIsNotCorrect_ErrorIntervalAsExpected(string before, string error, string after)
        {
            var prefix = "some prefix ";
            var str = prefix + before + error + after;
            var ex = Assert.Throws<FunParseException>(() =>
                QuotationHelper.ReadQuotation(str, prefix.Length));
            Console.WriteLine("Origin string to parse: "+ str);
            Console.WriteLine("Parse error: [FU"+ ex.Code+"] "+ex.Message);
            var foundError = ex.Interval.SubString(str);
            Console.WriteLine($"Catched error string: \"{foundError}\"");
            Assert.AreEqual(error, foundError);
        }
        [TestCase("'\\' some postfix")]
        [TestCase("'something \\' some postfix")]
        [TestCase("'")]
        public void CloseQuoteMissing_returnsNegativeOne(string text)
        {
            var (result, resultPosition) = QuotationHelper.ReadQuotation(text, 0);
            Assert.AreEqual(-1, resultPosition);
        }

    }
}