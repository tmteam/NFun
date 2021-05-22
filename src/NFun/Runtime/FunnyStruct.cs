using System.Collections.Generic;
using System.Linq;

namespace NFun.Runtime
{
    public interface IReadonlyFunnyStruct
    {
        IEnumerable<(string,object)> Fields { get; }
        object GetValue(string field);
    }
    
    public class FunnyStruct:IReadonlyFunnyStruct
    {
        public static FunnyStruct Create(params (string,object)[] fields) =>
            new (
                fields.ToDictionary(f => f.Item1, f => f.Item2)
            );
        
        private readonly Dictionary<string, object> _values;
        internal FunnyStruct(Dictionary<string,object> values) => _values = values;

        public IEnumerable<(string, object)> Fields => _values.Select(v => (v.Key, v.Value));

        public object GetValue(string field) => _values[field];
        public override string ToString() 
            => "the{ "+string.Join("; ", _values.Select(v=> $"{v.Key}={v.Value}"))+" }";

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
    }
}