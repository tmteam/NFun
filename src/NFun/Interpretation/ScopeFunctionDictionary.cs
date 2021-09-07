using System.Collections.Generic;
using System.Linq;
using NFun.Interpretation.Functions;

namespace NFun.Interpretation {

internal sealed class ScopeFunctionDictionary : IFunctionDictionary {
    private readonly IFunctionDictionary _originalDictionary;
    private readonly Dictionary<string, IFunctionSignature> _functions = new();
    private readonly Dictionary<string, List<IFunctionSignature>> _overloads = new();

    public ScopeFunctionDictionary(IFunctionDictionary originalDictionary) { _originalDictionary = originalDictionary; }

    public IList<IFunctionSignature> SearchAllFunctionsIgnoreCase(string name, int argCount) {
        //code used only in error handling. No need to optimize.
        var lowerName = GetOverloadName(name.ToLower(), argCount);
        var results = new List<IFunctionSignature>();
        foreach (var (key, value) in _functions)
        {
            if (key.ToLower() == lowerName)
            {
                results.Add(value);
            }
        }

        if (results.Any())
            return results;
        else
            return _originalDictionary.SearchAllFunctionsIgnoreCase(name, argCount);
    }

    public IList<IFunctionSignature> GetOverloads(string name) {
        var origins = _originalDictionary.GetOverloads(name);
        if (!_overloads.TryGetValue(name, out var signatures))
            return origins;
        return signatures.Union(origins).ToList();
    }

    public IFunctionSignature GetOrNull(string name, int argCount) {
        var overloadName = GetOverloadName(name, argCount);
        _functions.TryGetValue(overloadName, out var signature);
        if (signature == null)
            return _originalDictionary.GetOrNull(name, argCount);
        return signature;
    }

    public bool TryAdd(IFunctionSignature function) {
        var name = GetOverloadName(function.Name, function.ArgTypes.Length);
        if (_functions.ContainsKey(name))
            return false;
        _functions.Add(name, function);
        if (!_overloads.ContainsKey(function.Name))
        {
            _overloads.Add(function.Name, new List<IFunctionSignature>());
        }

        _overloads[function.Name].Add(function);
        return true;
    }

    private static string GetOverloadName(string name, int argCount)
        => name + " " + argCount;
}

}