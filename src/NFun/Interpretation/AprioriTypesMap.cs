using System;
using System.Collections;
using System.Collections.Generic;
using NFun.Types;

namespace NFun.Interpretation {

public class AprioriTypesMap : IEnumerable<KeyValuePair<string, FunnyType>> {
    public AprioriTypesMap() { _typesMap = new Dictionary<string, FunnyType>(StringComparer.OrdinalIgnoreCase); }

    private AprioriTypesMap(Dictionary<string, FunnyType> items) { _typesMap = items; }

    private readonly Dictionary<string, FunnyType> _typesMap;

    public void Add(string id, FunnyType type) => _typesMap.Add(id, type);
    public IEnumerator<KeyValuePair<string, FunnyType>> GetEnumerator() => _typesMap.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => _typesMap.GetEnumerator();

    public AprioriTypesMap CloneWith(string name, FunnyType type) {
        var dicCopy = new Dictionary<string, FunnyType>(_typesMap) { { name, type } };
        return new AprioriTypesMap(dicCopy);
    }
}

}