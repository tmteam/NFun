using System;
using System.Collections;
using System.Collections.Generic;

namespace NFun.Interpretation; 

public interface IAprioriTypesMap : IEnumerable<KeyValuePair<string, FunnyType>> { }

public class EmptyAprioriTypesMap : IAprioriTypesMap {
    public static readonly EmptyAprioriTypesMap Instance = new();
    private readonly KeyValuePair<string, FunnyType>[] _arr = Array.Empty<KeyValuePair<string, FunnyType>>();
    private EmptyAprioriTypesMap() { }
    public IEnumerator<KeyValuePair<string, FunnyType>> GetEnumerator() => ((IEnumerable<KeyValuePair<string, FunnyType>>)_arr).GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => _arr.GetEnumerator();
}

public class SingleAprioriTypesMap : IAprioriTypesMap {
    private readonly KeyValuePair<string, FunnyType>[] _arr;
    public SingleAprioriTypesMap(string key, FunnyType type) {
        _arr = new[] { new KeyValuePair<string, FunnyType>(key, type) };
    }
    public IEnumerator<KeyValuePair<string, FunnyType>> GetEnumerator() => ((IEnumerable<KeyValuePair<string, FunnyType>>)_arr).GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => _arr.GetEnumerator();
}

public class MutableAprioriTypesMap : IAprioriTypesMap {
    public MutableAprioriTypesMap() { _typesMap = new Dictionary<string, FunnyType>(StringComparer.OrdinalIgnoreCase); }

    private MutableAprioriTypesMap(Dictionary<string, FunnyType> items) { _typesMap = items; }

    private readonly Dictionary<string, FunnyType> _typesMap;

    public void Add(string id, FunnyType type) => _typesMap.Add(id, type);
    public IEnumerator<KeyValuePair<string, FunnyType>> GetEnumerator() => _typesMap.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => _typesMap.GetEnumerator();

    public MutableAprioriTypesMap CloneWith(string name, FunnyType type) {
        var dicCopy = new Dictionary<string, FunnyType>(_typesMap) { { name, type } };
        return new MutableAprioriTypesMap(dicCopy);
    }
}