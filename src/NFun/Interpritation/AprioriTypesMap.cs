using System;
using System.Collections;
using System.Collections.Generic;
using NFun.Types;

namespace NFun.Interpritation
{
    public class AprioriTypesMap:IEnumerable<KeyValuePair<string, VarType>>
    {
        public static AprioriTypesMap Empty  => new ();
        //todo - make apriori immutable
        private AprioriTypesMap()
        {
            _typesMap = new(StringComparer.OrdinalIgnoreCase);
        }

        private AprioriTypesMap(Dictionary<string,VarType> items)
        {
            _typesMap = items;
        }
        public void Add(string id, VarType type) => _typesMap.Add(id, type);

        private readonly Dictionary<string, VarType> _typesMap;
        public IEnumerator<KeyValuePair<string, VarType>> GetEnumerator() => _typesMap.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => _typesMap.GetEnumerator();

        public AprioriTypesMap CloneWith(string name, VarType type)
        {
            var dicCopy = new Dictionary<string, VarType>(_typesMap);
            dicCopy.Add(name, type);
            return new AprioriTypesMap(dicCopy);
        }
            
        
    }
}