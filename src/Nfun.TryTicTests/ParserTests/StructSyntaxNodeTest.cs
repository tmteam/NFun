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
            var structSyntaxNode = ParserTestHelper.ParseSingleEquation<StructInitSyntaxNode>(text);
            Assert.AreEqual(1, structSyntaxNode.Children.Count());
            AssertGenericIntConstantDefenition(structSyntaxNode.Fields.First(),"a",(ulong)1);
        }
        
        [Test]
        public void TwoElementsConstStruct()
        {
            var text = @" @{ a = 1; b = 2 }";
            var tree   = Parser.Parse(Tokenizer.ToFlow(text));
            var structSyntaxNode = ParserTestHelper.ParseSingleEquation<StructInitSyntaxNode>(text);

            Assert.AreEqual(2, structSyntaxNode.Children.Count());
            
            AssertGenericIntConstantDefenition(structSyntaxNode.Fields.First(),"a",(ulong)1);
            AssertGenericIntConstantDefenition(structSyntaxNode.Fields.Skip(1).First(),"b",(ulong)2);
        }
        
        [TestCase("@{a 2}")]
        [TestCase("@{a =2b=3}")]
        [TestCase("@{a =2;b3}")]
        [TestCase("@{a =2;b ==3}")]
        [TestCase("@{a =2; 3}")]

        public void ObviousFailed(string text) 
            => Assert.Throws<FunParseException>(()=> Parser.Parse(Tokenizer.ToFlow(text)));

        private static void AssertGenericIntConstantDefenition(FieldDefenition eq, string varName, object val)
        {
            Assert.AreEqual(varName, eq.Name);
            Assert.IsInstanceOf<GenericIntSyntaxNode>(eq.Node);
            Assert.AreEqual(val, ((GenericIntSyntaxNode) eq.Node).Value);
        }
    }
}