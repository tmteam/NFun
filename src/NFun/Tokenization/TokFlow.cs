using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NFun.Tokenization;

public class TokFlow {
    private static readonly Tok PreviousBeforeFlowTok = Tok.New(TokType.NotAToken, 0, 0);
    private static readonly Tok CurrentAfterEofFlowTok = Tok.New(TokType.Eof, 0, 0);

    private readonly Tok[] _tokens;

    public TokFlow(Tok[] tokens) => _tokens = tokens;

    public TokFlow(IEnumerable<Tok> tokens) => _tokens = tokens.ToArray();

    public bool IsDone => _tokens.Length <= CurrentTokenPosition;

    public IReadOnlyList<Tok> MoveUntilOneOfThe(params TokType[] types) {
        var results = new List<Tok>();
        while (!IsDone)
        {
            MoveNext();
            var current = Current;
            if (current == null || types.Contains(current.Type))
                return results;
            results.Add(current);
        }

        return results;
    }

    public int CurrentTokenPosition { get; private set; }
    public Tok Current => IsDone ? CurrentAfterEofFlowTok : _tokens[CurrentTokenPosition];
    public bool IsStart => CurrentTokenPosition == 0;
    public Tok Previous => IsStart ? PreviousBeforeFlowTok : _tokens[CurrentTokenPosition - 1];

    /// <summary>
    /// When true, ':' after an identifier is NOT parsed as a type annotation.
    /// Used inside array bracket context where ':' means slice.
    /// </summary>
    public bool SuppressTypeAnnotation { get; set; }

    /// <summary>
    /// When true, NewLine tokens act as statement terminators inside
    /// expression reading — they break binary-operator chains rather than
    /// being treated as ignorable whitespace. Set by LangParser around
    /// each statement-level expression read; default false preserves
    /// expression-mode semantics where `;` (tokenised as NewLine) is
    /// free-form whitespace between sub-expressions. (BugHunt-stmt #66.)
    /// </summary>
    public bool RespectNewLines { get; set; }

    public bool SkipNewLines() {
        bool result = false;
        while (!IsDone && IsCurrent(TokType.NewLine))
        {
            result = true;
            MoveNext();
        }

        return result;
    }

    /// <summary>
    /// Variant of <see cref="SkipNewLines"/> used by the binary-operator
    /// chain reader. Skips NewLines unless <see cref="RespectNewLines"/>
    /// is set AND the next non-NL token is a leading-continuation operator
    /// (ambiguous unary form like `+`/`-`). In that case the NL acts as
    /// a statement terminator and the chain must stop. Other leading-NL
    /// followers (binary-only ops, closers, `else`) are still skipped so
    /// inline `then-value ; else …` continues to parse.
    /// </summary>
    public bool SkipNewLinesInBinaryChain() {
        if (!RespectNewLines)
            return SkipNewLines();
        if (!IsCurrent(TokType.NewLine))
            return false;
        // Peek past NewLines to see what follows.
        int pos = CurrentTokenPosition;
        while (pos < _tokens.Length && _tokens[pos].Type == TokType.NewLine) pos++;
        if (pos >= _tokens.Length) return false;
        var nextType = _tokens[pos].Type;
        if (IsLeadingContinuationAmbiguous(nextType))
            return false; // keep the NL — it terminates the expression
        // Safe to skip — next token is unambiguous (binary-only or non-op)
        while (!IsDone && IsCurrent(TokType.NewLine)) MoveNext();
        return true;
    }

    private static bool IsLeadingContinuationAmbiguous(TokType type) =>
        // Operators whose surface form ALSO has a unary reading — `+x`, `-x`.
        // Any other leading-op is unambiguous as binary and we can skip the NL.
        type == TokType.Plus || type == TokType.Minus;

    public bool IsCurrent(TokType type) {
        if (IsDone) return type == TokType.Eof;

        return _tokens[CurrentTokenPosition].Type == type;
    }


    public Tok Peek => PeekNext(1);
    public int CurrentTokenFinishPosition
    {
        get
        {
            if (!IsDone)
                return Current.Finish;
            if (_tokens.Length == 0)
                return 0;
            return _tokens[^1].Finish;
        }
    }

    public int CurrentTokenStartPosition
    {
        get
        {
            if (!IsDone)
                return Current.Start;
            if (_tokens.Length == 0)
                return 0;
            return _tokens[^1].Finish;
        }
    }


    private Tok PeekNext(int offset) {
        if (_tokens.Length <= (CurrentTokenPosition + offset + 1))
            return null;
        return _tokens[CurrentTokenPosition + offset];
    }

    public void Move(int position) {
        if (_tokens.Length <= position)
            CurrentTokenPosition = position - 1;
        else
            CurrentTokenPosition = position;
    }

    public void MoveNext() {
        if (!IsDone)
            CurrentTokenPosition++;
    }

    public bool IsPrevious(TokType tokType) {
        if (CurrentTokenPosition <= 0)
            return false;
        return _tokens[CurrentTokenPosition - 1].Type == tokType;
    }

    public Tok GetTokenAt(int position) => _tokens[position];

    public override string ToString() {
        var sb = new StringBuilder("Flow [ ");
        if (CurrentTokenPosition > 0)
            sb.Append($"prev: {Previous.Type}; ");
        sb.Append($"->cur: {Current.Type}; ");
        sb.Append($"next: {PeekNext(1)?.Type} ]");

        return sb.ToString();
    }
}
