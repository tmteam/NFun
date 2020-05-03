using System;
using System.Collections.Generic;
using System.Linq;
using NFun.ParseErrors;

namespace NFun.Tokenization
{

    class InterpolationLayer
    {
        public char OpenQuoteSymbol;
        public int FigureBracketsDiff;
    }
    public class Tokenizer
    {
        #region statics
        public static TokFlow ToFlow(string input) 
            => new TokFlow(ToTokens(input));

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
        
        private static readonly Dictionary<string, TokType> _keywords = new Dictionary<string, TokType>
        {
            {"in", TokType.In},

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
           
            {"int16", TokType.Int16Type},
            {"int", TokType.Int32Type},
            {"int32", TokType.Int32Type},
            {"int64", TokType.Int64Type},
            
            {"byte",  TokType.UInt8Type},
            {"uint8",  TokType.UInt8Type},
            {"uint16", TokType.UInt16Type},
            {"uint",   TokType.UInt32Type},
            {"uint32", TokType.UInt32Type},
            {"uint64", TokType.UInt64Type},
            
            {"anything", TokType.AnythingType},
            
            {"never", TokType.Reserved},
            {"base", TokType.Reserved},

            {"void", TokType.Reserved},
            {"char", TokType.Reserved},
            {"int8", TokType.Reserved},
            {"ip", TokType.Reserved},
            {"date", TokType.Reserved},
            
            {"let", TokType.Reserved},
            {"var", TokType.Reserved},
            {"val", TokType.Reserved},
            {"from", TokType.Reserved},
            {"to", TokType.Reserved},
            {"of", TokType.Reserved},
            {"import", TokType.Reserved},
            {"async", TokType.Reserved},
            {"await", TokType.Reserved},
            {"int128", TokType.Reserved},
            {"uint128", TokType.Reserved},
            {"decimal", TokType.Reserved},
            {"number", TokType.Reserved},
            {"num", TokType.Reserved},
            {"type", TokType.Reserved},
            {"struct", TokType.Reserved},
            {"where", TokType.Reserved},
            {"unless", TokType.Reserved},
            {"switch", TokType.Reserved},
            {"case", TokType.Reserved},
            {"match", TokType.Reserved},
            {"optional", TokType.Reserved},
            {"out",   TokType.Reserved},
            {"nil",  TokType.Reserved},
            {"null",  TokType.Reserved},
            {"none",  TokType.Reserved},
            {"pass",  TokType.Reserved},
            {"for",   TokType.Reserved},
            {"do",    TokType.Reserved},
            {"while", TokType.Reserved},
            {"until", TokType.Reserved},

            // {"bad",   TokType.Reserved},
            {"fail",   TokType.Reserved},
            {"error",   TokType.Reserved},
            {"oops",   TokType.Reserved},
            {"throw", TokType.Reserved},
            {"result", TokType.Reserved},
            {"_", TokType.Reserved},
         
        };
        
        private static int SkipComments(string str, int position)
        {
            if(str[position]!='#')
                throw new InvalidOperationException("'#' symbol expected");
            if(str.Length == position+1)
                return position;

            int index = position+2;
            for (; index < str.Length && str[index] != '\r' && str[index] != '\n' ; index++){}
            
            return index;
        }
        
        private static Tok ReadIdOrKeyword(string str, int position)
        {
            int finish = position;
            for (; finish < str.Length && (IsLetter(str[finish]) || IsDigit(str[finish])); finish++){}

            var word = str.Substring(position, finish - position);
            //is id a keyword
            if (_keywords.ContainsKey(word))
                return Tok.New(_keywords[word], word, position, finish);
            else
                return Tok.New(TokType.Id, word,position, finish);
        }

        private static bool IsLetter(char val) => val == '_' ||  (val >= 'a' && val <= 'z') || (val >= 'A' && val <= 'Z');

        private static bool IsDigit(char val) => char.IsDigit(val); // val >= '0' && val <= '9';

        private static bool IsQuote(char val) => val == '\''|| val == '\"';
        private static Tok ReadNumber(string str, int position)
        {
            int dotPostition = -1;
            bool hasTypeSpecifier = false;
            int dotCount = 0;
            int index = position;
            for (; index < str.Length; index++)
            {
                if (IsDigit(str[index]))
                    continue;

                if (hasTypeSpecifier)
                {
                    if (str[index] >= 'a' && str[index] <= 'f')
                        continue;
                    if (str[index] >= 'A' && str[index] <= 'F')
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

                if (str[index] == '_')
                    continue;

                if (!hasTypeSpecifier && str[index] == '.' && dotPostition == -1)
                {
                    dotCount++;
                    dotPostition = index;
                    continue;
                }
                break;
            }

            var type = TokType.IntNumber;
            //if dot is last then skip
            if (dotPostition == index - 1)
            {
                if (hasTypeSpecifier) type = TokType.HexOrBinaryNumber;
                else if(dotCount>0)   type = TokType.RealNumber;

                return Tok.New(type, str.Substring(position, index - position - 1), position, index - 1);
            }

            if (index < str.Length && IsLetter(str[index]))
            {
                var txtToken = ReadIdOrKeyword(str, index);
                return Tok.New(TokType.NotAToken, str.Substring(position, txtToken.Finish - position),
                    position, txtToken.Finish);
            }
            if (hasTypeSpecifier)   type = TokType.HexOrBinaryNumber;
            else if (dotCount > 0)  type = TokType.RealNumber;

            return Tok.New(type, str.Substring(position, index - position), position, index);
        }

        #endregion

        private Tok TryReadUncommonSpecialSymbols(string str, int position, char current)
        {
            char? next = position < str.Length - 1
                    ? str[position + 1]
                    : (char?)null;

            switch (current)
            {
                //interpolation is the only case with state.
                //we need to count figure brackets diff to understand
                //where does interpolation stops
                case '{':
                    {

                        if (_isInIterpolation)
                            _interpolationLayers.Peek().FigureBracketsDiff++;
                        return Tok.New(TokType.FiObr, position, position + 1);
                    }

                case '}':
                    {
                        if (_isInIterpolation)
                        {
                            if (_interpolationLayers.Peek().FigureBracketsDiff == 0)
                                return ReadText(str, position);
                            _interpolationLayers.Peek().FigureBracketsDiff--;

                        }
                        return Tok.New(TokType.FiCbr, position, position + 1);
                    }
                case ',': return Tok.New(TokType.Sep, position, position + 1);
                case '&': return Tok.New(TokType.BitAnd, position, position + 1);
                case '^': return Tok.New(TokType.BitXor, position, position + 1);
                case '|': return Tok.New(TokType.BitOr, position, position + 1);
                case '/': return Tok.New(TokType.Div, position, position + 1);
                case '+': return Tok.New(TokType.Plus, position, position + 1);
                case '%': return Tok.New(TokType.Rema, position, position + 1);
                case '(': return Tok.New(TokType.Obr, position, position + 1);
                case ')': return Tok.New(TokType.Cbr, position, position + 1);
                case '[': return Tok.New(TokType.ArrOBr, position, position + 1);
                case ']': return Tok.New(TokType.ArrCBr, position, position + 1);
                case ':': return Tok.New(TokType.Colon, position, position + 1);
                case '~': return Tok.New(TokType.BitInverse, position, position + 1);
                case '-' when next == '-':
                    return Tok.New(TokType.Attribute, position, position + 2);
                case '-' when next == '>':
                    return Tok.New(TokType.AnonymFun, position, position + 2);
                case '-':
                    return Tok.New(TokType.Minus, position, position + 1);
                case '*' when next == '*':
                    return Tok.New(TokType.Pow, position, position + 2);
                case '*':
                    return Tok.New(TokType.Mult, position, position + 1);
                case '>' when next == '=':
                    return Tok.New(TokType.MoreOrEqual, position, position + 2);
                case '>' when next == '>':
                    return Tok.New(TokType.BitShiftRight, position, position + 2);
                case '>':
                    return Tok.New(TokType.More, position, position + 1);
                case '<' when next == '=':
                    return Tok.New(TokType.LessOrEqual, position, position + 2);
                case '<' when next == '<':
                    return Tok.New(TokType.BitShiftLeft, position, position + 2);
                case '<':
                    return Tok.New(TokType.Less, position, position + 1);
                case '=' when next == '=':
                    return Tok.New(TokType.Equal, position, position + 2);
                case '=':
                    return Tok.New(TokType.Def, position, position + 1);
                case '.' when next == '.':
                    return Tok.New(TokType.TwoDots, position, position + 2);
                case '.':
                    return Tok.New(TokType.PipeForward, position, position + 1);
                case '!' when next == '=':
                    return Tok.New(TokType.NotEqual, position, position + 2);
                default:
                    return null;
            }
        }

        private bool _isInIterpolation = false;
        private Stack<InterpolationLayer> _interpolationLayers = new Stack<InterpolationLayer>();

        private Tok TryReadNext(string str, int position)
        {
            if (position >= str.Length)
                return Tok.New(TokType.Eof, position, position);

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

            if (current == 0)
                return Tok.New(TokType.Eof, "", position, position);

            if (current == '\r' || current == '\n' || current == ';')
                return Tok.New(TokType.NewLine, current.ToString(), position, position + 1);

            if (IsDigit(current))
                return ReadNumber(str, position);

            if (IsLetter(current))
                return ReadIdOrKeyword(str, position);

            if (TryReadUncommonSpecialSymbols(str, position, current) is Tok tok)
                return tok;

            if (IsQuote(current))
                return ReadText(str, position);

            return Tok.New(TokType.NotAToken, current.ToString(), position, position + 1);
        }

        /// <exception cref="FunParseException"></exception>
        private Tok ReadText(string str, int startPosition)
        {
            var openQuoteSymbol = str[startPosition];
            bool closeIntepolation = false;
            if (openQuoteSymbol == '}')
            {
                closeIntepolation = true;
                openQuoteSymbol = _interpolationLayers.Pop().OpenQuoteSymbol;
                _isInIterpolation = _interpolationLayers.Count > 0;
            }

            var expectedClosingSymbol = openQuoteSymbol;

            if (startPosition >= str.Length - 1)
                throw ErrorFactory.QuoteAtEndOfString(expectedClosingSymbol, startPosition, startPosition + 1);

            var (result, endPosition) = QuotationReader.ReadQuotation(str, startPosition);
            if (endPosition == -1)
                throw ErrorFactory.ClosingQuoteIsMissed(expectedClosingSymbol, startPosition, str.Length);


            var closeQuoteSymbol = str[endPosition];
            if (closeQuoteSymbol == '{')
            {
                _isInIterpolation = true;
                var layer = new InterpolationLayer() {FigureBracketsDiff = 0, OpenQuoteSymbol = openQuoteSymbol};
                _interpolationLayers.Push(layer);
                
                if(closeIntepolation)
                    return Tok.New(TokType.TextMidInterpolation, result, startPosition, endPosition + 1);
                else
                    return Tok.New(TokType.TextOpenInterpolation, result, startPosition, endPosition + 1);
            }
            else
            {
                if (closeQuoteSymbol != expectedClosingSymbol)
                    throw ErrorFactory.ClosingQuoteIsMissed(expectedClosingSymbol, startPosition, endPosition);
                if (closeIntepolation)
                    return Tok.New(TokType.TextCloseInterpolation, result, startPosition, endPosition + 1);
                else
                    return Tok.New(TokType.Text, result, startPosition, endPosition + 1);
            }
        }
    }
}