using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace NFun.Tic;

/// <summary>
/// Specialized (string → TicNode) dictionary optimized for 0-2 entries.
/// 0-2 entries: inline key/value fields, no Dictionary allocation.
/// 3+ entries: spills to Dictionary (one-way, no shrink-back).
/// </summary>
sealed class FieldMap : IEnumerable<KeyValuePair<string, TicNode>> {
    private string _key0, _key1;
    private TicNode _val0, _val1;
    private Dictionary<string, TicNode> _dict; // null when inlined
    private int _inlineCount; // 0..2 when inlined, -1 when using _dict

    public FieldMap() => _inlineCount = 0;

    public FieldMap(string key, TicNode value) {
        _key0 = key;
        _val0 = value;
        _inlineCount = 1;
    }

    /// <summary>Takes ownership of dictionary. Flattens to inline if ≤2 entries.</summary>
    public FieldMap(Dictionary<string, TicNode> dict) {
        if (dict.Count <= 2) {
            _inlineCount = 0;
            foreach (var (key, value) in dict)
                AddInline(key, value);
        } else {
            _dict = dict;
            _inlineCount = -1;
        }
    }

    public int Count {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _inlineCount >= 0 ? _inlineCount : _dict!.Count;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public TicNode GetValueOrNull(string key) {
        if (_inlineCount >= 0) {
            if (_inlineCount >= 1 && _key0 == key) return _val0;
            if (_inlineCount >= 2 && _key1 == key) return _val1;
            return null;
        }
        _dict!.TryGetValue(key, out var result);
        return result;
    }

    public void Add(string key, TicNode value) {
        if (_inlineCount >= 0) {
            if (_inlineCount < 2) {
                AddInline(key, value);
                return;
            }
            SpillToDictionary();
        }
        _dict!.Add(key, value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void AddInline(string key, TicNode value) {
        if (_inlineCount == 0) {
            _key0 = key; _val0 = value; _inlineCount = 1;
        } else {
            _key1 = key; _val1 = value; _inlineCount = 2;
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private void SpillToDictionary() {
        _dict = new Dictionary<string, TicNode>(4) {
            { _key0!, _val0! }, { _key1!, _val1! }
        };
        _key0 = _key1 = null;
        _val0 = _val1 = null;
        _inlineCount = -1;
    }

    /// <summary>Indexer setter for patterns like nodes[key] = value.</summary>
    public TicNode this[string key] {
        set {
            if (_inlineCount >= 0) {
                if (_inlineCount >= 1 && _key0 == key) { _val0 = value; return; }
                if (_inlineCount >= 2 && _key1 == key) { _val1 = value; return; }
                Add(key, value);
                return;
            }
            _dict![key] = value;
        }
    }

    // --- Iteration ---

    public ValuesEnumerable Values => new(this);
    public Enumerator GetEnumerator() => new(this);
    IEnumerator<KeyValuePair<string, TicNode>> IEnumerable<KeyValuePair<string, TicNode>>.GetEnumerator()
        => new BoxedEnumerator(this);
    IEnumerator IEnumerable.GetEnumerator() => new BoxedEnumerator(this);

    public struct Enumerator {
        private readonly FieldMap _map;
        private int _index;
        private Dictionary<string, TicNode>.Enumerator _dictEnum;

        internal Enumerator(FieldMap map) {
            _map = map;
            _index = -1;
            _dictEnum = map._dict?.GetEnumerator() ?? default;
        }

        public bool MoveNext() {
            if (_map._inlineCount >= 0)
                return ++_index < _map._inlineCount;
            return _dictEnum.MoveNext();
        }

        public KeyValuePair<string, TicNode> Current =>
            _map._inlineCount >= 0
                ? (_index == 0
                    ? new KeyValuePair<string, TicNode>(_map._key0!, _map._val0!)
                    : new KeyValuePair<string, TicNode>(_map._key1!, _map._val1!))
                : _dictEnum.Current;
    }

    private sealed class BoxedEnumerator : IEnumerator<KeyValuePair<string, TicNode>> {
        private readonly FieldMap _map;
        private int _index;
        private Dictionary<string, TicNode>.Enumerator _dictEnum;
        internal BoxedEnumerator(FieldMap map) {
            _map = map; _index = -1;
            if (map._dict != null) _dictEnum = map._dict.GetEnumerator();
        }
        public bool MoveNext() {
            if (_map._inlineCount >= 0) return ++_index < _map._inlineCount;
            return _dictEnum.MoveNext();
        }
        public KeyValuePair<string, TicNode> Current =>
            _map._inlineCount >= 0
                ? (_index == 0
                    ? new KeyValuePair<string, TicNode>(_map._key0!, _map._val0!)
                    : new KeyValuePair<string, TicNode>(_map._key1!, _map._val1!))
                : _dictEnum.Current;
        object IEnumerator.Current => Current;
        public void Reset() => _index = -1;
        public void Dispose() { }
    }

    // Values iteration
    public readonly struct ValuesEnumerable {
        private readonly FieldMap _map;
        internal ValuesEnumerable(FieldMap map) => _map = map;
        public ValuesEnumerator GetEnumerator() => new(_map);
    }

    public struct ValuesEnumerator {
        private readonly FieldMap _map;
        private int _index;
        private Dictionary<string, TicNode>.ValueCollection.Enumerator _dictEnum;
        internal ValuesEnumerator(FieldMap map) {
            _map = map; _index = -1;
            _dictEnum = map._dict?.Values.GetEnumerator() ?? default;
        }
        public bool MoveNext() {
            if (_map._inlineCount >= 0) return ++_index < _map._inlineCount;
            return _dictEnum.MoveNext();
        }
        public TicNode Current =>
            _map._inlineCount >= 0
                ? (_index == 0 ? _map._val0! : _map._val1!)
                : _dictEnum.Current;
    }
}
