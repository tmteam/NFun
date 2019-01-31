using System.Collections.Generic;
using System.Linq;

namespace Funny.Tokenization
{
    public class TokenFlow
    {
        private readonly Tok[] _tokens;

        public TokenFlow(IEnumerable<Tok> tokens)
        {
            _tokens = tokens.ToArray();
        }
        public int CurrentPos { get; private set; }
        public bool IsLast => _tokens.Length <= CurrentPos + 1;
        public bool IsDone => _tokens.Length <= CurrentPos;
        public Tok Current => IsDone ? null : _tokens[CurrentPos];
        
        public bool IsCurrent(TokType type) 
            => Current?.Type == type;

        public bool IsNext(TokType type) 
            => PeekNext(1)?.Is(type) == true;

        public bool IsNext(TokType type, string val)
        {
            if (IsLast)
                return false;
            var next = Peek;
            
            return next.Type == type && next.Value.Equals(val);
        }
        public bool IsCurrent(TokType type, string val)
        {
            if (IsDone)
                return false;
            var cur = Current;
            
            return cur.Type == type && cur.Value==val;
        }
        public Tok Peek => PeekNext((1));
        public Tok PeekNext(int offset)
        {
            if (_tokens.Length <= (CurrentPos + offset + 1))
                return null;
            return _tokens[CurrentPos + offset];
        }

        
        public Tok MoveNext()
        {
            if (IsDone)
                return null;
            
            CurrentPos++;
            return Current;
        }

        public bool IsPrevious(TokType tokType)
        {
            if (CurrentPos <= 0)
                return false;
            return _tokens[CurrentPos - 1].Type == tokType;
        }
    }
}