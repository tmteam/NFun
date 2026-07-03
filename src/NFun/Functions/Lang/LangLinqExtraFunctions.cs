using System.Collections.Generic;
using System.Linq;
using NFun.Exceptions;
using NFun.Functions.Collections;
using NFun.Interpretation.Functions;
using NFun.Runtime.Arrays;
using NFun.Runtime.Lists;
using NFun.Types;

namespace NFun.Functions.Lang;

// Round 6 lang LINQ migration — continuation. Functions returning `T[]` (legacy
// StateArray) get FixedArray-returning lang variants. See
// `LangLinqFunctions.cs` for the first batch and architectural rationale.

public class UniteEnumerableFunction : GenericFunctionBase {
    public UniteEnumerableFunction() : base(
        "unite",
        FunnyType.FixedArrayOf(FunnyType.Generic(0)),
        FunnyType.EnumerableOf(FunnyType.Generic(0)),
        FunnyType.EnumerableOf(FunnyType.Generic(0))) { ArgProperties = FunArgProperty.FromNames("a", "b"); }

    public override IConcreteFunction CreateConcrete(FunnyType[] concrete, IFunctionSelectorContext _) =>
        new Impl(concrete[0]);

    private sealed class Impl : FunctionWithTwoArgs {
        private readonly FunnyType _elem;
        public Impl(FunnyType elem) : base("unite",
            FunnyType.FixedArrayOf(elem),
            FunnyType.EnumerableOf(elem),
            FunnyType.EnumerableOf(elem)) => _elem = elem;
        public override object Calc(object a, object b) {
            if (a is FunnyNone && b is FunnyNone)
                return new FixedFunnyArray(_elem, System.Array.Empty<object>());
            var arr1 = ToXxxRuntimeIter.AsObjects(a);
            var arr2 = ToXxxRuntimeIter.AsObjects(b);
            return new FixedFunnyArray(_elem, arr1.Union(arr2).ToArray());
        }
    }
}

public class UniqueEnumerableFunction : GenericFunctionBase {
    public UniqueEnumerableFunction() : base(
        "unique",
        FunnyType.FixedArrayOf(FunnyType.Generic(0)),
        FunnyType.EnumerableOf(FunnyType.Generic(0)),
        FunnyType.EnumerableOf(FunnyType.Generic(0))) { ArgProperties = FunArgProperty.FromNames("a", "b"); }

    public override IConcreteFunction CreateConcrete(FunnyType[] concrete, IFunctionSelectorContext _) =>
        new Impl(concrete[0]);

    private sealed class Impl : FunctionWithTwoArgs {
        private readonly FunnyType _elem;
        public Impl(FunnyType elem) : base("unique",
            FunnyType.FixedArrayOf(elem),
            FunnyType.EnumerableOf(elem),
            FunnyType.EnumerableOf(elem)) => _elem = elem;
        public override object Calc(object a, object b) {
            if (a is FunnyNone && b is FunnyNone)
                return new FixedFunnyArray(_elem, System.Array.Empty<object>());
            var arr1 = ToXxxRuntimeIter.AsObjects(a).ToList();
            var arr2 = ToXxxRuntimeIter.AsObjects(b).ToList();
            return new FixedFunnyArray(_elem, arr1.Except(arr2).Concat(arr2.Except(arr1)).ToArray());
        }
    }
}

public class IntersectEnumerableFunction : GenericFunctionBase {
    public IntersectEnumerableFunction() : base(
        "intersect",
        FunnyType.FixedArrayOf(FunnyType.Generic(0)),
        FunnyType.EnumerableOf(FunnyType.Generic(0)),
        FunnyType.EnumerableOf(FunnyType.Generic(0))) { ArgProperties = FunArgProperty.FromNames("a", "b"); }

    public override IConcreteFunction CreateConcrete(FunnyType[] concrete, IFunctionSelectorContext _) =>
        new Impl(concrete[0]);

    private sealed class Impl : FunctionWithTwoArgs {
        private readonly FunnyType _elem;
        public Impl(FunnyType elem) : base("intersect",
            FunnyType.FixedArrayOf(elem),
            FunnyType.EnumerableOf(elem),
            FunnyType.EnumerableOf(elem)) => _elem = elem;
        public override object Calc(object a, object b) {
            if (a is FunnyNone || b is FunnyNone)
                return new FixedFunnyArray(_elem, System.Array.Empty<object>());
            var arr1 = ToXxxRuntimeIter.AsObjects(a);
            var arr2 = ToXxxRuntimeIter.AsObjects(b);
            return new FixedFunnyArray(_elem, arr1.Intersect(arr2).ToArray());
        }
    }
}

public class ExceptEnumerableFunction : GenericFunctionBase {
    public ExceptEnumerableFunction() : base(
        "except",
        FunnyType.FixedArrayOf(FunnyType.Generic(0)),
        FunnyType.EnumerableOf(FunnyType.Generic(0)),
        FunnyType.EnumerableOf(FunnyType.Generic(0))) { ArgProperties = FunArgProperty.FromNames("a", "b"); }

    public override IConcreteFunction CreateConcrete(FunnyType[] concrete, IFunctionSelectorContext _) =>
        new Impl(concrete[0]);

    private sealed class Impl : FunctionWithTwoArgs {
        private readonly FunnyType _elem;
        public Impl(FunnyType elem) : base("except",
            FunnyType.FixedArrayOf(elem),
            FunnyType.EnumerableOf(elem),
            FunnyType.EnumerableOf(elem)) => _elem = elem;
        public override object Calc(object a, object b) {
            var arr1 = ToXxxRuntimeIter.AsObjects(a);
            var arr2 = ToXxxRuntimeIter.AsObjects(b);
            return new FixedFunnyArray(_elem, arr1.Except(arr2).ToArray());
        }
    }
}

public class AppendEnumerableFunction : GenericFunctionWithTwoArguments {
    public AppendEnumerableFunction() : base(
        "append",
        FunnyType.FixedArrayOf(FunnyType.Generic(0)),
        FunnyType.EnumerableOf(FunnyType.Generic(0)),
        FunnyType.Generic(0)) { ArgProperties = FunArgProperty.FromNames("arr", "element"); }

    protected override object Calc(object a, object b) {
        var elemType = ReturnType.FixedArrayTypeSpecification.FunnyType;
        var items = ToXxxRuntimeIter.AsObjects(a).Append(b).ToArray();
        return new FixedFunnyArray(elemType, items);
    }
}

public class RepeatEnumerableFunction : GenericFunctionBase {
    // `repeat(elem, count)` is a CONSTRUCTOR (not a LINQ transformation), so the
    // result is `array<T>` (MutableArrayOf — mutable fixed-length) rather than
    // `fixedArray<T>`. Allows downstream `setAt(repeat(...), i, v)` and similar
    // indexed-write idioms (LeetCode 1859).
    public RepeatEnumerableFunction() : base(
        "repeat",
        FunnyType.MutableArrayOf(FunnyType.Generic(0)),
        FunnyType.Generic(0),
        FunnyType.Int32) { ArgProperties = FunArgProperty.FromNames("element", "count"); }

    public override IConcreteFunction CreateConcrete(FunnyType[] concreteTypesMap, IFunctionSelectorContext context) {
        var elem = concreteTypesMap[0];
        return new ConcreteRepeat(elem) {
            Name = Name,
            ArgTypes = new[] { elem, FunnyType.Int32 },
            ReturnType = FunnyType.MutableArrayOf(elem)
        };
    }

    private class ConcreteRepeat : FunctionWithTwoArgs {
        private readonly FunnyType _elem;
        public ConcreteRepeat(FunnyType elem) { _elem = elem; }
        public override object Calc(object a, object b) {
            var count = (int)b;
            if (count < 0) throw new FunnyRuntimeException("Repeat count cannot be negative");
            return new MutableFunnyArray(_elem, Enumerable.Repeat(a, count).ToArray());
        }
    }
}

public class SliceEnumerableFunction : GenericFunctionBase {
    public SliceEnumerableFunction() : base(
        CoreFunNames.SliceName,
        FunnyType.FixedArrayOf(FunnyType.Generic(0)),
        FunnyType.EnumerableOf(FunnyType.Generic(0)),
        FunnyType.Int32,
        FunnyType.Int32) { ArgProperties = FunArgProperty.FromNames("arr", "from", "to"); }

    protected override object Calc(object[] args) {
        var start = (int)args[1];
        if (start < 0) throw new FunnyRuntimeException("Argument out of range");
        var end = (int)args[2];
        if (end < 0) throw new FunnyRuntimeException("Argument out of range");
        if (start > end) throw new FunnyRuntimeException("Start cannot be more than end");
        var elemType = ReturnType.FixedArrayTypeSpecification.FunnyType;
        var src = ToXxxRuntimeIter.AsObjects(args[0]).ToList();
        var stop = end == int.MaxValue ? src.Count : System.Math.Min(end + 1, src.Count);
        if (start >= src.Count)
            return new FixedFunnyArray(elemType, System.Array.Empty<object>());
        return new FixedFunnyArray(elemType, src.GetRange(start, stop - start).ToArray());
    }
}

public class SliceWithStepEnumerableFunction : GenericFunctionBase {
    public SliceWithStepEnumerableFunction() : base(
        CoreFunNames.SliceName,
        FunnyType.FixedArrayOf(FunnyType.Generic(0)),
        FunnyType.EnumerableOf(FunnyType.Generic(0)),
        FunnyType.Int32,
        FunnyType.Int32,
        FunnyType.Int32) { ArgProperties = FunArgProperty.FromNames("arr", "from", "to", "step"); }

    protected override object Calc(object[] args) {
        var start = (int)args[1];
        var end = (int)args[2];
        var step = (int)args[3];
        if (step <= 0) throw new FunnyRuntimeException("Argument out of range");
        if (start < 0 || end < 0) throw new FunnyRuntimeException("Argument out of range");
        var elemType = ReturnType.FixedArrayTypeSpecification.FunnyType;
        var src = ToXxxRuntimeIter.AsObjects(args[0]).ToList();
        var stop = end == int.MaxValue ? src.Count : System.Math.Min(end + 1, src.Count);
        if (start >= src.Count)
            return new FixedFunnyArray(elemType, System.Array.Empty<object>());
        var result = new List<object>();
        for (int i = start; i < stop; i += step) result.Add(src[i]);
        return new FixedFunnyArray(elemType, result.ToArray());
    }
}

public class ChunkEnumerableFunction : GenericFunctionBase {
    public ChunkEnumerableFunction() : base(
        "chunk",
        FunnyType.FixedArrayOf(FunnyType.FixedArrayOf(FunnyType.Generic(0))),
        FunnyType.EnumerableOf(FunnyType.Generic(0)),
        FunnyType.Int32) { ArgProperties = FunArgProperty.FromNames("arr", "size"); }

    public override IConcreteFunction CreateConcrete(FunnyType[] concrete, IFunctionSelectorContext _) =>
        new Impl(concrete[0]);

    private sealed class Impl : FunctionWithTwoArgs {
        private readonly FunnyType _elem;
        public Impl(FunnyType elem) : base("chunk",
            FunnyType.FixedArrayOf(FunnyType.FixedArrayOf(elem)),
            FunnyType.EnumerableOf(elem),
            FunnyType.Int32) => _elem = elem;
        public override object Calc(object a, object b) {
            var chunkSize = (int)b;
            if (chunkSize <= 0)
                throw new FunnyRuntimeException($"Chunk size is {chunkSize}. It has to be positive");
            var src = ToXxxRuntimeIter.AsObjects(a).ToList();
            var result = new List<object>();
            int i = 0;
            while (i < src.Count) {
                int size = System.Math.Min(chunkSize, src.Count - i);
                var chunk = new object[size];
                for (int j = 0; j < size; j++) chunk[j] = src[i + j];
                result.Add(new FixedFunnyArray(_elem, chunk));
                i += size;
            }
            return new FixedFunnyArray(FunnyType.FixedArrayOf(_elem), result.ToArray());
        }
    }
}

public class FoldEnumerableFunction : GenericFunctionWithTwoArguments {
    public FoldEnumerableFunction() : base(
        "fold", new[] { GenericConstrains.Any },
        FunnyType.Generic(0),
        FunnyType.EnumerableOf(FunnyType.Generic(0)),
        FunnyType.FunOf(FunnyType.Generic(0), FunnyType.Generic(0), FunnyType.Generic(0))) { ArgProperties = FunArgProperty.FromNames("arr", "f"); }

    protected override object Calc(object a, object b) {
        var src = ToXxxRuntimeIter.AsObjects(a).ToList();
        if (src.Count == 0)
            throw new FunnyRuntimeException("Input array is empty");
        if (b is FunctionWithTwoArgs fold2)
            return src.Aggregate((l, r) => fold2.Calc(l, r));
        var fold = (IConcreteFunction)b;
        return src.Aggregate((l, r) => fold.Calc(new[] { l, r }));
    }
}

public class FoldWithDefaultEnumerableFunction : GenericFunctionBase {
    public FoldWithDefaultEnumerableFunction() : base(
        "fold",
        returnType: FunnyType.Generic(1),
        FunnyType.EnumerableOf(FunnyType.Generic(0)),
        FunnyType.Generic(1),
        FunnyType.FunOf(
            returnType: FunnyType.Generic(1), FunnyType.Generic(1), FunnyType.Generic(0))) { ArgProperties = FunArgProperty.FromNames("arr", "seed", "f"); }

    protected override object Calc(object[] args) {
        var src = ToXxxRuntimeIter.AsObjects(args[0]).ToList();
        var defaultValue = args[1];
        var fold = (IConcreteFunction)args[2];
        if (fold is FunctionWithTwoArgs fold2)
            return src.Aggregate(defaultValue, (a, b) => fold2.Calc(a, b));
        return src.Aggregate(defaultValue, (a, b) => fold.Calc(new[] { a, b }));
    }
}
