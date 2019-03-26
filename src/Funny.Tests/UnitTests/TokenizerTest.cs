using System.Linq;
using Funny.Tokenization;
using NUnit.Framework;

namespace Funny.Tests.UnitTests
{
    [TestFixture]
    public class TokenizerTest
    {
        [TestCase("1",       TokType.Number)]
        [TestCase("x+1",     TokType.Id, TokType.Plus,         TokType.Number)]
        [TestCase("x and y", TokType.Id, TokType.And,          TokType.Id)]
        [TestCase("x or y",  TokType.Id, TokType.Or,           TokType.Id)]
        [TestCase("x xor y", TokType.Id, TokType.Xor,          TokType.Id)]
        [TestCase("x <> y",  TokType.Id, TokType.NotEqual,     TokType.Id)]
        [TestCase("x < y",   TokType.Id, TokType.Less,         TokType.Id)]
        [TestCase("x <= y",  TokType.Id, TokType.LessOrEqual,  TokType.Id)]
        [TestCase("x > y",   TokType.Id, TokType.More,         TokType.Id)]
        [TestCase("x >= y",  TokType.Id, TokType.MoreOrEqual,  TokType.Id)]
        [TestCase("x == y",  TokType.Id, TokType.Equal,        TokType.Id)]
        [TestCase("x**y",     TokType.Id, TokType.Pow,          TokType.Id)]
        [TestCase("x=y",     TokType.Id, TokType.Def,          TokType.Id)]
        [TestCase("o = a and b",  TokType.Id, TokType.Def, TokType.Id, TokType.And,TokType.Id)]
        [TestCase("o = a == b",TokType.Id, TokType.Def, TokType.Id, TokType.Equal,TokType.Id)]
        [TestCase("o = a + b",    TokType.Id, TokType.Def, TokType.Id, TokType.Plus,TokType.Id)]
        [TestCase("o = a <> b",TokType.Id, TokType.Def, TokType.Id, TokType.NotEqual,TokType.Id)]
        [TestCase("o = if a>b then 1 else x",
                                TokType.Id, TokType.Def, 
                                TokType.If, TokType.Id, TokType.More, TokType.Id, 
                                TokType.Then, TokType.Number, 
                                TokType.Else, TokType.Id)]
        [TestCase("o = 'hiWorld'", TokType.Id, TokType.Def, TokType.Text)]
        [TestCase("o = ''+'hiWorld'", TokType.Id, TokType.Def, TokType.Text, TokType.Plus, TokType.Text)]
        [TestCase("x:real", TokType.Id, TokType.Сolon, TokType.RealType)]
        [TestCase("x:int", TokType.Id, TokType.Сolon, TokType.IntType)]
        [TestCase("x:text", TokType.Id, TokType.Сolon, TokType.TextType)]
        [TestCase("x:bool", TokType.Id, TokType.Сolon, TokType.BoolType)]
        [TestCase("x|>y",  TokType.Id, TokType.PipeForward, TokType.Id)] 
        [TestCase("x|>y(1)|>z",  TokType.Id, 
            TokType.PipeForward, TokType.Id, TokType.Obr, TokType.Number, TokType.Cbr,
            TokType.PipeForward, TokType.Id)]
        [TestCase("[0..1]", TokType.ArrOBr, TokType.Number, TokType.TwoDots, TokType.Number, TokType.ArrCBr)]
        [TestCase("[0..1..2]", 
            TokType.ArrOBr, 
            TokType.Number, TokType.TwoDots, TokType.Number,TokType.TwoDots, TokType.Number,
            TokType.ArrCBr)]
        [TestCase("0.", TokType.Number, TokType.NotAToken)]
        [TestCase("0.1", TokType.Number)]

        public void TestTokinization(string exp, params TokType[] expected)
        {
             var tokens =  Tokenizer
                 .ToTokens(exp)
                 .Select(s=>s.Type)
                 .ToArray();
            CollectionAssert.AreEqual(expected,tokens);
        }
        
        
    }

}