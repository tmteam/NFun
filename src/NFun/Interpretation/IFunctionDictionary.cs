using System;
using System.Collections.Generic;
using System.Linq;
using NFun.Exceptions;
using NFun.Interpretation.Functions;

namespace NFun.Interpretation; 

public interface IFunctionDictionary {
    IList<IFunctionSignature> SearchAllFunctionsIgnoreCase(string name, int argCount);
    IFunctionSignature GetOrNull(string name, int argCount);
}


internal sealed class ImmutableFunctionDictionary : IFunctionDictionary {
    private readonly Dictionary<string, IFunctionSignature> _functions;


    public ImmutableFunctionDictionary(IConcreteFunction[] concretes, GenericFunctionBase[] generics) {
        _functions = new Dictionary<string, IFunctionSignature>(concretes.Length + generics.Length);
        foreach (var concrete in concretes) TryAdd(concrete);
        foreach (var generic in generics) TryAdd(generic);
    }

    private ImmutableFunctionDictionary(Dictionary<string, IFunctionSignature> functions) {
        _functions = functions;
    }

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

    public ImmutableFunctionDictionary CloneWith(params IFunctionSignature[] functions) {
        if (functions.Length == 0)
            return this;
        var newFunctions = new Dictionary<string, IFunctionSignature>(_functions);
        var dic = new ImmutableFunctionDictionary(newFunctions);
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
        if (_functions.ContainsKey(name))
            return false;
        _functions.Add(name, function);
        return true;
    }

    private static string GetOverloadName(string name, int argCount)
        => name + " " + argCount;
}


internal sealed class ScopeFunctionDictionary : IFunctionDictionary {
    private readonly IFunctionDictionary _origin;
    private readonly Dictionary<string, IFunctionSignature> _functions = new();

    public ScopeFunctionDictionary(IFunctionDictionary origin) { _origin = origin; }

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

    public void Add(IFunctionSignature function) {
        var name = GetOverloadName(function.Name, function.ArgTypes.Length);
#if DEBUG
        if (_functions.ContainsKey(name))
            AssertChecks.Panic($"Function overload {name} already exists in function map");
#endif
        _functions.Add(name, function);
    }

    private static string GetOverloadName(string name, int argCount)
        => name + " " + argCount;
}