using System.Collections.Generic;

namespace Funny.Tokenization
{
    public class Tokenizer
    {
        public static IEnumerable<Tok> ToTokens(string input)
        {
            var reader = new Tokenizer();
            for (int i = 0; i<input.Length; )
            {
                var res = reader.TryReadNext(input, i);
                yield return res;
                if (res.Is(TokType.Eof)) 
                    yield break;
                i = res.Finish;
            }
        }
        
        
        private readonly Dictionary<char, TokType> _symbols = new Dictionary<char, TokType>
        {
            {',', TokType.Sep},
            {'/', TokType.Div},
            {'*', TokType.Mult},
            {'+', TokType.Plus},
            {'-', TokType.Minus},
            {'%', TokType.Rema},
            {'^', TokType.Pow},
            {'(', TokType.Obr},
            {')', TokType.Cbr},
        };
        
        private readonly Dictionary<string, TokType> _keywords = new Dictionary<string, TokType>
        {
            {"and", TokType.And},
            {"or", TokType.Or},
            {"xor", TokType.Xor},
            {"not", TokType.Not},
        };
        
        public Tok TryReadNext(string str, int position)
        {
            if(position>=str.Length)
                return Tok.New(TokType.Eof, position);
            var current = str[position];
            if (current == ' ' || current == '\t')
                return TryReadNext(str, position + 1);

            if(current== 0)
                return  Tok.New(TokType.Eof, "", position+1);
            
            if (current == '\r' || current == '\n')
                return Tok.New(TokType.NewLine, current.ToString(), position+1);
            
            if (IsDigit(current))
                return ReadNumber(str, position);
            
            if (IsSpecial(current) is TokType symbol )
                return Tok.New(symbol, current.ToString(), position+1);

            if (IsLetter(current)){
                int start = position;
                for (; start < str.Length && (IsLetter(str[start]) || IsDigit(str[start])); start++);

                var id = str.Substring(position, start - position);
                //is id a keyword
                if(_keywords.ContainsKey(id))
                    return Tok.New(_keywords[id], id, start);
                else
                    return Tok.New(TokType.Id,id , start);
            }

            if (TryReadUncommonSpecialSymbols(str, position, current) is Tok tok) 
                return tok;

            return Tok.New(TokType.NotAToken, null, position+1);
        }

        private static Tok TryReadUncommonSpecialSymbols(string str, int position, char current)
        {
            char? next = position < str.Length - 1 
                    ? str[position + 1] 
                    : (char?) null;
            
            switch (current)
            {
                case '>' when next == '=':
                    return  Tok.New(TokType.MoreOrEqual, position + 2);
                case '>':
                    return Tok.New(TokType.More, position + 1);
                case '<' when  next == '=':
                    return Tok.New(TokType.LessOrEqual, position + 2);
                
                case '<' when next == '>':
                    return Tok.New(TokType.NotEqual, position+2);
                case '<':
                    return Tok.New(TokType.Less, position+1);
                
                case '=' when next == '=':
                    return Tok.New(TokType.Equal, position + 2);
                case '=':
                    return Tok.New(TokType.Def, position + 1);
                default:
                    return null;
            }
        }


        private TokType? IsSpecial(char val) =>
            _symbols.ContainsKey(val) ? _symbols[val] : (TokType?)null;
        
        private bool IsLetter(char val) =>val == '_' ||  (val >= 'a' && val <= 'z');
        private bool IsDigit(char val) => val >= '0' && val <= '9';
        
        
        private Tok ReadNumber(string str, int position)
        {
            int i = position;
            bool hasOneDot = false;
            bool hasTypeSpecifier = false;
            for (; i < str.Length; i++)
            {
                if (IsDigit(str[i])) 
                    continue;
                
                if (hasTypeSpecifier)
                {
                    if(str[i]>='a' && str[i] <='f' )
                        continue;
                    if(str[i]>='A' && str[i] <='F' )
                        continue;
                }
                else if (i == position + 1 && str[position] == '0')
                {
                    if (str[i] == 'x' || str[i] == 'b')
                    {
                        hasTypeSpecifier = true;
                        continue;
                    }
                }

                if(str[i]=='_')
                    continue;
                
                if (!hasTypeSpecifier && str[i] == '.' && !hasOneDot)
                {
                    hasOneDot = true;
                    continue;
                }

                
                break;
            }
            return Tok.New(TokType.Number, str.Substring(position, i - position), i);
        }
    }
}