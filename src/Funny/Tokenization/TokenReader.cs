using System.Collections.Generic;

namespace Funny.Tokenization
{
    class TokenReader
    {
        private readonly Dictionary<char, TokType> _symbols = new Dictionary<char, TokType>
        {
            {'/', TokType.Div},
            {'*', TokType.Mult},
            {'+', TokType.Plus},
            {'-', TokType.Minus},
            {'%', TokType.Rema},
            {'^', TokType.Pow},
            {'(', TokType.Obr},
            {')', TokType.Cbr},
            {'=', TokType.Equal},
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
                int i = position;
                for (; i < str.Length && (IsLetter(str[i]) || IsDigit(str[i])); i++);
                return Tok.New(TokType.Id, str.Substring(position, i- position), i);
            }
            return Tok.New(TokType.NotAToken, null, position+1);
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