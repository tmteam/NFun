using System;
using System.Collections.Generic;
using System.Linq;
using NFun.Exceptions;
using NFun.Interpretation.Functions;
using NFun.Runtime.Arrays;
using NFun.Types;

namespace NFun.Functions {

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
        FunnyType.Fun(FunnyType.Generic(1), FunnyType.Generic(0))) { }

    public override IConcreteFunction CreateConcrete(FunnyType[] concreteTypesMap) {
        var res = new ConcreteMap {
            Name = Name,
            ArgTypes = new[] {
                FunnyType.ArrayOf(concreteTypesMap[0]),
                FunnyType.Fun(concreteTypesMap[1], concreteTypesMap[0])
            },
            ReturnType = FunnyType.ArrayOf(concreteTypesMap[1])
        };
        return res;
    }

    class ConcreteMap : FunctionWithTwoArgs {
        public override object Calc(object a, object b) {
            var arr = (IFunnyArray)a;
            var type = ReturnType.ArrayTypeSpecification.FunnyType;
            if (b is FunctionWithSingleArg mapFunc)
                return new EnumerableFunnyArray(arr.Select(e => mapFunc.Calc(e)), type);

            var map = (IConcreteFunction)b;

            return new EnumerableFunnyArray(arr.Select(e => map.Calc(new[] { e })), type);
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
        FunnyType.Fun(FunnyType.Generic(1), FunnyType.Generic(0))) { }

    public override IConcreteFunction CreateConcrete(FunnyType[] concreteTypes) {
        var concrete = concreteTypes[1].BaseType switch {
                           BaseFunnyType.UInt32 => new ConcreteMapSumBase((a, b) => (UInt32)a + (UInt32)b, (UInt32)0),
                           BaseFunnyType.UInt64 => new ConcreteMapSumBase((a, b) => (UInt64)a + (UInt64)b, (UInt64)0),
                           BaseFunnyType.Int32  => new ConcreteMapSumBase((a, b) => (Int32)a + (Int32)b, 0),
                           BaseFunnyType.Int64  => new ConcreteMapSumBase((a, b) => (Int64)a + (Int64)b, (Int64)0),
                           BaseFunnyType.Real   => new ConcreteMapSumBase((a, b) => (double)a + (double)b, (double)0),
                           _                    => throw new ArgumentOutOfRangeException()
                       };
        concrete.Name = Id;
        concrete.ArgTypes = SubstitudeArgTypes(concreteTypes);
        concrete.ReturnType = concreteTypes[1];
        return concrete;
    }

    class ConcreteMapSumBase : FunctionWithTwoArgs {
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
        var start = ((int)args[1]);
        if (start < 0)
            throw new FunnyRuntimeException("Argument out of range");
        var end = ((int)args[2]);
        if (end < 0)
            throw new FunnyRuntimeException("Argument out of range");
        if (end != 0 && start > end)
            throw new FunnyRuntimeException("Start cannot be more than end");
        var step = ((int)args[3]);
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

        var arr = funArray.As<IComparable>().ToArray();
        Array.Sort(arr);
        return new ImmutableFunnyArray(arr, funArray.ElementType);
    }
}

public class MedianFunction : GenericFunctionBase {
    public MedianFunction() : base(
        "median", GenericConstrains.Comparable, FunnyType.Generic(0),
        FunnyType.ArrayOf(FunnyType.Generic(0))) { }

    protected override object Calc(object[] args)
        => GetMedian(((IFunnyArray)args[0]).As<IComparable>());

    private static IComparable GetMedian(IEnumerable<IComparable> source) {
        // Create a copy of the input, and sort the copy
        var temp = source.ToArray();
        Array.Sort(temp);

        int count = temp.Length;
        if (count == 0)
            throw new InvalidOperationException("Empty collection");
        if (count % 2 == 0)
            return temp[count / 2 - 1];
        return temp[count / 2];
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

public class MultiSumFunction : GenericFunctionBase {
    private const string Id = "sum";

    public MultiSumFunction() : base(
        Id, GenericConstrains.Arithmetical, FunnyType.Generic(0),
        FunnyType.ArrayOf(FunnyType.Generic(0))) { }

    public override IConcreteFunction CreateConcrete(FunnyType[] concreteTypes) =>
        concreteTypes[0].BaseType switch {
            BaseFunnyType.UInt16 => new UInt16Function(),
            BaseFunnyType.UInt32 => new UInt32Function(),
            BaseFunnyType.UInt64 => new UInt64Function(),
            BaseFunnyType.Int16  => new Int16Function(),
            BaseFunnyType.Int32  => new Int32Function(),
            BaseFunnyType.Int64  => new Int64Function(),
            BaseFunnyType.Real   => new RealFunction(),
            _                    => throw new ArgumentOutOfRangeException()
        };

    private class RealFunction : FunctionWithSingleArg {
        public RealFunction() : base(Id, FunnyType.Real, FunnyType.ArrayOf(FunnyType.Real)) { }

        public override object Calc(object a) => ((IFunnyArray)a).As<double>().Sum();
    }

    private class Int16Function : FunctionWithSingleArg {
        public Int16Function() : base(Id, FunnyType.Int16, FunnyType.ArrayOf(FunnyType.Int16)) { }

        public override object Calc(object a) {
            short answer = 0;
            foreach (var i in ((IFunnyArray)a).As<short>())
                answer += i;
            return answer;
        }
    }

    private class Int32Function : FunctionWithSingleArg {
        public Int32Function() : base(Id, FunnyType.Int32, FunnyType.ArrayOf(FunnyType.Int32)) { }

        public override object Calc(object a) => ((IFunnyArray)a).As<int>().Sum();
    }

    private class Int64Function : FunctionWithSingleArg {
        public Int64Function() : base(Id, FunnyType.Int64, FunnyType.ArrayOf(FunnyType.Int64)) { }

        public override object Calc(object a) => ((IFunnyArray)a).As<long>().Sum();
    }

    private class UInt16Function : FunctionWithSingleArg {
        public UInt16Function() : base(Id, FunnyType.UInt16, FunnyType.ArrayOf(FunnyType.UInt16)) { }

        public override object Calc(object a) {
            ushort answer = 0;
            foreach (var i in ((IFunnyArray)a).As<ushort>())
                answer += i;
            return answer;
        }
    }

    private class UInt32Function : FunctionWithSingleArg {
        public UInt32Function() : base(Id, FunnyType.UInt32, FunnyType.ArrayOf(FunnyType.UInt32)) { }

        public override object Calc(object a) {
            uint answer = 0;
            foreach (var i in ((IFunnyArray)a).As<uint>())
                answer += i;
            return answer;
        }
    }

    private class UInt64Function : FunctionWithSingleArg {
        public UInt64Function() : base(Id, FunnyType.UInt64, FunnyType.ArrayOf(FunnyType.UInt64)) { }

        public override object Calc(object a) {
            ulong answer = 0;
            foreach (var i in ((IFunnyArray)a).As<ulong>())
                answer += i;
            return answer;
        }
    }
}


public class RangeFunction : GenericFunctionBase {
    public RangeFunction() : base(
        CoreFunNames.RangeName,
        GenericConstrains.Numbers,
        FunnyType.ArrayOf(FunnyType.Generic(0)), FunnyType.Generic(0), FunnyType.Generic(0)) { }

    public override IConcreteFunction CreateConcrete(FunnyType[] concreteTypes) =>
        concreteTypes[0].BaseType switch {
            BaseFunnyType.UInt8  => new UInt8Function(),
            BaseFunnyType.UInt16 => new UInt16Function(),
            BaseFunnyType.UInt32 => new UInt32Function(),
            BaseFunnyType.UInt64 => new UInt64Function(),
            BaseFunnyType.Int16  => new Int16Function(),
            BaseFunnyType.Int32  => new Int32Function(),
            BaseFunnyType.Int64  => new Int64Function(),
            BaseFunnyType.Real   => new RealFunction(),
            _                    => throw new NotSupportedException()
        };

    private const string Id = "range";

    class Int16Function : FunctionWithTwoArgs {
        public Int16Function() : base(Id, FunnyType.ArrayOf(FunnyType.Int16), FunnyType.Int16, FunnyType.Int16) { }

        public override object Calc(object a, object b) {
            var start = ((short)a);
            var end = ((short)b);
            var result = new List<short>();

            if (start < end)
                for (var i = start; i <= end; i += 1)
                    result.Add(i);
            else
                for (var i = start; i >= end; i -= 1)
                    result.Add(i);

            return new ImmutableFunnyArray(result.ToArray(), FunnyType.Int16);
        }
    }

    class Int32Function : FunctionWithTwoArgs {
        public Int32Function() : base(Id, FunnyType.ArrayOf(FunnyType.Int32), FunnyType.Int32, FunnyType.Int32) { }

        public override object Calc(object a, object b) {
            var start = ((int)a);
            var end = ((int)b);
            var result = new List<int>();

            if (start < end)
                for (int i = start; i <= end; i += 1)
                    result.Add(i);
            else
                for (int i = start; i >= end; i -= 1)
                    result.Add(i);
            return new ImmutableFunnyArray(result.ToArray());
        }
    }

    class Int64Function : FunctionWithTwoArgs {
        public Int64Function() : base(Id, FunnyType.ArrayOf(FunnyType.Int64), FunnyType.Int64, FunnyType.Int64) { }

        public override object Calc(object a, object b) {
            var start = (long)a;
            var end = (long)b;
            var result = new List<long>();

            if (start < end)
                for (var i = start; i <= end; i += 1)
                    result.Add(i);
            else
                for (var i = start; i >= end; i -= 1)
                    result.Add(i);
            return new ImmutableFunnyArray(result.ToArray());
        }
    }

    class UInt8Function : FunctionWithTwoArgs {
        public UInt8Function() : base(Id, FunnyType.ArrayOf(FunnyType.UInt8), FunnyType.UInt8, FunnyType.UInt8) { }

        public override object Calc(object a, object b) {
            var start = ((byte)a);
            var end = ((byte)b);
            var result = new List<byte>();

            if (start < end)
                for (var i = start; i <= end; i += 1)
                    result.Add(i);
            else
                for (var i = start; i >= end; i -= 1)
                    result.Add(i);
            return new ImmutableFunnyArray(result.ToArray());
        }
    }

    class UInt16Function : FunctionWithTwoArgs {
        public UInt16Function() : base(Id, FunnyType.ArrayOf(FunnyType.UInt16), FunnyType.UInt16, FunnyType.UInt16) { }

        public override object Calc(object a, object b) {
            var start = ((ushort)a);
            var end = ((ushort)b);
            var result = new List<ushort>();

            if (start < end)
                for (var i = start; i <= end; i += 1)
                    result.Add(i);
            else
                for (var i = start; i >= end; i -= 1)
                    result.Add(i);
            return new ImmutableFunnyArray(result.ToArray());
        }
    }

    class UInt32Function : FunctionWithTwoArgs {
        public UInt32Function() : base(Id, FunnyType.ArrayOf(FunnyType.UInt32), FunnyType.UInt32, FunnyType.UInt32) { }

        public override object Calc(object a, object b) {
            var start = ((uint)a);
            var end = ((uint)b);
            var result = new List<uint>();

            if (start < end)
                for (var i = start; i <= end; i += 1)
                    result.Add(i);
            else
                for (var i = start; i >= end; i -= 1)
                    result.Add(i);
            return new ImmutableFunnyArray(result.ToArray());
        }
    }

    class UInt64Function : FunctionWithTwoArgs {
        public UInt64Function() : base(Id, FunnyType.ArrayOf(FunnyType.UInt64), FunnyType.UInt64, FunnyType.UInt64) { }

        public override object Calc(object a, object b) {
            var start = (ulong)a;
            var end = (ulong)b;
            var result = new List<ulong>();

            if (start < end)
                for (var i = start; i <= end; i += 1)
                    result.Add(i);
            else
                for (var i = start; i >= end; i -= 1)
                    result.Add(i);
            return new ImmutableFunnyArray(result.ToArray());
        }
    }

    class RealFunction : FunctionWithTwoArgs {
        public RealFunction() : base(Id, FunnyType.ArrayOf(FunnyType.Real), FunnyType.Real, FunnyType.Real) { }

        public override object Calc(object a, object b) {
            var start = (double)a;
            var end = (double)b;

            var result = new List<double>();

            if (start < end)
                for (var i = start; i <= end; i += 1)
                    result.Add(i);
            else
                for (var i = start; i >= end; i -= 1)
                    result.Add(i);
            return new ImmutableFunnyArray(result.ToArray());
        }
    }
}

public class RangeStepFunction : GenericFunctionBase {
    public RangeStepFunction() : base(
        CoreFunNames.RangeName,
        GenericConstrains.Numbers,
        FunnyType.ArrayOf(FunnyType.Generic(0)), FunnyType.Generic(0), FunnyType.Generic(0), FunnyType.Generic(0)) { }

    public override IConcreteFunction CreateConcrete(FunnyType[] concreteTypes) =>
        concreteTypes[0].BaseType switch {
            BaseFunnyType.UInt8  => new UInt8Function(),
            BaseFunnyType.UInt16 => new UInt16Function(),
            BaseFunnyType.UInt32 => new UInt32Function(),
            BaseFunnyType.UInt64 => new UInt64Function(),
            BaseFunnyType.Int16  => new Int16Function(),
            BaseFunnyType.Int32  => new Int32Function(),
            BaseFunnyType.Int64  => new Int64Function(),
            BaseFunnyType.Real   => new RealFunction(),
            _                    => throw new ArgumentOutOfRangeException()
        };

    private const string Id = "rangeWithStep";

    class Int16Function : FunctionWithManyArguments {
        public Int16Function() : base(
            Id, FunnyType.ArrayOf(FunnyType.Int16), FunnyType.Int16, FunnyType.Int16,
            FunnyType.Int16) { }

        public override object Calc(object[] args) {
            var start = ((int)args[0]);
            var end = ((int)args[1]);
            var step = ((int)args[2]);
            if (step <= 0)
                throw new FunnyRuntimeException("Step has to be positive");

            var result = new List<int>();
            if (start < end)
                for (int i = start; i <= end; i += step)
                    result.Add(i);
            else
                for (int i = start; i >= end; i -= step)
                    result.Add(i);
            return new ImmutableFunnyArray(result.ToArray());
        }
    }

    class Int32Function : FunctionWithManyArguments {
        public Int32Function() : base(
            Id, FunnyType.ArrayOf(FunnyType.Int32), FunnyType.Int32, FunnyType.Int32,
            FunnyType.Int32) { }

        public override object Calc(object[] args) {
            var start = ((int)args[0]);
            var end = ((int)args[1]);
            var step = ((int)args[2]);
            if (step <= 0)
                throw new FunnyRuntimeException("Step has to be positive");

            var result = new List<int>();
            if (start < end)
                for (int i = start; i <= end; i += step)
                    result.Add(i);
            else
                for (int i = start; i >= end; i -= step)
                    result.Add(i);
            return new ImmutableFunnyArray(result.ToArray());
        }
    }

    class Int64Function : FunctionWithManyArguments {
        public Int64Function() : base(
            Id, FunnyType.ArrayOf(FunnyType.Int64), FunnyType.Int64, FunnyType.Int64,
            FunnyType.Int64) { }

        public override object Calc(object[] args) {
            var start = ((long)args[0]);
            var end = ((long)args[1]);
            var step = ((long)args[2]);
            if (step <= 0)
                throw new FunnyRuntimeException("Step has to be positive");

            var result = new List<long>();
            if (start < end)
                for (var i = start; i <= end; i += step)
                    result.Add(i);
            else
                for (var i = start; i >= end; i -= step)
                    result.Add(i);
            return new ImmutableFunnyArray(result.ToArray());
        }
    }

    class UInt8Function : FunctionWithManyArguments {
        public UInt8Function() : base(
            Id, FunnyType.ArrayOf(FunnyType.UInt8), FunnyType.UInt8, FunnyType.UInt8,
            FunnyType.UInt8) { }

        public override object Calc(object[] args) {
            var start = ((byte)args[0]);
            var end = ((byte)args[1]);
            var step = ((byte)args[2]);
            if (step <= 0)
                throw new FunnyRuntimeException("Step has to be positive");

            var result = new List<byte>();
            if (start < end)
                for (var i = start; i <= end; i += step)
                    result.Add(i);
            else
                for (var i = start; i >= end; i -= step)
                    result.Add(i);
            return new ImmutableFunnyArray(result.ToArray());
        }
    }

    class UInt16Function : FunctionWithManyArguments {
        public UInt16Function() : base(
            Id, FunnyType.ArrayOf(FunnyType.UInt16), FunnyType.UInt16, FunnyType.UInt16,
            FunnyType.UInt16) { }

        public override object Calc(object[] args) {
            var start = ((ushort)args[0]);
            var end = ((ushort)args[1]);
            var step = ((ushort)args[2]);
            if (step <= 0)
                throw new FunnyRuntimeException("Step has to be positive");

            var result = new List<UInt16>();
            if (start < end)
                for (var i = start; i <= end; i += step)
                    result.Add(i);
            else
                for (var i = start; i >= end; i -= step)
                    result.Add(i);
            return new ImmutableFunnyArray(result.ToArray());
        }
    }

    class UInt32Function : FunctionWithManyArguments {
        public UInt32Function() : base(
            Id, FunnyType.ArrayOf(FunnyType.UInt32), FunnyType.UInt32, FunnyType.UInt32,
            FunnyType.UInt32) { }

        public override object Calc(object[] args) {
            var start = ((UInt32)args[0]);
            var end = ((UInt32)args[1]);
            var step = ((uint)args[2]);
            if (step <= 0)
                throw new FunnyRuntimeException("Step has to be positive");

            var result = new List<UInt32>();
            if (start < end)
                for (var i = start; i <= end; i += step)
                    result.Add(i);
            else
                for (var i = start; i >= end; i -= step)
                    result.Add(i);
            return new ImmutableFunnyArray(result.ToArray());
        }
    }

    class UInt64Function : FunctionWithManyArguments {
        public UInt64Function() : base(
            Id, FunnyType.ArrayOf(FunnyType.UInt64), FunnyType.UInt64, FunnyType.UInt64,
            FunnyType.UInt64) { }

        public override object Calc(object[] args) {
            var start = ((ulong)args[0]);
            var end = ((ulong)args[1]);
            var step = ((ulong)args[2]);
            if (step <= 0)
                throw new FunnyRuntimeException("Step has to be positive");

            var result = new List<UInt64>();
            if (start < end)
                for (var i = start; i <= end; i += step)
                    result.Add(i);
            else
                for (var i = start; i >= end; i -= step)
                    result.Add(i);
            return new ImmutableFunnyArray(result.ToArray());
        }
    }

    class RealFunction : FunctionWithManyArguments {
        public RealFunction() : base(
            Id, FunnyType.ArrayOf(FunnyType.Real), FunnyType.Real, FunnyType.Real,
            FunnyType.Real) { }

        public override object Calc(object[] args) {
            var start = ((double)args[0]);
            var end = ((double)args[1]);
            var step = ((double)args[2]);
            if (step <= 0)
                throw new FunnyRuntimeException("Step has to be positive");

            var result = new List<double>();
            if (start < end)
                for (var i = start; i <= end; i += step)
                    result.Add(i);
            else
                for (var i = start; i >= end; i -= step)
                    result.Add(i);
            return new ImmutableFunnyArray(result.ToArray());
        }
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
        var start = ((int)args[1]);
        if (start < 0)
            throw new FunnyRuntimeException("Argument out of range");

        var end = ((int)args[2]);
        if (end < 0)
            throw new FunnyRuntimeException("Argument out of range");

        if (end != 0 && start > end)
            throw new FunnyRuntimeException("Start cannot be more than end");

        var arr = (IFunnyArray)args[0];
        return arr.Slice(start, (end == int.MaxValue ? null : (int?)end), null);
    }
}

public class GetGenericFunctionDefinition : GenericFunctionWithTwoArguments {
    public GetGenericFunctionDefinition() : base(
        CoreFunNames.GetElementName,
        FunnyType.Generic(0),
        FunnyType.ArrayOf(FunnyType.Generic(0)),
        FunnyType.Int32) { }

    protected override object Calc(object a, object b) {
        var index = ((int)b);
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

        var index = ((int)args[1]);
        if (index < 0)
            throw new FunnyRuntimeException("Argument out of range");
        if (index > arr.Count + 1)
            throw new FunnyRuntimeException("Argument out of range");
        var val = args[2];

        var newArr = new object[arr.ClrArray.Length];
        arr.ClrArray.CopyTo(newArr, 0);
        newArr.SetValue(val, index);
        return new ImmutableFunnyArray(newArr, arr.ElementType);
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
        var chunkSize = ((int)b);
        if (chunkSize <= 0)
            throw new FunnyRuntimeException("Chunk size is " + chunkSize + ". It has to be positive");

        var originInputType = FunnyType.ArrayOf(arr.ElementType);

        var res = arr
                  .Select((x, i) => new { Index = i, Value = x })
                  .GroupBy(x => x.Index / chunkSize)
                  .Select(x => new EnumerableFunnyArray(x.Select(v => v.Value), originInputType));
        return new EnumerableFunnyArray(res, FunnyType.ArrayOf(originInputType));
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

        return new EnumerableFunnyArray(arr.SelectMany(o => (IFunnyArray)o), originInputType);
    }
}

public class FoldGenericFunctionDefinition : GenericFunctionWithTwoArguments {
    public FoldGenericFunctionDefinition() : base(
        "fold", new[] { GenericConstrains.Any },
        FunnyType.Generic(0),
        FunnyType.ArrayOf(FunnyType.Generic(0)),
        FunnyType.Fun(FunnyType.Generic(0), FunnyType.Generic(0), FunnyType.Generic(0))) { }

    protected override object Calc(object arg1, object arg2) {
        var arr = (IFunnyArray)arg1;
        if (arr.Count == 0)
            throw new FunnyRuntimeException("Input array is empty");
        if (arg2 is FunctionWithTwoArgs fold2)
            return arr.Aggregate((a, b) => fold2.Calc(a, b));

        var fold = (IConcreteFunction)arg2;

        return arr.Aggregate((a, b) => fold.Calc(new[] { a, b }));
    }
}

public class FoldWithDefaultsGenericFunctionDefinition : GenericFunctionBase {
    public FoldWithDefaultsGenericFunctionDefinition() : base(
        "fold",
        returnType: FunnyType.Generic(1),
        FunnyType.ArrayOf(FunnyType.Generic(0)),
        FunnyType.Generic(1),
        FunnyType.Fun(
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
        return new EnumerableFunnyArray(arr1.Union(arr2), arr1.ElementType);
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
        return new EnumerableFunnyArray(arr1.Except(arr2).Concat(arr2.Except(arr1)), arr1.ElementType);
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
        return new EnumerableFunnyArray(arr1.Intersect(arr2), arr1.ElementType);
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
        var res = new EnumerableFunnyArray(arr1.Concat(arr2), arr1.ElementType);
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
        var res = new EnumerableFunnyArray(arr1.Append(arr2), arr1.ElementType);
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
        return new EnumerableFunnyArray(arr1.Except(arr2), arr1.ElementType);
    }
}

public class CountOfGenericFunctionDefinition : GenericFunctionWithTwoArguments {
    public CountOfGenericFunctionDefinition() : base(
        "count",
        FunnyType.Int32,
        FunnyType.ArrayOf(FunnyType.Generic(0)),
        FunnyType.Fun(FunnyType.Bool, FunnyType.Generic(0))) { }

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
        FunnyType.Fun(FunnyType.Bool, FunnyType.Generic(0))) { }

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
        FunnyType.Fun(FunnyType.Bool, FunnyType.Generic(0))) { }

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
        FunnyType.Fun(FunnyType.Bool, FunnyType.Generic(0))) { }

    protected override object Calc(object a, object b) {
        var arr = (IFunnyArray)a;
        if (b is FunctionWithSingleArg predicate)
            return new EnumerableFunnyArray(arr.Where(e => (bool)predicate.Calc(e)), arr.ElementType);
        var filter = (IConcreteFunction)b;

        return new EnumerableFunnyArray(arr.Where(e => (bool)filter.Calc(new[] { e })), arr.ElementType);
    }
}

public class RepeatGenericFunctionDefinition : GenericFunctionBase {
    public RepeatGenericFunctionDefinition() : base(
        "repeat",
        FunnyType.ArrayOf(FunnyType.Generic(0)),
        FunnyType.Generic(0),
        FunnyType.Int32) { }

    public override IConcreteFunction CreateConcrete(FunnyType[] concreteTypesMap) {
        var res = new ConcreteRepeat {
            Name = Name,
            ArgTypes = new[] { concreteTypesMap[0], FunnyType.Int32 },
            ReturnType = FunnyType.ArrayOf(concreteTypesMap[0])
        };
        return res;
    }

    class ConcreteRepeat : FunctionWithTwoArgs {
        public override object Calc(object a, object b)
            => new EnumerableFunnyArray(Enumerable.Repeat(a, (int)b), this.ArgTypes[0]);
    }
}

public class ReverseGenericFunctionDefinition : GenericFunctionWithSingleArgument {
    public ReverseGenericFunctionDefinition() : base(
        "reverse",
        FunnyType.ArrayOf(FunnyType.Generic(0)),
        FunnyType.ArrayOf(FunnyType.Generic(0))) { }

    protected override object Calc(object a) {
        var arr = (IFunnyArray)a;
        return new EnumerableFunnyArray(arr.Reverse(), arr.ElementType);
    }
}

public class TakeGenericFunctionDefinition : GenericFunctionWithTwoArguments {
    public TakeGenericFunctionDefinition() : base(
        "take",
        FunnyType.ArrayOf(FunnyType.Generic(0)),
        FunnyType.ArrayOf(FunnyType.Generic(0)),
        FunnyType.Int32) { }

    protected override object Calc(object a, object b)
        => ((IFunnyArray)a).Slice(null, ((int)b) - 1, 1);
}

public class SkipGenericFunctionDefinition : GenericFunctionWithTwoArguments {
    public SkipGenericFunctionDefinition() : base(
        "skip",
        FunnyType.ArrayOf(FunnyType.Generic(0)),
        FunnyType.ArrayOf(FunnyType.Generic(0)),
        FunnyType.Int32) { }

    protected override object Calc(object a, object b)
        => ((IFunnyArray)a).Slice(((int)b), null, 1);
}

}