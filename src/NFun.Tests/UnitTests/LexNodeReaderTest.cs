using NFun.Parsing;
using NFun.Tokenization;
using NUnit.Framework;

namespace Funny.Tests.UnitTests
{
    public class LexNodeReaderTest
    {
        [TestCase("y", SyntaxNodeType.Var)]
        [TestCase("x*y", SyntaxNodeType.Fun)]
        [TestCase("-x", SyntaxNodeType.Fun)]
        [TestCase("1+2", SyntaxNodeType.Fun)]
        [TestCase("1+2/3-1.0", SyntaxNodeType.Fun)]
        [TestCase("1+2/(3-1.0)", SyntaxNodeType.Fun)]
        [TestCase("1", SyntaxNodeType.Number)]
        [TestCase("(1)", SyntaxNodeType.Number)]
        [TestCase("1.0", SyntaxNodeType.Number)]
        [TestCase("'someText'", SyntaxNodeType.Text)]
        [TestCase("'  someText'", SyntaxNodeType.Text)]
        public void ReadExpressionOrNull_SingleExpression_retunsIt(string value, SyntaxNodeType type)
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
        
        [TestCase("'123' \r y=1", SyntaxNodeType.Text,0, 5)]
        [TestCase("123 \r y=1", SyntaxNodeType.Number, 0,3)]
        [TestCase("123*1 \r y=1", SyntaxNodeType.Fun, 0,5)]
        [TestCase("(123*1) \r y=1", SyntaxNodeType.Fun, 0,7)]
        [TestCase("-1 \r y=1", SyntaxNodeType.Fun, 0,2)]
        [TestCase(" -1  \r y=1", SyntaxNodeType.Fun, 1,3)]
        [TestCase("   '123'", SyntaxNodeType.Text,3,8)]

        public void ReadExpressionOrNull_SeveralExpressions_retunsFirstExpressionWithCorrectBounds(string value, SyntaxNodeType type,
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