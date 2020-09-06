using System.Collections;
using System.Collections.Generic;

namespace NFun.Runtime.Arrays
{
    class RangeEnumerator : IEnumerator<object>
    {
        private readonly int _step;

        public RangeEnumerator(int start, int count, int step)
        {
            _start = start;
            _current = start;
            _finish = start+ count;
            _step = step;
        }

        private int _current;
        private readonly int _finish;
        private readonly int _start;

        public bool MoveNext()
        {
            if (_current < _finish)
            {
                _current += _step;
            }
            return _current < _finish;
        }

        public void Reset()
        {
            _current = _start;
        }

        public object Current => _current;

        object IEnumerator.Current => Current;

        public void Dispose() {}
    }
}