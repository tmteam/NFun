using System.Collections.Generic;
using System.Linq;
using NFun.Tokenization;
using NUnit.Framework;

namespace NFun.UnitTests {

[TestFixture]
public class TokenizerTest {
    [TestCase("1", TokType.IntNumber)]
    [TestCase("x+1", TokType.Id, TokType.Plus, TokType.IntNumber)]
    [TestCase("x and y", TokType.Id, TokType.And, TokType.Id)]
    [TestCase("x or y", TokType.Id, TokType.Or, TokType.Id)]
    [TestCase("x xor y", TokType.Id, TokType.Xor, TokType.Id)]
    [TestCase("x != y", TokType.Id, TokType.NotEqual, TokType.Id)]
    [TestCase("x < y", TokType.Id, TokType.Less, TokType.Id)]
    [TestCase("x <= y", TokType.Id, TokType.LessOrEqual, TokType.Id)]
    [TestCase("x > y", TokType.Id, TokType.More, TokType.Id)]
    [TestCase("x >= y", TokType.Id, TokType.MoreOrEqual, TokType.Id)]
    [TestCase("x == y", TokType.Id, TokType.Equal, TokType.Id)]
    [TestCase("x**y", TokType.Id, TokType.Pow, TokType.Id)]
    [TestCase("x=y", TokType.Id, TokType.Def, TokType.Id)]
    [TestCase("o = a and b", TokType.Id, TokType.Def, TokType.Id, TokType.And, TokType.Id)]
    [TestCase("o = a == b", TokType.Id, TokType.Def, TokType.Id, TokType.Equal, TokType.Id)]
    [TestCase("o = a + b", TokType.Id, TokType.Def, TokType.Id, TokType.Plus, TokType.Id)]
    [TestCase("o = a != b", TokType.Id, TokType.Def, TokType.Id, TokType.NotEqual, TokType.Id)]
    [TestCase(
        "o = if (a>b)  1 else x",
        TokType.Id, TokType.Def,
        TokType.If, TokType.Obr, TokType.Id, TokType.More, TokType.Id,
        TokType.Cbr, TokType.IntNumber,
        TokType.Else, TokType.Id)]
    [TestCase("o = 'hiWorld'", TokType.Id, TokType.Def, TokType.Text)]
    [TestCase("o = ''+'hiWorld'", TokType.Id, TokType.Def, TokType.Text, TokType.Plus, TokType.Text)]
    [TestCase("x:real", TokType.Id, TokType.Colon, TokType.RealType)]
    [TestCase("x:int", TokType.Id, TokType.Colon, TokType.Int32Type)]
    [TestCase("x:int32", TokType.Id, TokType.Colon, TokType.Int32Type)]
    [TestCase("x:int64", TokType.Id, TokType.Colon, TokType.Int64Type)]
    [TestCase("x:text", TokType.Id, TokType.Colon, TokType.TextType)]
    [TestCase("x:bool", TokType.Id, TokType.Colon, TokType.BoolType)]
    [TestCase("x.y", TokType.Id, TokType.Dot, TokType.Id)]
    [TestCase(
        "x.y(1).z", TokType.Id,
        TokType.Dot, TokType.Id, TokType.Obr, TokType.IntNumber, TokType.Cbr,
        TokType.Dot, TokType.Id)]
    [TestCase("[0..1]", TokType.ArrOBr, TokType.IntNumber, TokType.TwoDots, TokType.IntNumber, TokType.ArrCBr)]
    [TestCase(
        "[0..1..2]",
        TokType.ArrOBr,
        TokType.IntNumber, TokType.TwoDots, TokType.IntNumber, TokType.TwoDots, TokType.IntNumber,
        TokType.ArrCBr)]
    [TestCase("0.", TokType.IntNumber, TokType.Dot)]
    [TestCase("0.1", TokType.RealNumber)]
    [TestCase("0xFF", TokType.HexOrBinaryNumber)]
    [TestCase("0xFF_01", TokType.HexOrBinaryNumber)]
    [TestCase("0b001", TokType.HexOrBinaryNumber)]
    [TestCase("0b001_0", TokType.HexOrBinaryNumber)]
    [TestCase("0xFF.a", TokType.HexOrBinaryNumber, TokType.Dot, TokType.Id)]
    [TestCase("0xFF_01.a", TokType.HexOrBinaryNumber, TokType.Dot, TokType.Id)]
    [TestCase("0b001.a", TokType.HexOrBinaryNumber, TokType.Dot, TokType.Id)]
    [TestCase("0b001_0.a", TokType.HexOrBinaryNumber, TokType.Dot, TokType.Id)]
    [TestCase("0xFF)", TokType.HexOrBinaryNumber, TokType.Cbr)]
    [TestCase("0xFF_01)", TokType.HexOrBinaryNumber, TokType.Cbr)]
    [TestCase("0b001)", TokType.HexOrBinaryNumber, TokType.Cbr)]
    [TestCase("0b001_0)", TokType.HexOrBinaryNumber, TokType.Cbr)]
    [TestCase("(0xFF)", TokType.Obr, TokType.HexOrBinaryNumber, TokType.Cbr)]
    [TestCase("(0xFF_01)", TokType.Obr, TokType.HexOrBinaryNumber, TokType.Cbr)]
    [TestCase("(0b001)", TokType.Obr, TokType.HexOrBinaryNumber, TokType.Cbr)]
    [TestCase("(0b001_0)", TokType.Obr, TokType.HexOrBinaryNumber, TokType.Cbr)]
    [TestCase("0x", TokType.NotAToken)]
    [TestCase("0b", TokType.NotAToken)]
    [TestCase("0bFF", TokType.NotAToken)]
    [TestCase("0xGG", TokType.NotAToken)]
    [TestCase("0x.y", TokType.NotAToken, TokType.Dot, TokType.Id)]
    [TestCase("0b.y", TokType.NotAToken, TokType.Dot, TokType.Id)]
    [TestCase("0xGG.y", TokType.NotAToken, TokType.Dot, TokType.Id)]
    [TestCase("0x)", TokType.NotAToken, TokType.Cbr)]
    [TestCase("0b)", TokType.NotAToken, TokType.Cbr)]
    [TestCase("0bFF)", TokType.NotAToken, TokType.Cbr)]
    [TestCase("0xGG)", TokType.NotAToken, TokType.Cbr)]
    [TestCase("(0x)", TokType.Obr, TokType.NotAToken, TokType.Cbr)]
    [TestCase("(0b)", TokType.Obr, TokType.NotAToken, TokType.Cbr)]
    [TestCase("(0bFF)", TokType.Obr, TokType.NotAToken, TokType.Cbr)]
    [TestCase("(0xGG)", TokType.Obr, TokType.NotAToken, TokType.Cbr)]
    [TestCase("1y = x", TokType.NotAToken, TokType.Def, TokType.Id)]
    [TestCase("1y", TokType.NotAToken)]
    [TestCase("0.0f", TokType.NotAToken)]
    [TestCase(
        "y = ['foo','bar']", TokType.Id, TokType.Def,
        TokType.ArrOBr,
        TokType.Text, TokType.Sep, TokType.Text,
        TokType.ArrCBr)]
    [TestCase(@"z = true#", TokType.Id, TokType.Def, TokType.True)]
    [TestCase(@"true#", TokType.True)]
    [TestCase(@"true #", TokType.True)]
    [TestCase(@"true #comment", TokType.True)]
    [TestCase(
        "0.foo()",
        TokType.IntNumber, TokType.Dot, TokType.Id, TokType.Obr, TokType.Cbr)]
    [TestCase("1.y", TokType.IntNumber, TokType.Dot, TokType.Id)]
    [TestCase(
        "y = 1; z = 2.0",
        TokType.Id, TokType.Def, TokType.IntNumber,
        TokType.NewLine,
        TokType.Id, TokType.Def, TokType.RealNumber)]
    [TestCase(
        "y = a+1; z = b+2.0",
        TokType.Id, TokType.Def, TokType.Id, TokType.Plus, TokType.IntNumber,
        TokType.NewLine,
        TokType.Id, TokType.Def, TokType.Id, TokType.Plus, TokType.RealNumber)]
    [TestCase(
        "x1 = 1.0\n ; \ny = 2",
        TokType.Id, TokType.Def, TokType.RealNumber,
        TokType.NewLine, TokType.NewLine, TokType.NewLine,
        TokType.Id, TokType.Def, TokType.IntNumber)]
    [TestCase(
        "x = 1; z = x # z == 2",
        TokType.Id, TokType.Def, TokType.IntNumber,
        TokType.NewLine,
        TokType.Id, TokType.Def, TokType.Id)]
    [TestCase(";;;", TokType.NewLine, TokType.NewLine, TokType.NewLine)]
    [TestCase(
        "f(x) = x*x; y = f(10); z = y",
        TokType.Id, TokType.Obr, TokType.Id, TokType.Cbr, TokType.Def, TokType.Id, TokType.Mult, TokType.Id,
        TokType.NewLine,
        TokType.Id, TokType.Def, TokType.Id, TokType.Obr, TokType.IntNumber, TokType.Cbr,
        TokType.NewLine,
        TokType.Id, TokType.Def, TokType.Id)]
    [TestCase(
        "'{112}'",
        TokType.TextOpenInterpolation, TokType.IntNumber, TokType.TextCloseInterpolation)]
    [TestCase(
        "'{112}';",
        TokType.TextOpenInterpolation, TokType.IntNumber, TokType.TextCloseInterpolation, TokType.NewLine)]
    [TestCase(
        "'1+2 = {12}'",
        TokType.TextOpenInterpolation, TokType.IntNumber, TokType.TextCloseInterpolation)]
    [TestCase(
        "'hello{o}world{b}'",
        TokType.TextOpenInterpolation, TokType.Id, TokType.TextMidInterpolation,
        TokType.Id, TokType.TextCloseInterpolation)]
    [TestCase("'{''}'", TokType.TextOpenInterpolation, TokType.Text, TokType.TextCloseInterpolation)]
    [TestCase(
        "'pre{ {a} + 'pre{0+1}' }after'",
        //'pre{ {a} +
        TokType.TextOpenInterpolation, TokType.FiObr, TokType.Id, TokType.FiCbr, TokType.Plus,
        //'pre{0+1}'
        TokType.TextOpenInterpolation, TokType.IntNumber, TokType.Plus, TokType.IntNumber,
        TokType.TextCloseInterpolation,
        //}after'
        TokType.TextCloseInterpolation)]
    [TestCase(
        "'pre{ 'pre{0+1}' }after'",
        //'pre{
        TokType.TextOpenInterpolation,
        //'pre{0+1}'
        TokType.TextOpenInterpolation, TokType.IntNumber, TokType.Plus, TokType.IntNumber,
        TokType.TextCloseInterpolation,
        //}after'
        TokType.TextCloseInterpolation)]
    [TestCase(
        "'pre{ {a} }'", TokType.TextOpenInterpolation, TokType.FiObr, TokType.Id, TokType.FiCbr,
        TokType.TextCloseInterpolation)]
    [TestCase(
        "'pre{ {} }'", TokType.TextOpenInterpolation, TokType.FiObr, TokType.FiCbr,
        TokType.TextCloseInterpolation)]
    [TestCase(
        "'pre{ {1} 2 }'", TokType.TextOpenInterpolation, TokType.FiObr,
        TokType.IntNumber, TokType.FiCbr, TokType.IntNumber, TokType.TextCloseInterpolation)]
    [TestCase(
        "'pre{ 'pre{0}after' }after'",
        //'pre{
        TokType.TextOpenInterpolation,
        //'pre{0}'
        TokType.TextOpenInterpolation, TokType.IntNumber, TokType.TextCloseInterpolation,
        //}after'
        TokType.TextCloseInterpolation)]
    [TestCase("{a =1 }", TokType.FiObr, TokType.Id, TokType.Def, TokType.IntNumber, TokType.FiCbr)]
    [TestCase(
        "out = {a =1; b = 2 }",
        TokType.Id, TokType.Def,
        TokType.FiObr,
        TokType.Id, TokType.Def, TokType.IntNumber,
        TokType.NewLine,
        TokType.Id, TokType.Def, TokType.IntNumber,
        TokType.FiCbr)]
    public void TokenFlowIsCorrect_ExpectEof(string exp, params TokType[] expected) {
        var tokens = new List<TokType>();
        foreach (var token in Tokenizer.ToTokens(exp))
        {
            tokens.Add(token.Type);
        }

        //Add Eof at end of flow for test readability
        CollectionAssert.AreEquivalent(expected.Append(TokType.Eof), tokens);
    }

    [TestCase("\t   true   \t", TokType.True)]
    [TestCase(" x", TokType.Id)]
    [TestCase("  somebody", TokType.Id)]
    [TestCase("  -", TokType.Minus)]
    [TestCase("!= ", TokType.NotEqual)]
    [TestCase("/  ", TokType.Div)]
    [TestCase(" * ", TokType.Mult)]
    [TestCase(" % ", TokType.Rema)]
    [TestCase(" =  ", TokType.Def)]
    public void ToTokens_SingleTokenWithOffsetIsCorrectAndContainsCorrectBounds(string expression, TokType type) {
        var startOffset = expression.Length - expression.TrimStart().Length;
        var endOffset = expression.Length - expression.TrimEnd().Length;
        CheckSingleToken(expression, 0, type, startOffset, expression.Length - endOffset);
    }

    [TestCase("x", TokType.Id)]
    [TestCase("whatisthat", TokType.Id)]
    [TestCase("*", TokType.Mult)]
    [TestCase("**", TokType.Pow)]
    [TestCase("/", TokType.Div)]
    [TestCase("=", TokType.Def)]
    [TestCase("==", TokType.Equal)]
    [TestCase("else", TokType.Else)]
    [TestCase("if", TokType.If)]
    [TestCase(">=", TokType.MoreOrEqual)]
    [TestCase(">", TokType.More)]
    [TestCase("'sometext'", TokType.Text)]
    [TestCase("12345", TokType.IntNumber)]
    [TestCase("0", TokType.IntNumber)]
    [TestCase("0.1", TokType.RealNumber)]
    [TestCase("0x00f", TokType.HexOrBinaryNumber)]
    [TestCase("123.4312_1", TokType.RealNumber)]
    [TestCase("false", TokType.False)]
    [TestCase("123abc", TokType.NotAToken)]
    public void ToTokens_SingleTokenIsCorrectAndContainsCorrectBounds(string expression, TokType type)
        => CheckSingleToken(expression, 0, type, 0, expression.Length);

    [TestCase("", TokType.Eof, 0, 0)]
    [TestCase("x", TokType.Id, 0, 1)]
    [TestCase("*", TokType.Mult, 0, 1)]
    [TestCase("**", TokType.Pow, 0, 2)]
    [TestCase("-", TokType.Minus, 0, 1)]
    [TestCase("else", TokType.Else, 0, 4)]
    [TestCase("if", TokType.If, 0, 2)]
    [TestCase("(2+3)", TokType.Obr, 0, 1)]
    [TestCase("if(2+3)", TokType.If, 0, 2)]
    public void ToTokens_FirstTokenIsCorrectAndContainsCorrectBounds(
        string expression, TokType type, int start, int end)
        => CheckSingleToken(expression, 0, type, start, end);

    private static void CheckSingleToken(string expression, int tokenNumber, TokType type, int start, int end) {
        var tokens = Tokenizer.ToTokens(expression);
        var first = tokens.ElementAt(tokenNumber);
        Assert.Multiple(
            () => {
                Assert.AreEqual(type, first.Type);
                Assert.AreEqual(start, first.Start, "start");
                Assert.AreEqual(end, first.Finish, "end");
            });
    }

    [TestCase("x+y", TokType.Plus, 1, 2)]
    [TestCase("-1", TokType.IntNumber, 1, 2)]
    [TestCase("-123", TokType.IntNumber, 1, 4)]
    [TestCase("-123.1", TokType.RealNumber, 1, 6)]
    [TestCase("else  if ", TokType.If, 6, 8)]
    [TestCase(" +if", TokType.If, 2, 4)]
    [TestCase("(2+3)", TokType.IntNumber, 1, 2)]
    [TestCase("if(2+3)", TokType.Obr, 2, 3)]
    [TestCase("if", TokType.Eof, 2, 2)]
    [TestCase("x", TokType.Eof, 1, 1)]
    public void ToTokens_secondTokenIsCorrectAndContainsCorrectBounds(
        string expression, TokType type, int start,
        int end)
        => CheckSingleToken(expression, 1, type, start, end);
}

}