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
        FunnyType.ArrayOf(FunnyType.Generic(0))) { }

    protected override object Calc(object[] args) {
        var arr = (IFunnyArray)args[0];
        var ans = arr.GetElementOrNull(arr.Count - 1);
        return ans ?? throw new FunnyRuntimeException("Array is empty");
    }
}

public class FirstFunction : GenericFunctionBase {
    public FirstFunction() : base(
        "first",
        FunnyType.Generic(0),
        FunnyType.ArrayOf(FunnyType.Generic(0))) { }

    protected override object Calc(object[] args) {
        var arr = (IFunnyArray)args[0];
        var ans = arr.GetElementOrNull(0);
        return ans ?? throw new FunnyRuntimeException("Array is empty");
    }
}

public class CountFunction : GenericFunctionWithSingleArgument {
    public CountFunction() : base(
        "count",
        FunnyType.Int32,
        FunnyType.ArrayOf(FunnyType.Generic(0))) { }

    protected override object Calc(object a)
        => ((IFunnyArray)a).Count;
}

public class MapFunction : GenericFunctionBase {
    public MapFunction() : base(
        "map",
        FunnyType.ArrayOf(FunnyType.Generic(1)),
        FunnyType.ArrayOf(FunnyType.Generic(0)),
        FunnyType.FunOf(FunnyType.Generic(1), FunnyType.Generic(0))) { }

    public override IConcreteFunction CreateConcrete(FunnyType[] concreteTypesMap, IFunctionSelectorContext context) {
        var res = new ConcreteMap {
            Name = Name,
            ArgTypes = new[] {
                FunnyType.ArrayOf(concreteTypesMap[0]),
                FunnyType.FunOf(concreteTypesMap[1], concreteTypesMap[0])
            },
            ReturnType = FunnyType.ArrayOf(concreteTypesMap[1])
        };
        return res;
    }

    private class ConcreteMap : FunctionWithTwoArgs {
        public override object Calc(object a, object b) {
            var arr = (IFunnyArray)a;
            var type = ReturnType.ArrayTypeSpecification.FunnyType;
            if (b is FunctionWithSingleArg mapFunc)
                return FunnyArrayTools.CreateEnumerable(arr.Select(e => mapFunc.Calc(e)), type);

            var map = (IConcreteFunction)b;

            return FunnyArrayTools.CreateEnumerable(arr.Select(e => map.Calc(new[] { e })), type);
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
        FunnyType.FunOf(FunnyType.Generic(1), FunnyType.Generic(0))) { }

    public override IConcreteFunction CreateConcrete(FunnyType[] concreteTypes, IFunctionSelectorContext context) {
        var concrete = concreteTypes[1].BaseType switch {
                           BaseFunnyType.UInt32 => new ConcreteMapSumBase((a, b) => (UInt32)a + (UInt32)b, (UInt32)0),
                           BaseFunnyType.UInt64 => new ConcreteMapSumBase((a, b) => (UInt64)a + (UInt64)b, (UInt64)0),
                           BaseFunnyType.Int32  => new ConcreteMapSumBase((a, b) => (Int32)a + (Int32)b, 0),
                           BaseFunnyType.Int64  => new ConcreteMapSumBase((a, b) => (Int64)a + (Int64)b, (Int64)0),
                           BaseFunnyType.Real => context.RealTypeSelect(
                               ifIsDouble: new ConcreteMapSumBase((a, b) => (double)a + (double)b, (double)0),
                               ifIsDecimal: new ConcreteMapSumBase((a, b) => (decimal)a + (decimal)b, (decimal)0)),
                           _ => throw new ArgumentOutOfRangeException()
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
        FunnyType.ArrayOf(FunnyType.Generic(0))) { }

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
        FunnyType.Int32) { }

    protected override object Calc(object[] args) {
        var start = (int)args[1];
        if (start < 0)
            throw new FunnyRuntimeException("Argument out of range");
        var end = (int)args[2];
        if (end < 0)
            throw new FunnyRuntimeException("Argument out of range");
        if (end != 0 && start > end)
            throw new FunnyRuntimeException("Start cannot be more than end");
        var step = (int)args[3];
        if (step < 0)
            throw new FunnyRuntimeException("Argument out of range");
        if (step == 0)
            step = 1;
        var arr = (IFunnyArray)args[0];
        return arr.Slice(start, end, step);
    }
}

public class SortFunction : GenericFunctionBase {
    public SortFunction() : base(
        "sort", GenericConstrains.Comparable,
        FunnyType.ArrayOf(FunnyType.Generic(0)), FunnyType.ArrayOf(FunnyType.Generic(0))) { }

    protected override object Calc(object[] args) {
        var funArray = (IFunnyArray)args[0];

        var arr = funArray.As<IComparable>().ToArray(funArray.Count);
        Array.Sort(arr);
        return FunnyArrayTools.CreateArray(arr, funArray.ElementType);
    }
}

public class SortDescendingFunction : GenericFunctionBase {
    public SortDescendingFunction() : base(
        "sortDescending", GenericConstrains.Comparable,
        FunnyType.ArrayOf(FunnyType.Generic(0)), FunnyType.ArrayOf(FunnyType.Generic(0))) { }

    protected override object Calc(object[] args) {
        var funArray = (IFunnyArray)args[0];

        var arr = funArray.As<IComparable>().ToArray(funArray.Count);
        Array.Sort(arr);
        Array.Reverse(arr);
        return FunnyArrayTools.CreateArray(arr, funArray.ElementType);
    }
}

public class SortMapFunction : GenericFunctionBase {
    public SortMapFunction() : base(
        "sort", new[] { GenericConstrains.Any, GenericConstrains.Comparable },
        FunnyType.ArrayOf(FunnyType.Generic(0)),
        FunnyType.ArrayOf(FunnyType.Generic(0)),
        FunnyType.FunOf(FunnyType.Generic(1), FunnyType.Generic(0))) { }

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
        FunnyType.FunOf(FunnyType.Generic(1), FunnyType.Generic(0))) { }

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
        FunnyType.ArrayOf(FunnyType.Generic(0))) { }

    protected override object Calc(object[] args)
    {
        var array = (IFunnyArray)args[0];
        return GetMedian(array.As<IComparable>(), array.Count);
    }

    private static IComparable GetMedian(IEnumerable<IComparable> source, int size) {
        // Create a copy of the input, and sort the copy
        var temp = source.ToArray(size);
        Array.Sort(temp);

        int count = temp.Length;
        if (count == 0)
            throw new InvalidOperationException("Empty collection");
        return temp[(count - 1) / 2];
    }
}

public class MaxElementFunction : GenericFunctionBase {
    public MaxElementFunction() : base(
        "max", GenericConstrains.Comparable, FunnyType.Generic(0),
        FunnyType.ArrayOf(FunnyType.Generic(0))) { }

    protected override object Calc(object[] args) {
        var array = (IFunnyArray)args[0];
        return array.As<IComparable>().Max();
    }
}

public class MinElementFunction : GenericFunctionBase {
    public MinElementFunction() : base(
        "min", GenericConstrains.Comparable, FunnyType.Generic(0),
        FunnyType.ArrayOf(FunnyType.Generic(0))) { }

    protected override object Calc(object[] args) {
        var array = (IFunnyArray)args[0];
        return array.As<IComparable>().Min();
    }
}

public class SliceGenericFunctionDefinition : GenericFunctionBase {
    public SliceGenericFunctionDefinition() : base(
        CoreFunNames.SliceName,
        FunnyType.ArrayOf(FunnyType.Generic(0)),
        FunnyType.ArrayOf(FunnyType.Generic(0)),
        FunnyType.Int32,
        FunnyType.Int32) { }

    protected override object Calc(object[] args) {
        var start = (int)args[1];
        if (start < 0)
            throw new FunnyRuntimeException("Argument out of range");

        var end = (int)args[2];
        if (end < 0)
            throw new FunnyRuntimeException("Argument out of range");

        if (end != 0 && start > end)
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
    public SetGenericFunctionDefinition() : base(
        "set",
        FunnyType.ArrayOf(FunnyType.Generic(0)),
        FunnyType.ArrayOf(FunnyType.Generic(0)),
        FunnyType.Int32,
        FunnyType.Generic(0)) { }

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

public class FindGenericFunctionDefinition : GenericFunctionWithTwoArguments {
    public FindGenericFunctionDefinition() : base(
        "find",
        FunnyType.Int32,
        FunnyType.ArrayOf(FunnyType.Generic(0)),
        FunnyType.Generic(0)) { }

    protected override object Calc(object a, object b) {
        var arr = (IFunnyArray)a;
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

public class ChunkGenericFunctionDefinition : GenericFunctionWithTwoArguments {
    public ChunkGenericFunctionDefinition() : base(
        "chunk",
        FunnyType.ArrayOf(FunnyType.ArrayOf(FunnyType.Generic(0))),
        FunnyType.ArrayOf(FunnyType.Generic(0)),
        FunnyType.Int32) { }

    protected override object Calc(object a, object b) {
        var arr = (IFunnyArray)a;
        var chunkSize = (int)b;
        if (chunkSize <= 0)
            throw new FunnyRuntimeException($"Chunk size is {chunkSize}. It has to be positive");

        var elementType = arr.ElementType;
        var result = new List<IFunnyArray>();

        int i = 0;
        while (i < arr.Count) {
            int size = Math.Min(chunkSize, arr.Count - i);
            var chunk = new object[size];
            Array.Copy(arr.ClrArray, i, chunk, 0, size);
            result.Add(new ImmutableFunnyArray(chunk, elementType));
            i += size;
        }

        return new EnumerableFunnyArray(result, FunnyType.ArrayOf(FunnyType.ArrayOf(elementType)));
    }

}

public class FlatGenericFunctionDefinition : GenericFunctionWithSingleArgument {
    public FlatGenericFunctionDefinition() : base(
        "flat",
        FunnyType.ArrayOf(FunnyType.Generic(0)),
        FunnyType.ArrayOf(FunnyType.ArrayOf(FunnyType.Generic(0)))) { }

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
        FunnyType.FunOf(FunnyType.Generic(0), FunnyType.Generic(0), FunnyType.Generic(0))) { }

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
            returnType: FunnyType.Generic(1), FunnyType.Generic(1), FunnyType.Generic(0))) { }

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

public class UniteGenericFunctionDefinition : GenericFunctionWithTwoArguments {
    public UniteGenericFunctionDefinition() : base(
        "unite",
        FunnyType.ArrayOf(FunnyType.Generic(0)),
        FunnyType.ArrayOf(FunnyType.Generic(0)),
        FunnyType.ArrayOf(FunnyType.Generic(0))) { }

    protected override object Calc(object a, object b) {
        var arr1 = (IFunnyArray)a;
        var arr2 = (IFunnyArray)b;
        return FunnyArrayTools.CreateEnumerable(arr1.Union(arr2), arr1.ElementType);
    }
}

public class UniqueGenericFunctionDefinition : GenericFunctionWithTwoArguments {
    public UniqueGenericFunctionDefinition() : base(
        "unique",
        FunnyType.ArrayOf(FunnyType.Generic(0)),
        FunnyType.ArrayOf(FunnyType.Generic(0)),
        FunnyType.ArrayOf(FunnyType.Generic(0))) { }

    protected override object Calc(object a, object b) {
        var arr1 = (IFunnyArray)a;
        var arr2 = (IFunnyArray)b;
        return FunnyArrayTools.CreateEnumerable(arr1.Except(arr2).Concat(arr2.Except(arr1)), arr1.ElementType);
    }
}

public class IntersectGenericFunctionDefinition : GenericFunctionWithTwoArguments {
    public IntersectGenericFunctionDefinition() : base(
        "intersect",
        FunnyType.ArrayOf(FunnyType.Generic(0)),
        FunnyType.ArrayOf(FunnyType.Generic(0)),
        FunnyType.ArrayOf(FunnyType.Generic(0))) { }

    protected override object Calc(object a, object b) {
        var arr1 = (IFunnyArray)a;
        var arr2 = (IFunnyArray)b;
        return FunnyArrayTools.CreateEnumerable(arr1.Intersect(arr2), arr1.ElementType);
    }
}

public class ConcatArraysGenericFunctionDefinition : GenericFunctionWithTwoArguments {
    public ConcatArraysGenericFunctionDefinition() : base(
        "concat",
        FunnyType.ArrayOf(FunnyType.Generic(0)),
        FunnyType.ArrayOf(FunnyType.Generic(0)),
        FunnyType.ArrayOf(FunnyType.Generic(0))) { }

    protected override object Calc(object a, object b) {
        var arr1 = (IFunnyArray)a;
        var arr2 = (IFunnyArray)b;
        var res = FunnyArrayTools.CreateEnumerable(arr1.Concat(arr2), arr1.ElementType);
        return res;
    }
}

public class AppendGenericFunctionDefinition : GenericFunctionWithTwoArguments {
    public AppendGenericFunctionDefinition() : base(
        "append",
        FunnyType.ArrayOf(FunnyType.Generic(0)),
        FunnyType.ArrayOf(FunnyType.Generic(0)),
        FunnyType.Generic(0)) { }

    protected override object Calc(object a, object b) {
        var arr1 = (IFunnyArray)a;
        var arr2 = b;
        var res = FunnyArrayTools.CreateEnumerable(arr1.Append(arr2), arr1.ElementType);
        return res;
    }
}

public class SubstractArraysGenericFunctionDefinition : GenericFunctionWithTwoArguments {
    public SubstractArraysGenericFunctionDefinition() : base(
        "except",
        FunnyType.ArrayOf(FunnyType.Generic(0)),
        FunnyType.ArrayOf(FunnyType.Generic(0)),
        FunnyType.ArrayOf(FunnyType.Generic(0))) { }

    protected override object Calc(object a, object b) {
        var arr1 = (IFunnyArray)a;
        var arr2 = (IFunnyArray)b;
        return FunnyArrayTools.CreateEnumerable(arr1.Except(arr2), arr1.ElementType);
    }
}

public class CountOfGenericFunctionDefinition : GenericFunctionWithTwoArguments {
    public CountOfGenericFunctionDefinition() : base(
        "count",
        FunnyType.Int32,
        FunnyType.ArrayOf(FunnyType.Generic(0)),
        FunnyType.FunOf(FunnyType.Bool, FunnyType.Generic(0))) { }

    protected override object Calc(object a, object b) {
        var arr = (IFunnyArray)a;
        var filter = (IConcreteFunction)b;

        return arr.Count(arg => (bool)filter.Calc(new[] { arg }));
    }
}

public class HasAnyGenericFunctionDefinition : GenericFunctionWithSingleArgument {
    public HasAnyGenericFunctionDefinition() : base(
        "any",
        FunnyType.Bool,
        FunnyType.ArrayOf(FunnyType.Generic(0))) { }

    protected override object Calc(object a)
        => ((IFunnyArray)a).Count > 0;
}

public class AnyGenericFunctionDefinition : GenericFunctionWithTwoArguments {
    public AnyGenericFunctionDefinition() : base(
        "any",
        FunnyType.Bool,
        FunnyType.ArrayOf(FunnyType.Generic(0)),
        FunnyType.FunOf(FunnyType.Bool, FunnyType.Generic(0))) { }

    protected override object Calc(object a, object b) {
        var arr = (IFunnyArray)a;

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
        FunnyType.ArrayOf(FunnyType.Generic(0)),
        FunnyType.FunOf(FunnyType.Bool, FunnyType.Generic(0))) { }

    protected override object Calc(object a, object b) {
        var arr = (IFunnyArray)a;
        var filter = (IConcreteFunction)b;

        return arr.All(e => (bool)filter.Calc(new[] { e }));
    }
}

public class FilterGenericFunctionDefinition : GenericFunctionWithTwoArguments {
    public FilterGenericFunctionDefinition() : base(
        "filter",
        FunnyType.ArrayOf(FunnyType.Generic(0)),
        FunnyType.ArrayOf(FunnyType.Generic(0)),
        FunnyType.FunOf(FunnyType.Bool, FunnyType.Generic(0))) { }

    protected override object Calc(object a, object b) {
        var arr = (IFunnyArray)a;
        if (b is FunctionWithSingleArg predicate)
            return FunnyArrayTools.CreateEnumerable(arr.Where(e => (bool)predicate.Calc(e)), arr.ElementType);
        var filter = (IConcreteFunction)b;

        return FunnyArrayTools.CreateEnumerable(arr.Where(e => (bool)filter.Calc(new[] { e })), arr.ElementType);
    }
}

public class RepeatGenericFunctionDefinition : GenericFunctionBase {
    public RepeatGenericFunctionDefinition() : base(
        "repeat",
        FunnyType.ArrayOf(FunnyType.Generic(0)),
        FunnyType.Generic(0),
        FunnyType.Int32) { }

    public override IConcreteFunction CreateConcrete(FunnyType[] concreteTypesMap, IFunctionSelectorContext context) {
        var res = new ConcreteRepeat {
            Name = Name,
            ArgTypes = new[] { concreteTypesMap[0], FunnyType.Int32 },
            ReturnType = FunnyType.ArrayOf(concreteTypesMap[0])
        };
        return res;
    }

    private class ConcreteRepeat : FunctionWithTwoArgs {
        public override object Calc(object a, object b)
            => FunnyArrayTools.CreateEnumerable(Enumerable.Repeat(a, (int)b), this.ArgTypes[0]);
    }
}

public class ReverseGenericFunctionDefinition : GenericFunctionWithSingleArgument {
    public ReverseGenericFunctionDefinition() : base(
        "reverse",
        FunnyType.ArrayOf(FunnyType.Generic(0)),
        FunnyType.ArrayOf(FunnyType.Generic(0))) { }

    protected override object Calc(object a) {
        var arr = (IFunnyArray)a;
        return FunnyArrayTools.CreateEnumerable(arr.Reverse(), arr.ElementType);
    }
}

public class TakeGenericFunctionDefinition : GenericFunctionWithTwoArguments {
    public TakeGenericFunctionDefinition() : base(
        "take",
        FunnyType.ArrayOf(FunnyType.Generic(0)),
        FunnyType.ArrayOf(FunnyType.Generic(0)),
        FunnyType.Int32) { }

    protected override object Calc(object a, object b)
        => ((IFunnyArray)a).Slice(null, (int)b - 1, 1);
}

public class SkipGenericFunctionDefinition : GenericFunctionWithTwoArguments {
    public SkipGenericFunctionDefinition() : base(
        "skip",
        FunnyType.ArrayOf(FunnyType.Generic(0)),
        FunnyType.ArrayOf(FunnyType.Generic(0)),
        FunnyType.Int32) { }

    protected override object Calc(object a, object b)
        => ((IFunnyArray)a).Slice((int)b, null, 1);
}
