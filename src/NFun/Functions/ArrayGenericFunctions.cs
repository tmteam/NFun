using System;
using System.Collections.Generic;
using System.Linq;
using NFun.Exceptions;
using NFun.Functions.Lang;
using NFun.Interpretation.Functions;
using NFun.Runtime.Arrays;
using NFun.Types;

namespace NFun.Functions;

public class LastFunction : GenericFunctionBase {
    public LastFunction(): base(new FunctionSignatureDescription(
        name: "last",
        outputType: FunnyType.Generic(0),
        inputTypes: new[] { FunnyType.EnumerableOf(FunnyType.Generic(0)) },
        isExtension: true,
        argNames: new[] { "arr" })) { }

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
    public FirstFunction(): base(new FunctionSignatureDescription(
        name: "first",
        outputType: FunnyType.Generic(0),
        inputTypes: new[] { FunnyType.EnumerableOf(FunnyType.Generic(0)) },
        isExtension: true,
        argNames: new[] { "arr" })) { }

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
    public CountFunction(): base(new FunctionSignatureDescription(
        name: "count",
        outputType: FunnyType.Int32,
        inputTypes: new[] { FunnyType.EnumerableOf(FunnyType.Generic(0)) },
        isExtension: true,
        argNames: new[] { "arr" })) { }

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
    public MapFunction(): base(new FunctionSignatureDescription(
        name: "map",
        outputType: FunnyType.FixedArrayOf(FunnyType.Generic(1)),
        inputTypes: new[] { FunnyType.FixedArrayOf(FunnyType.Generic(0)), FunnyType.FunOf(FunnyType.Generic(1), FunnyType.Generic(0)) },
        isExtension: true,
        argNames: new[] { "arr", "f" })) { }

    public override IConcreteFunction CreateConcrete(FunnyType[] concreteTypesMap, IFunctionSelectorContext context) {
        var res = new ConcreteMap(context.Converter.TypeBehaviour.GetClrTypeFor(concreteTypesMap[0].BaseType)) {
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
        private readonly Type _lambdaInputClrType;
        public ConcreteMap(Type lambdaInputClrType) { _lambdaInputClrType = lambdaInputClrType; }
        public override object Calc(object a, object b) {
            var src = a switch {
                IFunnyArray ifa => ifa.Select(e => e),
                NFun.Runtime.Lists.IFunnyEnumerable ife => ife.Select(e => e),
                _ => throw new FunnyRuntimeException("map: unsupported collection shape"),
            };
            var elemType = ReturnType.FixedArrayTypeSpecification.FunnyType;
            var coerce = MapCoerce.Get(_lambdaInputClrType);
            if (b is FunctionWithSingleArg mapFunc)
                return new NFun.Runtime.Lists.FixedFunnyArray(elemType, src.Select(e => mapFunc.Calc(coerce(e))).ToArray());
            var map = (IConcreteFunction)b;
            return new NFun.Runtime.Lists.FixedFunnyArray(elemType, src.Select(e => map.Calc(new[] { coerce(e) })).ToArray());
        }
    }
}

// MapEnumerableFunction moved to NFun/Functions/Lang/MapEnumerableFunction.cs.

public class MultiMapSumFunction : GenericFunctionBase {
    private const string Id = "sum";

    public MultiMapSumFunction() : base(new FunctionSignatureDescription(
        name: Id,
        outputType: FunnyType.Generic(1),
        inputTypes: new[] { FunnyType.ArrayOf(FunnyType.Generic(0)), FunnyType.FunOf(FunnyType.Generic(1), FunnyType.Generic(0)) },
        isExtension: true,
        constrains: new[] { GenericConstrains.Any, GenericConstrains.Arithmetical },
        argNames: new[] { "arr", "f" })) { }

    public override IConcreteFunction CreateConcrete(FunnyType[] concreteTypes, IFunctionSelectorContext context) {
        var concrete = concreteTypes[1].BaseType switch {
                           BaseFunnyType.UInt32 => new ConcreteMapSumBase((a, b) => (UInt32)a + (UInt32)b, (UInt32)0),
                           BaseFunnyType.UInt64 => new ConcreteMapSumBase((a, b) => (UInt64)a + (UInt64)b, (UInt64)0),
                           BaseFunnyType.Int32  => new ConcreteMapSumBase((a, b) => (Int32)a + (Int32)b, 0),
                           BaseFunnyType.Int64  => new ConcreteMapSumBase((a, b) => (Int64)a + (Int64)b, (Int64)0),
                           BaseFunnyType.Float32 => new ConcreteMapSumBase((a, b) => (float)a + (float)b, (float)0),
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
    public IsInSingleGenericFunctionDefinition() : base(new FunctionSignatureDescription(
        name: CoreFunNames.In,
        outputType: FunnyType.Bool,
        inputTypes: new[] { FunnyType.Generic(0), FunnyType.ArrayOf(FunnyType.Generic(0)) },
        isExtension: true,
        argNames: new[] { "element", "arr" })) { }

    protected override object Calc(object[] args) {
        var val = args[0];
        var arr = (IFunnyArray)args[1];
        return arr.Any(a => TypeHelper.AreEqual(a, val));
    }
}

public class SliceWithStepGenericFunctionDefinition : GenericFunctionBase {
    public SliceWithStepGenericFunctionDefinition() : base(new FunctionSignatureDescription(
        name: CoreFunNames.SliceName,
        outputType: FunnyType.ArrayOf(FunnyType.Generic(0)),
        inputTypes: new[] { FunnyType.ArrayOf(FunnyType.Generic(0)), FunnyType.Int32, FunnyType.Int32, FunnyType.Int32 },
        isExtension: true,
        argNames: new[] { "arr", "from", "to", "step" })) { }

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
    public SortFunction(): base(new FunctionSignatureDescription(
        name: "sort",
        outputType: FunnyType.ArrayOf(FunnyType.Generic(0)),
        inputTypes: new[] { FunnyType.EnumerableOf(FunnyType.Generic(0)) },
        isExtension: true,
        constrains: new[] { GenericConstrains.Comparable },
        argNames: new[] { "arr" })) { }

    protected override object Calc(object[] args) {
        var src = NFun.Functions.Collections.ToXxxRuntimeIter.AsObjects(args[0]);
        var arr = src.Cast<IComparable>().ToArray();
        Array.Sort(arr);
        var elemType = ReturnType.ArrayTypeSpecification.FunnyType;
        return FunnyArrayTools.CreateArray(arr.Cast<object>().ToArray(), elemType);
    }
}

public class SortDescendingFunction : GenericFunctionBase {
    public SortDescendingFunction(): base(new FunctionSignatureDescription(
        name: "sortDescending",
        outputType: FunnyType.ArrayOf(FunnyType.Generic(0)),
        inputTypes: new[] { FunnyType.EnumerableOf(FunnyType.Generic(0)) },
        isExtension: true,
        constrains: new[] { GenericConstrains.Comparable },
        argNames: new[] { "arr" })) { }

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
    public SortMapFunction() : base(new FunctionSignatureDescription(
        name: "sort",
        outputType: FunnyType.ArrayOf(FunnyType.Generic(0)),
        inputTypes: new[] { FunnyType.ArrayOf(FunnyType.Generic(0)), FunnyType.FunOf(FunnyType.Generic(1), FunnyType.Generic(0)) },
        isExtension: true,
        constrains: new[] { GenericConstrains.Any, GenericConstrains.Comparable },
        argNames: new[] { "arr", "selector" })) { }

    protected override object Calc(object[] args) {
        var array = (IFunnyArray)args[0];
        var map = (IConcreteFunction)args[1];
        var sorted = array.OrderBy(a => (IComparable)map.Calc(new[] { a })).ToArray(array.Count);
        return FunnyArrayTools.CreateArray(sorted, array.ElementType);
    }
}

public class SortMapDescendingFunction : GenericFunctionBase {
    public SortMapDescendingFunction() : base(new FunctionSignatureDescription(
        name: "sortDescending",
        outputType: FunnyType.ArrayOf(FunnyType.Generic(0)),
        inputTypes: new[] { FunnyType.ArrayOf(FunnyType.Generic(0)), FunnyType.FunOf(FunnyType.Generic(1), FunnyType.Generic(0)) },
        isExtension: true,
        constrains: new[] { GenericConstrains.Any, GenericConstrains.Comparable },
        argNames: new[] { "arr", "selector" })) { }

    protected override object Calc(object[] args) {
        var array = (IFunnyArray)args[0];
        var map = (IConcreteFunction)args[1];
        var sorted = array.OrderByDescending(a => (IComparable)map.Calc(new[] { a })).ToArray(array.Count);
        return FunnyArrayTools.CreateArray(sorted, array.ElementType);
    }
}

public class MedianFunction : GenericFunctionBase {
    public MedianFunction(): base(new FunctionSignatureDescription(
        name: "median",
        outputType: FunnyType.Generic(0),
        inputTypes: new[] { FunnyType.EnumerableOf(FunnyType.Generic(0)) },
        isExtension: true,
        constrains: new[] { GenericConstrains.Comparable },
        argNames: new[] { "arr" })) { }

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
    public MaxElementFunction(): base(new FunctionSignatureDescription(
        name: "max",
        outputType: FunnyType.Generic(0),
        inputTypes: new[] { FunnyType.EnumerableOf(FunnyType.Generic(0)) },
        isExtension: true,
        constrains: new[] { GenericConstrains.Comparable },
        argNames: new[] { "arr" })) { }

    protected override object Calc(object[] args) {
        if (args[0] is NFun.Types.FunnyNone) throw new FunnyRuntimeException("Array is empty");
        var array = (NFun.Runtime.Lists.IFunnyEnumerable)args[0];
        if (array.Count == 0) throw new FunnyRuntimeException("Array is empty");
        // IEEE 754: NaN propagates through max — return NaN on first NaN element (double or float).
        // Without early-return, IComparable's ordering of NaN (as smallest) silently discards it.
        object result = null;
        foreach (var item in array)
        {
            if (IEEE754Guard.IsNaN(item)) return item;
            if (result == null || ((IComparable)item).CompareTo(result) > 0)
                result = item;
        }
        return result;
    }
}

public class MinElementFunction : GenericFunctionBase {
    public MinElementFunction(): base(new FunctionSignatureDescription(
        name: "min",
        outputType: FunnyType.Generic(0),
        inputTypes: new[] { FunnyType.EnumerableOf(FunnyType.Generic(0)) },
        isExtension: true,
        constrains: new[] { GenericConstrains.Comparable },
        argNames: new[] { "arr" })) { }

    protected override object Calc(object[] args) {
        if (args[0] is NFun.Types.FunnyNone) throw new FunnyRuntimeException("Array is empty");
        var array = (NFun.Runtime.Lists.IFunnyEnumerable)args[0];
        if (array.Count == 0) throw new FunnyRuntimeException("Array is empty");
        // IEEE 754: NaN propagates through min. Same reasoning as MaxElementFunction.
        object result = null;
        foreach (var item in array)
        {
            if (IEEE754Guard.IsNaN(item)) return item;
            if (result == null || ((IComparable)item).CompareTo(result) < 0)
                result = item;
        }
        return result;
    }
}

public class SliceGenericFunctionDefinition : GenericFunctionBase {
    public SliceGenericFunctionDefinition() : base(new FunctionSignatureDescription(
        name: CoreFunNames.SliceName,
        outputType: FunnyType.ArrayOf(FunnyType.Generic(0)),
        inputTypes: new[] { FunnyType.ArrayOf(FunnyType.Generic(0)), FunnyType.Int32, FunnyType.Int32 },
        isExtension: true,
        argNames: new[] { "arr", "from", "to" })) { }

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
    public GetGenericFunctionDefinition() : base(new FunctionSignatureDescription(
        name: CoreFunNames.GetElementName,
        outputType: FunnyType.Generic(0),
        inputTypes: new[] { FunnyType.ArrayOf(FunnyType.Generic(0)), FunnyType.Int32 },
        isExtension: true)) { }

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
    // Renamed from `set` to `setAt` (Stage C / Set work).
    public SetGenericFunctionDefinition() : base(new FunctionSignatureDescription(
        name: "setAt",
        outputType: FunnyType.ArrayOf(FunnyType.Generic(0)),
        inputTypes: new[] { FunnyType.ArrayOf(FunnyType.Generic(0)), FunnyType.Int32, FunnyType.Generic(0) },
        isExtension: true,
        argNames: new[] { "arr", "index", "value" })) { }

    protected override object Calc(object[] args) {
        var arr = (IFunnyArray)args[0];

        var index = (int)args[1];
        if (index < 0 || index >= arr.Count)
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
    public FindGenericFunctionDefinition(): base(new FunctionSignatureDescription(
        name: "find",
        outputType: FunnyType.Int32,
        inputTypes: new[] { FunnyType.EnumerableOf(FunnyType.Generic(0)), FunnyType.Generic(0) },
        isExtension: true,
        argNames: new[] { "arr", "element" })) { }

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
    public ChunkGenericFunctionDefinition() : base(new FunctionSignatureDescription(
        name: "chunk",
        outputType: FunnyType.ArrayOf(FunnyType.ArrayOf(FunnyType.Generic(0))),
        inputTypes: new[] { FunnyType.EnumerableOf(FunnyType.Generic(0)), FunnyType.Int32 },
        isExtension: true,
        argNames: new[] { "arr", "size" })) { }

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
    public FlatGenericFunctionDefinition() : base(new FunctionSignatureDescription(
        name: "flat",
        outputType: FunnyType.ArrayOf(FunnyType.Generic(0)),
        inputTypes: new[] { FunnyType.ArrayOf(FunnyType.ArrayOf(FunnyType.Generic(0))) },
        isExtension: true,
        argNames: new[] { "arr" })) { }

    protected override object Calc(object a) {
        // Dispatch on `IFunnyEnumerable` (the common base of both `IFunnyArray`
        // and the lang-mode `IFunnyMutableArray` / `IFunnyList` hierarchy).
        // The static signature `T[][] → T[]` lets lang-mode `list<list<T>>`
        // flow in via `list<T> ≤ T[]` subtype; runtime values then need both
        // shapes accepted. Bug hunt round 4 #22.
        var outer = (NFun.Runtime.Lists.IFunnyEnumerable)a;
        var inner = outer.ElementType.ArrayTypeSpecification.FunnyType;
        return FunnyArrayTools.CreateEnumerable(
            outer.SelectMany(o => (NFun.Runtime.Lists.IFunnyEnumerable)o),
            inner);
    }
}

public class FoldGenericFunctionDefinition : GenericFunctionWithTwoArguments {
    public FoldGenericFunctionDefinition() : base(new FunctionSignatureDescription(
        name: "fold",
        outputType: FunnyType.Generic(0),
        inputTypes: new[] { FunnyType.ArrayOf(FunnyType.Generic(0)), FunnyType.FunOf(FunnyType.Generic(0), FunnyType.Generic(0), FunnyType.Generic(0)) },
        isExtension: true,
        constrains: new[] { GenericConstrains.Any },
        argNames: new[] { "arr", "f" })) { }

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
    public FoldWithDefaultsGenericFunctionDefinition() : base(new FunctionSignatureDescription(
        name: "fold",
        outputType: FunnyType.Generic(1),
        inputTypes: new[] { FunnyType.ArrayOf(FunnyType.Generic(0)), FunnyType.Generic(1), FunnyType.FunOf( returnType: FunnyType.Generic(1), FunnyType.Generic(1), FunnyType.Generic(0)) },
        isExtension: true,
        argNames: new[] { "arr", "seed", "f" })) { }

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
    public UniteGenericFunctionDefinition() : base(new FunctionSignatureDescription(
        name: "unite",
        outputType: FunnyType.ArrayOf(FunnyType.Generic(0)),
        inputTypes: new[] { FunnyType.EnumerableOf(FunnyType.Generic(0)), FunnyType.EnumerableOf(FunnyType.Generic(0)) },
        isExtension: true,
        argNames: new[] { "a", "b" })) { }

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
    public UniqueGenericFunctionDefinition() : base(new FunctionSignatureDescription(
        name: "unique",
        outputType: FunnyType.ArrayOf(FunnyType.Generic(0)),
        inputTypes: new[] { FunnyType.EnumerableOf(FunnyType.Generic(0)), FunnyType.EnumerableOf(FunnyType.Generic(0)) },
        isExtension: true,
        argNames: new[] { "a", "b" })) { }

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
    public IntersectGenericFunctionDefinition() : base(new FunctionSignatureDescription(
        name: "intersect",
        outputType: FunnyType.ArrayOf(FunnyType.Generic(0)),
        inputTypes: new[] { FunnyType.EnumerableOf(FunnyType.Generic(0)), FunnyType.EnumerableOf(FunnyType.Generic(0)) },
        isExtension: true,
        argNames: new[] { "a", "b" })) { }

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
    public ConcatArraysGenericFunctionDefinition() : base(new FunctionSignatureDescription(
        name: "concat",
        outputType: FunnyType.ArrayOf(FunnyType.Generic(0)),
        inputTypes: new[] { FunnyType.ArrayOf(FunnyType.Generic(0)), FunnyType.ArrayOf(FunnyType.Generic(0)) },
        isExtension: true,
        argNames: new[] { "a", "b" })) { }

    protected override object Calc(object a, object b) {
        var arr1 = (IFunnyArray)a;
        var arr2 = (IFunnyArray)b;
        return FunnyArrayTools.CreateEnumerable(arr1.Concat(arr2), arr1.ElementType);
    }
}

public class AppendGenericFunctionDefinition : GenericFunctionWithTwoArguments {
    public AppendGenericFunctionDefinition() : base(new FunctionSignatureDescription(
        name: "append",
        outputType: FunnyType.ArrayOf(FunnyType.Generic(0)),
        inputTypes: new[] { FunnyType.ArrayOf(FunnyType.Generic(0)), FunnyType.Generic(0) },
        isExtension: true,
        argNames: new[] { "arr", "element" })) { }

    protected override object Calc(object a, object b) {
        var arr1 = (IFunnyArray)a;
        var arr2 = b;
        var res = FunnyArrayTools.CreateEnumerable(arr1.Append(arr2), arr1.ElementType);
        return res;
    }
}

public class SubstractArraysGenericFunctionDefinition : GenericFunctionBase {
    public SubstractArraysGenericFunctionDefinition() : base(new FunctionSignatureDescription(
        name: "except",
        outputType: FunnyType.ArrayOf(FunnyType.Generic(0)),
        inputTypes: new[] { FunnyType.EnumerableOf(FunnyType.Generic(0)), FunnyType.EnumerableOf(FunnyType.Generic(0)) },
        isExtension: true,
        argNames: new[] { "a", "b" })) { }

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
    public CountOfGenericFunctionDefinition(): base(new FunctionSignatureDescription(
        name: "count",
        outputType: FunnyType.Int32,
        inputTypes: new[] { FunnyType.EnumerableOf(FunnyType.Generic(0)), FunnyType.FunOf(FunnyType.Bool, FunnyType.Generic(0)) },
        isExtension: true,
        argNames: new[] { "arr", "predicate" })) { }

    protected override object Calc(object a, object b) {
        if (a is NFun.Types.FunnyNone) return 0;
        var arr = (NFun.Runtime.Lists.IFunnyEnumerable)a;
        var filter = (IConcreteFunction)b;

        return arr.Count(arg => (bool)filter.Calc(new[] { arg }));
    }
}

public class HasAnyGenericFunctionDefinition : GenericFunctionWithSingleArgument {
    public HasAnyGenericFunctionDefinition(): base(new FunctionSignatureDescription(
        name: "any",
        outputType: FunnyType.Bool,
        inputTypes: new[] { FunnyType.EnumerableOf(FunnyType.Generic(0)) },
        isExtension: true,
        argNames: new[] { "arr" })) { }

    protected override object Calc(object a) {
        if (a is NFun.Types.FunnyNone) return false;
        return ((NFun.Runtime.Lists.IFunnyEnumerable)a).Count > 0;
    }
}

public class AnyGenericFunctionDefinition : GenericFunctionWithTwoArguments {
    public AnyGenericFunctionDefinition(): base(new FunctionSignatureDescription(
        name: "any",
        outputType: FunnyType.Bool,
        inputTypes: new[] { FunnyType.EnumerableOf(FunnyType.Generic(0)), FunnyType.FunOf(FunnyType.Bool, FunnyType.Generic(0)) },
        isExtension: true,
        argNames: new[] { "arr", "predicate" })) { }

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
    public AllGenericFunctionDefinition(): base(new FunctionSignatureDescription(
        name: "all",
        outputType: FunnyType.Bool,
        inputTypes: new[] { FunnyType.EnumerableOf(FunnyType.Generic(0)), FunnyType.FunOf(FunnyType.Bool, FunnyType.Generic(0)) },
        isExtension: true,
        argNames: new[] { "arr", "predicate" })) { }

    protected override object Calc(object a, object b) {
        if (a is NFun.Types.FunnyNone) return true; // vacuously true
        var arr = (NFun.Runtime.Lists.IFunnyEnumerable)a;
        var filter = (IConcreteFunction)b;

        return arr.All(e => (bool)filter.Calc(new[] { e }));
    }
}

public class FilterGenericFunctionDefinition : GenericFunctionBase {
    public FilterGenericFunctionDefinition() : base(new FunctionSignatureDescription(
        name: "filter",
        outputType: FunnyType.ArrayOf(FunnyType.Generic(0)),
        inputTypes: new[] { FunnyType.EnumerableOf(FunnyType.Generic(0)), FunnyType.FunOf(FunnyType.Bool, FunnyType.Generic(0)) },
        isExtension: true,
        argNames: new[] { "arr", "predicate" })) { }

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
    public RepeatGenericFunctionDefinition() : base(new FunctionSignatureDescription(
        name: "repeat",
        outputType: FunnyType.ArrayOf(FunnyType.Generic(0)),
        inputTypes: new[] { FunnyType.Generic(0), FunnyType.Int32 },
        isExtension: true,
        argNames: new[] { "element", "count" })) { }

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
    public ReverseGenericFunctionDefinition() : base(new FunctionSignatureDescription(
        name: "reverse",
        outputType: FunnyType.ArrayOf(FunnyType.Generic(0)),
        inputTypes: new[] { FunnyType.ArrayOf(FunnyType.Generic(0)) },
        isExtension: true,
        argNames: new[] { "arr" })) { }

    protected override object Calc(object a) {
        var arr = (IFunnyArray)a;
        return FunnyArrayTools.CreateEnumerable(arr.Reverse(), arr.ElementType);
    }
}

public class TakeGenericFunctionDefinition : GenericFunctionBase {
    public TakeGenericFunctionDefinition() : base(new FunctionSignatureDescription(
        name: "take",
        outputType: FunnyType.ArrayOf(FunnyType.Generic(0)),
        inputTypes: new[] { FunnyType.EnumerableOf(FunnyType.Generic(0)), FunnyType.Int32 },
        isExtension: true,
        argNames: new[] { "arr", "count" })) { }

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
    public SkipGenericFunctionDefinition() : base(new FunctionSignatureDescription(
        name: "skip",
        outputType: FunnyType.ArrayOf(FunnyType.Generic(0)),
        inputTypes: new[] { FunnyType.EnumerableOf(FunnyType.Generic(0)), FunnyType.Int32 },
        isExtension: true,
        argNames: new[] { "arr", "count" })) { }

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
