using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace NFun.Runtime {

public class FunnyStruct : IReadOnlyDictionary<string, object> {
    internal class FieldsDictionary: Dictionary<string,object> {
        public FieldsDictionary(int capacity): base(capacity, StringComparer.InvariantCultureIgnoreCase) {
            
        }
    }
    public static FunnyStruct Create(params (string, object)[] fields) {
        var values = new FieldsDictionary(fields.Length);
        foreach (var field in fields)
            values.Add(field.Item1, field.Item2);
        return new(values);
    }

    private readonly Dictionary<string, object> _values;
    internal FunnyStruct(FieldsDictionary values) => _values = values;
     
    public object GetValue(string field) => _values[field];

    public override string ToString()
        => "{ " + string.Join(", ", _values.Select(v => $"{v.Key}={v.Value}")) + " }";

    IEnumerator IEnumerable.GetEnumerator() => _values.GetEnumerator();

    public IEnumerator<KeyValuePair<string, object>> GetEnumerator() => _values.GetEnumerator();

    public override bool Equals(object obj) {
        if (obj is not FunnyStruct str)
            return false;
        if (_values.Count != str._values.Count)
            return false;
        foreach (var (key, value) in _values)
        {
            var otherValue = str.GetValue(key);
            if (!otherValue.Equals(value))
                return false;
        }

        return true;
    }

    public int Count => _values.Count;
    public bool ContainsKey(string key) => _values.ContainsKey(key);


    public bool TryGetValue(string key, out object value) => _values.TryGetValue(key, out value);

    public object this[string key] => _values[key];
    public IEnumerable<string> Keys => _values.Keys;
    public IEnumerable<object> Values => _values.Values;
}

}