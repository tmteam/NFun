using System.Collections.Generic;
using NFun.Runtime;

namespace NFun.Tokenization
{
    public class Tokenizer
    {
        public static TokenFlow ToFlow(string input) 
            => new TokenFlow(ToTokens(input));

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
            {'&', TokType.BitAnd},
            {'^', TokType.BitXor},
            {'/', TokType.Div},
            {'+', TokType.Plus},
            {'-', TokType.Minus},
            {'%', TokType.Rema},
            {'(', TokType.Obr},
            {')', TokType.Cbr},
            {'[', TokType.ArrOBr},
            {']', TokType.ArrCBr},
            {':', TokType.Colon},
            {'@', TokType.ArrConcat},
            {'~', TokType.BitInverse},
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
            {"int", TokType.IntType},
            {"anything", TokType.AnythingType},
            {"in", TokType.In}
        };
        
        public Tok TryReadNext(string str, int position)
        {
            if(position>=str.Length)
                return Tok.New(TokType.Eof, position);
            var current = str[position];
            if (current == ' ' || current == '\t')
                return TryReadNext(str, position + 1);

            if (current == '/')
            {
                var newPosition = SkipComments(str, position);
                if (newPosition != position)
                    return TryReadNext(str, newPosition);
            }

            if(current== 0)
                return  Tok.New(TokType.Eof, "", position+1);
            
            if (current == '\r' || current == '\n')
                return Tok.New(TokType.NewLine, current.ToString(), position+1);
            
            if (IsDigit(current))
                return ReadNumber(str, position);
            
            if (IsSpecial(current) is TokType symbol )
                return Tok.New(symbol, current.ToString(), position+1);

            if (IsLetter(current))
                return ReadIdOrKeyword(str, position);

            if (TryReadUncommonSpecialSymbols(str, position, current) is Tok tok) 
                return tok;

            if (IsQuote(current))
                return ReadText(str, position);
            
            return Tok.New(TokType.NotAToken, null, position+1);
        }
        private int SkipComments(string str, int position){
            //int start = position;
            if(str[position]!='/')
                return position;
            if(str.Length== position+1)
                return position;
            if (str[position + 1] == '/')
            {
                //singleLineComment
                int index = position+2;
                for (; index < str.Length && str[index] != '\r' && str[index] != '\n' ; index++)
                {}

                return index;
            }
            else if(str[position+1]== '*')
            {
                //multiline comments
                int index = position+2;
                for (; index < str.Length; index++)
                {
                    if (str[index - 1] == '*' && str[index] == '/')
                        return index + 1;
                }
                
                throw new FunParseException("Multiline comment not closed.  '*/' not found");
            }
            else
                return position;
        }
        private Tok ReadIdOrKeyword(string str, int position)
        {
            int start = position;
            for (; start < str.Length && (IsLetter(str[start]) || IsDigit(str[start])); start++)
            {}

            var word = str.Substring(position, start - position);
            //is id a keyword
            if (_keywords.ContainsKey(word))
                return Tok.New(_keywords[word], word, start);
            else
                return Tok.New(TokType.Id, word, start);
        }

        private static Tok TryReadUncommonSpecialSymbols(string str, int position, char current)
        {
            char? next = position < str.Length - 1 
                    ? str[position + 1] 
                    : (char?) null;
            
            switch (current)
            {
                case '*' when  next == '*':
                    return Tok.New(TokType.Pow, position+2);
                case '*' :
                    return Tok.New(TokType.Mult, position+1);
                case '|' when  next == '>':
                    return Tok.New(TokType.PipeForward, position+2);
                case '|':
                    return Tok.New(TokType.BitOr, position+1);
                case '>' when next == '=':
                    return  Tok.New(TokType.MoreOrEqual, position + 2);
                case '>' when next == '>':
                    return  Tok.New(TokType.BitShiftRight, position + 2);
                case '>':
                    return Tok.New(TokType.More, position + 1);
                case '<' when  next == '=':
                    return Tok.New(TokType.LessOrEqual, position + 2);
                case '<' when  next == '<':
                    return Tok.New(TokType.BitShiftLeft, position + 2);
                case '<' when next == '>':
                    return Tok.New(TokType.NotEqual, position+2);
                case '<':
                    return Tok.New(TokType.Less, position+1);
                case '=' when next == '=':
                    return Tok.New(TokType.Equal, position + 2);
                case '=' when next == '>':
                    return Tok.New(TokType.AnonymFun, position + 2);
                case '=':
                    return Tok.New(TokType.Def, position + 1);
                case '.' when next=='.':
                    return Tok.New(TokType.TwoDots, position+2);
                default:
                    return null;
            }
        }

        private TokType? IsSpecial(char val) =>
            _symbols.ContainsKey(val) ? _symbols[val] : (TokType?)null;
        
        private bool IsLetter(char val) =>val == '_' ||  (val >= 'a' && val <= 'z');

        private bool IsDigit(char val) => val >= '0' && val <= '9';

        private bool IsQuote(char val) => val == '\''|| val == '\"'; 

        private Tok ReadText(string str, int position)
        {
            for (var i = position+1; i < str.Length; i++)
            {
                if(IsQuote(str[i]))
                    return Tok.New(TokType.Text, str.Substring(position+1, i - position-1), i+1);
            }
            return Tok.New(TokType.NotAToken, str.Length);
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

            if (index < str.Length)
            {
                if (IsLetter(str[index ]))
                {
                    var txtToken = ReadIdOrKeyword(str, index);
                    return Tok.New(TokType.NotAToken, str.Substring(position,txtToken.Finish - position), txtToken.Finish);
                }
            }
            //if dot is last then skip
            if(dotPostition==index-1)
                return Tok.New(TokType.Number, str.Substring(position, index - position-1), index-1);
            return Tok.New(TokType.Number, str.Substring(position, index - position), index);
        }
    }
}