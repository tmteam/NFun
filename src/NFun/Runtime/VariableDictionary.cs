using System;
using System.Collections.Generic;
using NFun.Interpretation.Nodes;
using NFun.Tokenization;
using NFun.Types;

namespace NFun.Runtime {

internal class VariableDictionary {
    private readonly Dictionary<string, VariableUsages> _variables;

    internal VariableDictionary() =>
        _variables = new Dictionary<string, VariableUsages>(StringComparer.OrdinalIgnoreCase);

    internal VariableDictionary(int capacity) =>
        _variables = new Dictionary<string, VariableUsages>(capacity, StringComparer.OrdinalIgnoreCase);

    internal VariableDictionary(IEnumerable<VariableSource> sources) {
        _variables = new Dictionary<string, VariableUsages>(StringComparer.OrdinalIgnoreCase);

        foreach (var variableSource in sources)
        {
            _variables.Add(variableSource.Name, new VariableUsages(variableSource));
        }
    }

    internal void AddOrReplace(VariableSource source) => _variables[source.Name] = new VariableUsages(source);

    /// <summary>
    /// Returns false if variable is already registered
    /// </summary>
    internal bool TryAdd(VariableSource source) {
        var name = source.Name;
        if (_variables.ContainsKey(name))
            return false;
        _variables.Add(name, new VariableUsages(source));
        return true;
    }

    /// <summary>
    /// Returns false if variable is already registered
    /// </summary>
    internal bool TryAdd(VariableUsages usages) {
        var name = usages.Source.Name;
        if (_variables.ContainsKey(name))
            return false;
        _variables.Add(name, usages);
        return true;
    }

    internal VariableSource GetSourceOrNull(string id) =>
        _variables.TryGetValue(id, out var v) ? v.Source : null;

    public VariableExpressionNode CreateVarNode(string id, Interval interval, FunnyType type) {
        if (!_variables.TryGetValue(id, out var usage))
        {
            // Variable is not declared yet.
            // Access to not declared variable means that it is input
            var source = VariableSource.CreateWithoutStrictTypeLabel(id, type, FunnyVarAccess.Input);
            usage = new VariableUsages(source);
            _variables.Add(id, usage);
        }

        var node = new VariableExpressionNode(usage.Source, interval);
        usage.Usages.AddLast(node);
        return node;
    }

    internal VariableUsages GetSuperAnonymousVariableOrNull() {
        foreach (var key in _variables.Keys)
        {
            if (Helper.DoesItLooksLikeSuperAnonymousVariable(key))
                return _variables[key];
        }

        return null;
    }

    internal VariableUsages GetUsages(string id) => _variables[id];
    internal bool TryGetUsages(string id, out VariableUsages usage) => _variables.TryGetValue(id, out usage);

    internal VariableUsages[] GetAllUsages() {
        var sources = new VariableUsages[_variables.Count];
        var i = 0;
        foreach (var variable in _variables)
        {
            sources[i] = variable.Value;
            i++;
        }

        return sources;
    }

    internal VariableSource[] GetAllSources() {
        var sources = new VariableSource[_variables.Count];
        var i = 0;
        foreach (var variable in _variables)
        {
            sources[i] = variable.Value.Source;
            i++;
        }

        return sources;
    }
}

}