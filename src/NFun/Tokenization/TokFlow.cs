using System.Collections.Generic;
using System.Linq;

namespace NFun.Tokenization
{
    public class TokFlow
    {
        private static readonly Tok PreviousBeforeFlowTok = Tok.New(TokType.NotAToken, 0, 0);
        private static readonly Tok CurrentAfterEofFlowTok = Tok.New(TokType.Eof, 0, 0);
        
        private readonly Tok[] _tokens;

        public TokFlow(IEnumerable<Tok> tokens)
        {
            _tokens = tokens.ToArray();
        }

        private int _currentPos; 
        public bool IsDone => _tokens.Length <= _currentPos;

        public Tok[] MoveUntilOneOfThe(params TokType[] types)
        {
            var results = new List<Tok>();
            while (!IsDone)
            {
                MoveNext();
                var current = Current;
                if (current==null || types.Contains(current.Type))
                    return results.ToArray();
                results.Add(current); 
            }
            return results.ToArray();
        }

        public int CurrentTokenPosition => _currentPos;
        public Tok Current => IsDone ? CurrentAfterEofFlowTok : _tokens[_currentPos];
        public bool IsStart => _currentPos == 0;
        public Tok Previous => IsStart ? PreviousBeforeFlowTok : _tokens[_currentPos - 1];
        public void SkipNewLines()
        {
            while (!IsDone && IsCurrent(TokType.NewLine)) 
                MoveNext();
        }

        public bool IsCurrent(TokType type)
        {
            if (IsDone) return type == TokType.Eof;
            
            return _tokens[_currentPos].Type == type;
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
                return _tokens[_tokens.Length - 1].Finish;
            }
        }

        public Tok PeekNext(int offset)
        {
            if (_tokens.Length <= (_currentPos + offset + 1))
                return null;
            return _tokens[_currentPos + offset];
        }

        public void Move(int position)
        {
            if(_tokens.Length<=position)
                _currentPos = position - 1;
            else
                _currentPos = position;
        }
        public void MoveNext()
        {
            if (!IsDone)
                _currentPos++;
        }

        public bool IsPrevious(TokType tokType)
        {
            if (_currentPos <= 0)
                return false;
            return _tokens[_currentPos - 1].Type == tokType;
        }
    }
}