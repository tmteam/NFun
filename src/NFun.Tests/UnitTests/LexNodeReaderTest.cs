using NFun.Parsing;
using NFun.Tokenization;
using NUnit.Framework;

namespace Funny.Tests.UnitTests
{
    public class LexNodeReaderTest
    {
        [TestCase("y", LexNodeType.Var)]
        [TestCase("x*y", LexNodeType.Fun)]
        [TestCase("-x", LexNodeType.Fun)]
        [TestCase("1+2", LexNodeType.Fun)]
        [TestCase("1+2/(3-1.0)", LexNodeType.Fun)]
        [TestCase("1", LexNodeType.Number)]
        [TestCase("1.0", LexNodeType.Number)]
        [TestCase("'someText'", LexNodeType.Text)]
        [TestCase("'  someText'", LexNodeType.Text)]
        [TestCase("'123' \r y=1", LexNodeType.Text, 4)]
        [TestCase("123 \r y=1", LexNodeType.Number, 2)]
        [TestCase("123*1 \r y=1", LexNodeType.Fun, 4)]
        [TestCase("(123*1) \r y=1", LexNodeType.Fun, 6)]
        [TestCase("-1 \r y=1", LexNodeType.Fun, 1)]
        public void ReadExpressionOrNull_retunsExpression(string value, LexNodeType type)
        {
            var flow = new TokenFlow(Tokenizer.ToTokens(value));
            LexNodeReader reader = new LexNodeReader(flow);
            var ex = reader.ReadExpressionOrNull();
            Assert.IsNotNull(ex);
            Assert.Multiple(() =>
            {
                Assert.AreEqual(type, ex.Type);
                Assert.AreEqual(0, ex.Start,"start index failed");
                Assert.AreEqual(value.Length - 1 , ex.Finish,"finish index failed");
            });
        }
        
        [TestCase("'123' \r y=1", LexNodeType.Text,0, 4)]
        [TestCase("123 \r y=1", LexNodeType.Number, 0,2)]
        [TestCase("123*1 \r y=1", LexNodeType.Fun, 0,4)]
        [TestCase("(123*1) \r y=1", LexNodeType.Fun, 0,6)]
        [TestCase("-1 \r y=1", LexNodeType.Fun, 0,1)]
        [TestCase("   '123'", LexNodeType.Text,3,7)]

        public void ReadExpressionOrNull_retunsFirstExpressionWithCorrectBounds(string value, LexNodeType type,
            int start,int end)
        {
            var flow = new TokenFlow(Tokenizer.ToTokens(value));
            LexNodeReader reader = new LexNodeReader(flow);
            var ex = reader.ReadExpressionOrNull();
            Assert.IsNotNull(ex);
            Assert.Multiple(() =>
            {
                Assert.AreEqual(type, ex.Type);
                Assert.AreEqual(0, ex.Start);
                Assert.AreEqual(end, ex.Finish);
            });
        }
    }
}