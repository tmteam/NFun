using System;
using System.Collections.Generic;

namespace NFun.Tokenization;

/// <summary>
/// Post-processing pass that takes raw tokens from the existing Tokenizer
/// and inserts INDENT/DEDENT tokens for indent-based block structure.
///
/// Implements NFun Indent Rules Specification (Specs/IndentRules.md):
/// - Tabs OR spaces per file, never both (mixed → error)
/// - Any indent size, consistent within block
/// - Inconsistent dedent → error
/// - Empty lines and comment-only lines → ignored
/// - Multiple dedent at once → OK
/// - Trailing whitespace → ignored
/// - Lines inside unclosed brackets → indent ignored (free-form)
/// </summary>
internal static class IndentTokenizer {

    public static Tok[] Process(Tok[] rawTokens, string source) {
        var result = new List<Tok>(rawTokens.Length + 16);
        var indentStack = new Stack<int>();
        indentStack.Push(0);

        // Track whether file uses tabs or spaces (null = not yet determined)
        bool? usesTabs = null;
        // Track bracket depth — inside brackets, indent is ignored
        int bracketDepth = 0;

        for (int i = 0; i < rawTokens.Length; i++) {
            var tok = rawTokens[i];

            // Track bracket depth for free-form inside (), [], {}
            if (tok.Type == TokType.ParenthObr || tok.Type == TokType.ArrOBr || tok.Type == TokType.FiObr)
                bracketDepth++;
            else if (tok.Type == TokType.ParenthCbr || tok.Type == TokType.ArrCBr || tok.Type == TokType.FiCbr)
                bracketDepth = Math.Max(0, bracketDepth - 1);

            if (tok.Type == TokType.NewLine) {
                // Inside brackets — indent is free-form, just pass NewLine through
                if (bracketDepth > 0) {
                    result.Add(tok);
                    continue;
                }

                // Skip blank lines: find next non-newline token
                var nextIdx = FindNextNonNewline(rawTokens, i + 1);
                if (nextIdx == -1 || rawTokens[nextIdx].Type == TokType.Eof) {
                    i = nextIdx == -1 ? rawTokens.Length - 1 : nextIdx - 1;
                    continue;
                }

                var nextTok = rawTokens[nextIdx];
                var (indent, lineHasTabs, lineHasSpaces) = MeasureIndent(source, nextTok);

                // Rule 1: Tabs vs spaces — detect and enforce consistency
                if (indent > 0) {
                    if (lineHasTabs && lineHasSpaces) {
                        int line = CountLines(source, nextTok.Start);
                        throw new InvalidOperationException(
                            $"Indentation error: mixed tabs and spaces at line {line}");
                    }
                    if (lineHasTabs || lineHasSpaces) {
                        bool thisLineUsesTabs = lineHasTabs;
                        if (usesTabs == null) {
                            usesTabs = thisLineUsesTabs;
                        } else if (usesTabs.Value != thisLineUsesTabs) {
                            int line = CountLines(source, nextTok.Start);
                            var expected = usesTabs.Value ? "tabs" : "spaces";
                            var found = thisLineUsesTabs ? "tabs" : "spaces";
                            throw new InvalidOperationException(
                                $"Indentation error: file uses {expected}, but {found} found at line {line}");
                        }
                    }
                }

                var currentIndent = indentStack.Peek();

                if (indent > currentIndent) {
                    result.Add(tok); // NewLine
                    indentStack.Push(indent);
                    result.Add(Tok.New(TokType.Indent, nextTok.Start, nextTok.Start));
                }
                else if (indent < currentIndent) {
                    // Rule 3: Inconsistent dedent → error
                    // Emit DEDENTs for each level we're leaving
                    while (indentStack.Count > 1 && indentStack.Peek() > indent) {
                        indentStack.Pop();
                        result.Add(Tok.New(TokType.Dedent, nextTok.Start, nextTok.Start));
                    }
                    // Verify we landed on an exact prior level
                    if (indentStack.Peek() != indent) {
                        int line = CountLines(source, nextTok.Start);
                        throw new InvalidOperationException(
                            $"Indentation error: unindent does not match any outer indentation level at line {line}");
                    }
                    result.Add(tok); // NewLine after dedents
                }
                else {
                    // Same level — statement separator
                    result.Add(tok);
                }

                // Skip intermediate newlines (already found next non-newline)
                i = nextIdx - 1;
            }
            else if (tok.Type == TokType.Eof) {
                // Emit remaining DEDENTs before EOF
                while (indentStack.Count > 1) {
                    indentStack.Pop();
                    result.Add(Tok.New(TokType.Dedent, tok.Start, tok.Start));
                }
                result.Add(tok);
            }
            else {
                result.Add(tok);
            }
        }

        return result.ToArray();
    }

    private static int FindNextNonNewline(Tok[] tokens, int startIndex) {
        for (int i = startIndex; i < tokens.Length; i++) {
            if (tokens[i].Type != TokType.NewLine)
                return i;
        }
        return -1;
    }

    /// <summary>
    /// Measure indentation: count whitespace from line start to token position.
    /// Returns (indent level, has tabs, has spaces).
    /// Tab = 1 unit, Space = 1 unit. They are NOT mixed (Rule 1 catches mixing).
    /// </summary>
    private static (int indent, bool hasTabs, bool hasSpaces) MeasureIndent(string source, Tok token) {
        var pos = token.Start;
        // Walk backwards to find start of line
        var lineStart = pos;
        while (lineStart > 0 && source[lineStart - 1] != '\n' && source[lineStart - 1] != '\r')
            lineStart--;

        int indent = 0;
        bool hasTabs = false, hasSpaces = false;
        for (int i = lineStart; i < pos; i++) {
            if (source[i] == ' ') { indent++; hasSpaces = true; }
            else if (source[i] == '\t') { indent++; hasTabs = true; }
            else break;
        }
        return (indent, hasTabs, hasSpaces);
    }

    /// <summary>Count line number (1-based) for error messages.</summary>
    private static int CountLines(string source, int position) {
        int line = 1;
        for (int i = 0; i < position && i < source.Length; i++)
            if (source[i] == '\n') line++;
        return line;
    }
}
