using NFun.Exceptions;
using NFun.Interpretation.Functions;
using NFun.Runtime;
using NFun.Runtime.Lists;
using NFun.Types;

namespace NFun.Functions.Collections;

// Stage 5 / Map.2: access and mutation API for lang-mode `map<K, V>`. The
// surface mirrors `Dictionary<K, V>` ergonomics from .NET/Java with two
// distinct shapes for "lookup that may fail":
//
//   * `get(m, k): V?`                — Optional return — natural fallback via `??`
//   * `tryGet(m, k): {value, success}` — struct return — disambiguates
//      "absent" vs "present but null/none" when V is itself an optional type
//
// Same dual-shape pattern for removal: `removeKey` (Optional) vs `tryRemove`
// (struct). All are 2- or 3-arg extension-style functions taking the map as
// the first argument.

/// <summary>
/// <c>setKey(m: map&lt;K, V&gt;, k: K, v: V)</c> — set or overwrite the value
/// for <paramref name="k"/>. Returns <c>none</c> (mutation-only).
/// </summary>
public class MapSetKeyFunction : GenericFunctionBase {
    public MapSetKeyFunction() : base(
        "setKey",
        FunnyType.None,
        FunnyType.MapOf(FunnyType.Generic(0), FunnyType.Generic(1)),
        FunnyType.Generic(0),
        FunnyType.Generic(1)) {
        ArgProperties = FunArgProperty.FromNames("map", "key", "value");
    }

    protected override object Calc(object[] args) {
        if (args[0] is not IFunnyMap m)
            throw new FunnyRuntimeException("setKey() requires a mutable map");
        m.Set(args[1], args[2]);
        return FunnyNone.Instance;
    }
}

/// <summary>
/// <c>tryAddKey(m: map&lt;K, V&gt;, k: K, v: V): bool</c> — insert only if
/// <paramref name="k"/> is not already present. Returns <c>true</c> when
/// added, <c>false</c> when the key already existed (no overwrite).
/// </summary>
public class MapTryAddKeyFunction : GenericFunctionBase {
    public MapTryAddKeyFunction() : base(
        "tryAddKey",
        FunnyType.Bool,
        FunnyType.MapOf(FunnyType.Generic(0), FunnyType.Generic(1)),
        FunnyType.Generic(0),
        FunnyType.Generic(1)) {
        ArgProperties = FunArgProperty.FromNames("map", "key", "value");
    }

    protected override object Calc(object[] args) {
        if (args[0] is not IFunnyMap m)
            throw new FunnyRuntimeException("tryAddKey() requires a mutable map");
        if (m.ContainsKey(args[1])) return false;
        m.Set(args[1], args[2]);
        return true;
    }
}

/// <summary>
/// <c>removeKey(m: map&lt;K, V&gt;, k: K): V?</c> — remove entry by key.
/// Returns the removed value as <c>V?</c>, or <c>none</c> when the key was
/// absent.
/// </summary>
public class MapRemoveKeyFunction : GenericFunctionWithTwoArguments {
    public MapRemoveKeyFunction() : base(
        "removeKey",
        FunnyType.OptionalOf(FunnyType.Generic(1)),
        FunnyType.MapOf(FunnyType.Generic(0), FunnyType.Generic(1)),
        FunnyType.Generic(0)) {
        ArgProperties = FunArgProperty.FromNames("map", "key");
    }

    protected override object Calc(object a, object b) {
        if (a is not IFunnyMap m)
            throw new FunnyRuntimeException("removeKey() requires a mutable map");
        var v = m.GetOrNull(b);
        if (v == null) return FunnyNone.Instance;
        m.Remove(b);
        return v;
    }
}

/// <summary>
/// <c>containsKey(m: map&lt;K, V&gt;, k: K): bool</c> — true iff
/// <paramref name="k"/> is a member of <paramref name="m"/>.
/// </summary>
public class MapContainsKeyFunction : GenericFunctionWithTwoArguments {
    public MapContainsKeyFunction() : base(
        "containsKey",
        FunnyType.Bool,
        FunnyType.MapOf(FunnyType.Generic(0), FunnyType.Generic(1)),
        FunnyType.Generic(0)) {
        ArgProperties = FunArgProperty.FromNames("map", "key");
    }

    protected override object Calc(object a, object b) {
        if (a is not IFunnyMap m)
            throw new FunnyRuntimeException("containsKey() requires a map");
        return m.ContainsKey(b);
    }
}

/// <summary>
/// <c>get(m: map&lt;K, V&gt;, k: K): V?</c> — lookup. Returns the value as
/// <c>V?</c>, or <c>none</c> when the key is absent. Natural fallback usage:
/// <c>m.get(k) ?? defaultValue</c>.
/// </summary>
public class MapGetFunction : GenericFunctionWithTwoArguments {
    public MapGetFunction() : base(
        "get",
        FunnyType.OptionalOf(FunnyType.Generic(1)),
        FunnyType.MapOf(FunnyType.Generic(0), FunnyType.Generic(1)),
        FunnyType.Generic(0)) {
        ArgProperties = FunArgProperty.FromNames("map", "key");
    }

    protected override object Calc(object a, object b) {
        if (a is not IFunnyMap m)
            throw new FunnyRuntimeException("get() requires a map");
        var v = m.GetOrNull(b);
        return v ?? (object)FunnyNone.Instance;
    }
}

/// <summary>
/// <c>tryGet(m: map&lt;K, V&gt;, k: K): {value:V, success:bool}</c> —
/// struct-shaped lookup. Disambiguates "key absent" (success=false) from "key
/// present with a none-valued V" (success=true, value=none).
///
/// When <c>success=false</c>, <c>value</c> carries the type's default
/// (mirrors the C# <c>TryGetValue(out var v)</c> pattern).
/// </summary>
public class MapTryGetFunction : GenericFunctionWithTwoArguments {
    public MapTryGetFunction() : base(
        "tryGet",
        FunnyType.StructOf(
            ("value", FunnyType.Generic(1)),
            ("success", FunnyType.Bool)),
        FunnyType.MapOf(FunnyType.Generic(0), FunnyType.Generic(1)),
        FunnyType.Generic(0)) {
        ArgProperties = FunArgProperty.FromNames("map", "key");
    }

    protected override object Calc(object a, object b) {
        if (a is not IFunnyMap m)
            throw new FunnyRuntimeException("tryGet() requires a map");
        bool found = m.ContainsKey(b);
        object v = found ? m.GetOrNull(b) : m.ValueType.GetDefaultFunnyValue();
        return FunnyStruct.Create(("value", v), ("success", found));
    }
}

/// <summary>
/// <c>tryRemoveKey(m: map&lt;K, V&gt;, k: K): {value:V, success:bool}</c> —
/// struct-shaped removal. Same disambiguation rationale as
/// <see cref="MapTryGetFunction"/>: when <c>success=false</c>, <c>value</c>
/// carries the type's default.
/// <para>Named with the <c>Key</c> suffix to avoid collision with
/// <c>set.tryRemove(item): bool</c> — both would land at the same
/// <c>(name, arity)</c> slot otherwise. The user-facing convention "Map
/// operations end with <c>Key</c>" remains uniform for setKey / tryAddKey /
/// removeKey / containsKey / tryRemoveKey.</para>
/// </summary>
public class MapTryRemoveFunction : GenericFunctionWithTwoArguments {
    public MapTryRemoveFunction() : base(
        "tryRemoveKey",
        FunnyType.StructOf(
            ("value", FunnyType.Generic(1)),
            ("success", FunnyType.Bool)),
        FunnyType.MapOf(FunnyType.Generic(0), FunnyType.Generic(1)),
        FunnyType.Generic(0)) {
        ArgProperties = FunArgProperty.FromNames("map", "key");
    }

    protected override object Calc(object a, object b) {
        if (a is not IFunnyMap m)
            throw new FunnyRuntimeException("tryRemove() requires a mutable map");
        bool found = m.ContainsKey(b);
        object v;
        if (found) {
            v = m.GetOrNull(b);
            m.Remove(b);
        } else {
            v = m.ValueType.GetDefaultFunnyValue();
        }
        return FunnyStruct.Create(("value", v), ("success", found));
    }
}
