using System;
using System.Linq;
using NFun.Exceptions;
using NFun.Functions.Collections;
using NFun.Interpretation.Functions;
using NFun.Runtime.Arrays;
using NFun.Runtime.Lists;
using NFun.Types;

namespace NFun.Functions.Lang;

// Lang-mode LINQ functions. Mirror ee-mode versions in ArrayGenericFunctions.cs
// but widen inputs to Enumerable<T> (accepts list/array/fixedArray/set/map) and
// return FixedArray<T> (immutable LINQ result — consistent with MapEnumerableFunction).
//
// Function name (per TIC registry key `name+arity`) is identical to ee-mode — TIC
// dispatches per-mode registry (lang uses these, ee uses the legacy ones).
//
// Bug hunt round 6 #32 follow-up: closes Bug 32 full spec
// `arr?.sort().reverse() ?? [0]` by having sort/reverse return FixedArray
// (a StateCollection, not legacy StateArray) — so the LCA at the `??` join
// goes through the round-6 cross-Constructor StateCollection path and widens
// to FixedArray instead of collapsing to Any.

public class SortEnumerableFunction : GenericFunctionBase {
    public SortEnumerableFunction() : base(
        "sort", GenericConstrains.Comparable,
        FunnyType.FixedArrayOf(FunnyType.Generic(0)),
        FunnyType.EnumerableOf(FunnyType.Generic(0))) { ArgProperties = FunArgProperty.FromNames("arr"); }

    protected override object Calc(object[] args) {
        var src = ToXxxRuntimeIter.AsObjects(args[0]);
        var arr = src.Cast<IComparable>().ToArray();
        Array.Sort(arr);
        var elemType = ReturnType.FixedArrayTypeSpecification.FunnyType;
        return new FixedFunnyArray(elemType, arr.Cast<object>().ToArray());
    }
}

public class SortDescendingEnumerableFunction : GenericFunctionBase {
    public SortDescendingEnumerableFunction() : base(
        "sortDescending", GenericConstrains.Comparable,
        FunnyType.FixedArrayOf(FunnyType.Generic(0)),
        FunnyType.EnumerableOf(FunnyType.Generic(0))) { ArgProperties = FunArgProperty.FromNames("arr"); }

    protected override object Calc(object[] args) {
        var src = ToXxxRuntimeIter.AsObjects(args[0]);
        var arr = src.Cast<IComparable>().ToArray();
        Array.Sort(arr);
        Array.Reverse(arr);
        var elemType = ReturnType.FixedArrayTypeSpecification.FunnyType;
        return new FixedFunnyArray(elemType, arr.Cast<object>().ToArray());
    }
}

public class SortMapEnumerableFunction : GenericFunctionBase {
    public SortMapEnumerableFunction() : base(
        "sort", new[] { GenericConstrains.Any, GenericConstrains.Comparable },
        FunnyType.FixedArrayOf(FunnyType.Generic(0)),
        FunnyType.EnumerableOf(FunnyType.Generic(0)),
        FunnyType.FunOf(FunnyType.Generic(1), FunnyType.Generic(0))) { ArgProperties = FunArgProperty.FromNames("arr", "selector"); }

    protected override object Calc(object[] args) {
        var src = ToXxxRuntimeIter.AsObjects(args[0]);
        var map = (IConcreteFunction)args[1];
        var sorted = src.OrderBy(a => (IComparable)map.Calc(new[] { a })).ToArray();
        var elemType = ReturnType.FixedArrayTypeSpecification.FunnyType;
        return new FixedFunnyArray(elemType, sorted);
    }
}

public class SortMapDescendingEnumerableFunction : GenericFunctionBase {
    public SortMapDescendingEnumerableFunction() : base(
        "sortDescending", new[] { GenericConstrains.Any, GenericConstrains.Comparable },
        FunnyType.FixedArrayOf(FunnyType.Generic(0)),
        FunnyType.EnumerableOf(FunnyType.Generic(0)),
        FunnyType.FunOf(FunnyType.Generic(1), FunnyType.Generic(0))) { ArgProperties = FunArgProperty.FromNames("arr", "selector"); }

    protected override object Calc(object[] args) {
        var src = ToXxxRuntimeIter.AsObjects(args[0]);
        var map = (IConcreteFunction)args[1];
        var sorted = src.OrderByDescending(a => (IComparable)map.Calc(new[] { a })).ToArray();
        var elemType = ReturnType.FixedArrayTypeSpecification.FunnyType;
        return new FixedFunnyArray(elemType, sorted);
    }
}

public class ReverseEnumerableFunction : GenericFunctionWithSingleArgument {
    public ReverseEnumerableFunction() : base(
        "reverse",
        FunnyType.FixedArrayOf(FunnyType.Generic(0)),
        FunnyType.EnumerableOf(FunnyType.Generic(0))) { ArgProperties = FunArgProperty.FromNames("arr"); }

    protected override object Calc(object a) {
        var src = ToXxxRuntimeIter.AsObjects(a).ToArray();
        Array.Reverse(src);
        var elemType = ReturnType.FixedArrayTypeSpecification.FunnyType;
        return new FixedFunnyArray(elemType, src);
    }
}

public class FilterEnumerableFunction : GenericFunctionBase {
    public FilterEnumerableFunction() : base(
        "filter",
        FunnyType.FixedArrayOf(FunnyType.Generic(0)),
        FunnyType.EnumerableOf(FunnyType.Generic(0)),
        FunnyType.FunOf(FunnyType.Bool, FunnyType.Generic(0))) { ArgProperties = FunArgProperty.FromNames("arr", "predicate"); }

    public override IConcreteFunction CreateConcrete(FunnyType[] concrete, IFunctionSelectorContext _) {
        var elem = concrete[0];
        return new Impl(elem);
    }

    private sealed class Impl : FunctionWithTwoArgs {
        private readonly FunnyType _elem;
        public Impl(FunnyType elem) : base("filter",
            FunnyType.FixedArrayOf(elem),
            FunnyType.EnumerableOf(elem),
            FunnyType.FunOf(FunnyType.Bool, elem)) => _elem = elem;
        public override object Calc(object a, object b) {
            if (a is FunnyNone)
                return new FixedFunnyArray(_elem, Array.Empty<object>());
            var src = ToXxxRuntimeIter.AsObjects(a);
            var picked = b is FunctionWithSingleArg p
                ? src.Where(e => (bool)p.Calc(e))
                : src.Where(e => (bool)((IConcreteFunction)b).Calc(new[] { e }));
            return new FixedFunnyArray(_elem, picked.ToArray());
        }
    }
}

public class TakeEnumerableFunction : GenericFunctionBase {
    public TakeEnumerableFunction() : base(
        "take",
        FunnyType.FixedArrayOf(FunnyType.Generic(0)),
        FunnyType.EnumerableOf(FunnyType.Generic(0)),
        FunnyType.Int32) { ArgProperties = FunArgProperty.FromNames("arr", "count"); }

    public override IConcreteFunction CreateConcrete(FunnyType[] concrete, IFunctionSelectorContext _) =>
        new Impl(concrete[0]);

    private sealed class Impl : FunctionWithTwoArgs {
        private readonly FunnyType _elem;
        public Impl(FunnyType elem) : base("take",
            FunnyType.FixedArrayOf(elem),
            FunnyType.EnumerableOf(elem),
            FunnyType.Int32) => _elem = elem;
        public override object Calc(object a, object b) {
            var count = (int)b;
            if (count < 0) throw new FunnyRuntimeException("Take count cannot be negative");
            if (a is FunnyNone || count == 0)
                return new FixedFunnyArray(_elem, Array.Empty<object>());
            return new FixedFunnyArray(_elem, ToXxxRuntimeIter.AsObjects(a).Take(count).ToArray());
        }
    }
}

public class SkipEnumerableFunction : GenericFunctionBase {
    public SkipEnumerableFunction() : base(
        "skip",
        FunnyType.FixedArrayOf(FunnyType.Generic(0)),
        FunnyType.EnumerableOf(FunnyType.Generic(0)),
        FunnyType.Int32) { ArgProperties = FunArgProperty.FromNames("arr", "count"); }

    public override IConcreteFunction CreateConcrete(FunnyType[] concrete, IFunctionSelectorContext _) =>
        new Impl(concrete[0]);

    private sealed class Impl : FunctionWithTwoArgs {
        private readonly FunnyType _elem;
        public Impl(FunnyType elem) : base("skip",
            FunnyType.FixedArrayOf(elem),
            FunnyType.EnumerableOf(elem),
            FunnyType.Int32) => _elem = elem;
        public override object Calc(object a, object b) {
            var count = (int)b;
            if (count < 0) throw new FunnyRuntimeException("Skip count cannot be negative");
            if (a is FunnyNone)
                return new FixedFunnyArray(_elem, Array.Empty<object>());
            return new FixedFunnyArray(_elem, ToXxxRuntimeIter.AsObjects(a).Skip(count).ToArray());
        }
    }
}

public class FlatEnumerableFunction : GenericFunctionWithSingleArgument {
    public FlatEnumerableFunction() : base(
        "flat",
        FunnyType.FixedArrayOf(FunnyType.Generic(0)),
        FunnyType.EnumerableOf(FunnyType.EnumerableOf(FunnyType.Generic(0)))) { ArgProperties = FunArgProperty.FromNames("arr"); }

    protected override object Calc(object a) {
        var outer = (IFunnyEnumerable)a;
        // Outer element type may be Array (legacy) or another Enumerable.
        // Strip the outer-element wrapper to recover the inner element type.
        var outerElem = outer.ElementType;
        var innerElem = outerElem.BaseType == BaseFunnyType.ArrayOf
            ? outerElem.ArrayTypeSpecification.FunnyType
            : outerElem.BaseType == BaseFunnyType.FixedArray
                ? outerElem.FixedArrayTypeSpecification.FunnyType
                : outerElem.BaseType == BaseFunnyType.List
                    ? outerElem.ListTypeSpecification.FunnyType
                    : outerElem.BaseType == BaseFunnyType.MutableArray
                        ? outerElem.MutableArrayTypeSpecification.FunnyType
                        : outerElem;
        var items = outer.SelectMany(o => (IFunnyEnumerable)o).ToArray();
        return new FixedFunnyArray(innerElem, items);
    }
}

public class ConcatEnumerableFunction : GenericFunctionWithTwoArguments {
    // Lang concat — accepts any two Enumerables. Returns FixedArray<T>.
    // Special-case: when both inputs are text (IFunnyArray of Char), preserve
    // text identity by returning TextFunnyArray (`char[]` semantic). This
    // matches user expectations — `concat('a', 'b')` → "ab" stays text-typed
    // and prints as a string. Other element types produce FixedArray<T>.
    public ConcatEnumerableFunction() : base(
        "concat",
        FunnyType.FixedArrayOf(FunnyType.Generic(0)),
        FunnyType.EnumerableOf(FunnyType.Generic(0)),
        FunnyType.EnumerableOf(FunnyType.Generic(0))) { ArgProperties = FunArgProperty.FromNames("a", "b"); }

    protected override object Calc(object a, object b) {
        // Text preservation: both args are IFunnyArray of Char → produce TextFunnyArray.
        if (a is IFunnyArray ta && b is IFunnyArray tb
            && ta.ElementType.BaseType == BaseFunnyType.Char
            && tb.ElementType.BaseType == BaseFunnyType.Char)
            return new TextFunnyArray(ta.ToText() + tb.ToText());
        var arr1 = ToXxxRuntimeIter.AsObjects(a);
        var arr2 = ToXxxRuntimeIter.AsObjects(b);
        var elemType = ReturnType.FixedArrayTypeSpecification.FunnyType;
        return new FixedFunnyArray(elemType, arr1.Concat(arr2).ToArray());
    }
}
