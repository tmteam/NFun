using System.Linq;
using NFun.SyntaxParsing;
using NFun.SyntaxParsing.SyntaxNodes;
using NFun.Tokenization;
using NUnit.Framework;

namespace NFun.UnitTests.ParserTests;

public static class ParserTestHelper {
    public static Tout AssertType<Tout>(this object input, string message = null) {
        Assert.IsInstanceOf<Tout>(input, message);
        return (Tout)input;
    }

    public static TSyntaxNode ParseSingleEquation<TSyntaxNode>(string text) {
        var tree = Parser.Parse(Tokenizer.ToFlow(text));
        var eq = ((EquationSyntaxNode)tree.Children.First()).Children.First();
        Assert.IsInstanceOf<TSyntaxNode>(eq);
        return (TSyntaxNode)eq;
    }
}
