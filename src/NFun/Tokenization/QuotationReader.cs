using System.Text;
using NFun.ParseErrors;

namespace NFun.Tokenization; 

public static class QuotationReader {
    /// <summary>
    /// Convert escaped string until ' or  "  or { symbols
    /// </summary>
    /// <returns>result: escaped string, resultPosition: index of closing quote symbol. -1 if no close quote symbol found</returns>
    public static (string result, int resultPosition) ReadQuotation(
        string rawString, int startPosition, char? quoteSymbol,
        bool denyNewlineInStrings = false, int escapeLevel = 0) {
        var sb = new StringBuilder();
        int lastNonEscaped = startPosition + 1;

        int i = lastNonEscaped;
        var closeQuotationPosition = 0;
        for (; i < rawString.Length; i++)
        {
            var current = rawString[i];
            if (denyNewlineInStrings && (current == '\n' || current == '\r'))
                throw Errors.NewlineInString(startPosition, i + 1);
            if (current == quoteSymbol)
            {
                closeQuotationPosition = i;
                break;
            }
            if (current == '{' && escapeLevel == 0)
            {
                closeQuotationPosition = i;
                break;
            }

            // Dollar-prefix interpolation: N consecutive $ + { triggers interpolation
            if (current == '$' && escapeLevel > 0) {
                var dollarCount = 1;
                var j = i + 1;
                while (j < rawString.Length && rawString[j] == '$') { dollarCount++; j++; }
                if (j < rawString.Length && rawString[j] == '{' && dollarCount >= escapeLevel) {
                    // Flush content before dollar run
                    if (lastNonEscaped < i)
                        sb.Append(rawString.Substring(lastNonEscaped, i - lastNonEscaped));
                    // Emit excess dollars as literal
                    for (var k = 0; k < dollarCount - escapeLevel; k++)
                        sb.Append('$');
                    closeQuotationPosition = j; // position of '{'
                    i = j;
                    lastNonEscaped = j; // prevent stale flush after loop
                    break;
                }
                // Not a trigger — all $ are literal, continue scanning normally
            }

            if (rawString[i] != '\\')
                continue;

            if (lastNonEscaped != i)
            {
                var prev = rawString.Substring(lastNonEscaped, i - lastNonEscaped);
                sb.Append(prev);
            }

            if (i == rawString.Length - 1)
                throw Errors.BackslashAtEndOfText(i, i + 1);

            var next = rawString[i + 1];
            var symbol = next switch {
                             '\\' => '\\',
                             'n'  => '\n',
                             'r'  => '\r',
                             '\'' => '\'',
                             '"'  => '"',
                             't'  => '\t',
                             '{'  => '{',
                             '}'  => '}',
                             '$'  => '$',
                             _    => throw Errors.UnknownEscapeSequence(next.ToString(), i, i + 2)
                         };
            sb.Append(symbol);
            i++;
            lastNonEscaped = i + 1;
        }

        if (closeQuotationPosition == 0)
            return ("", -1);

        if (lastNonEscaped == startPosition + 1)
            return (rawString.Substring(startPosition + 1, i - startPosition - 1), i);

        if (lastNonEscaped <= rawString.Length - 1)
        {
            var prev = rawString.Substring(lastNonEscaped, i - lastNonEscaped);
            sb.Append(prev);
        }

        return (sb.ToString(), closeQuotationPosition);
    }

    /// <summary>
    /// Pre-scan to find the closing triple-quote position and determine baseline indentation.
    /// Tracks brace depth to skip interpolation content.
    /// </summary>
    /// <returns>(closingPosition: index of first quote char of closing triple-quote, baseline: whitespace prefix)</returns>
    public static (int closingPosition, string baseline) FindTripleQuoteClosing(
        string rawString, int contentStart, char quoteChar, int openingPosition,
        int escapeLevel = 0) {
        // Check if the very first line is the closing triple-quote (empty content)
        {
            var wsEnd = contentStart;
            while (wsEnd < rawString.Length && (rawString[wsEnd] == ' ' || rawString[wsEnd] == '\t'))
                wsEnd++;
            if (wsEnd + 2 < rawString.Length
                && rawString[wsEnd] == quoteChar
                && rawString[wsEnd + 1] == quoteChar
                && rawString[wsEnd + 2] == quoteChar
                && IsValidAfterClosingTripleQuote(rawString, wsEnd + 3)) {
                var baseline = rawString.Substring(contentStart, wsEnd - contentStart);
                return (wsEnd, baseline);
            }
        }

        var braceDepth = 0;
        var i = contentStart;
        while (i < rawString.Length) {
            var c = rawString[i];
            if (c == '\\' && i + 1 < rawString.Length) {
                i += 2; // skip escape sequence
                continue;
            }

            // Brace depth tracking for interpolation
            if (escapeLevel == 0) {
                if (c == '{' && braceDepth == 0) { braceDepth = 1; i++; continue; }
            } else {
                // Dollar-prefix: N $ + { enters interpolation at depth 0
                if (c == '$' && braceDepth == 0) {
                    var dollarCount = 1;
                    var j = i + 1;
                    while (j < rawString.Length && rawString[j] == '$') { dollarCount++; j++; }
                    if (j < rawString.Length && rawString[j] == '{' && dollarCount >= escapeLevel) {
                        braceDepth = 1;
                        i = j + 1; // skip past '{'
                        continue;
                    }
                    // Not a trigger — $ is literal, continue
                }
            }
            if (c == '{' && braceDepth > 0) { braceDepth++; i++; continue; }
            if (c == '}' && braceDepth > 0) { braceDepth--; i++; continue; }
            if (braceDepth > 0) { i++; continue; }

            // At depth 0, check for line start → whitespace → '''
            if (c == '\n' || c == '\r') {
                // Skip line ending
                var lineStart = i + 1;
                if (c == '\r' && lineStart < rawString.Length && rawString[lineStart] == '\n')
                    lineStart++;

                // Scan whitespace
                var wsEnd = lineStart;
                while (wsEnd < rawString.Length && (rawString[wsEnd] == ' ' || rawString[wsEnd] == '\t'))
                    wsEnd++;

                // Check for triple-quote
                if (wsEnd + 2 < rawString.Length
                    && rawString[wsEnd] == quoteChar
                    && rawString[wsEnd + 1] == quoteChar
                    && rawString[wsEnd + 2] == quoteChar
                    && IsValidAfterClosingTripleQuote(rawString, wsEnd + 3)) {
                    var baseline = rawString.Substring(lineStart, wsEnd - lineStart);
                    return (wsEnd, baseline);
                }
                i = lineStart;
                continue;
            }
            i++;
        }
        throw Errors.TripleQuotedStringNotClosed(quoteChar, openingPosition, rawString.Length);
    }

    /// <summary>
    /// Check that what follows the closing triple-quote is valid (EOF, newline, separator, operator, etc.)
    /// </summary>
    private static bool IsValidAfterClosingTripleQuote(string rawString, int afterClose) {
        while (afterClose < rawString.Length && rawString[afterClose] == ' ')
            afterClose++;
        return afterClose >= rawString.Length
               || rawString[afterClose] == '\n'
               || rawString[afterClose] == '\r'
               || rawString[afterClose] == ';'
               || rawString[afterClose] == '#'
               || rawString[afterClose] == ')'
               || rawString[afterClose] == ']'
               || rawString[afterClose] == '['
               || rawString[afterClose] == '.'
               || rawString[afterClose] == ','
               || rawString[afterClose] == '='
               || rawString[afterClose] == '!'
               || rawString[afterClose] == '+'
               || rawString[afterClose] == '-'
               || rawString[afterClose] == '*'
               || rawString[afterClose] == '/';
    }

    /// <summary>
    /// Read content of a triple-quoted string, processing escape sequences and trimming baseline indentation.
    /// Stops at '{' (for interpolation) or at the closing triple-quote.
    /// </summary>
    /// <param name="rawString">The full source string</param>
    /// <param name="contentStart">Position of first content character (after skipping baseline on current line)</param>
    /// <param name="quoteChar">The quote character (' or ")</param>
    /// <param name="baseline">Whitespace prefix to strip from each new line</param>
    /// <param name="closingPosition">Position of the closing triple-quote (from pre-scan)</param>
    /// <returns>(result: processed string content, resultPosition: position of '{' or closing triple-quote)</returns>
    public static (string result, int resultPosition) ReadTripleQuotation(
        string rawString, int contentStart, char quoteChar, string baseline, int closingPosition,
        int escapeLevel = 0) {
        // Empty content — contentStart is already at closing triple-quote
        if (contentStart >= closingPosition)
            return ("", closingPosition);

        var sb = new StringBuilder();
        var i = contentStart;
        int lastNonEscaped = contentStart;

        while (i < rawString.Length) {
            var current = rawString[i];

            // Check for interpolation
            if (current == '{' && escapeLevel == 0) {
                // Flush pending content
                if (lastNonEscaped < i)
                    sb.Append(rawString.Substring(lastNonEscaped, i - lastNonEscaped));
                return (sb.ToString(), i);
            }

            // Dollar-prefix interpolation: N consecutive $ + { triggers interpolation
            if (current == '$' && escapeLevel > 0) {
                var dollarCount = 1;
                var j = i + 1;
                while (j < rawString.Length && rawString[j] == '$') { dollarCount++; j++; }
                if (j < rawString.Length && rawString[j] == '{' && dollarCount >= escapeLevel) {
                    // Flush content before dollar run
                    if (lastNonEscaped < i)
                        sb.Append(rawString.Substring(lastNonEscaped, i - lastNonEscaped));
                    // Emit excess dollars as literal
                    for (var k = 0; k < dollarCount - escapeLevel; k++)
                        sb.Append('$');
                    return (sb.ToString(), j); // j = position of '{'
                }
                // Not a trigger — all $ are literal, continue scanning normally
            }

            // Check for newline — need to handle trim and check for closing
            if (current == '\n' || current == '\r') {
                // Flush content before newline
                if (lastNonEscaped < i)
                    sb.Append(rawString.Substring(lastNonEscaped, i - lastNonEscaped));

                var lineEndStart = i;
                var nextLineStart = i + 1;
                if (current == '\r' && nextLineStart < rawString.Length && rawString[nextLineStart] == '\n')
                    nextLineStart++;

                // Is the next line the closing triple-quote?
                if (nextLineStart <= closingPosition && IsClosingLine(rawString, nextLineStart, closingPosition)) {
                    // Don't include the trailing newline — it's part of the closing delimiter
                    return (sb.ToString(), closingPosition);
                }

                // It's a content newline — add \n
                sb.Append('\n');

                // Skip baseline indentation on the new line
                var afterBaseline = SkipAndVerifyBaseline(rawString, nextLineStart, baseline, lineEndStart);
                i = afterBaseline;
                lastNonEscaped = afterBaseline;
                continue;
            }

            // Handle escape sequences
            if (current == '\\') {
                if (lastNonEscaped < i)
                    sb.Append(rawString.Substring(lastNonEscaped, i - lastNonEscaped));

                if (i + 1 >= rawString.Length)
                    throw Errors.BackslashAtEndOfText(i, i + 1);

                var next = rawString[i + 1];
                var symbol = next switch {
                    '\\' => '\\',
                    'n'  => '\n',
                    'r'  => '\r',
                    '\'' => '\'',
                    '"'  => '"',
                    't'  => '\t',
                    '{'  => '{',
                    '}'  => '}',
                    '$'  => '$',
                    _    => throw Errors.UnknownEscapeSequence(next.ToString(), i, i + 2)
                };
                sb.Append(symbol);
                i += 2;
                lastNonEscaped = i;
                continue;
            }

            i++;
        }

        // Should not reach here — pre-scan guarantees closing exists
        throw Errors.TripleQuotedStringNotClosed(quoteChar, contentStart, rawString.Length);
    }

    /// <summary>
    /// Check if a line starting at lineStart is the closing triple-quote line at closingPosition.
    /// </summary>
    private static bool IsClosingLine(string rawString, int lineStart, int closingPosition) {
        // The closing line starts at lineStart with whitespace up to closingPosition
        for (var j = lineStart; j < closingPosition; j++) {
            if (rawString[j] != ' ' && rawString[j] != '\t')
                return false;
        }
        return lineStart <= closingPosition;
    }

    /// <summary>
    /// Skip baseline indentation at the start of a content line. Verifies that the line
    /// has at least the baseline indentation. Blank lines are exempted.
    /// </summary>
    /// <returns>Position after the baseline prefix has been skipped.</returns>
    private static int SkipAndVerifyBaseline(string rawString, int lineStart, string baseline, int errorPos) {
        if (baseline.Length == 0)
            return lineStart;

        // Check if this is a blank line (empty or whitespace-only before next newline/EOF)
        var scanEnd = lineStart;
        while (scanEnd < rawString.Length && rawString[scanEnd] != '\n' && rawString[scanEnd] != '\r')
            scanEnd++;
        var lineContent = rawString.Substring(lineStart, scanEnd - lineStart);
        if (string.IsNullOrWhiteSpace(lineContent))
            return scanEnd; // blank line — skip all whitespace, will just produce empty

        // Non-blank line — must have at least baseline indentation
        for (var j = 0; j < baseline.Length; j++) {
            if (lineStart + j >= rawString.Length)
                throw Errors.InsufficientIndentation(errorPos, lineStart + j);

            var actual = rawString[lineStart + j];
            var expected = baseline[j];

            if (actual != expected) {
                // Mixed tabs and spaces?
                if ((actual == ' ' || actual == '\t') && (expected == ' ' || expected == '\t'))
                    throw Errors.MixedIndentation(errorPos, lineStart + j + 1);
                // Less indentation
                throw Errors.InsufficientIndentation(errorPos, lineStart + j + 1);
            }
        }

        return lineStart + baseline.Length;
    }
}