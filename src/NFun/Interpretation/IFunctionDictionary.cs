using System;
using System.Collections.Generic;
using System.Linq;
using NFun.Exceptions;
using NFun.Interpretation.Functions;

namespace NFun.Interpretation;

public interface IFunctionDictionary {
    IList<IFunctionSignature> SearchAllFunctionsIgnoreCase(string name, int argCount);
    IFunctionSignature GetOrNull(string name, int argCount);

    /// <summary>
    /// Finds best matching function given positional arg count and named arg names.
    /// Returns null if no match found.
    /// </summary>
    IFunctionSignature FindOrNull(string name, int positionalArgCount, string[] namedArgNames) =>
        FunctionDictionaryHelper.DefaultFindOrNull(this, name, positionalArgCount, namedArgNames);
}

/// <summary>
/// Shared resolution logic for FindOrNull across dictionary implementations.
/// </summary>
internal static class FunctionDictionaryHelper {
    public static IFunctionSignature DefaultFindOrNull(
        IFunctionDictionary dict, string name, int positionalArgCount, string[] namedArgNames) {
        var hasNamed = namedArgNames != null && namedArgNames.Length > 0;

        // Try exact arity match first
        var totalArgs = positionalArgCount + (hasNamed ? namedArgNames.Length : 0);
        var exact = dict.GetOrNull(name, totalArgs);
        if (exact != null)
        {
            // No named args → exact match is good
            if (!hasNamed) return exact;
            // Has named args → validate ArgProperties
            if (HasMatchingArgProperties(exact, positionalArgCount, namedArgNames))
                return exact;
        }

        // No overload search available in default implementation (no secondary index)
        return null;
    }

    /// <summary>
    /// Finds best match among overloads, considering defaults and params.
    /// </summary>
    public static IFunctionSignature FindAmongOverloads(
        IReadOnlyList<IFunctionSignature> overloads,
        int positionalArgCount,
        string[] namedArgNames) {
        if (overloads == null)
            return null;
        foreach (var sig in overloads)
        {
            if (sig.ArgProperties == null)
                continue;
            if (FitsWithDefaults(sig, positionalArgCount, namedArgNames))
                return sig;
        }
        return null;
    }

    /// <summary>Checks that ArgProperties has all named arg names and they don't overlap with positional slots.</summary>
    public static bool HasMatchingArgProperties(
        IFunctionSignature sig, int positionalArgCount, string[] namedArgNames) {
        var props = sig.ArgProperties;
        if (props == null || props.Length != sig.ArgTypes.Length)
            return false;
        foreach (var namedName in namedArgNames)
        {
            var found = false;
            for (int i = 0; i < props.Length; i++)
            {
                if (string.Equals(props[i].Name, namedName, StringComparison.OrdinalIgnoreCase))
                {
                    if (i < positionalArgCount)
                        return false; // overlaps positional
                    found = true;
                    break;
                }
            }
            if (!found)
                return false;
        }
        return true;
    }

    /// <summary>Checks if a signature with defaults can be satisfied by given args.</summary>
    private static bool FitsWithDefaults(
        IFunctionSignature sig, int positionalArgCount, string[] namedArgNames) {
        var props = sig.ArgProperties;
        var paramCount = props.Length;
        bool hasParams = paramCount > 0 && props[^1].IsParams;
        var nonParamsCount = hasParams ? paramCount - 1 : paramCount;

        // Positional args must fit within non-params slots (or overflow into params)
        if (!hasParams && positionalArgCount > paramCount)
            return false;
        if (hasParams && positionalArgCount < nonParamsCount)
        {
            // Check named args can fill the gap, or defaults exist
        }

        // Check all named args exist and don't overlap with positional slots
        foreach (var namedName in namedArgNames)
        {
            var found = false;
            for (int i = 0; i < paramCount; i++)
            {
                if (string.Equals(props[i].Name, namedName, StringComparison.OrdinalIgnoreCase))
                {
                    if (i < positionalArgCount)
                        return false; // overlaps positional
                    found = true;
                    break;
                }
            }
            if (!found)
                return false;
        }

        // Check all required (non-default, non-params) slots are filled
        for (int i = 0; i < nonParamsCount; i++)
        {
            if (i < positionalArgCount)
                continue; // filled by positional
            bool filledByNamed = false;
            foreach (var namedName in namedArgNames)
            {
                if (string.Equals(props[i].Name, namedName, StringComparison.OrdinalIgnoreCase))
                { filledByNamed = true; break; }
            }
            if (!filledByNamed && !props[i].HasDefault)
                return false;
        }

        return true;
    }
}


internal sealed class ImmutableFunctionDictionary : IFunctionDictionary {
    public ImmutableFunctionDictionary(IConcreteFunction[] concretes, GenericFunctionBase[] generics)
    {
        _functions = new Dictionary<string, IFunctionSignature>(concretes.Length + generics.Length);
        _overloads = new Dictionary<string, List<IFunctionSignature>>();
        foreach (var concrete in concretes) TryAdd(concrete);
        foreach (var generic in generics) TryAdd(generic);
    }

    private ImmutableFunctionDictionary(
        Dictionary<string, IFunctionSignature> functions,
        Dictionary<string, List<IFunctionSignature>> overloads) {
        _functions = functions;
        _overloads = overloads;
    }

    private readonly Dictionary<string, IFunctionSignature> _functions;
    private readonly Dictionary<string, List<IFunctionSignature>> _overloads;

    public IList<IFunctionSignature> SearchAllFunctionsIgnoreCase(string name, int argCount) {
        var lowerName = GetOverloadName(name.ToLower(), argCount);
        var results = new List<IFunctionSignature>();
        foreach (var function in _functions)
        {
            if (function.Key.ToLower() == lowerName)
            {
                results.Add(function.Value);
            }
        }

        return results;
    }

    public IFunctionSignature GetOrNull(string name, int argCount) {
        var overloadName = GetOverloadName(name, argCount);
        _functions.TryGetValue(overloadName, out var signature);
        return signature;
    }

    public IFunctionSignature FindOrNull(string name, int positionalArgCount, string[] namedArgNames) {
        var hasNamed = namedArgNames != null && namedArgNames.Length > 0;

        // Try exact arity first
        var totalArgs = positionalArgCount + (hasNamed ? namedArgNames.Length : 0);
        var exact = GetOrNull(name, totalArgs);
        if (exact != null)
        {
            if (!hasNamed) return exact;
            if (FunctionDictionaryHelper.HasMatchingArgProperties(exact, positionalArgCount, namedArgNames))
                return exact;
        }

        // Search overloads for match with defaults/params
        if (_overloads.TryGetValue(name, out var list))
            return FunctionDictionaryHelper.FindAmongOverloads(list, positionalArgCount, namedArgNames ?? Array.Empty<string>());
        return null;
    }

    public ImmutableFunctionDictionary CloneWith(params IFunctionSignature[] functions) {
        if (functions.Length == 0)
            return this;
        var newFunctions = new Dictionary<string, IFunctionSignature>(_functions, StringComparer.OrdinalIgnoreCase);
        var newOverloads = new Dictionary<string, List<IFunctionSignature>>(_overloads.Count);
        foreach (var (key, list) in _overloads)
            newOverloads[key] = new List<IFunctionSignature>(list);
        var dic = new ImmutableFunctionDictionary(newFunctions, newOverloads);
        foreach (var function in functions)
        {
            if (!dic.TryAdd(function))
                throw new InvalidOperationException(
                    $"function with signature {GetOverloadName(function.Name, function.ArgTypes.Length)} already exists");
        }

        return dic;
    }

    private bool TryAdd(IFunctionSignature function) {
        var name = GetOverloadName(function.Name, function.ArgTypes.Length);
        if (!_functions.TryAdd(name, function))
            return false;
        if (!_overloads.TryGetValue(function.Name, out var list))
        {
            list = new List<IFunctionSignature>();
            _overloads[function.Name] = list;
        }
        list.Add(function);
        return true;
    }

    private static string GetOverloadName(string name, int argCount)
        => name + " " + argCount;
}


internal sealed class ScopeFunctionDictionary : IFunctionDictionary {
    public ScopeFunctionDictionary(IFunctionDictionary origin) {
        _origin = origin;
        _functions = new();
        _overloads = new();
    }

    public ScopeFunctionDictionary(IFunctionDictionary origin, int scopeCapacity)
    {
        _origin = origin;
        _functions = new(scopeCapacity);
        _overloads = new();
    }

    private readonly IFunctionDictionary _origin;
    private readonly Dictionary<string, IFunctionSignature> _functions;
    private readonly Dictionary<string, List<IFunctionSignature>> _overloads;

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
            return _origin.SearchAllFunctionsIgnoreCase(name, argCount);
    }

    public IFunctionSignature GetOrNull(string name, int argCount) {
        var overloadName = GetOverloadName(name, argCount);
        _functions.TryGetValue(overloadName, out var signature);
        if (signature == null)
            return _origin.GetOrNull(name, argCount);
        return signature;
    }

    public IFunctionSignature FindOrNull(string name, int positionalArgCount, string[] namedArgNames) {
        var hasNamed = namedArgNames != null && namedArgNames.Length > 0;

        // Try exact arity in local scope
        var totalArgs = positionalArgCount + (hasNamed ? namedArgNames.Length : 0);
        var overloadName = GetOverloadName(name, totalArgs);
        if (_functions.TryGetValue(overloadName, out var localExact))
        {
            if (!hasNamed) return localExact;
            if (FunctionDictionaryHelper.HasMatchingArgProperties(localExact, positionalArgCount, namedArgNames))
                return localExact;
        }

        // Search local overloads for match with defaults/params
        if (_overloads.TryGetValue(name, out var list))
        {
            var match = FunctionDictionaryHelper.FindAmongOverloads(
                list, positionalArgCount, namedArgNames ?? Array.Empty<string>());
            if (match != null)
                return match;
        }

        // Delegate to origin
        return _origin.FindOrNull(name, positionalArgCount, namedArgNames);
    }

    public void Add(IFunctionSignature function) {
        var name = GetOverloadName(function.Name, function.ArgTypes.Length);
#if DEBUG
        if (_functions.ContainsKey(name))
            AssertChecks.Panic($"Function overload {name} already exists in function map");
#endif
        _functions.Add(name, function);
        if (!_overloads.TryGetValue(function.Name, out var list))
        {
            list = new List<IFunctionSignature>();
            _overloads[function.Name] = list;
        }
        list.Add(function);
    }

    private static string GetOverloadName(string name, int argCount)
        => name + " " + argCount;
}
