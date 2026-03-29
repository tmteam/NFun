using System;
using System.Collections.Generic;
using System.Linq;
using NFun.Exceptions;
using NFun.Interpretation.Functions;

namespace NFun.Interpretation;

public interface IFunctionRegistry {
    IList<IFunctionSignature> SearchAllFunctionsIgnoreCase(string name, int argCount);
    IFunctionSignature GetOrNull(string name, int argCount);

    /// <summary>
    /// Finds best matching function given positional arg count and named arg names.
    /// Returns null if no match found.
    /// </summary>
    IFunctionSignature FindOrNull(string name, int positionalArgCount, string[] namedArgNames) =>
        FunctionRegistryHelper.DefaultFindOrNull(this, name, positionalArgCount, namedArgNames);
}

/// <summary>
/// Compact storage for all overloads of a single function name.
/// Inline fields for arity 0-4 (covers 99%+ of functions), overflow dict for exotic arities.
/// </summary>
internal struct OverloadSet {
    private IFunctionSignature _a0, _a1, _a2, _a3, _a4;
    private Dictionary<int, IFunctionSignature> _overflow;

    public IFunctionSignature GetByArity(int argCount) => argCount switch {
        0 => _a0, 1 => _a1, 2 => _a2, 3 => _a3, 4 => _a4,
        _ => _overflow != null && _overflow.TryGetValue(argCount, out var s) ? s : null
    };

    /// <summary>Returns false if arity slot already occupied.</summary>
    public bool TryAdd(IFunctionSignature sig) {
        var arity = sig.ArgTypes.Length;
        switch (arity) {
            case 0: if (_a0 != null) return false; _a0 = sig; return true;
            case 1: if (_a1 != null) return false; _a1 = sig; return true;
            case 2: if (_a2 != null) return false; _a2 = sig; return true;
            case 3: if (_a3 != null) return false; _a3 = sig; return true;
            case 4: if (_a4 != null) return false; _a4 = sig; return true;
            default:
                _overflow ??= new Dictionary<int, IFunctionSignature>();
                return _overflow.TryAdd(arity, sig);
        }
    }

    /// <summary>Iterates all registered overloads (non-null).</summary>
    public void ForEach(Action<IFunctionSignature> action) {
        if (_a0 != null) action(_a0);
        if (_a1 != null) action(_a1);
        if (_a2 != null) action(_a2);
        if (_a3 != null) action(_a3);
        if (_a4 != null) action(_a4);
        if (_overflow != null)
            foreach (var kv in _overflow) action(kv.Value);
    }

    /// <summary>Collects all overloads into a list. Used for FindAmongOverloads.</summary>
    public List<IFunctionSignature> ToList() {
        var list = new List<IFunctionSignature>();
        ForEach(list.Add);
        return list;
    }

    public OverloadSet Clone() {
        var copy = this; // struct copy — inline fields copied
        if (_overflow != null)
            copy._overflow = new Dictionary<int, IFunctionSignature>(_overflow);
        return copy;
    }

}

/// <summary>
/// Shared resolution logic for FindOrNull across registry implementations.
/// </summary>
internal static class FunctionRegistryHelper {
    public static IFunctionSignature DefaultFindOrNull(
        IFunctionRegistry dict, string name, int positionalArgCount, string[] namedArgNames) {
        var hasNamed = namedArgNames != null && namedArgNames.Length > 0;

        // Try exact arity match first
        var totalArgs = positionalArgCount + (hasNamed ? namedArgNames.Length : 0);
        var exact = dict.GetOrNull(name, totalArgs);
        if (exact != null)
        {
            if (!hasNamed) return exact;
            if (HasMatchingArgProperties(exact, positionalArgCount, namedArgNames))
                return exact;
        }

        return null;
    }

    /// <summary>
    /// Finds best match among overloads, considering defaults and params.
    /// </summary>
    public static IFunctionSignature FindAmongOverloads(
        List<IFunctionSignature> overloads,
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
                        return false;
                    found = true;
                    break;
                }
            }
            if (!found)
                return false;
        }
        return true;
    }

    private static bool FitsWithDefaults(
        IFunctionSignature sig, int positionalArgCount, string[] namedArgNames) {
        var props = sig.ArgProperties;
        var paramCount = props.Length;
        bool hasParams = paramCount > 0 && props[^1].IsParams;
        var nonParamsCount = hasParams ? paramCount - 1 : paramCount;

        if (!hasParams && positionalArgCount > paramCount)
            return false;

        foreach (var namedName in namedArgNames)
        {
            var found = false;
            for (int i = 0; i < paramCount; i++)
            {
                if (string.Equals(props[i].Name, namedName, StringComparison.OrdinalIgnoreCase))
                {
                    if (i < positionalArgCount)
                        return false;
                    found = true;
                    break;
                }
            }
            if (!found)
                return false;
        }

        for (int i = 0; i < nonParamsCount; i++)
        {
            if (i < positionalArgCount)
                continue;
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


internal sealed class ImmutableFunctionRegistry : IFunctionRegistry {
    public ImmutableFunctionRegistry(IConcreteFunction[] concretes, GenericFunctionBase[] generics)
    {
        _registry = new Dictionary<string, OverloadSet>(concretes.Length + generics.Length);
        foreach (var concrete in concretes) TryAdd(concrete);
        foreach (var generic in generics) TryAdd(generic);
    }

    private ImmutableFunctionRegistry(Dictionary<string, OverloadSet> registry) =>
        _registry = registry;

    private readonly Dictionary<string, OverloadSet> _registry;

    public IList<IFunctionSignature> SearchAllFunctionsIgnoreCase(string name, int argCount) {
        //code used only in error handling. No need to optimize.
        var lowerName = name.ToLower();
        var results = new List<IFunctionSignature>();
        foreach (var (key, set) in _registry)
        {
            if (key.ToLower() == lowerName)
            {
                var sig = set.GetByArity(argCount);
                if (sig != null)
                    results.Add(sig);
            }
        }
        return results;
    }

    public IFunctionSignature GetOrNull(string name, int argCount) =>
        _registry.TryGetValue(name, out var set) ? set.GetByArity(argCount) : null;

    public IFunctionSignature FindOrNull(string name, int positionalArgCount, string[] namedArgNames) {
        var hasNamed = namedArgNames != null && namedArgNames.Length > 0;

        var totalArgs = positionalArgCount + (hasNamed ? namedArgNames.Length : 0);
        var exact = GetOrNull(name, totalArgs);
        if (exact != null)
        {
            if (!hasNamed) return exact;
            if (FunctionRegistryHelper.HasMatchingArgProperties(exact, positionalArgCount, namedArgNames))
                return exact;
        }

        // Search overloads for match with defaults/params
        if (_registry.TryGetValue(name, out var set))
            return FunctionRegistryHelper.FindAmongOverloads(
                set.ToList(), positionalArgCount, namedArgNames ?? Array.Empty<string>());
        return null;
    }

    public ImmutableFunctionRegistry CloneWith(params IFunctionSignature[] functions) {
        if (functions.Length == 0)
            return this;
        var newRegistry = new Dictionary<string, OverloadSet>(_registry.Count, StringComparer.OrdinalIgnoreCase);
        foreach (var (key, set) in _registry)
            newRegistry[key] = set.Clone();
        var dic = new ImmutableFunctionRegistry(newRegistry);
        foreach (var function in functions)
        {
            if (!dic.TryAdd(function))
                throw new InvalidOperationException(
                    $"function with signature {function.Name}/{function.ArgTypes.Length} already exists");
        }
        return dic;
    }

    private bool TryAdd(IFunctionSignature function) {
        if (!_registry.TryGetValue(function.Name, out var set))
            set = new OverloadSet();
        if (!set.TryAdd(function))
            return false;
        _registry[function.Name] = set;
        return true;
    }
}


internal sealed class ScopeFunctionRegistry : IFunctionRegistry {
    public ScopeFunctionRegistry(IFunctionRegistry origin) {
        _origin = origin;
        _local = new();
    }

    public ScopeFunctionRegistry(IFunctionRegistry origin, int scopeCapacity)
    {
        _origin = origin;
        _local = new(scopeCapacity);
    }

    private readonly IFunctionRegistry _origin;
    private readonly Dictionary<string, OverloadSet> _local;

    public IList<IFunctionSignature> SearchAllFunctionsIgnoreCase(string name, int argCount) {
        //code used only in error handling. No need to optimize.
        var lowerName = name.ToLower();
        var results = new List<IFunctionSignature>();
        foreach (var (key, set) in _local)
        {
            if (key.ToLower() == lowerName)
            {
                var sig = set.GetByArity(argCount);
                if (sig != null)
                    results.Add(sig);
            }
        }

        if (results.Any())
            return results;
        else
            return _origin.SearchAllFunctionsIgnoreCase(name, argCount);
    }

    public IFunctionSignature GetOrNull(string name, int argCount) {
        if (_local.TryGetValue(name, out var set))
        {
            var sig = set.GetByArity(argCount);
            if (sig != null) return sig;
        }
        return _origin.GetOrNull(name, argCount);
    }

    public IFunctionSignature FindOrNull(string name, int positionalArgCount, string[] namedArgNames) {
        var hasNamed = namedArgNames != null && namedArgNames.Length > 0;

        var totalArgs = positionalArgCount + (hasNamed ? namedArgNames.Length : 0);
        if (_local.TryGetValue(name, out var localSet))
        {
            var localExact = localSet.GetByArity(totalArgs);
            if (localExact != null)
            {
                if (!hasNamed) return localExact;
                if (FunctionRegistryHelper.HasMatchingArgProperties(localExact, positionalArgCount, namedArgNames))
                    return localExact;
            }

            // Search local overloads for match with defaults/params
            var match = FunctionRegistryHelper.FindAmongOverloads(
                localSet.ToList(), positionalArgCount, namedArgNames ?? Array.Empty<string>());
            if (match != null)
                return match;
        }

        return _origin.FindOrNull(name, positionalArgCount, namedArgNames);
    }

    public void Add(IFunctionSignature function) {
        if (!_local.TryGetValue(function.Name, out var set))
            set = new OverloadSet();
#if DEBUG
        if (set.GetByArity(function.ArgTypes.Length) != null)
            AssertChecks.Panic($"Function overload {function.Name}/{function.ArgTypes.Length} already exists in function map");
#endif
        set.TryAdd(function);
        _local[function.Name] = set;
    }
}
