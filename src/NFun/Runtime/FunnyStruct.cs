using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace NFun.Runtime;

public class FunnyStruct : IReadOnlyDictionary<string, object> {
    internal class FieldsDictionary : Dictionary<string, object> {
        public FieldsDictionary(int capacity) : base(capacity, StringComparer.InvariantCultureIgnoreCase) { }
    }

    public static FunnyStruct Create(params (string, object)[] fields) {
        var values = new FieldsDictionary(fields.Length);
        foreach (var field in fields)
            values.Add(field.Item1, field.Item2);
        return new(values);
    }

    private readonly Dictionary<string, object> _values;

    internal FunnyStruct(FieldsDictionary values) => _values = values;

    public object this[string key] => _values[key];

    public IEnumerable<string> Keys => _values.Keys;

    public IEnumerable<object> Values => _values.Values;

    public int Count => _values.Count;

    public object GetValue(string field) => _values[field];

    /// <summary>In-place field mutation. Used by lang-mode field assignment.</summary>
    internal void SetValue(string field, object value) => _values[field] = value;

    public override string ToString() =>
        "{ " + string.Join(", ", _values.Select(v => $"{v.Key}={v.Value}")) + " }";

    IEnumerator IEnumerable.GetEnumerator() => _values.GetEnumerator();

    public IEnumerator<KeyValuePair<string, object>> GetEnumerator() => _values.GetEnumerator();

    public override bool Equals(object obj) {
        if (obj is not FunnyStruct str)
            return false;
        // Structural equality: same field count + same fields + same values.
        // Per spec: "same list of fields (id and types) and all values equal."
        // {a=1} != {a=1, b=2} — different field count.
        if (_values.Count != str._values.Count)
            return false;
        foreach (var (key, value) in _values)
        {
            if (!str._values.TryGetValue(key, out var otherValue))
                return false;
            if (!otherValue.Equals(value))
                return false;
        }

        return true;
    }

    public override int GetHashCode() {
        // Include field count for better distribution — structs with different
        // field counts are never equal, so their hashes can differ.
        unchecked {
            int hash = _values.Count;
            foreach (var (key, value) in _values)
                hash = hash * 31 + key.GetHashCode();
            return hash;
        }
    }

    public bool ContainsKey(string key) => _values.ContainsKey(key);

    public bool TryGetValue(string key, out object value) => _values.TryGetValue(key, out value);
}
