using System;
using System.Collections.Generic;
using NFun.Exceptions;

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

                // Implicit line continuation — matches Basics.md "line breaks are
                // ignored when reading an expression". Two trigger conditions:
                //   (1) trailing operator: previous emitted token is a binary
                //       operator → expression incomplete, next line is RHS.
                //   (2) leading operator: next line starts with a binary operator
                //       that cannot begin a statement → previous expression
                //       continues on this line.
                // Suppressed when the previous real token is `:` (block opener) —
                // a new indented block starts there, even if its first token is
                // syntactically a leading operator like unary `-`.
                var prevReal = FindPreviousRealToken(result);
                bool atBlockOpener = prevReal == TokType.Colon;
                if (!atBlockOpener
                    && (IsTrailingContinuation(result) || IsLeadingContinuationOperator(nextTok.Type))) {
                    i = nextIdx - 1;
                    continue;
                }
                var (indent, lineHasTabs, lineHasSpaces) = MeasureIndent(source, nextTok);

                // Rule 1: Tabs vs spaces — detect and enforce consistency
                if (indent > 0) {
                    if (lineHasTabs && lineHasSpaces) {
                        int line = CountLines(source, nextTok.Start);
                        throw new FunnyParseException(0,
                            $"Indentation error: mixed tabs and spaces at line {line}",
                            new Interval(nextTok.Start, nextTok.Start));
                    }
                    if (lineHasTabs || lineHasSpaces) {
                        bool thisLineUsesTabs = lineHasTabs;
                        if (usesTabs == null) {
                            usesTabs = thisLineUsesTabs;
                        } else if (usesTabs.Value != thisLineUsesTabs) {
                            int line = CountLines(source, nextTok.Start);
                            var expected = usesTabs.Value ? "tabs" : "spaces";
                            var found = thisLineUsesTabs ? "tabs" : "spaces";
                            throw new FunnyParseException(0,
                                $"Indentation error: file uses {expected}, but {found} found at line {line}",
                                new Interval(nextTok.Start, nextTok.Start));
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
                    // Verify we landed on an exact prior level.
                    //
                    // Exception: continuation keywords (else / elif / catch /
                    // anyway) that continue an enclosing if/when/try statement
                    // may legitimately appear at a column that isn't on the
                    // stack when the originating keyword sits inline after
                    // something else, e.g.:
                    //     x = try:
                    //             riskyOp()
                    //         catch:                 ← indent 4 (no stack level)
                    //             fallback()
                    // The originating `try:` was on a line whose own indent is
                    // 0 (`x = …`), so `catch:` at indent 4 falls between the
                    // stack's 0 and the body's 8. Accept it — the body after
                    // the continuation keyword re-INDENTs and resumes the
                    // normal stack discipline.
                    if (indentStack.Peek() != indent && !IsContinuationKeyword(nextTok.Type)) {
                        int line = CountLines(source, nextTok.Start);
                        throw new FunnyParseException(0,
                            $"Indentation error: unindent does not match any outer indentation level at line {line}",
                            new Interval(nextTok.Start, nextTok.Start));
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

    // Keywords that continue an enclosing if/when/try statement. They may
    // appear at any indent ≥ the originating keyword's line indent — the
    // body that follows re-establishes the stack via a normal INDENT.
    private static bool IsContinuationKeyword(TokType type) =>
        type == TokType.Else || type == TokType.Elif
        || type == TokType.Catch || type == TokType.Anyway;

    /// <summary>
    /// Operators that cannot begin a statement — a line starting with one of these
    /// must be continuation of the previous expression. Matches Basics.md spec:
    /// "When reading an expression, line breaks are ignored". Includes `+`/`-`/`*`/`/`
    /// even though they have unary forms — the same line-break-ignore rule applies
    /// in expression mode where `y = 5\n-3` parses as `y = 5-3`, not `y = 5; -3`.
    /// </summary>
    private static bool IsLeadingContinuationOperator(TokType type) =>
        // Arithmetic
        type == TokType.Plus || type == TokType.Minus
        || type == TokType.Mult || type == TokType.Div
        || type == TokType.DivInt || type == TokType.Rema || type == TokType.Pow
        // Logical
        || type == TokType.And || type == TokType.Or || type == TokType.Xor
        // Comparison
        || type == TokType.Equal || type == TokType.NotEqual
        || type == TokType.Less || type == TokType.More
        || type == TokType.LessOrEqual || type == TokType.MoreOrEqual
        // Bitwise
        || type == TokType.BitOr || type == TokType.BitAnd || type == TokType.BitXor
        || type == TokType.BitShiftLeft || type == TokType.BitShiftRight
        // Optional / coalesce / chain
        || type == TokType.NullCoalesce || type == TokType.SafeAccess
        || type == TokType.Dot
        // Misc
        || type == TokType.In
        || type == TokType.TwoDots;

    /// <summary>
    /// True iff the previous emitted token is a binary operator awaiting its RHS.
    /// In that case the next NewLine is not a statement separator — the expression
    /// continues.
    /// </summary>
    private static bool IsTrailingContinuation(List<Tok> emitted) =>
        IsBinaryTrailingOperator(FindPreviousRealToken(emitted));

    /// <summary>
    /// Find the previous real (non-NL / non-INDENT / non-DEDENT) token type in
    /// the emitted stream, or NotAToken if none.
    /// </summary>
    private static TokType FindPreviousRealToken(List<Tok> emitted) {
        for (int i = emitted.Count - 1; i >= 0; i--) {
            var t = emitted[i].Type;
            if (t == TokType.NewLine || t == TokType.Indent || t == TokType.Dedent)
                continue;
            return t;
        }
        return TokType.NotAToken;
    }

    /// <summary>
    /// Operators that can legitimately end a line WITH the RHS on the next line.
    /// Subset of binary operators — postfix-only forms (`!`, superscript) are
    /// excluded because if they appear, the expression is already complete.
    /// `Sep` (',') and openers are already handled by bracketDepth.
    /// </summary>
    private static bool IsBinaryTrailingOperator(TokType type) =>
        type == TokType.Plus || type == TokType.Minus
        || type == TokType.Mult || type == TokType.Div
        || type == TokType.DivInt || type == TokType.Rema || type == TokType.Pow
        || type == TokType.And || type == TokType.Or || type == TokType.Xor
        || type == TokType.Equal || type == TokType.NotEqual
        || type == TokType.Less || type == TokType.More
        || type == TokType.LessOrEqual || type == TokType.MoreOrEqual
        || type == TokType.BitOr || type == TokType.BitAnd || type == TokType.BitXor
        || type == TokType.BitShiftLeft || type == TokType.BitShiftRight
        || type == TokType.NullCoalesce
        || type == TokType.In
        || type == TokType.TwoDots
        || type == TokType.Def
        || type == TokType.Not
        || type == TokType.Arrow;

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
