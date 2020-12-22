using System.Linq;
using NFun.Exceptions;
using NFun.SyntaxParsing;
using NFun.SyntaxParsing.SyntaxNodes;
using NFun.Tokenization;
using NUnit.Framework;

namespace Nfun.ModuleTests.ParserTests
{
    public class StructSyntaxNodeParserTest
    {
        [Test]
        public void SingleElementConstStruct()
        {
            var text = @" @{ a = 1 }";
            var structSyntaxNode = ParserTestHelper.ParseSingleEquation<StructSyntaxNode>(text);
            Assert.AreEqual(1, structSyntaxNode.Children.Count());
            AssertGenericIntConstantDefenition(structSyntaxNode.Children.First(),"a",(ulong)1);
        }
        
        [Test]
        public void TwoElementsConstStruct()
        {
            var text = @" @{ a = 1; b = 2 }";
            var tree   = Parser.Parse(Tokenizer.ToFlow(text));
            var structSyntaxNode = ParserTestHelper.ParseSingleEquation<StructSyntaxNode>(text);

            Assert.AreEqual(2, structSyntaxNode.Children.Count());
            
            AssertGenericIntConstantDefenition(structSyntaxNode.Children.First(),"a",(ulong)1);
            AssertGenericIntConstantDefenition(structSyntaxNode.Children.Skip(1).First(),"b",(ulong)2);
        }
        
        [TestCase("@{a 2}")]
        [TestCase("@{a =2b=3}")]
        [TestCase("@{a =2;b3}")]
        [TestCase("@{a =2;b ==3}")]
        [TestCase("@{a =2; 3}")]

        public void ObviousFailed(string text) 
            => Assert.Throws<FunParseException>(()=> Parser.Parse(Tokenizer.ToFlow(text)));

        private static void AssertGenericIntConstantDefenition(ISyntaxNode eq, string varName, object val)
        {
            Assert.IsInstanceOf<EquationSyntaxNode>(eq);
            var equation = (EquationSyntaxNode) eq;
            Assert.AreEqual(varName, equation.Id);
            Assert.IsInstanceOf<GenericIntSyntaxNode>(equation.Expression);
            Assert.AreEqual(val, ((GenericIntSyntaxNode) equation.Expression).Value);
        }
    }
}