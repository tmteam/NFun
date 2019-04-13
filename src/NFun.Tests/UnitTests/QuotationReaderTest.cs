using NFun.Tokenization;
using NUnit.Framework;

namespace Funny.Tests.UnitTests
{
    public class QuotationReaderTest
    {
        [TestCase("  \\\\","  \\")]
        [TestCase("","")]
        [TestCase("test","test")]
        [TestCase("\\t","\t")]
        [TestCase("\\n","\n")]
        [TestCase("\\'","'")]
        [TestCase("\\r","\r")]
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
        [TestCase("e\\\\mm\'","e\\mm'")]
        [TestCase(" \\r\r"," \r\r")]
        [TestCase("\t \\n\n","\t \n\n")]
        public void TextIsCorrect_EscapedReplacedWell(string origin, string expected)
        {
            var (parsed, error) =  QuotationReader.TryReplaceEscaped(origin);
            Assert.AreEqual(Interval.Empty, error);
            Assert.AreEqual(expected, parsed);
        }
        [TestCase("something","\\","")]
        [TestCase("something","\\","")]
        [TestCase("something","\\ ","else")]
        [TestCase("something","\\e","lse")]
        [TestCase("something \\\\","\\e","lse")]
        [TestCase("","\\e","lse")]
        [TestCase("","\\","")]
        [TestCase("","\\","")]
        [TestCase("","\\G","")]
        [TestCase("","\\(","")]
        [TestCase("\\\\","\\(","hi")]

        public void TextIsNotCorrect_ErrorIntervalAsExpected(string before, string error, string after)
        {
            var (parsed, ierror) =  QuotationReader.TryReplaceEscaped(before+error+after);
            Assert.AreEqual(new Interval(before.Length,before.Length+error.Length),
                ierror);
            Assert.IsNull(parsed);
        }
    }
}