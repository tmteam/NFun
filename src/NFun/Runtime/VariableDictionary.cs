using System;
using System.Collections.Generic;
using System.Linq;

namespace NFun.Runtime;

internal class VariableDictionary:IReadonlyVariableDictionary {
    private readonly Dictionary<string, VariableSource> _variables;

    internal VariableDictionary() {
        _variables = new Dictionary<string, VariableSource>(StringComparer.OrdinalIgnoreCase);
    }

    internal VariableDictionary(int capacity) {
        _variables = new Dictionary<string, VariableSource>(capacity, StringComparer.OrdinalIgnoreCase);
    }

    internal VariableDictionary(IEnumerable<VariableSource> sources, int capacity) {
        _variables = new Dictionary<string, VariableSource>(capacity, StringComparer.OrdinalIgnoreCase);

        foreach (var variableSource in sources)
            _variables.Add(variableSource.Name, variableSource);
    }   

    public int Count => _variables.Count;

    internal void AddOrReplace(VariableSource source) => _variables[source.Name] = source;

    /// <summary>
    /// Returns false if variable is already registered
    /// </summary>
    internal bool TryAdd(VariableSource source) {
        var name = source.Name;
        if (_variables.ContainsKey(name))
            return false;
        _variables.Add(name, source);
        return true;
    }

    public VariableSource GetOrNull(string id) =>
        _variables.TryGetValue(id, out var v) ? v : null;
    
    public IReadonlyVariableDictionary Clone() => new VariableDictionary(_variables.Values.Select(v=>v.Clone()), _variables.Count);

    public VariableSource[] GetAllAsArray() {
        var sources = new VariableSource[_variables.Count];
        var i = 0;
        foreach (var variable in _variables)
        {
            sources[i] = variable.Value;
            i++;
        }

        return sources;
    }

    public IEnumerable<VariableSource> GetAll() => _variables.Values;
}