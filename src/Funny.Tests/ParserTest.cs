using System.IO.Pipes;
using System.Linq;
using Funny.Parsing;
using Funny.Tokenization;
using NUnit.Framework;

namespace Funny.Tests
{
    [TestFixture]
    public class ParserTest
    {
        [TestCase("y = 1+x", "1+x")]
        [TestCase("y = 1")]
        [TestCase("y = x+z*(x-z)", "x+z*(x-z)")]
        public void SingleEquatationParserTest(string text, string expectedExpr, params string[] variables)
        {
            var parsed   = Parser.Parse(Tokenizer.ToFlow(text));

            Assert.AreEqual(1, parsed.Equatations.Length);
            var equatation = parsed.Equatations.First();
            
            Assert.Multiple(() =>
            {
                Assert.AreEqual("y", equatation.Id);
                AssertParsed(equatation.Expression, expectedExpr);
            });
        }
        [TestCase("y(x) = 1+x", "1+x", "x")]
        [TestCase("y() = 1", "1")]
        [TestCase("y(x,z) = x+z*(x-z)", "x+z*(x-z)","x","z")]
        public void SingleFunParserTest(string text, string expectedExpr, params string[] variables)
        {
            var eq   = Parser.Parse(Tokenizer.ToFlow(text));

            Assert.AreEqual(1, eq.UserFuns.Length);
            var fun = eq.UserFuns.First();
            
            Assert.Multiple(() =>
            {
                Assert.AreEqual("y", fun.Id);
                AssertParsed(fun, expectedExpr,variables);
            });
        }

        [Test]
        public void ComplexTextTest()
        {
            var text = @"
                    max(x,y) = if x>y then x else y
                    max3(x,y,z) = max(x,max(y,z))
                    
                    y1 = max3(x,y,z)
                    y2 =  max(x,y)+1
                ";
            var eq   = Parser.Parse(Tokenizer.ToFlow(text));
            
            Assert.Multiple(()=>
            {
                Assert.AreEqual(2, eq.UserFuns.Length);
                Assert.AreEqual(2, eq.Equatations.Length);
            });
            
            var maxf = eq.UserFuns.SingleOrDefault(f => f.Id == "max");
            var max3f = eq.UserFuns.SingleOrDefault(f => f.Id == "max3");

            var y1equatation = eq.Equatations.SingleOrDefault(e => e.Id == "y1");
            var y2equatation = eq.Equatations.SingleOrDefault(e => e.Id == "y2");
            
            AssertParsed(maxf,"if x>y then x else y", "x","y" );
            AssertParsed(max3f,"max(x,max(y,z))", "x","y","z" );
            AssertParsed(y1equatation.Expression,  "max3(x,y,z)");
            AssertParsed(y2equatation.Expression,  "max(x,y)+1");

        }
        
        private void AssertParsed(UserFunDef fun,string expectedExpr, params string[] variables)
        {
            Assert.Multiple(() =>
            {
                CollectionAssert.AreEqual(variables, fun.Args);
                AssertParsed(fun.Node, expectedExpr);
            });
        }
        private void AssertParsed(LexNode node,string expectedExpr)
        {
            var expectedExpression = new LexNodeReader(Tokenizer.ToFlow(expectedExpr)).ReadExpression();
            AssertEquals(expectedExpression, node);
        }

        private void AssertEquals(LexNode expected, LexNode actual)
        {
            Assert.AreEqual(expected?.ToString(), actual?.ToString());
        }
        
    }
}