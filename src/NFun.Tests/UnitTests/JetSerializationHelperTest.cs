using System;
using System.Collections.Generic;
using System.Text;
using NFun.Jet;
using NUnit.Framework;

namespace Funny.Tests.UnitTests
{
    [TestFixture]
    public class JetSerializationHelperTest
    {

        [TestCase("  \\\\")]
        [TestCase("")]
        [TestCase("test")]
        [TestCase(" test")]
        [TestCase("test ")]
        [TestCase(" test ")]
        [TestCase(" test 2")]
        [TestCase(" ")]
        [TestCase("   ")]
        [TestCase("     ")]
        [TestCase("       test 4")]
        [TestCase("       \r")]
        [TestCase("   \t    \r\n    ")]
        [TestCase("1 2 3")]
        [TestCase("ein hier kommt die sonne")]
        [TestCase("he said: 'whazuuup'\r\n and run")]
        [TestCase("he said: \"whazuuup\"\r\n and run")]
        [TestCase("he said: \"whazuuup\"\r\n and run")]

        [TestCase("\t")]
        [TestCase("\n")]
        [TestCase("'")]
        [TestCase("\r")]
        [TestCase("\v")]
        [TestCase("\f")]
        [TestCase("}")]
        [TestCase("{")]
        [TestCase("\\\"")]
        [TestCase("\\\\")]
        [TestCase("e\\'")]
        [TestCase("it's mine!")]

        [TestCase("#\\r")]
        [TestCase("#\r")]

        [TestCase(" \\r\r")]
        [TestCase("\r\\r ")]
        [TestCase("  \\\\  ")]
        [TestCase("John: \\'fuck you!\\', he stops.")]
        [TestCase("w\t")]
        [TestCase("w\\\\\t")]
        [TestCase("q\\t")]
        [TestCase("w\\\"")]
        [TestCase(" \\r")]
        [TestCase("\\t \\n")]
        [TestCase("q\\tg")]
        [TestCase("e\\\\mm\\'")]
        [TestCase("\t \\n\n")]
        public void TextIsCorrect_EscapedConvertBack_ConvertedEqualToOrigin(string text)
        {
            var parsed = JetSerializationHelper.ToJetEscaped(text);
            var restored = JetSerializationHelper.FromJetEscaped(parsed);

            Assert.AreEqual(text, restored);
        }
        [Test]
        public void StringIsEmpty_escapedReturned()
        {
            Assert.AreEqual("\\e", JetSerializationHelper.ToJetEscaped(""));
        }
    }
}
