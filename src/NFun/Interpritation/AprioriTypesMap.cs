using System;
using System.Collections;
using System.Collections.Generic;
using NFun.Types;

namespace NFun.Interpritation
{
    public class AprioriTypesMap:IEnumerable<KeyValuePair<string, FunnyType>>
    {
        public static AprioriTypesMap Empty  => new ();
        private AprioriTypesMap()
        {
            _typesMap = new(StringComparer.OrdinalIgnoreCase);
        }

        private AprioriTypesMap(Dictionary<string,FunnyType> items)
        {
            _typesMap = items;
        }
        public void Add(string id, FunnyType type) => _typesMap.Add(id, type);

        private readonly Dictionary<string, FunnyType> _typesMap;
        public IEnumerator<KeyValuePair<string, FunnyType>> GetEnumerator() => _typesMap.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => _typesMap.GetEnumerator();

        public AprioriTypesMap CloneWith(string name, FunnyType type)
        {
            var dicCopy = new Dictionary<string, FunnyType>(_typesMap) {{name, type}};
            return new AprioriTypesMap(dicCopy);
        }
    }
}