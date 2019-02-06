using System.Linq;
using Funny.Tokenization;
using NUnit.Framework;

namespace Funny.Tests
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
        [TestCase("x^y",     TokType.Id, TokType.Pow,          TokType.Id)]
        [TestCase("x=y",     TokType.Id, TokType.Def,          TokType.Id)]
        [TestCase("o = a and b",  TokType.Id, TokType.Def, TokType.Id, TokType.And,TokType.Id)]
        [TestCase("o = a == b",TokType.Id, TokType.Def, TokType.Id, TokType.Equal,TokType.Id)]
        [TestCase("o = a + b",    TokType.Id, TokType.Def, TokType.Id, TokType.Plus,TokType.Id)]
        [TestCase("o = a <> b",TokType.Id, TokType.Def, TokType.Id, TokType.NotEqual,TokType.Id)]

        public void TestTokinization(string exp, params TokType[] expected)
        {
             var tokens =  Tokenizer
                 .ToTokens(exp)
                 .Select(s=>s.Type)
                 .ToArray();
            CollectionAssert.AreEqual(expected,tokens);
        }
        
        
    }

    public static class Tt
    {
        
        public static Tok Id(string val)
            => Tok.New(TokType.Id, val,0);

        public static Tok Num(string val)
            => Tok.New(TokType.Number,val,0);
        public static Tok Plus()
            => Tok.New(TokType.Plus,0);
        public static Tok Minus()
            => Tok.New(TokType.Minus,0);

    }
}