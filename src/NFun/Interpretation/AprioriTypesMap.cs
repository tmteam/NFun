using System;
using System.Collections;
using System.Collections.Generic;

namespace NFun.Interpretation;

public interface IAprioriTypesMap : IEnumerable<AprioriVarInfo> { }

internal class EmptyAprioriTypesMap : IAprioriTypesMap {
    public static readonly EmptyAprioriTypesMap Instance = new();
    private readonly AprioriVarInfo[] _arr = Array.Empty<AprioriVarInfo>();
    private EmptyAprioriTypesMap() { }
    public IEnumerator<AprioriVarInfo> GetEnumerator() => ((IEnumerable<AprioriVarInfo>)_arr).GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => _arr.GetEnumerator();
}

internal class SingleAprioriTypesMap : IAprioriTypesMap {
    private readonly AprioriVarInfo[] _arr;
    public SingleAprioriTypesMap(string key, FunnyType type) => _arr = new[] { new AprioriVarInfo(key, type) };
    public IEnumerator<AprioriVarInfo> GetEnumerator() => ((IEnumerable<AprioriVarInfo>)_arr).GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => _arr.GetEnumerator();
}

internal class MutableAprioriTypesMap : IAprioriTypesMap {
    public MutableAprioriTypesMap() => _typesMap = new Dictionary<string, AprioriVarInfo>(StringComparer.OrdinalIgnoreCase);

    private MutableAprioriTypesMap(Dictionary<string, AprioriVarInfo> items) => _typesMap = items;

    private readonly Dictionary<string, AprioriVarInfo> _typesMap;

    public void Add(string name, FunnyType type) => _typesMap.Add(name, new AprioriVarInfo(name, type));

    public IEnumerator<AprioriVarInfo> GetEnumerator() => _typesMap.Values.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => _typesMap.GetEnumerator();

    public MutableAprioriTypesMap CloneWith(string name, FunnyType type) {
        var dicCopy = new Dictionary<string, AprioriVarInfo>(_typesMap) { { name, new AprioriVarInfo(name, type) } };
        return new MutableAprioriTypesMap(dicCopy);
    }
}

public struct AprioriVarInfo {
    public string Name;
    public FunnyType Type;

    public AprioriVarInfo(string name, FunnyType type) {
        Name = name;
        Type = type;
    }
};

