using System.Collections.Generic;
using System.Linq;
using NFun.Interpretation.Functions;
using NFun.Runtime.Arrays;
using NFun.Runtime.Lists;
using NFun.Types;

namespace NFun.Functions.Collections;

// Stage C — the `toXxx(...)` collection-conversion family. Each function:
//   * takes any IFunnyEnumerable<T> (Enumerable<T> typeclass) on the input,
//   * builds a fresh container of the named kind and copies elements over.
// Element type is preserved (invariant). Duplicates collapse for `toSet`.
//
// The body works against the IFunnyEnumerable element-by-element view, so it
// uniformly handles ee `T[]` / lang `list<T>` / `array<T>` / `fixedArray<T>` /
// `set<T>` and future collection kinds.

internal static class ToXxxRuntimeIter {
    public static IEnumerable<object> AsObjects(object o) {
        switch (o) {
            case IFunnyArray a: return a;
            case IFunnyEnumerable e: return e;
            // FunnyNone (default for optional-typed slot) — treat as empty.
            case FunnyNone _: return System.Linq.Enumerable.Empty<object>();
            case null: return System.Linq.Enumerable.Empty<object>();
            // Bare CLR System.Array (passed by host code via the typed-input API)
            // — wrap as IEnumerable<object> so the same iterator path applies.
            case System.Collections.IEnumerable enumerable when o is not string:
                return enumerable.Cast<object>();
            default:
                throw new Exceptions.FunnyRuntimeException(
                    $"toXxx: unsupported collection shape {o?.GetType()}");
        }
    }
}

public class ToListFunction : GenericFunctionBase {
    public ToListFunction() : base(
        "toList",
        FunnyType.ListOf(FunnyType.Generic(0)),
        FunnyType.EnumerableOf(FunnyType.Generic(0)))
        => ArgProperties = FunArgProperty.FromNames("xs");

    public override IConcreteFunction CreateConcrete(FunnyType[] concrete, IFunctionSelectorContext _) {
        var elem = concrete[0];
        return new Impl(elem);
    }

    private sealed class Impl : FunctionWithSingleArg {
        private readonly FunnyType _elem;
        public Impl(FunnyType elem)
            : base("toList", FunnyType.ListOf(elem), FunnyType.EnumerableOf(elem)) => _elem = elem;
        public override object Calc(object a) =>
            new MutableFunnyList(_elem, ToXxxRuntimeIter.AsObjects(a));
    }
}

public class ToArrayFunction : GenericFunctionBase {
    public ToArrayFunction() : base(
        "toArray",
        FunnyType.MutableArrayOf(FunnyType.Generic(0)),
        FunnyType.EnumerableOf(FunnyType.Generic(0)))
        => ArgProperties = FunArgProperty.FromNames("xs");

    public override IConcreteFunction CreateConcrete(FunnyType[] concrete, IFunctionSelectorContext _) {
        var elem = concrete[0];
        return new Impl(elem);
    }

    private sealed class Impl : FunctionWithSingleArg {
        private readonly FunnyType _elem;
        public Impl(FunnyType elem)
            : base("toArray", FunnyType.MutableArrayOf(elem), FunnyType.EnumerableOf(elem)) => _elem = elem;
        public override object Calc(object a) {
            // Materialize once so we know the length without enumerating twice.
            var buf = new System.Collections.Generic.List<object>();
            foreach (var it in ToXxxRuntimeIter.AsObjects(a)) buf.Add(it);
            return new MutableFunnyArray(_elem, buf.ToArray());
        }
    }
}

public class ToFixedArrayFunction : GenericFunctionBase {
    public ToFixedArrayFunction() : base(
        "toFixedArray",
        FunnyType.FixedArrayOf(FunnyType.Generic(0)),
        FunnyType.EnumerableOf(FunnyType.Generic(0)))
        => ArgProperties = FunArgProperty.FromNames("xs");

    public override IConcreteFunction CreateConcrete(FunnyType[] concrete, IFunctionSelectorContext _) {
        var elem = concrete[0];
        return new Impl(elem);
    }

    private sealed class Impl : FunctionWithSingleArg {
        private readonly FunnyType _elem;
        public Impl(FunnyType elem)
            : base("toFixedArray", FunnyType.FixedArrayOf(elem), FunnyType.EnumerableOf(elem)) => _elem = elem;
        public override object Calc(object a) {
            var buf = new System.Collections.Generic.List<object>();
            foreach (var it in ToXxxRuntimeIter.AsObjects(a)) buf.Add(it);
            return new FixedFunnyArray(_elem, buf.ToArray());
        }
    }
}

public class ToSetFunction : GenericFunctionBase {
    public ToSetFunction() : base(
        "toSet",
        FunnyType.SetOf(FunnyType.Generic(0)),
        FunnyType.EnumerableOf(FunnyType.Generic(0)))
        => ArgProperties = FunArgProperty.FromNames("xs");

    public override IConcreteFunction CreateConcrete(FunnyType[] concrete, IFunctionSelectorContext _) {
        var elem = concrete[0];
        // Set element type must be Immutable (per Collections.md typeclass table).
        // The factory `set(...)` already enforces this; `.toSet()` conversion must
        // too, otherwise users sneak past with `[{a=1}].toSet()` → `set<{a:Int32}>`
        // which violates the Immutable contract. Bug hunt round 6 #31.
        ImmutableTypePredicate.RequireImmutable(elem, "toSet", "element");
        return new Impl(elem);
    }

    private sealed class Impl : FunctionWithSingleArg {
        private readonly FunnyType _elem;
        public Impl(FunnyType elem)
            : base("toSet", FunnyType.SetOf(elem), FunnyType.EnumerableOf(elem)) => _elem = elem;
        public override object Calc(object a) =>
            new MutableFunnySet(_elem, ToXxxRuntimeIter.AsObjects(a));
    }
}
