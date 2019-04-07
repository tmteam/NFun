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
        [TestCase("1+2/3-1.0", LexNodeType.Fun)]
        [TestCase("1+2/(3-1.0)", LexNodeType.Fun)]
        [TestCase("1", LexNodeType.Number)]
        [TestCase("(1)", LexNodeType.Number)]
        [TestCase("1.0", LexNodeType.Number)]
        [TestCase("'someText'", LexNodeType.Text)]
        [TestCase("'  someText'", LexNodeType.Text)]
        public void ReadExpressionOrNull_SingleExpression_retunsIt(string value, LexNodeType type)
        {
            var flow = new TokenFlow(Tokenizer.ToTokens(value));
            LexNodeReader reader = new LexNodeReader(flow);
            var ex = reader.ReadExpressionOrNull();
            Assert.IsNotNull(ex);
            Assert.Multiple(() =>
            {
                Assert.AreEqual(type, ex.Type);
                Assert.AreEqual(0, ex.Start,"start index failed");
                Assert.AreEqual(value.Length, ex.Finish,"finish index failed");
            });
        }
        
        [TestCase("'123' \r y=1", LexNodeType.Text,0, 5)]
        [TestCase("123 \r y=1", LexNodeType.Number, 0,3)]
        [TestCase("123*1 \r y=1", LexNodeType.Fun, 0,5)]
        [TestCase("(123*1) \r y=1", LexNodeType.Fun, 0,7)]
        [TestCase("-1 \r y=1", LexNodeType.Fun, 0,2)]
        [TestCase(" -1  \r y=1", LexNodeType.Fun, 1,3)]
        [TestCase("   '123'", LexNodeType.Text,3,8)]

        public void ReadExpressionOrNull_SeveralExpressions_retunsFirstExpressionWithCorrectBounds(string value, LexNodeType type,
            int start,int end)
        {
            var flow = new TokenFlow(Tokenizer.ToTokens(value));
            LexNodeReader reader = new LexNodeReader(flow);
            var ex = reader.ReadExpressionOrNull();
            Assert.IsNotNull(ex);
            Assert.Multiple(() =>
            {
                Assert.AreEqual(type, ex.Type);
                Assert.AreEqual(start, ex.Start,"start index failed");
                Assert.AreEqual(end, ex.Finish,"finish index failed");
            });
        }
    }
}