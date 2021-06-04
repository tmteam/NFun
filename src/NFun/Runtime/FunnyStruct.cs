using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace NFun.Runtime
{
    public class FunnyStruct: IReadOnlyDictionary<string, object>
    {
        public static FunnyStruct Create(params (string,object)[] fields) =>
            new (
                fields.ToDictionary(f => f.Item1, f => f.Item2)
            );
        
        private readonly Dictionary<string, object> _values;
        internal FunnyStruct(Dictionary<string,object> values) => _values = values;
        
        public object GetValue(string field) => _values[field];
        public override string ToString() 
            => "the{ "+string.Join(", ", _values.Select(v=> $"{v.Key}={v.Value}"))+" }";

        IEnumerator IEnumerable.GetEnumerator() => _values.GetEnumerator();

        public IEnumerator<KeyValuePair<string, object>> GetEnumerator() => _values.GetEnumerator();

        public override bool Equals(object obj)
        {
            if (!(obj is FunnyStruct str))
                return false;
            if (_values.Count != str._values.Count)
                return false;
            foreach (var field in _values)
            {
                var otherValue = str.GetValue(field.Key);
                if (!otherValue.Equals(field.Value))
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