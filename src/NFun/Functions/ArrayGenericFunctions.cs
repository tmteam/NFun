using System;
using System.Collections.Generic;
using System.Linq;
using NFun.Exceptions;
using NFun.Interpretation.Functions;
using NFun.Runtime.Arrays;
using NFun.Types;

namespace NFun.Functions;

public class LastFunction : GenericFunctionBase {
    public LastFunction() : base(
        "last",
        FunnyType.Generic(0),
        FunnyType.EnumerableOf(FunnyType.Generic(0))) { ArgProperties = FunArgProperty.FromNames("arr"); }

    protected override object Calc(object[] args) {
        if (args[0] is NFun.Types.FunnyNone) throw new FunnyRuntimeException("Array is empty");
        var ans = LastViaInterface(args[0]);
        return ans ?? throw new FunnyRuntimeException("Array is empty");
    }

    internal static object LastViaInterface(object arg) {
        switch (arg) {
            case IFunnyArray a: return a.GetElementOrNull(a.Count - 1);
            case NFun.Runtime.Lists.IFunnyFixedArray f: return f.GetElementOrNull(f.Count - 1);
            case NFun.Runtime.Lists.IFunnyMutableArray m: return m.GetElementOrNull(m.Count - 1);
            case NFun.Runtime.Lists.IFunnyEnumerable e: {
                object last = null; foreach (var v in e) last = v; return last;
            }
            default: throw new FunnyRuntimeException("Unsupported enumerable shape");
        }
    }
}

public class FirstFunction : GenericFunctionBase {
    public FirstFunction() : base(
        "first",
        FunnyType.Generic(0),
        FunnyType.EnumerableOf(FunnyType.Generic(0))) { ArgProperties = FunArgProperty.FromNames("arr"); }

    protected override object Calc(object[] args) {
        if (args[0] is NFun.Types.FunnyNone) throw new FunnyRuntimeException("Array is empty");
        var ans = FirstViaInterface(args[0]);
        return ans ?? throw new FunnyRuntimeException("Array is empty");
    }

    internal static object FirstViaInterface(object arg) {
        switch (arg) {
            case IFunnyArray a: return a.GetElementOrNull(0);
            case NFun.Runtime.Lists.IFunnyFixedArray f: return f.GetElementOrNull(0);
            case NFun.Runtime.Lists.IFunnyMutableArray m: return m.GetElementOrNull(0);
            case NFun.Runtime.Lists.IFunnyEnumerable e: {
                foreach (var v in e) return v; return null;
            }
            default: throw new FunnyRuntimeException("Unsupported enumerable shape");
        }
    }
}

public class CountFunction : GenericFunctionWithSingleArgument {
    public CountFunction() : base(
        "count",
        FunnyType.Int32,
        FunnyType.EnumerableOf(FunnyType.Generic(0))) { ArgProperties = FunArgProperty.FromNames("arr"); }

    protected override object Calc(object a) {
        // `default` for an Enumerable-constrained generic resolves to FunnyNone (no concrete
        // collection bound). Treat as empty: count is 0.
        if (a is NFun.Types.FunnyNone) return 0;
        return ((NFun.Runtime.Lists.IFunnyEnumerable)a).Count;
    }
}

/// <summary>
/// ee-mode `map(arr, fn)` — strict
/// <c>(FixedArray&lt;T0&gt;, (T0)-&gt;T1) -&gt; FixedArray&lt;T1&gt;</c>.
/// StateCollection-based input keeps strict back-prop precision through
/// element invariance (integer literals pin through closure-in-array).
/// ee-mode has one collection kind and no Map&lt;K,V&gt; — Enumerable would
/// only add overhead. See <see cref="MapEnumerableFunction"/> for lang-mode.
/// </summary>
public class MapFunction : GenericFunctionBase {
    public MapFunction() : base(
        "map",
        FunnyType.FixedArrayOf(FunnyType.Generic(1)),
        FunnyType.FixedArrayOf(FunnyType.Generic(0)),
        FunnyType.FunOf(FunnyType.Generic(1), FunnyType.Generic(0))) { ArgProperties = FunArgProperty.FromNames("arr", "f"); }

    public override IConcreteFunction CreateConcrete(FunnyType[] concreteTypesMap, IFunctionSelectorContext context) {
        var res = new ConcreteMap {
            Name = Name,
            ArgTypes = new[] {
                FunnyType.FixedArrayOf(concreteTypesMap[0]),
                FunnyType.FunOf(concreteTypesMap[1], concreteTypesMap[0])
            },
            ReturnType = FunnyType.FixedArrayOf(concreteTypesMap[1])
        };
        return res;
    }

    private class ConcreteMap : FunctionWithTwoArgs {
        public override object Calc(object a, object b) {
            var src = a switch {
                IFunnyArray ifa => ifa.Select(e => e),
                NFun.Runtime.Lists.IFunnyEnumerable ife => ife.Select(e => e),
                _ => throw new FunnyRuntimeException("map: unsupported collection shape"),
            };
            var elemType = ReturnType.FixedArrayTypeSpecification.FunnyType;
            if (b is FunctionWithSingleArg mapFunc)
                return new NFun.Runtime.Lists.FixedFunnyArray(elemType, src.Select(e => mapFunc.Calc(e)).ToArray());
            var map = (IConcreteFunction)b;
            return new NFun.Runtime.Lists.FixedFunnyArray(elemType, src.Select(e => map.Calc(new[] { e })).ToArray());
        }
    }
}

/// <summary>
/// Lang-mode `map(arr, fn)` — widened
/// <c>(Enumerable&lt;T0&gt;, (T0)-&gt;T1) -&gt; FixedArray&lt;T1&gt;</c>.
/// Accepts any iterable including Map&lt;K,V&gt; via synthesized pair-struct.
/// Trade-off: reduced back-prop precision for nested numeric upcast — see
/// <c>Specs/Tic/TicTechnicalDebt.md</c> #16.
///
/// <para>map is the only LINQ function that needs this split — other LINQ
/// funcs either don't transform element type (filter / any / all) or collapse
/// to scalars (count / sum), so their CompCs cross-Apply path has no precision
/// issue.</para>
/// </summary>
public class MapEnumerableFunction : GenericFunctionBase {
    public MapEnumerableFunction() : base(
        "map",
        FunnyType.FixedArrayOf(FunnyType.Generic(1)),
        FunnyType.EnumerableOf(FunnyType.Generic(0)),
        FunnyType.FunOf(FunnyType.Generic(1), FunnyType.Generic(0))) { ArgProperties = FunArgProperty.FromNames("arr", "f"); }

    public override IConcreteFunction CreateConcrete(FunnyType[] concreteTypesMap, IFunctionSelectorContext context) {
        var res = new ConcreteMap {
            Name = Name,
            ArgTypes = new[] {
                FunnyType.EnumerableOf(concreteTypesMap[0]),
                FunnyType.FunOf(concreteTypesMap[1], concreteTypesMap[0])
            },
            ReturnType = FunnyType.FixedArrayOf(concreteTypesMap[1])
        };
        return res;
    }

    private class ConcreteMap : FunctionWithTwoArgs {
        public override object Calc(object a, object b) {
            var src = a switch {
                IFunnyArray ifa => ifa.Select(e => e),
                NFun.Runtime.Lists.IFunnyEnumerable ife => ife.Select(e => e),
                _ => throw new FunnyRuntimeException("map: unsupported collection shape"),
            };
            var elemType = ReturnType.FixedArrayTypeSpecification.FunnyType;
            if (b is FunctionWithSingleArg mapFunc)
                return new NFun.Runtime.Lists.FixedFunnyArray(elemType, src.Select(e => mapFunc.Calc(e)).ToArray());
            var map = (IConcreteFunction)b;
            return new NFun.Runtime.Lists.FixedFunnyArray(elemType, src.Select(e => map.Calc(new[] { e })).ToArray());
        }
    }
}

public class MultiMapSumFunction : GenericFunctionBase {
    private const string Id = "sum";

    public MultiMapSumFunction() : base(
        Id,
        new[] { GenericConstrains.Any, GenericConstrains.Arithmetical },
        returnType: FunnyType.Generic(1),
        FunnyType.ArrayOf(FunnyType.Generic(0)),
        FunnyType.FunOf(FunnyType.Generic(1), FunnyType.Generic(0))) { ArgProperties = FunArgProperty.FromNames("arr", "f"); }

    public override IConcreteFunction CreateConcrete(FunnyType[] concreteTypes, IFunctionSelectorContext context) {
        var concrete = concreteTypes[1].BaseType switch {
                           BaseFunnyType.UInt32 => new ConcreteMapSumBase((a, b) => (UInt32)a + (UInt32)b, (UInt32)0),
                           BaseFunnyType.UInt64 => new ConcreteMapSumBase((a, b) => (UInt64)a + (UInt64)b, (UInt64)0),
                           BaseFunnyType.Int32  => new ConcreteMapSumBase((a, b) => (Int32)a + (Int32)b, 0),
                           BaseFunnyType.Int64  => new ConcreteMapSumBase((a, b) => (Int64)a + (Int64)b, (Int64)0),
                           BaseFunnyType.Real => context.RealTypeSelect(
                               ifIsDouble: new ConcreteMapSumBase((a, b) => (double)a + (double)b, (double)0),
                               ifIsDecimal: new ConcreteMapSumBase((a, b) => (decimal)a + (decimal)b, (decimal)0)),
                           _ => throw new NFunImpossibleException("Unsupported type for this function")
                       };
        concrete.Name = Id;
        concrete.ArgTypes = SubstitudeArgTypes(concreteTypes);
        concrete.ReturnType = concreteTypes[1];
        return concrete;
    }

    private class ConcreteMapSumBase : FunctionWithTwoArgs {
        private readonly Func<object, object, object> _solver;
        private readonly object _defaultValue;

        public ConcreteMapSumBase(Func<object, object, object> solver, object defaultValue) {
            _solver = solver;
            _defaultValue = defaultValue;
        }

        public override object Calc(object a, object b) {
            var arr = (IFunnyArray)a;
            var acc = _defaultValue;

            if (b is FunctionWithSingleArg mapFunc)
            {
                foreach (var e in arr.Select(e => mapFunc.Calc(e)))
                    acc = _solver(acc, e);
            }
            else
            {
                var map = (IConcreteFunction)b;
                foreach (var e in arr.Select(e => map.Calc(new[] { e })))
                    acc = _solver(acc, e);
            }

            return acc;
        }
    }
}

public class IsInSingleGenericFunctionDefinition : GenericFunctionBase {
    public IsInSingleGenericFunctionDefinition() : base(
        CoreFunNames.In,
        FunnyType.Bool,
        FunnyType.Generic(0),
        FunnyType.ArrayOf(FunnyType.Generic(0))) { ArgProperties = FunArgProperty.FromNames("element", "arr"); }

    protected override object Calc(object[] args) {
        var val = args[0];
        var arr = (IFunnyArray)args[1];
        return arr.Any(a => TypeHelper.AreEqual(a, val));
    }
}

public class SliceWithStepGenericFunctionDefinition : GenericFunctionBase {
    public SliceWithStepGenericFunctionDefinition() : base(
        CoreFunNames.SliceName,
        FunnyType.ArrayOf(FunnyType.Generic(0)),
        FunnyType.ArrayOf(FunnyType.Generic(0)),
        FunnyType.Int32,
        FunnyType.Int32,
        FunnyType.Int32) { ArgProperties = FunArgProperty.FromNames("arr", "from", "to", "step"); }

    protected override object Calc(object[] args) {
        var start = (int)args[1];
        if (start < 0)
            throw new FunnyRuntimeException("Argument out of range");
        var end = (int)args[2];
        if (end < 0)
            throw new FunnyRuntimeException("Argument out of range");
        if (start > end)
            throw new FunnyRuntimeException("Start cannot be more than end");
        var step = (int)args[3];
        if (step <= 0)
            throw new FunnyRuntimeException("Argument out of range");
        var arr = (IFunnyArray)args[0];
        return arr.Slice(start, end, step);
    }
}

public class SortFunction : GenericFunctionBase {
    public SortFunction() : base(
        "sort", GenericConstrains.Comparable,
        FunnyType.ArrayOf(FunnyType.Generic(0)), FunnyType.EnumerableOf(FunnyType.Generic(0))) { ArgProperties = FunArgProperty.FromNames("arr"); }

    protected override object Calc(object[] args) {
        var src = NFun.Functions.Collections.ToXxxRuntimeIter.AsObjects(args[0]);
        var arr = src.Cast<IComparable>().ToArray();
        Array.Sort(arr);
        var elemType = ReturnType.ArrayTypeSpecification.FunnyType;
        return FunnyArrayTools.CreateArray(arr.Cast<object>().ToArray(), elemType);
    }
}

public class SortDescendingFunction : GenericFunctionBase {
    public SortDescendingFunction() : base(
        "sortDescending", GenericConstrains.Comparable,
        FunnyType.ArrayOf(FunnyType.Generic(0)), FunnyType.EnumerableOf(FunnyType.Generic(0))) { ArgProperties = FunArgProperty.FromNames("arr"); }

    protected override object Calc(object[] args) {
        var src = NFun.Functions.Collections.ToXxxRuntimeIter.AsObjects(args[0]);
        var arr = src.Cast<IComparable>().ToArray();
        Array.Sort(arr);
        Array.Reverse(arr);
        var elemType = ReturnType.ArrayTypeSpecification.FunnyType;
        return FunnyArrayTools.CreateArray(arr.Cast<object>().ToArray(), elemType);
    }
}

public class SortMapFunction : GenericFunctionBase {
    public SortMapFunction() : base(
        "sort", new[] { GenericConstrains.Any, GenericConstrains.Comparable },
        FunnyType.ArrayOf(FunnyType.Generic(0)),
        FunnyType.ArrayOf(FunnyType.Generic(0)),
        FunnyType.FunOf(FunnyType.Generic(1), FunnyType.Generic(0))) { ArgProperties = FunArgProperty.FromNames("arr", "selector"); }

    protected override object Calc(object[] args) {
        var array = (IFunnyArray)args[0];
        var map = (IConcreteFunction)args[1];
        var sorted = array.OrderBy(a => (IComparable)map.Calc(new[] { a })).ToArray(array.Count);
        return FunnyArrayTools.CreateArray(sorted, array.ElementType);
    }
}

public class SortMapDescendingFunction : GenericFunctionBase {
    public SortMapDescendingFunction() : base(
        "sortDescending", new[] { GenericConstrains.Any, GenericConstrains.Comparable },
        FunnyType.ArrayOf(FunnyType.Generic(0)),
        FunnyType.ArrayOf(FunnyType.Generic(0)),
        FunnyType.FunOf(FunnyType.Generic(1), FunnyType.Generic(0))) { ArgProperties = FunArgProperty.FromNames("arr", "selector"); }

    protected override object Calc(object[] args) {
        var array = (IFunnyArray)args[0];
        var map = (IConcreteFunction)args[1];
        var sorted = array.OrderByDescending(a => (IComparable)map.Calc(new[] { a })).ToArray(array.Count);
        return FunnyArrayTools.CreateArray(sorted, array.ElementType);
    }
}

public class MedianFunction : GenericFunctionBase {
    public MedianFunction() : base(
        "median", GenericConstrains.Comparable, FunnyType.Generic(0),
        FunnyType.EnumerableOf(FunnyType.Generic(0))) { ArgProperties = FunArgProperty.FromNames("arr"); }

    protected override object Calc(object[] args)
    {
        if (args[0] is NFun.Types.FunnyNone) throw new FunnyRuntimeException("Array is empty");
        var array = (NFun.Runtime.Lists.IFunnyEnumerable)args[0];
        return GetMedian(array.Cast<IComparable>(), array.Count);
    }

    private static IComparable GetMedian(IEnumerable<IComparable> source, int size) {
        // Create a copy of the input, and sort the copy
        var temp = source.ToArray(size);
        Array.Sort(temp);

        int count = temp.Length;
        if (count == 0)
            throw new FunnyRuntimeException("Array is empty");
        return temp[(count - 1) / 2];
    }
}

public class MaxElementFunction : GenericFunctionBase {
    public MaxElementFunction() : base(
        "max", GenericConstrains.Comparable, FunnyType.Generic(0),
        FunnyType.EnumerableOf(FunnyType.Generic(0))) { ArgProperties = FunArgProperty.FromNames("arr"); }

    protected override object Calc(object[] args) {
        if (args[0] is NFun.Types.FunnyNone) throw new FunnyRuntimeException("Array is empty");
        var array = (NFun.Runtime.Lists.IFunnyEnumerable)args[0];
        if (array.Count == 0) throw new FunnyRuntimeException("Array is empty");
        // IEEE 754: NaN propagates through max — LINQ Max ignores NaN, so use manual loop
        object result = null;
        foreach (var item in array)
        {
            if (item is double d && double.IsNaN(d)) return item;
            if (result == null || ((IComparable)item).CompareTo(result) > 0)
                result = item;
        }
        return result;
    }
}

public class MinElementFunction : GenericFunctionBase {
    public MinElementFunction() : base(
        "min", GenericConstrains.Comparable, FunnyType.Generic(0),
        FunnyType.EnumerableOf(FunnyType.Generic(0))) { ArgProperties = FunArgProperty.FromNames("arr"); }

    protected override object Calc(object[] args) {
        if (args[0] is NFun.Types.FunnyNone) throw new FunnyRuntimeException("Array is empty");
        var array = (NFun.Runtime.Lists.IFunnyEnumerable)args[0];
        if (array.Count == 0) throw new FunnyRuntimeException("Array is empty");
        // IEEE 754: NaN propagates through min — LINQ Min ignores NaN, so use manual loop
        object result = null;
        foreach (var item in array)
        {
            if (item is double d && double.IsNaN(d)) return item;
            if (result == null || ((IComparable)item).CompareTo(result) < 0)
                result = item;
        }
        return result;
    }
}

public class SliceGenericFunctionDefinition : GenericFunctionBase {
    public SliceGenericFunctionDefinition() : base(
        CoreFunNames.SliceName,
        FunnyType.ArrayOf(FunnyType.Generic(0)),
        FunnyType.ArrayOf(FunnyType.Generic(0)),
        FunnyType.Int32,
        FunnyType.Int32) { ArgProperties = FunArgProperty.FromNames("arr", "from", "to"); }

    protected override object Calc(object[] args) {
        var start = (int)args[1];
        if (start < 0)
            throw new FunnyRuntimeException("Argument out of range");

        var end = (int)args[2];
        if (end < 0)
            throw new FunnyRuntimeException("Argument out of range");

        if (start > end)
            throw new FunnyRuntimeException("Start cannot be more than end");

        var arr = (IFunnyArray)args[0];
        return arr.Slice(start, end == int.MaxValue ? null : end, null);
    }
}

public class GetGenericFunctionDefinition : GenericFunctionWithTwoArguments {
    public GetGenericFunctionDefinition() : base(
        CoreFunNames.GetElementName,
        FunnyType.Generic(0),
        FunnyType.ArrayOf(FunnyType.Generic(0)),
        FunnyType.Int32) { }

    protected override object Calc(object a, object b) {
        var index = (int)b;
        if (index < 0)
            throw new FunnyRuntimeException("Argument out of range");

        var arr = (IFunnyArray)a;
        var res = arr.GetElementOrNull(index);

        if (res == null)
            throw new FunnyRuntimeException("Argument out of range");
        return res;
    }
}

public class SetGenericFunctionDefinition : GenericFunctionBase {
    // Renamed from `set` to `setAt` (Stage C / Set work): the bare `set` name
    // now belongs to the set-constructor factory. `setAt(arr, i, v)` returns a
    // new array with index i replaced — the persistent-array `with` operator.
    public SetGenericFunctionDefinition() : base(
        "setAt",
        FunnyType.ArrayOf(FunnyType.Generic(0)),
        FunnyType.ArrayOf(FunnyType.Generic(0)),
        FunnyType.Int32,
        FunnyType.Generic(0)) { ArgProperties = FunArgProperty.FromNames("arr", "index", "value"); }

    protected override object Calc(object[] args) {
        var arr = (IFunnyArray)args[0];

        var index = (int)args[1];
        if (index < 0)
            throw new FunnyRuntimeException("Argument out of range");
        if (index > arr.Count + 1)
            throw new FunnyRuntimeException("Argument out of range");
        var val = args[2];

        var newArr = new object[arr.ClrArray.Length];
        arr.ClrArray.CopyTo(newArr, 0);
        newArr.SetValue(val, index);
        return FunnyArrayTools.CreateArray(newArr, arr.ElementType);
    }
}

public class ContainsGenericFunctionDefinition : GenericFunctionWithTwoArguments {
    // Linear-scan baseline (works on any Enumerable<T>). Set has O(1) membership
    // via the underlying HashSet — short-circuit for that case.
    public ContainsGenericFunctionDefinition() : base(
        "contains",
        FunnyType.Bool,
        FunnyType.EnumerableOf(FunnyType.Generic(0)),
        FunnyType.Generic(0)) { ArgProperties = FunArgProperty.FromNames("arr", "element"); }

    protected override object Calc(object a, object b) {
        if (a is NFun.Types.FunnyNone) return false;
        if (a is NFun.Runtime.Lists.IFunnyMutableSet set) return set.Contains(b);
        var arr = (NFun.Runtime.Lists.IFunnyEnumerable)a;
        foreach (var element in arr)
            if (TypeHelper.AreEqual(element, b))
                return true;
        return false;
    }
}

public class FindGenericFunctionDefinition : GenericFunctionWithTwoArguments {
    public FindGenericFunctionDefinition() : base(
        "find",
        FunnyType.Int32,
        FunnyType.EnumerableOf(FunnyType.Generic(0)),
        FunnyType.Generic(0)) { ArgProperties = FunArgProperty.FromNames("arr", "element"); }

    protected override object Calc(object a, object b) {
        if (a is NFun.Types.FunnyNone) return -1;
        var arr = (NFun.Runtime.Lists.IFunnyEnumerable)a;
        var factor = b;
        int i = 0;
        foreach (var element in arr)
        {
            if (TypeHelper.AreEqual(element, factor))
                return i;
            i++;
        }
        return -1;
    }
}

public class ChunkGenericFunctionDefinition : GenericFunctionBase {
    public ChunkGenericFunctionDefinition() : base(
        "chunk",
        FunnyType.ArrayOf(FunnyType.ArrayOf(FunnyType.Generic(0))),
        FunnyType.EnumerableOf(FunnyType.Generic(0)),
        FunnyType.Int32) { ArgProperties = FunArgProperty.FromNames("arr", "size"); }

    public override IConcreteFunction CreateConcrete(FunnyType[] concrete, IFunctionSelectorContext _) =>
        new Impl(concrete[0]);

    private sealed class Impl : FunctionWithTwoArgs {
        private readonly FunnyType _elem;
        public Impl(FunnyType elem) : base("chunk",
            FunnyType.ArrayOf(FunnyType.ArrayOf(elem)),
            FunnyType.EnumerableOf(elem),
            FunnyType.Int32) => _elem = elem;
        public override object Calc(object a, object b) {
            var chunkSize = (int)b;
            if (chunkSize <= 0)
                throw new FunnyRuntimeException($"Chunk size is {chunkSize}. It has to be positive");
            var src = NFun.Functions.Collections.ToXxxRuntimeIter.AsObjects(a).ToList();
            var result = new List<IFunnyArray>();
            int i = 0;
            while (i < src.Count) {
                int size = Math.Min(chunkSize, src.Count - i);
                var chunk = new object[size];
                for (int j = 0; j < size; j++) chunk[j] = src[i + j];
                result.Add(new ImmutableFunnyArray(chunk, _elem));
                i += size;
            }
            return new EnumerableFunnyArray(result, FunnyType.ArrayOf(FunnyType.ArrayOf(_elem)));
        }
    }
}

public class FlatGenericFunctionDefinition : GenericFunctionWithSingleArgument {
    public FlatGenericFunctionDefinition() : base(
        "flat",
        FunnyType.ArrayOf(FunnyType.Generic(0)),
        FunnyType.ArrayOf(FunnyType.ArrayOf(FunnyType.Generic(0)))) { ArgProperties = FunArgProperty.FromNames("arr"); }

    protected override object Calc(object a) {
        var arr = (IFunnyArray)a;
        var originInputType = arr.ElementType.ArrayTypeSpecification.FunnyType;

        return FunnyArrayTools.CreateEnumerable(arr.SelectMany(o => (IFunnyArray)o), originInputType);
    }
}

public class FoldGenericFunctionDefinition : GenericFunctionWithTwoArguments {
    public FoldGenericFunctionDefinition() : base(
        "fold", new[] { GenericConstrains.Any },
        FunnyType.Generic(0),
        FunnyType.ArrayOf(FunnyType.Generic(0)),
        FunnyType.FunOf(FunnyType.Generic(0), FunnyType.Generic(0), FunnyType.Generic(0))) { ArgProperties = FunArgProperty.FromNames("arr", "f"); }

    protected override object Calc(object a, object b) {
        var arr = (IFunnyArray)a;
        if (arr.Count == 0)
            throw new FunnyRuntimeException("Input array is empty");
        if (b is FunctionWithTwoArgs fold2)
            return arr.Aggregate((l, r) => fold2.Calc(l, r));

        var fold = (IConcreteFunction)b;

        return arr.Aggregate((l, r) => fold.Calc(new[] { l, r }));
    }
}

public class FoldWithDefaultsGenericFunctionDefinition : GenericFunctionBase {
    public FoldWithDefaultsGenericFunctionDefinition() : base(
        "fold",
        returnType: FunnyType.Generic(1),
        FunnyType.ArrayOf(FunnyType.Generic(0)),
        FunnyType.Generic(1),
        FunnyType.FunOf(
            returnType: FunnyType.Generic(1), FunnyType.Generic(1), FunnyType.Generic(0))) { ArgProperties = FunArgProperty.FromNames("arr", "seed", "f"); }

    protected override object Calc(object[] args) {
        var arr = (IFunnyArray)args[0];
        var defaultValue = args[1];

        var fold = (IConcreteFunction)args[2];

        if (fold is FunctionWithTwoArgs fold2)
            return arr.Aggregate(defaultValue, (a, b) => fold2.Calc(a, b));
        else
            return arr.Aggregate(defaultValue, (a, b) => fold.Calc(new[] { a, b }));
    }
}

public class UniteGenericFunctionDefinition : GenericFunctionBase {
    public UniteGenericFunctionDefinition() : base(
        "unite",
        FunnyType.ArrayOf(FunnyType.Generic(0)),
        FunnyType.EnumerableOf(FunnyType.Generic(0)),
        FunnyType.EnumerableOf(FunnyType.Generic(0))) { ArgProperties = FunArgProperty.FromNames("a", "b"); }

    public override IConcreteFunction CreateConcrete(FunnyType[] concrete, IFunctionSelectorContext _) =>
        new Impl(concrete[0]);

    private sealed class Impl : FunctionWithTwoArgs {
        private readonly FunnyType _elem;
        public Impl(FunnyType elem) : base("unite",
            FunnyType.ArrayOf(elem),
            FunnyType.EnumerableOf(elem),
            FunnyType.EnumerableOf(elem)) => _elem = elem;
        public override object Calc(object a, object b) {
            if (a is NFun.Types.FunnyNone && b is NFun.Types.FunnyNone)
                return FunnyArrayTools.CreateEnumerable(System.Linq.Enumerable.Empty<object>(), _elem);
            var arr1 = NFun.Functions.Collections.ToXxxRuntimeIter.AsObjects(a);
            var arr2 = NFun.Functions.Collections.ToXxxRuntimeIter.AsObjects(b);
            return FunnyArrayTools.CreateEnumerable(arr1.Union(arr2), _elem);
        }
    }
}

public class UniqueGenericFunctionDefinition : GenericFunctionBase {
    public UniqueGenericFunctionDefinition() : base(
        "unique",
        FunnyType.ArrayOf(FunnyType.Generic(0)),
        FunnyType.EnumerableOf(FunnyType.Generic(0)),
        FunnyType.EnumerableOf(FunnyType.Generic(0))) { ArgProperties = FunArgProperty.FromNames("a", "b"); }

    public override IConcreteFunction CreateConcrete(FunnyType[] concrete, IFunctionSelectorContext _) =>
        new Impl(concrete[0]);

    private sealed class Impl : FunctionWithTwoArgs {
        private readonly FunnyType _elem;
        public Impl(FunnyType elem) : base("unique",
            FunnyType.ArrayOf(elem),
            FunnyType.EnumerableOf(elem),
            FunnyType.EnumerableOf(elem)) => _elem = elem;
        public override object Calc(object a, object b) {
            if (a is NFun.Types.FunnyNone && b is NFun.Types.FunnyNone)
                return FunnyArrayTools.CreateEnumerable(System.Linq.Enumerable.Empty<object>(), _elem);
            var arr1 = NFun.Functions.Collections.ToXxxRuntimeIter.AsObjects(a).ToList();
            var arr2 = NFun.Functions.Collections.ToXxxRuntimeIter.AsObjects(b).ToList();
            return FunnyArrayTools.CreateEnumerable(arr1.Except(arr2).Concat(arr2.Except(arr1)), _elem);
        }
    }
}

public class IntersectGenericFunctionDefinition : GenericFunctionBase {
    public IntersectGenericFunctionDefinition() : base(
        "intersect",
        FunnyType.ArrayOf(FunnyType.Generic(0)),
        FunnyType.EnumerableOf(FunnyType.Generic(0)),
        FunnyType.EnumerableOf(FunnyType.Generic(0))) { ArgProperties = FunArgProperty.FromNames("a", "b"); }

    public override IConcreteFunction CreateConcrete(FunnyType[] concrete, IFunctionSelectorContext _) =>
        new Impl(concrete[0]);

    private sealed class Impl : FunctionWithTwoArgs {
        private readonly FunnyType _elem;
        public Impl(FunnyType elem) : base("intersect",
            FunnyType.ArrayOf(elem),
            FunnyType.EnumerableOf(elem),
            FunnyType.EnumerableOf(elem)) => _elem = elem;
        public override object Calc(object a, object b) {
            if (a is NFun.Types.FunnyNone || b is NFun.Types.FunnyNone)
                return FunnyArrayTools.CreateEnumerable(System.Linq.Enumerable.Empty<object>(), _elem);
            var arr1 = NFun.Functions.Collections.ToXxxRuntimeIter.AsObjects(a);
            var arr2 = NFun.Functions.Collections.ToXxxRuntimeIter.AsObjects(b);
            return FunnyArrayTools.CreateEnumerable(arr1.Intersect(arr2), _elem);
        }
    }
}

public class ConcatArraysGenericFunctionDefinition : GenericFunctionWithTwoArguments {
    public ConcatArraysGenericFunctionDefinition() : base(
        "concat",
        FunnyType.ArrayOf(FunnyType.Generic(0)),
        FunnyType.ArrayOf(FunnyType.Generic(0)),
        FunnyType.ArrayOf(FunnyType.Generic(0))) { ArgProperties = FunArgProperty.FromNames("a", "b"); }

    protected override object Calc(object a, object b) {
        var arr1 = (IFunnyArray)a;
        var arr2 = (IFunnyArray)b;
        return FunnyArrayTools.CreateEnumerable(arr1.Concat(arr2), arr1.ElementType);
    }
}

public class AppendGenericFunctionDefinition : GenericFunctionWithTwoArguments {
    public AppendGenericFunctionDefinition() : base(
        "append",
        FunnyType.ArrayOf(FunnyType.Generic(0)),
        FunnyType.ArrayOf(FunnyType.Generic(0)),
        FunnyType.Generic(0)) { ArgProperties = FunArgProperty.FromNames("arr", "element"); }

    protected override object Calc(object a, object b) {
        var arr1 = (IFunnyArray)a;
        var arr2 = b;
        var res = FunnyArrayTools.CreateEnumerable(arr1.Append(arr2), arr1.ElementType);
        return res;
    }
}

public class SubstractArraysGenericFunctionDefinition : GenericFunctionBase {
    public SubstractArraysGenericFunctionDefinition() : base(
        "except",
        FunnyType.ArrayOf(FunnyType.Generic(0)),
        FunnyType.EnumerableOf(FunnyType.Generic(0)),
        FunnyType.EnumerableOf(FunnyType.Generic(0))) { ArgProperties = FunArgProperty.FromNames("a", "b"); }

    public override IConcreteFunction CreateConcrete(FunnyType[] concrete, IFunctionSelectorContext _) =>
        new Impl(concrete[0]);

    private sealed class Impl : FunctionWithTwoArgs {
        private readonly FunnyType _elem;
        public Impl(FunnyType elem) : base("except",
            FunnyType.ArrayOf(elem),
            FunnyType.EnumerableOf(elem),
            FunnyType.EnumerableOf(elem)) => _elem = elem;
        public override object Calc(object a, object b) {
            var arr1 = NFun.Functions.Collections.ToXxxRuntimeIter.AsObjects(a);
            var arr2 = NFun.Functions.Collections.ToXxxRuntimeIter.AsObjects(b);
            return FunnyArrayTools.CreateEnumerable(arr1.Except(arr2), _elem);
        }
    }
}

public class CountOfGenericFunctionDefinition : GenericFunctionWithTwoArguments {
    public CountOfGenericFunctionDefinition() : base(
        "count",
        FunnyType.Int32,
        FunnyType.EnumerableOf(FunnyType.Generic(0)),
        FunnyType.FunOf(FunnyType.Bool, FunnyType.Generic(0))) { ArgProperties = FunArgProperty.FromNames("arr", "predicate"); }

    protected override object Calc(object a, object b) {
        if (a is NFun.Types.FunnyNone) return 0;
        var arr = (NFun.Runtime.Lists.IFunnyEnumerable)a;
        var filter = (IConcreteFunction)b;

        return arr.Count(arg => (bool)filter.Calc(new[] { arg }));
    }
}

public class HasAnyGenericFunctionDefinition : GenericFunctionWithSingleArgument {
    public HasAnyGenericFunctionDefinition() : base(
        "any",
        FunnyType.Bool,
        FunnyType.EnumerableOf(FunnyType.Generic(0))) { ArgProperties = FunArgProperty.FromNames("arr"); }

    protected override object Calc(object a) {
        if (a is NFun.Types.FunnyNone) return false;
        return ((NFun.Runtime.Lists.IFunnyEnumerable)a).Count > 0;
    }
}

public class AnyGenericFunctionDefinition : GenericFunctionWithTwoArguments {
    public AnyGenericFunctionDefinition() : base(
        "any",
        FunnyType.Bool,
        FunnyType.EnumerableOf(FunnyType.Generic(0)),
        FunnyType.FunOf(FunnyType.Bool, FunnyType.Generic(0))) { ArgProperties = FunArgProperty.FromNames("arr", "predicate"); }

    protected override object Calc(object a, object b) {
        if (a is NFun.Types.FunnyNone) return false;
        var arr = (NFun.Runtime.Lists.IFunnyEnumerable)a;
        if (b is FunctionWithSingleArg predicate)
            return arr.Any(e => (bool)predicate.Calc(e));
        var filter = (IConcreteFunction)b;
        return arr.Any(e => (bool)filter.Calc(new[] { e }));
    }
}

public class AllGenericFunctionDefinition : GenericFunctionWithTwoArguments {
    public AllGenericFunctionDefinition() : base(
        "all",
        FunnyType.Bool,
        FunnyType.EnumerableOf(FunnyType.Generic(0)),
        FunnyType.FunOf(FunnyType.Bool, FunnyType.Generic(0))) { ArgProperties = FunArgProperty.FromNames("arr", "predicate"); }

    protected override object Calc(object a, object b) {
        if (a is NFun.Types.FunnyNone) return true; // vacuously true
        var arr = (NFun.Runtime.Lists.IFunnyEnumerable)a;
        var filter = (IConcreteFunction)b;

        return arr.All(e => (bool)filter.Calc(new[] { e }));
    }
}

public class FilterGenericFunctionDefinition : GenericFunctionBase {
    public FilterGenericFunctionDefinition() : base(
        "filter",
        FunnyType.ArrayOf(FunnyType.Generic(0)),
        FunnyType.EnumerableOf(FunnyType.Generic(0)),
        FunnyType.FunOf(FunnyType.Bool, FunnyType.Generic(0))) { ArgProperties = FunArgProperty.FromNames("arr", "predicate"); }

    public override IConcreteFunction CreateConcrete(FunnyType[] concrete, IFunctionSelectorContext _) {
        var elem = concrete[0];
        return new Impl(elem);
    }

    private sealed class Impl : FunctionWithTwoArgs {
        private readonly FunnyType _elem;
        public Impl(FunnyType elem) : base("filter",
            FunnyType.ArrayOf(elem),
            FunnyType.EnumerableOf(elem),
            FunnyType.FunOf(FunnyType.Bool, elem)) => _elem = elem;
        public override object Calc(object a, object b) {
            if (a is NFun.Types.FunnyNone)
                return FunnyArrayTools.CreateArray(Array.Empty<object>(), _elem);
            var src = NFun.Functions.Collections.ToXxxRuntimeIter.AsObjects(a);
            var picked = b is FunctionWithSingleArg p
                ? src.Where(e => (bool)p.Calc(e))
                : src.Where(e => (bool)((IConcreteFunction)b).Calc(new[] { e }));
            return FunnyArrayTools.CreateArray(picked.ToArray(), _elem);
        }
    }
}

public class RepeatGenericFunctionDefinition : GenericFunctionBase {
    public RepeatGenericFunctionDefinition() : base(
        "repeat",
        FunnyType.ArrayOf(FunnyType.Generic(0)),
        FunnyType.Generic(0),
        FunnyType.Int32) { ArgProperties = FunArgProperty.FromNames("element", "count"); }

    public override IConcreteFunction CreateConcrete(FunnyType[] concreteTypesMap, IFunctionSelectorContext context) {
        var res = new ConcreteRepeat {
            Name = Name,
            ArgTypes = new[] { concreteTypesMap[0], FunnyType.Int32 },
            ReturnType = FunnyType.ArrayOf(concreteTypesMap[0])
        };
        return res;
    }

    private class ConcreteRepeat : FunctionWithTwoArgs {
        public override object Calc(object a, object b) {
            var count = (int)b;
            if (count < 0) throw new FunnyRuntimeException("Repeat count cannot be negative");
            return FunnyArrayTools.CreateEnumerable(Enumerable.Repeat(a, count), ArgTypes[0]);
        }
    }
}

public class ReverseGenericFunctionDefinition : GenericFunctionWithSingleArgument {
    public ReverseGenericFunctionDefinition() : base(
        "reverse",
        FunnyType.ArrayOf(FunnyType.Generic(0)),
        FunnyType.ArrayOf(FunnyType.Generic(0))) { ArgProperties = FunArgProperty.FromNames("arr"); }

    protected override object Calc(object a) {
        var arr = (IFunnyArray)a;
        return FunnyArrayTools.CreateEnumerable(arr.Reverse(), arr.ElementType);
    }
}

public class TakeGenericFunctionDefinition : GenericFunctionBase {
    public TakeGenericFunctionDefinition() : base(
        "take",
        FunnyType.ArrayOf(FunnyType.Generic(0)),
        FunnyType.EnumerableOf(FunnyType.Generic(0)),
        FunnyType.Int32) { ArgProperties = FunArgProperty.FromNames("arr", "count"); }

    public override IConcreteFunction CreateConcrete(FunnyType[] concrete, IFunctionSelectorContext _) =>
        new Impl(concrete[0]);

    private sealed class Impl : FunctionWithTwoArgs {
        private readonly FunnyType _elem;
        public Impl(FunnyType elem) : base("take",
            FunnyType.ArrayOf(elem),
            FunnyType.EnumerableOf(elem),
            FunnyType.Int32) => _elem = elem;
        public override object Calc(object a, object b) {
            var count = (int)b;
            if (count < 0) throw new FunnyRuntimeException("Take count cannot be negative");
            if (a is NFun.Types.FunnyNone || count == 0)
                return FunnyArrayTools.CreateArray(Array.Empty<object>(), _elem);
            return FunnyArrayTools.CreateArray(
                NFun.Functions.Collections.ToXxxRuntimeIter.AsObjects(a).Take(count).ToArray(),
                _elem);
        }
    }
}

public class SkipGenericFunctionDefinition : GenericFunctionBase {
    public SkipGenericFunctionDefinition() : base(
        "skip",
        FunnyType.ArrayOf(FunnyType.Generic(0)),
        FunnyType.EnumerableOf(FunnyType.Generic(0)),
        FunnyType.Int32) { ArgProperties = FunArgProperty.FromNames("arr", "count"); }

    public override IConcreteFunction CreateConcrete(FunnyType[] concrete, IFunctionSelectorContext _) =>
        new Impl(concrete[0]);

    private sealed class Impl : FunctionWithTwoArgs {
        private readonly FunnyType _elem;
        public Impl(FunnyType elem) : base("skip",
            FunnyType.ArrayOf(elem),
            FunnyType.EnumerableOf(elem),
            FunnyType.Int32) => _elem = elem;
        public override object Calc(object a, object b) {
            var count = (int)b;
            if (count < 0) throw new FunnyRuntimeException("Skip count cannot be negative");
            if (a is NFun.Types.FunnyNone)
                return FunnyArrayTools.CreateArray(Array.Empty<object>(), _elem);
            return FunnyArrayTools.CreateArray(
                NFun.Functions.Collections.ToXxxRuntimeIter.AsObjects(a).Skip(count).ToArray(),
                _elem);
        }
    }
}
