using System;
using System.Collections.Generic;
using System.Globalization;
using NFun.Exceptions;
using NFun.ParseErrors;

namespace NFun.Tokenization;

class InterpolationLayer {
    public char OpenQuoteSymbol;
    /// <summary>
    /// Difference between open and close interpolation brackets count
    /// </summary>
    public int FigureBracketsDiff;
    public bool IsTripleQuoted;
    public string TripleQuoteBaseline;
    public int TripleQuoteClosingPosition;
    /// <summary>
    /// Number of $ signs before the opening quote. 0 = standard interpolation via {expr}.
    /// N > 0 = interpolation via $...${expr} (N dollars + brace).
    /// </summary>
    public int EscapeLevel;
}

/// <summary>
/// Turns input string into sequence of tokens
/// </summary>
public class Tokenizer {
    #region statics

    public static TokFlow ToFlow(string input, bool denyNewlineInStrings = false)
        => new(ToTokens(input, denyNewlineInStrings));

    public static IEnumerable<Tok> ToTokens(string input, bool denyNewlineInStrings = false) {
        var reader = new Tokenizer(denyNewlineInStrings);
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
        { "step", TokType.Step },
        { "in", TokType.In },
        { "rule", TokType.Rule },

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
        { "char", TokType.CharType },
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

        { "default", TokType.Default },


        //Reserved keywords:
        { "fun", TokType.Reserved },
        { "the", TokType.Reserved },
        { "_", TokType.Reserved },

        { "async", TokType.Reserved },
        { "await", TokType.Reserved },

        { "base", TokType.Reserved },
        { "bigInt", TokType.Reserved },

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
        { "none", TokType.None },
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
        while (finish < str.Length)
        {
            if (char.IsHighSurrogate(str[finish]) && finish + 1 < str.Length && char.IsLowSurrogate(str[finish + 1]))
            {
                var cp = char.ConvertToUtf32(str[finish], str[finish + 1]);
                if (!IsIdentContinue(cp))
                    break;
                finish += 2;
            }
            else if (IsLetter(str[finish]) || IsDigit(str[finish]))
            {
                finish++;
            }
            else
                break;
        }

        var word = str.Substring(position, finish - position);
        //is it id or keyword
        if (Keywords.TryGetValue(word, out var tok))
        {
            if (tok == TokType.Reserved)
            {
                if (word == "fun")
                    throw Errors.TokenIsReserved(new Interval(position, finish), word, "rule");
                else
                    throw Errors.TokenIsReserved(new Interval(position, finish), word);
            }
            return Tok.New(tok, word, position, finish);
        }
        else
            return Tok.New(TokType.Id, word, position, finish);
    }

    private static bool IsLetter(char val) =>
        val == '_' || char.IsLetter(val)
        || char.GetUnicodeCategory(val) == UnicodeCategory.OtherSymbol;

    /// <summary>
    /// Check if code point (may be above BMP) is a valid identifier start character.
    /// Covers surrogate pairs for emoji like 🎉, 🚀.
    /// </summary>
    private static bool IsIdentStart(int codePoint) {
        var cat = CharUnicodeInfo.GetUnicodeCategory(codePoint);
        return codePoint == '_'
               || cat is UnicodeCategory.UppercaseLetter
                   or UnicodeCategory.LowercaseLetter
                   or UnicodeCategory.TitlecaseLetter
                   or UnicodeCategory.ModifierLetter
                   or UnicodeCategory.OtherLetter
                   or UnicodeCategory.LetterNumber
                   or UnicodeCategory.OtherSymbol;
    }

    /// <summary>
    /// Check if code point (may be above BMP) is a valid identifier continuation character.
    /// </summary>
    private static bool IsIdentContinue(int codePoint) {
        if (IsIdentStart(codePoint))
            return true;
        var cat = CharUnicodeInfo.GetUnicodeCategory(codePoint);
        return cat is UnicodeCategory.NonSpacingMark
            or UnicodeCategory.SpacingCombiningMark
            or UnicodeCategory.DecimalDigitNumber
            or UnicodeCategory.ConnectorPunctuation;
    }

    private static bool IsDigit(char val) => char.IsDigit(val);

    private static bool IsQuote(char val) =>    val == '\''
                                                || val == '\"'
                                                || val == '‘'
                                                || val == '“'; //important  to support figure quotes

    private static Tok ReadNumberOrIp(string str, int position) {
        int lastDotPosition = -1;
        int secondDotPosition = -1;

        int dotsCount = 0;
        int index = position;

        for (; index < str.Length; index++)
        {
            var symbol = str[index];
            if (!IsDigit(symbol))
            {
                if (dotsCount == 3) // it is an ip address.
                    break;
                if (index == position + 1 && str[position] == '0')
                {
                    if (str[index] == 'x') return ReadHexNumber(str, position);
                    if (str[index] == 'b') return ReadBinNumber(str, position);
                }

                if (symbol == '.')
                {
                    if (dotsCount == 1)
                        secondDotPosition = index;

                    if (lastDotPosition == index - 1) //if two dots in a row
                        break;
                    lastDotPosition = index;
                    dotsCount++;
                }
                else if (symbol != '_')
                    break;
            }
        }

        //if dot is last then skip last dot
        if (lastDotPosition == index - 1)
        {
            index--;
            dotsCount--;
        }

        // Scientific notation: e.g. 1e10, 2.5e-3, 1E+10
        if (index < str.Length && dotsCount <= 1 && (str[index] == 'e' || str[index] == 'E'))
        {
            var ePos = index;
            index++; // skip 'e'/'E'
            if (index < str.Length && (str[index] == '+' || str[index] == '-'))
                index++; // skip sign
            var digitStart = index;
            bool hasDigit = false;
            while (index < str.Length && (IsDigit(str[index]) || str[index] == '_'))
            {
                if (IsDigit(str[index])) hasDigit = true;
                index++;
            }
            if (hasDigit) // at least one real digit after e[+/-]
                return Tok.SubString(str, TokType.RealNumber, position, index);
            // No digits after e → rollback, treat e as start of next token
            index = ePos;
        }

        return dotsCount switch {
                   0 => Tok.SubString(str, TokType.IntNumber, position, index),
                   1 => Tok.SubString(str, TokType.RealNumber, position, index),
                   2 => Tok.SubString(str, TokType.RealNumber, position, secondDotPosition),
                   3 => Tok.SubString(str, TokType.IpAddress, position, index),
                   _ => throw  new InvalidOperationException("fuu dot")
               };
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

            if (IsInvalidHexBinEnding(symbol, str, index, position, out var invalidToken))
                return invalidToken;
            break;
        }

        if (index - position == 2)
            return Tok.SubString(str, TokType.NotAToken, position, index);

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

            if (IsInvalidHexBinEnding(symbol, str, index, position, out var invalidToken))
                return invalidToken;
            break;
        }

        if (index - position == 2)
            return Tok.SubString(str, TokType.NotAToken, position, index);

        return Tok.SubString(str,TokType.HexOrBinaryNumber, position, index);
    }

    private static bool IsInvalidHexBinEnding(char symbol, string str, int index, int position, out Tok o) {
        if (IsLetter(symbol))
        {
            var end = ReadIdOrKeyword(str, index).Finish;
            o = Tok.SubString(str, TokType.NotAToken, position, end);
            return true;
        }
        else if (IsDigit(symbol))
        {
            var end = ReadNumberOrIp(str, index).Finish;
            o = Tok.SubString(str, TokType.NotAToken, position, end);
            return true;
        }

        o = default;
        return false;
    }


    #endregion


    private static int TryGetSuperscriptDigit(char c) => c switch {
        '²' => 2, '³' => 3, '⁴' => 4, '⁵' => 5,
        '⁶' => 6, '⁷' => 7, '⁸' => 8, '⁹' => 9,
        _ => -1
    };

    private Tok TryReadUncommonSpecialSymbols(string str, int position, char current) {
        // Superscript digits ²³⁴⁵⁶⁷⁸⁹ → single-digit postfix power
        if (TryGetSuperscriptDigit(current) >= 0) {
            if (position + 1 < str.Length && TryGetSuperscriptDigit(str[position + 1]) >= 0)
                throw Errors.ConsecutiveSuperscripts(position);
            return Tok.New(TokType.Superscript, TryGetSuperscriptDigit(current).ToString(), position, position + 1);
        }

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
                    if (_interpolationLayers.Peek().FigureBracketsDiff == 0) {
                        if (_interpolationLayers.Peek().IsTripleQuoted)
                            return ReadTripleQuotedText(str, position);
                        return ReadText(str, position);
                    }
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
            case '/' when next == '\'': return ReadCharLiteral(str, position);
            case '/': return Tok.New(TokType.Div, position, position + 1);
            case '+': return Tok.New(TokType.Plus, position, position + 1);
            case '%': return Tok.New(TokType.Rema, position, position + 1);
            case '(': return Tok.New(TokType.ParenthObr, position, position + 1);
            case ')': return Tok.New(TokType.ParenthCbr, position, position + 1);
            case '[': return Tok.New(TokType.ArrOBr, position, position + 1);
            case ']': return Tok.New(TokType.ArrCBr, position, position + 1);
            case ':': return Tok.New(TokType.Colon, position, position + 1);
            case '~': return Tok.New(TokType.BitInverse, position, position + 1);
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
            case '!': return Tok.New(TokType.ForceUnwrap, position, position + 1);
            case '?' when next == '?': return Tok.New(TokType.NullCoalesce, position, position + 2);
            case '?' when next == '.': return Tok.New(TokType.SafeAccess, position, position + 2);
            case '?': return Tok.New(TokType.Question, position, position + 1);
            case '∞': return Tok.New(TokType.Id, "∞", position, position + 1);
            case '≤': return Tok.New(TokType.LessOrEqual, position, position + 1);
            case '≥': return Tok.New(TokType.MoreOrEqual, position, position + 1);
            case '≠': return Tok.New(TokType.NotEqual, position, position + 1);
            default:
                return null;
        }
    }

    private readonly bool _denyNewlineInStrings;
    private bool _isInInterpolation = false;
    private readonly Stack<InterpolationLayer> _interpolationLayers = new();

    internal Tokenizer(bool denyNewlineInStrings = false) {
        _denyNewlineInStrings = denyNewlineInStrings;
    }

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

        if (IsDigit(current)) return ReadNumberOrIp(str, position);

        if (IsLetter(current)) return ReadIdOrKeyword(str, position);

        // Surrogate pair — check if it's an identifier start (emoji above BMP)
        if (char.IsHighSurrogate(current) && position + 1 < str.Length && char.IsLowSurrogate(str[position + 1]))
        {
            var cp = char.ConvertToUtf32(current, str[position + 1]);
            if (IsIdentStart(cp))
                return ReadIdOrKeyword(str, position);
        }

        if (current == '$') {
            var dollarCount = 1;
            var j = position + 1;
            while (j < str.Length && str[j] == '$') { dollarCount++; j++; }
            if (j < str.Length && IsQuote(str[j])) {
                if (IsTripleQuote(str, j))
                    return ReadTripleQuotedText(str, position);
                return ReadText(str, position);
            }
        }

        if (TryReadUncommonSpecialSymbols(str, position, current) is Tok tok) return tok;

        if (IsQuote(current)) {
            if (IsTripleQuote(str, position))
                return ReadTripleQuotedText(str, position);
            return ReadText(str, position);
        }

        return Tok.New(TokType.NotAToken, current.ToString(), position, position + 1);
    }

    /// <summary>
    /// Reads char literal: /'x' or /'\n' etc.
    /// Position points to '/' character.
    /// </summary>
    private static Tok ReadCharLiteral(string str, int position) {
        // position is at '/', position+1 is at '\''
        var start = position;
        var quotePos = position + 1;

        if (quotePos >= str.Length - 1)
            throw Errors.UnclosedCharLiteral(start, str.Length);

        var i = quotePos + 1; // first char after opening quote
        char value;

        if (i >= str.Length)
            throw Errors.UnclosedCharLiteral(start, str.Length);

        var current = str[i];

        if (current == '\'')
            throw Errors.EmptyCharLiteral(start, i + 1);

        if (current == '\\')
        {
            // escape sequence
            if (i + 1 >= str.Length)
                throw Errors.BackslashAtEndOfText(i, i + 1);

            var next = str[i + 1];
            value = next switch {
                '\\' => '\\',
                'n'  => '\n',
                'r'  => '\r',
                '\'' => '\'',
                '"'  => '"',
                't'  => '\t',
                _    => throw Errors.UnknownEscapeSequence(next.ToString(), i, i + 2)
            };
            i += 2; // skip both backslash and escape char
        }
        else
        {
            value = current;
            i++;
        }

        // Now i should point to closing quote
        if (i >= str.Length || str[i] != '\'')
        {
            // Check if there are more characters before closing quote
            if (i < str.Length && str[i] != '\'')
            {
                // Find the closing quote to report better error
                var end = i;
                while (end < str.Length && str[end] != '\'')
                    end++;
                if (end < str.Length)
                    end++; // include closing quote
                throw Errors.CharLiteralTooLong(start, end);
            }
            throw Errors.UnclosedCharLiteral(start, str.Length);
        }

        var finish = i + 1; // past closing quote
        return Tok.New(TokType.CharLiteral, value.ToString(), start, finish);
    }

    private static bool IsTripleQuote(string str, int position) =>
        position + 2 < str.Length
        && str[position + 1] == str[position]
        && str[position + 2] == str[position];

    /// <exception cref="FunnyParseException"></exception>
    private Tok ReadTripleQuotedText(string str, int startPosition) {
        char quoteChar;
        string baseline;
        int closingPosition;
        bool closeInterpolation = false;
        int contentStart;
        int escapeLevel = 0;

        if (str[startPosition] == '}') {
            // Resuming after interpolation
            closeInterpolation = true;
            var layer = _interpolationLayers.Pop();
            quoteChar = layer.OpenQuoteSymbol;
            baseline = layer.TripleQuoteBaseline;
            closingPosition = layer.TripleQuoteClosingPosition;
            escapeLevel = layer.EscapeLevel;
            _isInInterpolation = _interpolationLayers.Count > 0;
            contentStart = startPosition + 1; // after '}'
        } else {
            // First call — skip dollar prefix if present
            var pos = startPosition;
            while (pos < str.Length && str[pos] == '$') { escapeLevel++; pos++; }

            // Opening '''
            quoteChar = str[pos];
            var tripleEnd = pos + 3;

            // Verify newline after '''
            if (tripleEnd >= str.Length)
                throw Errors.TripleQuotedStringNotClosed(quoteChar, startPosition, str.Length);

            if (str[tripleEnd] != '\n' && str[tripleEnd] != '\r')
                throw Errors.NewlineRequiredAfterTripleQuote(startPosition, tripleEnd + 1);

            // Skip the newline after opening '''
            contentStart = tripleEnd + 1;
            if (str[tripleEnd] == '\r' && contentStart < str.Length && str[contentStart] == '\n')
                contentStart++;

            // Pre-scan for closing ''' and baseline
            (closingPosition, baseline) = QuotationReader.FindTripleQuoteClosing(
                str, contentStart, quoteChar, startPosition, escapeLevel);

            // Skip baseline on the first content line
            if (baseline.Length > 0 && contentStart < closingPosition)
                contentStart = SkipFirstLineBaseline(str, contentStart, baseline, closingPosition);
        }

        // Read content with trim margin
        var (result, endPosition) = QuotationReader.ReadTripleQuotation(
            str, contentStart, quoteChar, baseline, closingPosition, escapeLevel);

        if (endPosition == closingPosition) {
            // Reached closing '''
            var tokenFinish = closingPosition + 3; // past '''
            if (closeInterpolation)
                return Tok.New(TokType.TextCloseInterpolation, result, startPosition, tokenFinish);
            return Tok.New(TokType.Text, result, startPosition, tokenFinish);
        }

        if (str[endPosition] == '{') {
            // Entering interpolation
            _isInInterpolation = true;
            var layer = new InterpolationLayer {
                FigureBracketsDiff = 0,
                OpenQuoteSymbol = quoteChar,
                IsTripleQuoted = true,
                TripleQuoteBaseline = baseline,
                TripleQuoteClosingPosition = closingPosition,
                EscapeLevel = escapeLevel
            };
            _interpolationLayers.Push(layer);

            if (closeInterpolation)
                return Tok.New(TokType.TextMidInterpolation, result, startPosition, endPosition + 1);
            return Tok.New(TokType.TextOpenInterpolation, result, startPosition, endPosition + 1);
        }

        // Should not happen
        throw Errors.TripleQuotedStringNotClosed(quoteChar, startPosition, str.Length);
    }

    /// <summary>
    /// Skip baseline indentation on the first content line of a triple-quoted string.
    /// </summary>
    private static int SkipFirstLineBaseline(string str, int contentStart, string baseline, int closingPosition) {
        // Check if first line is blank (only whitespace before newline/closing)
        var scanEnd = contentStart;
        while (scanEnd < str.Length && str[scanEnd] != '\n' && str[scanEnd] != '\r')
            scanEnd++;

        // If first line is the closing line, no baseline to skip
        if (contentStart >= closingPosition)
            return contentStart;

        var lineContent = str.Substring(contentStart, Math.Min(scanEnd, closingPosition) - contentStart);
        if (string.IsNullOrWhiteSpace(lineContent))
            return scanEnd < closingPosition ? scanEnd : closingPosition;

        // Verify and skip baseline
        for (var j = 0; j < baseline.Length; j++) {
            if (contentStart + j >= str.Length || contentStart + j >= closingPosition)
                throw Errors.InsufficientIndentation(contentStart, contentStart + j);
            var actual = str[contentStart + j];
            var expected = baseline[j];
            if (actual != expected) {
                if ((actual == ' ' || actual == '\t') && (expected == ' ' || expected == '\t'))
                    throw Errors.MixedIndentation(contentStart, contentStart + j + 1);
                throw Errors.InsufficientIndentation(contentStart, contentStart + j + 1);
            }
        }
        return contentStart + baseline.Length;
    }

    /// <exception cref="FunnyParseException"></exception>
    private Tok ReadText(string str, int startPosition) {
        char openQuoteSymbol;
        bool closeInterpolation = false;
        int escapeLevel = 0;
        int quotePosition;

        if (str[startPosition] == '}') {
            closeInterpolation = true;
            var layer = _interpolationLayers.Pop();
            openQuoteSymbol = layer.OpenQuoteSymbol;
            escapeLevel = layer.EscapeLevel;
            quotePosition = startPosition;
            _isInInterpolation = _interpolationLayers.Count > 0;
        } else if (str[startPosition] == '$') {
            // Dollar-prefixed string: count $ signs, then find quote
            var i = startPosition;
            while (i < str.Length && str[i] == '$') { escapeLevel++; i++; }
            openQuoteSymbol = str[i];
            quotePosition = i;
        } else {
            openQuoteSymbol = str[startPosition];
            quotePosition = startPosition;
        }

        var expectedClosingSymbol = openQuoteSymbol;

        if (quotePosition >= str.Length - 1)
            throw Errors.QuoteAtEndOfString(expectedClosingSymbol, startPosition, startPosition + 1);

        var (result, endPosition) = QuotationReader.ReadQuotation(
            str, quotePosition, openQuoteSymbol, _denyNewlineInStrings, escapeLevel);
        if (endPosition == -1)
            throw Errors.ClosingQuoteIsMissed(expectedClosingSymbol, startPosition, str.Length);

        var closeQuoteSymbol = str[endPosition];
        if (closeQuoteSymbol == '{')
        {
            _isInInterpolation = true;
            var layer = new InterpolationLayer {
                FigureBracketsDiff = 0,
                OpenQuoteSymbol = openQuoteSymbol,
                EscapeLevel = escapeLevel
            };
            _interpolationLayers.Push(layer);

            if (closeInterpolation)
                return Tok.New(TokType.TextMidInterpolation, result, startPosition, endPosition + 1);
            else
                return Tok.New(TokType.TextOpenInterpolation, result, startPosition, endPosition + 1);
        }
        else
        {
            if (closeQuoteSymbol != expectedClosingSymbol)
                throw Errors.ClosingQuoteIsMissed(expectedClosingSymbol, startPosition, endPosition);
            if (closeInterpolation)
                return Tok.New(TokType.TextCloseInterpolation, result, startPosition, endPosition + 1);
            else
                return Tok.New(TokType.Text, result, startPosition, endPosition + 1);
        }
    }
}
