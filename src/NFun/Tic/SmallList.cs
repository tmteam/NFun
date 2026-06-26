using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace NFun.Tic;

/// <summary>List with inline first element; allocates a raw tail array on the 2nd item.</summary>
public sealed class SmallList<T> : IReadOnlyList<T> where T : class {
    private T _item0;
    private T[] _tail;
    private int _count;

    public int Count {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _count;
    }

    public T this[int index] {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get {
            if ((uint)index >= (uint)_count) ThrowOOR();
            return index == 0 ? _item0 : _tail![index - 1];
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        set {
            if ((uint)index >= (uint)_count) ThrowOOR();
            if (index == 0) _item0 = value;
            else _tail![index - 1] = value;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Add(T item) {
        if (_count == 0) {
            _item0 = item;
            _count = 1;
            return;
        }
        AddSlow(item);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private void AddSlow(T item) {
        var tailIndex = _count - 1;
        if (_tail == null) {
            _tail = new T[2];
            _tail[0] = item;
        } else if (tailIndex >= _tail.Length) {
            var newTail = new T[_tail.Length * 2];
            Array.Copy(_tail, newTail, _tail.Length);
            _tail = newTail;
            _tail[tailIndex] = item;
        } else {
            _tail[tailIndex] = item;
        }
        _count++;
    }

    public void AddRange(IEnumerable<T> items) {
        if (items is SmallList<T> other) {
            for (int i = 0; i < other._count; i++)
                Add(other[i]);
            return;
        }
        if (items is IReadOnlyList<T> list) {
            for (int i = 0; i < list.Count; i++)
                Add(list[i]);
            return;
        }
        foreach (var item in items)
            Add(item);
    }

    public bool Remove(T item) {
        for (int i = 0; i < _count; i++) {
            if (ReferenceEquals(this[i], item)) {
                RemoveAt(i);
                return true;
            }
        }
        return false;
    }

    private void RemoveAt(int index) {
        _count--;
        if (_count == 0) {
            _item0 = null;
            return;
        }
        if (index == 0) {
            _item0 = _tail![0];
            if (_count > 1)
                Array.Copy(_tail, 1, _tail, 0, _count - 1);
            _tail[_count - 1] = null;
        } else {
            var tailIndex = index - 1;
            if (tailIndex < _count - 1)
                Array.Copy(_tail!, tailIndex + 1, _tail!, tailIndex, _count - 1 - tailIndex);
            _tail![_count - 1] = null;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Clear() {
        if (_count == 0) return;
        _item0 = null;
        if (_tail != null)
            Array.Clear(_tail, 0, Math.Min(_count - 1, _tail.Length));
        _count = 0;
        // Keep _tail allocated for ClearAncestors + re-add reuse.
    }

    public T[] ToArray() {
        if (_count == 0) return Array.Empty<T>();
        if (_count == 1) return new[] { _item0 };
        var result = new T[_count];
        result[0] = _item0;
        Array.Copy(_tail!, 0, result, 1, _count - 1);
        return result;
    }

    /// <summary>Snapshot safe across mutations of the source list. Zero alloc for 0-2 elements.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Snapshot ToSnapshot() => new(_count, _item0, _count > 1 ? _tail![0] : default, _count > 2 ? ToArray() : null);

    public readonly struct Snapshot {
        private readonly int _count;
        private readonly T _item0;
        private readonly T _item1;
        private readonly T[] _array; // non-null only for 3+

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal Snapshot(int count, T item0, T item1, T[] array) {
            _count = count; _item0 = item0; _item1 = item1; _array = array;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public SnapshotEnumerator GetEnumerator() => new(_count, _item0, _item1, _array);

        public struct SnapshotEnumerator {
            private readonly int _count;
            private readonly T _item0;
            private readonly T _item1;
            private readonly T[] _array;
            private int _index;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            internal SnapshotEnumerator(int count, T item0, T item1, T[] array) {
                _count = count; _item0 = item0; _item1 = item1; _array = array; _index = -1;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool MoveNext() => ++_index < _count;

            public T Current {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get => _array != null ? _array[_index] : (_index == 0 ? _item0 : _item1);
            }
        }
    }

    public Enumerator GetEnumerator() => new(this);
    IEnumerator<T> IEnumerable<T>.GetEnumerator() => new BoxedEnumerator(this);
    IEnumerator IEnumerable.GetEnumerator() => new BoxedEnumerator(this);

    public struct Enumerator {
        private readonly SmallList<T> _list;
        private int _index;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal Enumerator(SmallList<T> list) { _list = list; _index = -1; }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool MoveNext() => ++_index < _list._count;

        public T Current {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _index == 0 ? _list._item0 : _list._tail![_index - 1];
        }
    }

    private sealed class BoxedEnumerator : IEnumerator<T> {
        private readonly SmallList<T> _list;
        private int _index;
        internal BoxedEnumerator(SmallList<T> list) { _list = list; _index = -1; }
        public bool MoveNext() => ++_index < _list._count;
        public T Current => _index == 0 ? _list._item0 : _list._tail![_index - 1];
        object IEnumerator.Current => Current;
        public void Reset() => _index = -1;
        public void Dispose() { }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void ThrowOOR() => throw new ArgumentOutOfRangeException("index");
}
