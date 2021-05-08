using System;
using System.Collections;
using System.Collections.Generic;
using NFun.Types;

namespace NFun.Interpritation
{
    public class AprioriTypesMap:IEnumerable<KeyValuePair<string, VarType>>
    {
        public static AprioriTypesMap Empty = new ();
        public void Add(string id, VarType type) => _typesMap.Add(id, type);

        private readonly Dictionary<string, VarType> _typesMap  = new(StringComparer.OrdinalIgnoreCase);
        public IEnumerator<KeyValuePair<string, VarType>> GetEnumerator() => _typesMap.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => _typesMap.GetEnumerator();
    }
}