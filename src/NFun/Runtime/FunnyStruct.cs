using System.Collections.Generic;
using System.Linq;

namespace NFun.Runtime
{
    public class FunnyStruct
    {
        public static FunnyStruct Create(params (string,object)[] fields) =>
            new (
                fields.ToDictionary(f => f.Item1, f => f.Item2)
            );

        public static FunnyStruct Create(string name, object value) => new FunnyStruct(new Dictionary<string, object>{{name,value}});
      
        private readonly Dictionary<string, object> _values;
        internal FunnyStruct(Dictionary<string,object> values) => _values = values;
        
        public object GetValue(string field) => _values[field];
        public override string ToString() 
            => "@{ "+string.Join("; ", _values.Select(v=> $"{v.Key}={v.Value}"))+" }";

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