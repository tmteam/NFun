using System;
using System.Collections.Generic;
using NFun.Exceptions;
using NFun.ParseErrors;

namespace NFun.Tokenization {

class InterpolationLayer {
    public char OpenQuoteSymbol;
    /// <summary>
    /// Difference between open and close interpolation brackets count
    /// </summary>
    public int FigureBracketsDiff;
}

/// <summary>
/// Turns input string into sequence of tokens
/// </summary>
public class Tokenizer {
    #region statics

    public static TokFlow ToFlow(string input)
        => new(ToTokens(input));

    public static IEnumerable<Tok> ToTokens(string input) {
        var reader = new Tokenizer();
        for (var i = 0;;)
        {
            var res = reader.TryReadNext(input, i);
            yield return res;
            if (res.Is(TokType.Eof))
                yield break;

            i = res.Finish;
        }
    }

    private static readonly Dictionary<string, TokType> Keywords = new() {
        { "in", TokType.In },
        { "fun", TokType.FunRule },
        { "rule", TokType.FunRule },

        { "and", TokType.And },
        { "or", TokType.Or },
        { "xor", TokType.Xor },
        { "not", TokType.Not },
        { "if", TokType.If },
        { "then", TokType.Then },
        { "else", TokType.Else },
        { "true", TokType.True },
        { "false", TokType.False },

        { "text", TokType.TextType },
        { "bool", TokType.BoolType },
        { "real", TokType.RealType },

        { "int16", TokType.Int16Type },
        { "int", TokType.Int32Type },
        { "int32", TokType.Int32Type },
        { "int64", TokType.Int64Type },

        { "byte", TokType.UInt8Type },
        { "uint8", TokType.UInt8Type },
        { "uint16", TokType.UInt16Type },
        { "uint", TokType.UInt32Type },
        { "uint32", TokType.UInt32Type },
        { "uint64", TokType.UInt64Type },

        //Reserved keywords:
        { "the", TokType.Reserved },

        { "%", TokType.Reserved },
        { "_", TokType.Reserved },

        { "async", TokType.Reserved },
        { "await", TokType.Reserved },

        { "base", TokType.Reserved },
        { "bigInt", TokType.Reserved },

        { "char", TokType.Reserved },
        { "case", TokType.Reserved },
        { "catch", TokType.Reserved },

        { "date", TokType.Reserved },
        { "decimal", TokType.Reserved },
        { "do", TokType.Reserved },

        { "error", TokType.Reserved },

        { "from", TokType.Reserved },
        { "finally", TokType.Reserved },
        { "for", TokType.Reserved },
        { "fail", TokType.Reserved },
        { "int8", TokType.Reserved },
        { "ip", TokType.Reserved },
        { "import", TokType.Reserved },
        { "int128", TokType.Reserved },

        { "let", TokType.Reserved },
        { "match", TokType.Reserved },
        { "mod", TokType.Reserved },
        { "never", TokType.Reserved },
        { "number", TokType.Reserved },
        { "num", TokType.Reserved },
        { "nil", TokType.Reserved },
        { "null", TokType.Reserved },
        { "none", TokType.Reserved },
        { "of", TokType.Reserved },
        { "optional", TokType.Reserved },
        { "output", TokType.Reserved },
        { "outputs", TokType.Reserved },
        { "oops", TokType.Reserved },
        { "pass", TokType.Reserved },
        { "rem", TokType.Reserved },
        { "return", TokType.Reserved },
        { "struct", TokType.Reserved },
        { "switch", TokType.Reserved },
        { "type", TokType.Reserved },
        { "try", TokType.Reserved },
        { "time", TokType.Reserved },
        { "throw", TokType.Reserved },
        { "to", TokType.Reserved },
        { "uint128", TokType.Reserved },

        { "var", TokType.Reserved },
        { "val", TokType.Reserved },
        { "void", TokType.Reserved },

        { "where", TokType.Reserved },
        { "while", TokType.Reserved },
        { "when", TokType.Reserved },
        { "until", TokType.Reserved },
        { "unless", TokType.Reserved }
    };

    private static int SkipComments(string str, int position) {
        if (str[position] != '#')
            throw new InvalidOperationException("'#' symbol expected");
        if (str.Length == position + 1)
            return position;

        int index = position + 2;
        for (; index < str.Length && str[index] != '\r' && str[index] != '\n'; index++)
        { }

        return index;
    }

    private static Tok ReadIdOrKeyword(string str, int position) {
        int finish = position;
        for (; finish < str.Length && (IsLetter(str[finish]) || IsDigit(str[finish])); finish++)
        { }

        var word = str.Substring(position, finish - position);
        //is it id or keyword
        if (Keywords.ContainsKey(word))
            return Tok.New(Keywords[word], word, position, finish);
        else
            return Tok.New(TokType.Id, word, position, finish);
    }

    private static bool IsLetter(char val) => val == '_' || (val >= 'a' && val <= 'z') || (val >= 'A' && val <= 'Z');

    private static bool IsDigit(char val) => char.IsDigit(val);

    public static bool IsQuote(char val) => val == '\'' || val == '\"';

    private static Tok ReadNumber(string str, int position) {
        int dotPosition = -1;
        int index = position;
        for (; index < str.Length; index++)
        {
            var symbol = str[index];
            if (IsDigit(symbol))
                continue;

            if (index == position + 1 && str[position] == '0')
            {
                if (str[index] == 'x') return ReadHexNumber(str, position);
                if (str[index] == 'b') return ReadBinNumber(str, position);
            }

            if (symbol == '_')
                continue;
            if (symbol == '.' && dotPosition == -1)
            {
                dotPosition = index;
                continue;
            }

            break;
        }

        //if dot is last then skip
        if (dotPosition == index - 1)
            return Tok.New(TokType.IntNumber, str.Substring(position, index - position - 1), position, index - 1);

        if (index < str.Length && IsLetter(str[index]))
        {
            var txtToken = ReadIdOrKeyword(str, index);
            return Tok.New(
                TokType.NotAToken, str.Substring(position, txtToken.Finish - position),
                position, txtToken.Finish);
        }

        var type = dotPosition == -1 ? TokType.IntNumber : TokType.RealNumber;
        return Tok.New(type, str.Substring(position, index - position), position, index);
    }

    private static Tok ReadHexNumber(string str, int position) {
        if (str[position] != '0' || str[position + 1] != 'x')
            throw new ArgumentException("Invalid Read hex usage");
        int index = position + 2;
        for (; index < str.Length; index++)
        {
            var symbol = str[index];
            if (IsDigit(symbol)) continue;
            if (symbol >= 'a' && symbol <= 'f') continue;
            if (symbol >= 'A' && symbol <= 'F') continue;
            if (str[index] == '_') continue;
            break;
        }

        if (index == position + 2)
        {
            var end = ReadIdOrKeyword(str, position + 2).Finish;
            return Tok.New(TokType.NotAToken, str.Substring(position, end - position), position, end);
        }

        return Tok.New(TokType.HexOrBinaryNumber, str.Substring(position, index - position), position, index);
    }

    private static Tok ReadBinNumber(string str, int position) {
        if (str[position] != '0' || str[position + 1] != 'b')
            throw new ArgumentException("Invalid Read bin usage");
        int index = position + 2;
        for (; index < str.Length; index++)
        {
            var symbol = str[index];
            if (symbol == '1' || symbol == '0' || symbol == '_')
                continue;
            break;
        }

        if (index == position + 2)
        {
            var end = ReadIdOrKeyword(str, position + 2).Finish;
            return Tok.New(TokType.NotAToken, str.Substring(position, end - position), position, end);
        }

        return Tok.New(TokType.HexOrBinaryNumber, str.Substring(position, index - position), position, index);
    }

    #endregion


    private Tok TryReadUncommonSpecialSymbols(string str, int position, char current) {
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
                if (_isInInterpolation)
                    _interpolationLayers.Peek().FigureBracketsDiff++;
                return Tok.New(TokType.FiObr, position, position + 1);
            }

            case '}':
            {
                if (_isInInterpolation)
                {
                    if (_interpolationLayers.Peek().FigureBracketsDiff == 0)
                        return ReadText(str, position);
                    _interpolationLayers.Peek().FigureBracketsDiff--;
                }

                return Tok.New(TokType.FiCbr, position, position + 1);
            }
            case ',': return Tok.New(TokType.Sep, position, position + 1);
            case '@': return Tok.New(TokType.MetaInfo, position, position + 1);
            case '&': return Tok.New(TokType.BitAnd, position, position + 1);
            case '^': return Tok.New(TokType.BitXor, position, position + 1);
            case '|': return Tok.New(TokType.BitOr, position, position + 1);
            case '/' when next == '/': return Tok.New(TokType.DivInt, position, position + 2);
            case '/': return Tok.New(TokType.Div, position, position + 1);
            case '+': return Tok.New(TokType.Plus, position, position + 1);
            case '%': return Tok.New(TokType.Rema, position, position + 1);
            case '(': return Tok.New(TokType.Obr, position, position + 1);
            case ')': return Tok.New(TokType.Cbr, position, position + 1);
            case '[': return Tok.New(TokType.ArrOBr, position, position + 1);
            case ']': return Tok.New(TokType.ArrCBr, position, position + 1);
            case ':': return Tok.New(TokType.Colon, position, position + 1);
            case '~': return Tok.New(TokType.BitInverse, position, position + 1);
            case '-' when next == '>': return Tok.New(TokType.Arrow, position, position + 2);
            case '-': return Tok.New(TokType.Minus, position, position + 1);
            case '*' when next == '*': return Tok.New(TokType.Pow, position, position + 2);
            case '*': return Tok.New(TokType.Mult, position, position + 1);
            case '>' when next == '=': return Tok.New(TokType.MoreOrEqual, position, position + 2);
            case '>' when next == '>': return Tok.New(TokType.BitShiftRight, position, position + 2);
            case '>': return Tok.New(TokType.More, position, position + 1);
            case '<' when next == '=': return Tok.New(TokType.LessOrEqual, position, position + 2);
            case '<' when next == '<': return Tok.New(TokType.BitShiftLeft, position, position + 2);
            case '<': return Tok.New(TokType.Less, position, position + 1);
            case '=' when next == '=': return Tok.New(TokType.Equal, position, position + 2);
            case '=': return Tok.New(TokType.Def, position, position + 1);
            case '.' when next == '.': return Tok.New(TokType.TwoDots, position, position + 2);
            case '.': return Tok.New(TokType.Dot, position, position + 1);
            case '!' when next == '=': return Tok.New(TokType.NotEqual, position, position + 2);
            default:
                return null;
        }
    }

    private bool _isInInterpolation = false;
    private readonly Stack<InterpolationLayer> _interpolationLayers = new();

    internal Tok TryReadNext(string str, int position) {
        char current;

        //Search for comments, spaces and or eof
        while (true)
        {
            if (position >= str.Length) return Tok.New(TokType.Eof, position, position);

            current = str[position];
            if (current == '#')
            {
                var newPosition = SkipComments(str, position);
                if (newPosition == position) newPosition++;
                position = newPosition;
            }
            else if (current == ' ' || current == '\t')
            {
                position++;
            }
            else break;
        }

        if (current == 0) return Tok.New(TokType.Eof, "", position, position);

        if (current == '\r' || current == '\n' || current == ';')
            return Tok.New(TokType.NewLine, current.ToString(), position, position + 1);

        if (IsDigit(current)) return ReadNumber(str, position);

        if (IsLetter(current)) return ReadIdOrKeyword(str, position);

        if (TryReadUncommonSpecialSymbols(str, position, current) is Tok tok) return tok;

        if (IsQuote(current)) return ReadText(str, position);

        return Tok.New(TokType.NotAToken, current.ToString(), position, position + 1);
    }

    /// <exception cref="FunnyParseException"></exception>
    private Tok ReadText(string str, int startPosition) {
        var openQuoteSymbol = str[startPosition];
        bool closeInterpolation = false;
        if (openQuoteSymbol == '}')
        {
            closeInterpolation = true;
            openQuoteSymbol = _interpolationLayers.Pop().OpenQuoteSymbol;
            _isInInterpolation = _interpolationLayers.Count > 0;
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
            _isInInterpolation = true;
            var layer = new InterpolationLayer { FigureBracketsDiff = 0, OpenQuoteSymbol = openQuoteSymbol };
            _interpolationLayers.Push(layer);

            if (closeInterpolation)
                return Tok.New(TokType.TextMidInterpolation, result, startPosition, endPosition + 1);
            else
                return Tok.New(TokType.TextOpenInterpolation, result, startPosition, endPosition + 1);
        }
        else
        {
            if (closeQuoteSymbol != expectedClosingSymbol)
                throw ErrorFactory.ClosingQuoteIsMissed(expectedClosingSymbol, startPosition, endPosition);
            if (closeInterpolation)
                return Tok.New(TokType.TextCloseInterpolation, result, startPosition, endPosition + 1);
            else
                return Tok.New(TokType.Text, result, startPosition, endPosition + 1);
        }
    }
}

}