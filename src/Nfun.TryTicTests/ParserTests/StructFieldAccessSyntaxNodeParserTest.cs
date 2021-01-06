using System.Linq;
using NFun.SyntaxParsing;
using NFun.SyntaxParsing.SyntaxNodes;
using NFun.Tokenization;
using NUnit.Framework;

namespace Nfun.ModuleTests.ParserTests
{
    public class StructFieldAccessSyntaxNodeParserTest
    {
        [Test]
        public void SingleFieldAccess()
        {
            var text = @"a.b";
            var node = ParserTestHelper.ParseSingleEquation<SyntaxFieldAccessSyntaxNode>(text);
            Assert.AreEqual("a", node.Source.AssertType<NamedIdSyntaxNode>().Id);
            Assert.AreEqual("b", node.FieldName);
        }
        [Test]
        public void ChainOf2FieldAccess()
        {
            var text = @"a.b.c";
            var node = ParserTestHelper.ParseSingleEquation<SyntaxFieldAccessSyntaxNode>(text);
            Assert.AreEqual("c", node.FieldName);
            var childAccess = node.Source.AssertType<SyntaxFieldAccessSyntaxNode>();
            
            Assert.AreEqual("a", childAccess.Source.AssertType<NamedIdSyntaxNode>().Id);
            Assert.AreEqual("b", childAccess.FieldName);
        }
        
        [Test]
        public void ChainOf3FieldAccess()
        {
            var text = @"a.b.c.d";
            var root = ParserTestHelper.ParseSingleEquation<SyntaxFieldAccessSyntaxNode>(text);
            Assert.AreEqual("d", root.FieldName);
            var childAccess = root.Source.AssertType<SyntaxFieldAccessSyntaxNode>();
            Assert.AreEqual("c", childAccess.FieldName);
            var childOfChildAccess = childAccess.Source.AssertType<SyntaxFieldAccessSyntaxNode>();
            Assert.AreEqual("b", childOfChildAccess.FieldName);
            Assert.AreEqual("a", childOfChildAccess.Source.AssertType<NamedIdSyntaxNode>().Id);
        }
        [Test]
        public void SingleFieldWithPipeFunctionCall()
        {
            var text = @"a.b.f()";  //equal f(mem(a,b))
            var node = ParserTestHelper.ParseSingleEquation<FunCallSyntaxNode>(text);
            Assert.AreEqual(1, node.Args.Length);
            var synode = node.Args[0] as SyntaxFieldAccessSyntaxNode;
            Assert.IsNotNull(synode);
            
            Assert.AreEqual("a", synode.Source.AssertType<NamedIdSyntaxNode>().Id);
            Assert.AreEqual("b", synode.FieldName);
        }
        
        [Test]
        public void MultipleFieldWithPipeFunctionCall()
        {
            var text = @"a.b.f().g().h.i.j()";  
            
            //equal j(a.b.f().g().h.i)
            //equal j(g(a.b.f()).h.i)

            //equal j(a.b.f().g().h.i)
            //equal j(g(f(a.b)).h.i)
            //equal j((g ( f( a->b)) ->h) ->i)

            var jCall = ParserTestHelper.ParseSingleEquation<FunCallSyntaxNode>(text);
            Assert.AreEqual("j",jCall.Id);
            var ifield = jCall.Args[0].AssertType<SyntaxFieldAccessSyntaxNode>("i");
            Assert.AreEqual("i", ifield.FieldName);
            var hfield = ifield.Source.AssertType<SyntaxFieldAccessSyntaxNode>("h");
            Assert.AreEqual("h", hfield.FieldName);
            var gCall = hfield.Source.AssertType<FunCallSyntaxNode>("g");
            Assert.AreEqual("g",gCall.Id);
            var fCall = gCall.Args[0].AssertType<FunCallSyntaxNode>("f");
            Assert.AreEqual("f",fCall.Id);
            var bfield = fCall.Args[0].AssertType<SyntaxFieldAccessSyntaxNode>("b");
            Assert.AreEqual("b",bfield.FieldName);
            var aVar = bfield.Source.AssertType<NamedIdSyntaxNode>("a");
            Assert.AreEqual("a",aVar.Id);
        }
    }
}