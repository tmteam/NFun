using System.Linq;
using NFun.SyntaxParsing;
using NFun.SyntaxParsing.SyntaxNodes;
using NFun.Tokenization;
using NUnit.Framework;

namespace Funny.Tests
{
    [TestFixture]
    public class ParserTest
    {
        /*
        [TestCase("y = 1+x", "1+x")]
        [TestCase("y = 1","1")]
        [TestCase("y = x+z*(x-z)", "x+z*(x-z)")]
        public void SingleEquationParsingTest(string text, string expectedExpr, params string[] variables)
        {
            var parsed   = TopLevelParser.Parse(Tokenizer.ToFlow(text));

            Assert.AreEqual(1, parsed.Nodes.Length);
            var Equation = parsed.Nodes.OfType<EquationSyntaxNode>().First();
            
            Assert.Multiple(() =>
            {
                Assert.AreEqual("y", Equation.Id);
                AssertParsed(Equation, expectedExpr);
            });
        }*/
        [TestCase("y(x) = 1+x", "1+x", "x")]
        [TestCase("y() = 1", "1")]
        [TestCase("y(x,z) = x+z*(x-z)", "x+z*(x-z)","x","z")]
        public void SingleFunctionParsingTest(string text, string expectedExpr, params string[] variables)
        {
            var eq   = Parser.Parse(Tokenizer.ToFlow(text));

            Assert.AreEqual(1, eq.Nodes.Count(n=>n is UserFunctionDefenitionSyntaxNode));
            var fun = eq.Nodes.OfType<UserFunctionDefenitionSyntaxNode>().First();
            
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
                    max(x,y) = if (x>y) x else y
                    max3(x,y,z) = max(x,max(y,z))
                    
                    y1 = max3(x,y,z)
                    y2 =  max(x,y)+1
                ";
            var eq   = Parser.Parse(Tokenizer.ToFlow(text));

            var functions = eq.Nodes.OfType<UserFunctionDefenitionSyntaxNode>();
            
            Assert.Multiple(()=>
            {
                Assert.AreEqual(2, functions.Count());
                Assert.AreEqual(4, eq.Nodes.Length);
            });
            
            var maxf = functions.Single(f => f.Id == "max");
            var max3f = functions.Single(f => f.Id == "max3");

            var y1Equation = eq.Nodes.OfType<EquationSyntaxNode>().Single(e => e.Id == "y1");
            var y2Equation = eq.Nodes.OfType<EquationSyntaxNode>().Single(e => e.Id == "y2");
            
            AssertParsed(maxf,"if (x>y) x else y", "x","y" );
            AssertParsed(max3f,"max(x,max(y,z))", "x","y","z" );
            AssertParsed(y1Equation.Expression,  "max3(x,y,z)");
            AssertParsed(y2Equation.Expression,  "max(x,y)+1");

        }
        
        private void AssertParsed(UserFunctionDefenitionSyntaxNode fun,string expectedExpr, params string[] variables)
        {
            Assert.Multiple(() =>
            {
                CollectionAssert.AreEqual(variables, fun.Args.Select(a=>a.Id));
                AssertParsed(fun.Body, expectedExpr);
            });
        }
        private void AssertParsed(ISyntaxNode node,string expectedExpr)
        {
            var expectedExpression = SyntaxNodeReader.ReadNodeOrNull(Tokenizer.ToFlow(expectedExpr));
            AssertEquals(expectedExpression, node);
        }

        private void AssertEquals(ISyntaxNode expected, ISyntaxNode actual)
        {
            Assert.AreEqual(expected?.ToString(), actual?.ToString());
        }
        
    }
}