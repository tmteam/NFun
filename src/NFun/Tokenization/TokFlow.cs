using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NFun.Tokenization; 

public class TokFlow {
    private static readonly Tok PreviousBeforeFlowTok = Tok.New(TokType.NotAToken, 0, 0);
    private static readonly Tok CurrentAfterEofFlowTok = Tok.New(TokType.Eof, 0, 0);

    private readonly Tok[] _tokens;

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

    public bool SkipNewLines() {
        bool result = false;
        while (!IsDone && IsCurrent(TokType.NewLine))
        {
            result = true;
            MoveNext();
        }

        return result;
    }

    public bool IsCurrent(TokType type) {
        if (IsDone) return type == TokType.Eof;

        return _tokens[CurrentTokenPosition].Type == type;
    }


    public Tok Peek => PeekNext(1);
    public int Position
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

    public override string ToString() {
        var sb = new StringBuilder("Flow [ ");
        if (CurrentTokenPosition > 0)
            sb.Append($"prev: {Previous.Type}; ");
        sb.Append($"->cur: {Current.Type}; ");
        sb.Append($"next: {PeekNext(1)?.Type} ]");

        return sb.ToString();
    }
}