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
    /// Look up restricted to a specific call style (Direct = bare-call only,
    /// Extension = piped-call only). The "Both" namespace is searched in either case.
    /// </summary>
    IFunctionSignature GetOrNull(string name, int argCount, CallStyle callStyle) => GetOrNull(name, argCount);

    /// <summary>Returns true if any function with the given name exists at any arity.</summary>
    bool ContainsName(string name) => false;

    /// <summary>
    /// Finds best matching function given positional arg count and named arg names.
    /// Returns null if no match found.
    /// </summary>
    IFunctionSignature FindOrNull(string name, int positionalArgCount, string[] namedArgNames) =>
        FunctionRegistryHelper.DefaultFindOrNull(this, name, positionalArgCount, namedArgNames);

    /// <summary>
    /// Same as <see cref="FindOrNull(string,int,string[])"/> but restricted to the given call style.
    /// Used by extension-separation lookup so the fallback path doesn't leak across styles.
    /// </summary>
    IFunctionSignature FindOrNull(string name, int positionalArgCount, string[] namedArgNames, CallStyle callStyle) =>
        FindOrNull(name, positionalArgCount, namedArgNames);
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


public sealed class ImmutableFunctionRegistry : IFunctionRegistry {
    public ImmutableFunctionRegistry(IConcreteFunction[] concretes, GenericFunctionBase[] generics)
    {
        _direct = new Dictionary<string, OverloadSet>(concretes.Length + generics.Length);
        _extension = new Dictionary<string, OverloadSet>(concretes.Length + generics.Length);
        foreach (var concrete in concretes) TryAdd(concrete);
        foreach (var generic in generics) TryAdd(generic);
    }

    private ImmutableFunctionRegistry(
        Dictionary<string, OverloadSet> direct,
        Dictionary<string, OverloadSet> extension) {
        _direct = direct;
        _extension = extension;
    }

    private readonly Dictionary<string, OverloadSet> _direct;
    private readonly Dictionary<string, OverloadSet> _extension;

    public IList<IFunctionSignature> SearchAllFunctionsIgnoreCase(string name, int argCount) {
        //code used only in error handling. No need to optimize.
        var lowerName = name.ToLower();
        var results = new List<IFunctionSignature>();
        foreach (var (key, set) in _direct)
            if (key.ToLower() == lowerName)
            {
                var sig = set.GetByArity(argCount);
                if (sig != null) results.Add(sig);
            }
        foreach (var (key, set) in _extension)
            if (key.ToLower() == lowerName)
            {
                var sig = set.GetByArity(argCount);
                if (sig != null && !results.Contains(sig)) results.Add(sig);
            }
        return results;
    }

    public IFunctionSignature GetOrNull(string name, int argCount) =>
        // Search direct first, then extension. Functions with CallStyle.Both live in
        // both; the priority doesn't matter since the same instance is returned.
        (_direct.TryGetValue(name, out var dset) ? dset.GetByArity(argCount) : null)
        ?? (_extension.TryGetValue(name, out var eset) ? eset.GetByArity(argCount) : null);

    public IFunctionSignature GetOrNull(string name, int argCount, CallStyle callStyle) {
        if (callStyle == CallStyle.Extension)
            return _extension.TryGetValue(name, out var eset) ? eset.GetByArity(argCount) : null;
        // Direct or Both — direct dict.
        return _direct.TryGetValue(name, out var dset) ? dset.GetByArity(argCount) : null;
    }

    public bool ContainsName(string name) => _direct.ContainsKey(name) || _extension.ContainsKey(name);

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

        // Search overloads (direct first, then extension) for match with defaults/params.
        if (_direct.TryGetValue(name, out var dset))
        {
            var m = FunctionRegistryHelper.FindAmongOverloads(
                dset.ToList(), positionalArgCount, namedArgNames ?? Array.Empty<string>());
            if (m != null) return m;
        }
        if (_extension.TryGetValue(name, out var eset))
            return FunctionRegistryHelper.FindAmongOverloads(
                eset.ToList(), positionalArgCount, namedArgNames ?? Array.Empty<string>());
        return null;
    }

    public IFunctionSignature FindOrNull(string name, int positionalArgCount, string[] namedArgNames, CallStyle callStyle) {
        var hasNamed = namedArgNames != null && namedArgNames.Length > 0;

        var totalArgs = positionalArgCount + (hasNamed ? namedArgNames.Length : 0);
        var exact = GetOrNull(name, totalArgs, callStyle);
        if (exact != null)
        {
            if (!hasNamed) return exact;
            if (FunctionRegistryHelper.HasMatchingArgProperties(exact, positionalArgCount, namedArgNames))
                return exact;
        }

        var dict = callStyle == CallStyle.Extension ? _extension : _direct;
        if (dict.TryGetValue(name, out var set))
            return FunctionRegistryHelper.FindAmongOverloads(
                set.ToList(), positionalArgCount, namedArgNames ?? Array.Empty<string>());
        return null;
    }

    public ImmutableFunctionRegistry CloneWith(params IFunctionSignature[] functions) {
        if (functions.Length == 0)
            return this;
        var newDirect = new Dictionary<string, OverloadSet>(_direct.Count);
        foreach (var (key, set) in _direct) newDirect[key] = set.Clone();
        var newExt = new Dictionary<string, OverloadSet>(_extension.Count);
        foreach (var (key, set) in _extension) newExt[key] = set.Clone();
        var dic = new ImmutableFunctionRegistry(newDirect, newExt);
        foreach (var function in functions)
        {
            if (!dic.TryAdd(function))
                throw new InvalidOperationException(
                    $"function with signature {function.Name}/{function.ArgTypes.Length} already exists");
        }
        return dic;
    }

    private bool TryAdd(IFunctionSignature function) {
        bool ok = true;
        var cs = function.CallStyle;
        if (cs == CallStyle.Direct || cs == CallStyle.Both)
            ok &= TryAddTo(_direct, function);
        if (cs == CallStyle.Extension || cs == CallStyle.Both)
            ok &= TryAddTo(_extension, function);
        return ok;
    }

    private static bool TryAddTo(Dictionary<string, OverloadSet> dict, IFunctionSignature function) {
        if (!dict.TryGetValue(function.Name, out var set))
            set = new OverloadSet();
        if (!set.TryAdd(function))
            return false;
        dict[function.Name] = set;
        return true;
    }
}


public sealed class ScopeFunctionRegistry : IFunctionRegistry {
    public ScopeFunctionRegistry(IFunctionRegistry origin) {
        _origin = origin;
        _localDirect = new();
        _localExtension = new();
    }

    public ScopeFunctionRegistry(IFunctionRegistry origin, int scopeCapacity)
    {
        _origin = origin;
        _localDirect = new(scopeCapacity);
        _localExtension = new(scopeCapacity);
    }

    private readonly IFunctionRegistry _origin;
    private readonly Dictionary<string, OverloadSet> _localDirect;
    private readonly Dictionary<string, OverloadSet> _localExtension;

    public IList<IFunctionSignature> SearchAllFunctionsIgnoreCase(string name, int argCount) {
        //code used only in error handling. No need to optimize.
        var lowerName = name.ToLower();
        var results = new List<IFunctionSignature>();
        foreach (var (key, set) in _localDirect)
            if (key.ToLower() == lowerName)
            {
                var sig = set.GetByArity(argCount);
                if (sig != null) results.Add(sig);
            }
        foreach (var (key, set) in _localExtension)
            if (key.ToLower() == lowerName)
            {
                var sig = set.GetByArity(argCount);
                if (sig != null && !results.Contains(sig)) results.Add(sig);
            }

        if (results.Any())
            return results;
        else
            return _origin.SearchAllFunctionsIgnoreCase(name, argCount);
    }

    public IFunctionSignature GetOrNull(string name, int argCount) {
        var local = (_localDirect.TryGetValue(name, out var dset) ? dset.GetByArity(argCount) : null)
                 ?? (_localExtension.TryGetValue(name, out var eset) ? eset.GetByArity(argCount) : null);
        return local ?? _origin.GetOrNull(name, argCount);
    }

    public IFunctionSignature GetOrNull(string name, int argCount, CallStyle callStyle) {
        if (callStyle == CallStyle.Extension)
        {
            var local = _localExtension.TryGetValue(name, out var eset) ? eset.GetByArity(argCount) : null;
            return local ?? _origin.GetOrNull(name, argCount, callStyle);
        }
        {
            var local = _localDirect.TryGetValue(name, out var dset) ? dset.GetByArity(argCount) : null;
            return local ?? _origin.GetOrNull(name, argCount, callStyle);
        }
    }

    public bool ContainsName(string name) =>
        _localDirect.ContainsKey(name) || _localExtension.ContainsKey(name) || _origin.ContainsName(name);

    public IFunctionSignature FindOrNull(string name, int positionalArgCount, string[] namedArgNames) {
        var hasNamed = namedArgNames != null && namedArgNames.Length > 0;

        var totalArgs = positionalArgCount + (hasNamed ? namedArgNames.Length : 0);
        var local = (_localDirect.TryGetValue(name, out var dset) ? dset.GetByArity(totalArgs) : null)
                 ?? (_localExtension.TryGetValue(name, out var eset) ? eset.GetByArity(totalArgs) : null);
        if (local != null)
        {
            if (!hasNamed) return local;
            if (FunctionRegistryHelper.HasMatchingArgProperties(local, positionalArgCount, namedArgNames))
                return local;
        }

        // Search local overloads for match with defaults/params
        if (_localDirect.TryGetValue(name, out var dset2))
        {
            var match = FunctionRegistryHelper.FindAmongOverloads(
                dset2.ToList(), positionalArgCount, namedArgNames ?? Array.Empty<string>());
            if (match != null) return match;
        }
        if (_localExtension.TryGetValue(name, out var eset2))
        {
            var match = FunctionRegistryHelper.FindAmongOverloads(
                eset2.ToList(), positionalArgCount, namedArgNames ?? Array.Empty<string>());
            if (match != null) return match;
        }

        return _origin.FindOrNull(name, positionalArgCount, namedArgNames);
    }

    public IFunctionSignature FindOrNull(string name, int positionalArgCount, string[] namedArgNames, CallStyle callStyle) {
        var hasNamed = namedArgNames != null && namedArgNames.Length > 0;
        var totalArgs = positionalArgCount + (hasNamed ? namedArgNames.Length : 0);
        var localDict = callStyle == CallStyle.Extension ? _localExtension : _localDirect;
        IFunctionSignature local = localDict.TryGetValue(name, out var lset) ? lset.GetByArity(totalArgs) : null;
        if (local != null)
        {
            if (!hasNamed) return local;
            if (FunctionRegistryHelper.HasMatchingArgProperties(local, positionalArgCount, namedArgNames))
                return local;
        }
        if (localDict.TryGetValue(name, out var lset2))
        {
            var match = FunctionRegistryHelper.FindAmongOverloads(
                lset2.ToList(), positionalArgCount, namedArgNames ?? Array.Empty<string>());
            if (match != null) return match;
        }
        return _origin.FindOrNull(name, positionalArgCount, namedArgNames, callStyle);
    }

    public void Add(IFunctionSignature function) {
        if (function.CallStyle == CallStyle.Direct || function.CallStyle == CallStyle.Both)
            AddTo(_localDirect, function.Name, function);
        if (function.CallStyle == CallStyle.Extension || function.CallStyle == CallStyle.Both)
            AddTo(_localExtension, function.Name, function);
    }

    /// <summary>
    /// Add a function under a specific registry key (may differ from function.Name for extension functions
    /// when the caller uses the legacy <c>.</c>-prefixed key). The prefix is stripped and the function
    /// placed into the appropriate physical dictionary based on its <see cref="CallStyle"/>.
    /// </summary>
    public void Add(string registryKey, IFunctionSignature function) {
        // Legacy prefix handling: ".f" goes to extension dict under bare name, "f" to direct dict.
        // New callers should use Add(function) — the prefix path stays for backward compat.
        string bareName = registryKey.StartsWith('.') ? registryKey.Substring(1) : registryKey;
        bool routeToExtension = registryKey.StartsWith('.') || function.CallStyle == CallStyle.Extension;
        bool routeToDirect = !registryKey.StartsWith('.')
            && (function.CallStyle == CallStyle.Direct || function.CallStyle == CallStyle.Both);
        if (routeToDirect)
            AddTo(_localDirect, bareName, function);
        if (routeToExtension || function.CallStyle == CallStyle.Both)
            AddTo(_localExtension, bareName, function);
    }

    private static void AddTo(Dictionary<string, OverloadSet> dict, string key, IFunctionSignature function) {
        if (!dict.TryGetValue(key, out var set))
            set = new OverloadSet();
#if DEBUG
        if (set.GetByArity(function.ArgTypes.Length) != null)
            AssertChecks.Panic($"Function overload {key}/{function.ArgTypes.Length} already exists in function map");
#endif
        set.TryAdd(function);
        dict[key] = set;
    }
}
