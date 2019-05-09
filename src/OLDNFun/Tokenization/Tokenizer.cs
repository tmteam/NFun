using System;
using System.Collections.Generic;
using NFun.ParseErrors;

namespace NFun.Tokenization
{
    public class Tokenizer
    {
        public static TokenFlow ToFlow(string input) 
            => new TokenFlow(ToTokens(input));

        public static IEnumerable<Tok> ToTokens(string input)
        {
            var reader = new Tokenizer();
            for (int i = 0; ;)
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
            {'&', TokType.BitAnd},
            {'^', TokType.BitXor},
            {'|', TokType.BitOr},
            {'/', TokType.Div},
            {'+', TokType.Plus},
            {'-', TokType.Minus},
            {'%', TokType.Rema},
            {'(', TokType.Obr},
            {')', TokType.Cbr},
            {'[', TokType.ArrOBr},
            {']', TokType.ArrCBr},
            {':', TokType.Colon},
            {'@', TokType.Attribute},
            {'~', TokType.BitInverse}
        };
        
        private readonly Dictionary<string, TokType> _keywords = new Dictionary<string, TokType>
        {
            {"and",   TokType.And},
            {"or",    TokType.Or},
            {"xor",   TokType.Xor},
            {"not",   TokType.Not},
            {"if",    TokType.If},
            {"then",  TokType.Then},
            {"else",  TokType.Else},
            {"true",  TokType.True},
            {"false", TokType.False},
            {"text", TokType.TextType},
            {"bool", TokType.BoolType},
            {"real", TokType.RealType},
            {"int", TokType.Int32Type},
            {"int32", TokType.Int32Type},
            {"int64", TokType.Int64Type},
            {"anything", TokType.AnythingType},
            {"in", TokType.In}
        };
        
        public Tok TryReadNext(string str, int position)
        {
            if(position>=str.Length)
                return Tok.New(TokType.Eof, position,position);
            
            var current = str[position];
            if (current == '#')
            {
                var newPosition = SkipComments(str, position);
                if (newPosition == position)
                    newPosition++; 
                return TryReadNext(str, newPosition);
            }
            
            if (current == ' ' || current == '\t')
                return TryReadNext(str, position + 1);

            if(current== 0)
                return  Tok.New(TokType.Eof, "", position,position);
            
            if (current == '\r' || current == '\n')
                return Tok.New(TokType.NewLine, current.ToString(), position,position+1);
            
            if (IsDigit(current))
                return ReadNumber(str, position);
            
            if (IsSpecial(current) is TokType symbol )
                return Tok.New(symbol, current.ToString(), position,position+1);

            if (IsLetter(current))
                return ReadIdOrKeyword(str, position);

            if (TryReadUncommonSpecialSymbols(str, position, current) is Tok tok) 
                return tok;

            if (IsQuote(current))
                return ReadText(str, position);
            
            return Tok.New(TokType.NotAToken, current.ToString(), position,position+1);
        }
        
        private int SkipComments(string str, int position){
            if(str[position]!='#')
                throw new InvalidOperationException("'#' symbol expected");
            if(str.Length== position+1)
                return position;

            int index = position+2;
            for (; index < str.Length && str[index] != '\r' && str[index] != '\n' ; index++)
            {}
            
            return index;
        }
        private Tok ReadIdOrKeyword(string str, int position)
        {
            int finish = position;
            for (; finish < str.Length && (IsLetter(str[finish]) || IsDigit(str[finish])); finish++)
            {}

            var word = str.Substring(position, finish - position);
            //is id a keyword
            if (_keywords.ContainsKey(word))
                return Tok.New(_keywords[word], word, position, finish);
            else
                return Tok.New(TokType.Id, word,position, finish);
        }

        private static Tok TryReadUncommonSpecialSymbols(string str, int position, char current)
        {
            char? next = position < str.Length - 1 
                    ? str[position + 1] 
                    : (char?) null;
            
            switch (current)
            {
                case '*' when  next == '*':
                    return Tok.New(TokType.Pow, position, position+2);
                case '*' :
                    return Tok.New(TokType.Mult, position,position+1);
                case '>' when next == '=':
                    return  Tok.New(TokType.MoreOrEqual,position, position + 2);
                case '>' when next == '>':
                    return  Tok.New(TokType.BitShiftRight,position, position + 2);
                case '>':
                    return Tok.New(TokType.More, position,position + 1);
                case '<' when  next == '=':
                    return Tok.New(TokType.LessOrEqual, position,position + 2);
                case '<' when  next == '<':
                    return Tok.New(TokType.BitShiftLeft, position,position + 2);
                case '<':
                    return Tok.New(TokType.Less, position, position+1);
                case '=' when next == '=':
                    return Tok.New(TokType.Equal, position, position + 2);
                case '=' when next == '>':
                    return Tok.New(TokType.AnonymFun, position,position + 2);
                case '=':
                    return Tok.New(TokType.Def, position, position + 1);
                case '.' when next=='.':
                    return Tok.New(TokType.TwoDots, position, position+2);
                case '.':
                    return Tok.New(TokType.PipeForward, position, position+1);
                case '!' when next == '=':
                    return Tok.New(TokType.NotEqual, position, position+2);
                default:
                    return null;
            }
        }

        private TokType? IsSpecial(char val) =>
            _symbols.ContainsKey(val) ? _symbols[val] : (TokType?)null;
        
        private bool IsLetter(char val) =>val == '_' ||  (val >= 'a' && val <= 'z') || (val >= 'A' && val <= 'Z');

        private bool IsDigit(char val) => val >= '0' && val <= '9';

        private bool IsQuote(char val) => val == '\''|| val == '\"'; 

        /// <exception cref="FunParseException"></exception>
        private Tok ReadText(string str, int position)
        {
            var(result, endPosition) = QuotationReader.ReadQuotation(str, position);
            return Tok.New(TokType.Text, result,position, endPosition);
            /*for (var i = position+1; i < str.Length; i++)
            {
                if(IsQuote(str[i]))
                    return Tok.New(TokType.Text, 
                        str.Substring(position+1, i - position-1),position, i+1);
            }
            return Tok.New(TokType.NotAToken, position, str.Length);*/
        }
        
        private Tok ReadNumber(string str, int position)
        {
            int dotPostition = -1;
            bool hasTypeSpecifier = false;
            
            int index = position;
            for (; index < str.Length; index++)
            {
                if (IsDigit(str[index])) 
                    continue;
                
                if (hasTypeSpecifier)
                {
                    if(str[index]>='a' && str[index] <='f' )
                        continue;
                    if(str[index]>='A' && str[index] <='F' )
                        continue;
                }
                else if (index == position + 1 && str[position] == '0')
                {
                    if (str[index] == 'x' || str[index] == 'b')
                    {
                        hasTypeSpecifier = true;
                        continue;
                    }
                }

                if(str[index]=='_')
                    continue;
                
                if (!hasTypeSpecifier && str[index] == '.' && dotPostition==-1)
                {
                    dotPostition = index;
                    continue;
                }
                break;
            }
           
            //if dot is last then skip
            if(dotPostition==index-1)
                return Tok.New(TokType.Number, 
                    str.Substring(position, index - position-1),position, index-1);
            if (index < str.Length && IsLetter(str[index ]))
            {
                var txtToken = ReadIdOrKeyword(str, index);
                return Tok.New(TokType.NotAToken, str.Substring(position,txtToken.Finish - position), 
                    position,txtToken.Finish);
            }
            return Tok.New(TokType.Number, str.Substring(position, index - position), 
                position, index);
        }
    }
}