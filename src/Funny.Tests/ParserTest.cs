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
        [TestCase("y = 1","1")]
        [TestCase("y = x+z*(x-z)", "x+z*(x-z)")]
        public void SingleEquationParsingTest(string text, string expectedExpr, params string[] variables)
        {
            var parsed   = Parser.Parse(Tokenizer.ToFlow(text));

            Assert.AreEqual(1, parsed.Equations.Length);
            var Equation = parsed.Equations.First();
            
            Assert.Multiple(() =>
            {
                Assert.AreEqual("y", Equation.Id);
                AssertParsed(Equation.Expression, expectedExpr);
            });
        }
        [TestCase("y(x) = 1+x", "1+x", "x")]
        [TestCase("y() = 1", "1")]
        [TestCase("y(x,z) = x+z*(x-z)", "x+z*(x-z)","x","z")]
        public void SingleFunctionParsingTest(string text, string expectedExpr, params string[] variables)
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
        public void ComplexParsingTest()
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
                Assert.AreEqual(2, eq.Equations.Length);
            });
            
            var maxf = eq.UserFuns.Single(f => f.Id == "max");
            var max3f = eq.UserFuns.Single(f => f.Id == "max3");

            var y1Equation = eq.Equations.Single(e => e.Id == "y1");
            var y2Equation = eq.Equations.Single(e => e.Id == "y2");
            
            AssertParsed(maxf,"if x>y then x else y", "x","y" );
            AssertParsed(max3f,"max(x,max(y,z))", "x","y","z" );
            AssertParsed(y1Equation.Expression,  "max3(x,y,z)");
            AssertParsed(y2Equation.Expression,  "max(x,y)+1");

        }
        
        private void AssertParsed(LexFunction fun,string expectedExpr, params string[] variables)
        {
            Assert.Multiple(() =>
            {
                CollectionAssert.AreEqual(variables, fun.Args.Select(a=>a.Id));
                AssertParsed(fun.Node, expectedExpr);
            });
        }
        private void AssertParsed(LexNode node,string expectedExpr)
        {
            var expectedExpression = new LexNodeReader(Tokenizer.ToFlow(expectedExpr)).ReadExpressionOrNull();
            AssertEquals(expectedExpression, node);
        }

        private void AssertEquals(LexNode expected, LexNode actual)
        {
            Assert.AreEqual(expected?.ToString(), actual?.ToString());
        }
        
    }
}