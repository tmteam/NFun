using System.Collections;
using System.Linq;
using NFun.Exceptions;
using NFun.Interpretation.Functions;
using NFun.Runtime;
using NFun.Runtime.Lists;
using NFun.Types;

namespace NFun.Functions.Collections;

// Stage 3 / B.1: mutation API for lang-mode `list<T>`. Each function is
// generic in T and constrained on its first argument's signature shape
// (`list<T>`) — TIC rejects callers passing `T[]` (ee-mode array) or
// `fixedArray<T>` since neither fits the list signature (lattice subtype
// runs the other direction).

/// <summary>
/// <c>add(list&lt;T&gt;, T)</c> — appends an element. Returns <c>none</c>
/// (statement-shaped). Mutates the list in place; aliases see the change.
/// </summary>
public class ListAddFunction : GenericFunctionWithTwoArguments {
    public ListAddFunction() : base(
        "add",
        FunnyType.None,
        FunnyType.ListOf(FunnyType.Generic(0)),
        FunnyType.Generic(0)) {
        ArgProperties = FunArgProperty.FromNames("list", "item");
    }

    protected override object Calc(object a, object b) {
        if (a is not MutableFunnyList list)
            throw new FunnyRuntimeException("add() requires a mutable list");
        list.Add(b);
        return FunnyNone.Instance;
    }
}

/// <summary>
/// <c>addAll(list&lt;T&gt;, list&lt;T&gt;)</c> — appends every element of the
/// second list. Returns <c>none</c>. Note: second arg accepts list-typed values
/// only (Stage 0); future `Enumerable<T>` migration generalises this.
/// </summary>
public class ListAddAllFunction : GenericFunctionWithTwoArguments {
    public ListAddAllFunction() : base(
        "addAll",
        FunnyType.None,
        FunnyType.ListOf(FunnyType.Generic(0)),
        FunnyType.ListOf(FunnyType.Generic(0))) {
        ArgProperties = FunArgProperty.FromNames("list", "items");
    }

    protected override object Calc(object a, object b) {
        if (a is not MutableFunnyList target)
            throw new FunnyRuntimeException("addAll() requires a mutable list");
        if (b is not IEnumerable source)
            throw new FunnyRuntimeException("addAll() requires an iterable source");
        target.AddAll(source.Cast<object>());
        return FunnyNone.Instance;
    }
}

/// <summary>
/// <c>remove(list&lt;T&gt;, T): bool</c> — removes the first occurrence of the
/// element. Returns true when found and removed.
/// </summary>
public class ListRemoveFunction : GenericFunctionWithTwoArguments {
    public ListRemoveFunction() : base(
        "remove",
        FunnyType.Bool,
        FunnyType.ListOf(FunnyType.Generic(0)),
        FunnyType.Generic(0)) {
        ArgProperties = FunArgProperty.FromNames("list", "item");
    }

    protected override object Calc(object a, object b) {
        if (a is not MutableFunnyList list)
            throw new FunnyRuntimeException("remove() requires a mutable list");
        return list.Remove(b);
    }
}

/// <summary>
/// <c>removeAt(list&lt;T&gt;, int): T?</c> — removes the element at index,
/// returns it as <c>T?</c> (<c>none</c> when out of range).
/// </summary>
public class ListRemoveAtFunction : GenericFunctionWithTwoArguments {
    public ListRemoveAtFunction() : base(
        "removeAt",
        FunnyType.OptionalOf(FunnyType.Generic(0)),
        FunnyType.ListOf(FunnyType.Generic(0)),
        FunnyType.Int32) {
        ArgProperties = FunArgProperty.FromNames("list", "index");
    }

    protected override object Calc(object a, object b) {
        if (a is not MutableFunnyList list)
            throw new FunnyRuntimeException("removeAt() requires a mutable list");
        var idx = (int)b;
        var removed = list.RemoveAt(idx);
        return removed ?? (object)FunnyNone.Instance;
    }
}

/// <summary>
/// <c>removeLast(list&lt;T&gt;): T?</c> — removes the last element, returns
/// it as <c>T?</c> (<c>none</c> on empty list).
/// </summary>
public class ListRemoveLastFunction : GenericFunctionWithSingleArgument {
    public ListRemoveLastFunction() : base(
        "removeLast",
        FunnyType.OptionalOf(FunnyType.Generic(0)),
        FunnyType.ListOf(FunnyType.Generic(0))) {
        ArgProperties = FunArgProperty.FromNames("list");
    }

    protected override object Calc(object a) {
        if (a is not MutableFunnyList list)
            throw new FunnyRuntimeException("removeLast() requires a mutable list");
        var removed = list.RemoveLast();
        return removed ?? (object)FunnyNone.Instance;
    }
}

/// <summary>
/// <c>clear(Mutable&lt;T&gt;)</c> — drops every element from any mutable
/// collection (list / set / future queue). TIC rejects callers passing
/// fixedArray, ee-mode T[], or Enumerable — those don't satisfy the Mutable
/// typeclass constraint, so the error fires at parse time.
/// </summary>
public class ListClearFunction : GenericFunctionWithSingleArgument {
    public ListClearFunction() : base(
        "clear",
        FunnyType.None,
        FunnyType.MutableOf(FunnyType.Generic(0))) {
        ArgProperties = FunArgProperty.FromNames("xs");
    }

    protected override object Calc(object a) {
        switch (a) {
            case MutableFunnyList list: list.Clear(); return FunnyNone.Instance;
            case IFunnyMutableSet set: set.Clear(); return FunnyNone.Instance;
            case NFun.Runtime.Lists.IFunnyMutableArray:
                // Element-mutable but length-fixed — silent no-op would be
                // wrong; we should never get here when TIC accepts the call
                // (Mutable typeclass admits MutableArray). Document and throw.
                throw new FunnyRuntimeException(
                    "clear() on `array<T>` is not supported — array length is fixed");
            default:
                throw new FunnyRuntimeException(
                    $"clear() requires a mutable collection (list or set); got {a?.GetType().Name ?? "null"}");
        }
    }
}
