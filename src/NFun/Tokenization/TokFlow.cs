using System.Collections.Generic;
using System.Linq;

namespace NFun.Tokenization
{
    public class TokFlow
    {
        private readonly Tok[] _tokens;

        public TokFlow(IEnumerable<Tok> tokens)
        {
            _tokens = tokens.ToArray();
        }

        private int _currentPos; 
        public bool IsDone => _tokens.Length <= _currentPos;

        public Tok[] MoveUntilOneOfThe(params TokType[] types)
        {
            List<Tok> results = new List<Tok>();
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
        public Tok Current => IsDone ? null : _tokens[_currentPos];
        public bool IsStart => _currentPos == 0;
        public Tok Previous => IsStart ? null : _tokens[_currentPos - 1];
        public bool SkipNewLines()
        {
            bool result = false;
            while (!IsDone && IsCurrent(TokType.NewLine))
            {
                MoveNext();
                result = true;
            }

            return result;
        }

        public bool IsCurrent(TokType type) 
            => Current?.Type == type;

      
        public Tok Peek => PeekNext(1);
        public int Position => Current?.Start?? _tokens.LastOrDefault()?.Finish?? 0;

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