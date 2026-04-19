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

    public override string ToString() =>
        "{ " + string.Join(", ", _values.Select(v => $"{v.Key}={v.Value}")) + " }";

    IEnumerator IEnumerable.GetEnumerator() => _values.GetEnumerator();

    public IEnumerator<KeyValuePair<string, object>> GetEnumerator() => _values.GetEnumerator();

    public override bool Equals(object obj) {
        if (obj is not FunnyStruct str)
            return false;
        // Structural equality: iterate the SMALLER struct and require all its fields
        // to exist and match in the larger struct. Extra fields in the larger are ignored.
        // This matches TIC's structural subtyping: {x=1,y=2} == {x=1} → TIC coerces
        // both to {x:Int32}, so only shared fields matter at runtime.
        var (smaller, larger) = _values.Count <= str._values.Count
            ? (_values, str._values)
            : (str._values, _values);
        foreach (var (key, value) in smaller)
        {
            if (!larger.TryGetValue(key, out var otherValue))
                return false;
            if (!otherValue.Equals(value))
                return false;
        }

        return true;
    }

    public override int GetHashCode() {
        // Hash must be consistent with structural Equals: {x=1,y=2}.Equals({x=1}) = true
        // means their hashes must match. Since Equals ignores extra fields of the larger struct,
        // we cannot include field count or all fields in the hash.
        // Use a type-marker only — all FunnyStructs share the same bucket.
        // This is O(n) on collision chains but correct; struct equality comparison
        // is rare in hot paths (mostly used in == operator and 'in' checks).
        return typeof(FunnyStruct).GetHashCode();
    }

    public bool ContainsKey(string key) => _values.ContainsKey(key);

    public bool TryGetValue(string key, out object value) => _values.TryGetValue(key, out value);
}
