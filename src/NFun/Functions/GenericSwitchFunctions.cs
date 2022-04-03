using System;
using System.Collections.Generic;
using System.Linq;
using NFun.Exceptions;
using NFun.Interpretation.Functions;
using NFun.Runtime.Arrays;
using NFun.Types;

namespace NFun.Functions {

public class MultiSumFunction : GenericFunctionBase {
    private const string Id = "sum";

    public MultiSumFunction() : base(
        Id, GenericConstrains.Arithmetical, FunnyType.Generic(0),
        FunnyType.ArrayOf(FunnyType.Generic(0)))
    { }

    public override IConcreteFunction CreateConcrete(FunnyType[] concreteTypes, TypeBehaviour typeBehaviour) =>
        concreteTypes[0].BaseType switch
        {
            BaseFunnyType.UInt16 => UInt16Instance,
            BaseFunnyType.UInt32 => UInt32Instance,
            BaseFunnyType.UInt64 => UInt64Instance,
            BaseFunnyType.Int16  => Int16Instance,
            BaseFunnyType.Int32  => Int32Instance,
            BaseFunnyType.Int64  => Int64Instance,
            BaseFunnyType.Real   => typeBehaviour.RealTypeSelect<IConcreteFunction>(RealDoubleInstance, RealDecimalInstance),
            _                    => throw new ArgumentOutOfRangeException()
        };

    private static readonly RealDoubleFunction RealDoubleInstance = new();

    private class RealDoubleFunction : FunctionWithSingleArg {
        public RealDoubleFunction() : base(Id, FunnyType.Real, FunnyType.ArrayOf(FunnyType.Real))
        { }

        public override object Calc(object a) => ((IFunnyArray) a).As<double>().Sum();
    }

    private static RealDecimalFunction RealDecimalInstance = new();

    private class RealDecimalFunction : FunctionWithSingleArg {
        public RealDecimalFunction() : base(Id, FunnyType.Real, FunnyType.ArrayOf(FunnyType.Real))
        { }

        public override object Calc(object a) => ((IFunnyArray) a).As<decimal>().Sum();
    }

    private static readonly Int16Function Int16Instance = new();

    private class Int16Function : FunctionWithSingleArg {
        public Int16Function() : base(Id, FunnyType.Int16, FunnyType.ArrayOf(FunnyType.Int16))
        { }

        public override object Calc(object a)
        {
            short answer = 0;
            foreach (var i in ((IFunnyArray) a).As<short>())
                answer += i;
            return answer;
        }
    }

    private static readonly Int32Function Int32Instance = new();

    private class Int32Function : FunctionWithSingleArg {
        public Int32Function() : base(Id, FunnyType.Int32, FunnyType.ArrayOf(FunnyType.Int32))
        { }

        public override object Calc(object a) => ((IFunnyArray) a).As<int>().Sum();
    }

    private static readonly Int64Function Int64Instance = new();

    private class Int64Function : FunctionWithSingleArg {
        public Int64Function() : base(Id, FunnyType.Int64, FunnyType.ArrayOf(FunnyType.Int64))
        { }

        public override object Calc(object a) => ((IFunnyArray) a).As<long>().Sum();
    }

    private static readonly UInt16Function UInt16Instance = new();

    private class UInt16Function : FunctionWithSingleArg {
        public UInt16Function() : base(Id, FunnyType.UInt16, FunnyType.ArrayOf(FunnyType.UInt16))
        { }

        public override object Calc(object a)
        {
            ushort answer = 0;
            foreach (var i in ((IFunnyArray) a).As<ushort>())
                answer += i;
            return answer;
        }
    }

    private static readonly UInt32Function UInt32Instance = new();

    private class UInt32Function : FunctionWithSingleArg {
        public UInt32Function() : base(Id, FunnyType.UInt32, FunnyType.ArrayOf(FunnyType.UInt32))
        { }

        public override object Calc(object a)
        {
            uint answer = 0;
            foreach (var i in ((IFunnyArray) a).As<uint>())
                answer += i;
            return answer;
        }
    }

    private static readonly UInt64Function UInt64Instance = new();

    private class UInt64Function : FunctionWithSingleArg {
        public UInt64Function() : base(Id, FunnyType.UInt64, FunnyType.ArrayOf(FunnyType.UInt64))
        { }

        public override object Calc(object a)
        {
            ulong answer = 0;
            foreach (var i in ((IFunnyArray) a).As<ulong>())
                answer += i;
            return answer;
        }
    }
}

public class RangeFunction : GenericFunctionBase {
    public RangeFunction() : base(
        CoreFunNames.RangeName,
        GenericConstrains.Numbers,
        FunnyType.ArrayOf(FunnyType.Generic(0)), FunnyType.Generic(0), FunnyType.Generic(0))
    { }

    public override IConcreteFunction CreateConcrete(FunnyType[] concreteTypes, TypeBehaviour typeBehaviour) =>
        concreteTypes[0].BaseType switch
        {
            BaseFunnyType.UInt8                                => UInt8Function.Instance,
            BaseFunnyType.UInt16                               => UInt16Function.Instance,
            BaseFunnyType.UInt32                               => UInt32Function.Instance,
            BaseFunnyType.UInt64                               => UInt64Function.Instance,
            BaseFunnyType.Int16                                => Int16Function.Instance,
            BaseFunnyType.Int32                                => Int32Function.Instance,
            BaseFunnyType.Int64                                => Int64Function.Instance,
            BaseFunnyType.Real when typeBehaviour.DoubleIsReal => RealDoubleFunction.Instance,
            BaseFunnyType.Real                                 => RealDecimalFunction.Instance,
            _                                                  => throw new NotSupportedException()
        };

    private const string Id = "range";

    class Int16Function : FunctionWithTwoArgs {
        public static readonly Int16Function Instance = new();

        private Int16Function() : base(Id, FunnyType.ArrayOf(FunnyType.Int16), FunnyType.Int16, FunnyType.Int16)
        { }

        public override object Calc(object a, object b)
        {
            var start = ((short) a);
            var end = ((short) b);
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
        public static readonly Int32Function Instance = new();

        private Int32Function() : base(Id, FunnyType.ArrayOf(FunnyType.Int32), FunnyType.Int32, FunnyType.Int32)
        { }

        public override object Calc(object a, object b)
        {
            var start = ((int) a);
            var end = ((int) b);
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
        public static readonly Int64Function Instance = new();

        private Int64Function() : base(Id, FunnyType.ArrayOf(FunnyType.Int64), FunnyType.Int64, FunnyType.Int64)
        { }

        public override object Calc(object a, object b)
        {
            var start = (long) a;
            var end = (long) b;
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
        public static readonly UInt8Function Instance = new();

        private UInt8Function() : base(Id, FunnyType.ArrayOf(FunnyType.UInt8), FunnyType.UInt8, FunnyType.UInt8)
        { }

        public override object Calc(object a, object b)
        {
            var start = ((byte) a);
            var end = ((byte) b);
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
        public static readonly UInt16Function Instance = new();

        private UInt16Function() : base(Id, FunnyType.ArrayOf(FunnyType.UInt16), FunnyType.UInt16, FunnyType.UInt16)
        { }

        public override object Calc(object a, object b)
        {
            var start = ((ushort) a);
            var end = ((ushort) b);
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
        public static readonly UInt32Function Instance = new();

        private UInt32Function() : base(Id, FunnyType.ArrayOf(FunnyType.UInt32), FunnyType.UInt32, FunnyType.UInt32)
        { }

        public override object Calc(object a, object b)
        {
            var start = ((uint) a);
            var end = ((uint) b);
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
        public static readonly UInt64Function Instance = new();

        private UInt64Function() : base(Id, FunnyType.ArrayOf(FunnyType.UInt64), FunnyType.UInt64, FunnyType.UInt64)
        { }

        public override object Calc(object a, object b)
        {
            var start = (ulong) a;
            var end = (ulong) b;
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

    class RealDoubleFunction : FunctionWithTwoArgs {
        public static readonly RealDoubleFunction Instance = new();

        private RealDoubleFunction() : base(Id, FunnyType.ArrayOf(FunnyType.Real), FunnyType.Real, FunnyType.Real)
        { }

        public override object Calc(object a, object b)
        {
            var start = (double) a;
            var end = (double) b;

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

    class RealDecimalFunction : FunctionWithTwoArgs {
        public static readonly RealDecimalFunction Instance = new();

        private RealDecimalFunction() : base(Id, FunnyType.ArrayOf(FunnyType.Real), FunnyType.Real, FunnyType.Real)
        { }

        public override object Calc(object a, object b)
        {
            var start = (decimal) a;
            var end = (decimal) b;

            var result = new List<decimal>();

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
        FunnyType.ArrayOf(FunnyType.Generic(0)), FunnyType.Generic(0), FunnyType.Generic(0), FunnyType.Generic(0))
    { }

    public override IConcreteFunction CreateConcrete(FunnyType[] concreteTypes, TypeBehaviour typeBehaviour) =>
        concreteTypes[0].BaseType switch
        {
            BaseFunnyType.UInt8                                => UInt8Function.Instance,
            BaseFunnyType.UInt16                               => UInt16Function.Instance,
            BaseFunnyType.UInt32                               => UInt32Function.Instance,
            BaseFunnyType.UInt64                               => UInt64Function.Instance,
            BaseFunnyType.Int16                                => Int16Function.Instance,
            BaseFunnyType.Int32                                => Int32Function.Instance,
            BaseFunnyType.Int64                                => Int64Function.Instance,
            BaseFunnyType.Real when typeBehaviour.DoubleIsReal => RealDoubleFunction.Instance,
            BaseFunnyType.Real                                 => RealDecimalFunction.Instance,

            _ => throw new ArgumentOutOfRangeException()
        };

    private const string Id = "rangeWithStep";

    class Int16Function : FunctionWithManyArguments {
        public static readonly Int16Function Instance = new();

        private Int16Function() : base(
            Id, FunnyType.ArrayOf(FunnyType.Int16), FunnyType.Int16, FunnyType.Int16,
            FunnyType.Int16)
        { }

        public override object Calc(object[] args)
        {
            var start = ((int) args[0]);
            var end = ((int) args[1]);
            var step = ((int) args[2]);
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
        public static readonly Int32Function Instance = new();

        private Int32Function() : base(
            Id, FunnyType.ArrayOf(FunnyType.Int32), FunnyType.Int32, FunnyType.Int32,
            FunnyType.Int32)
        { }

        public override object Calc(object[] args)
        {
            var start = ((int) args[0]);
            var end = ((int) args[1]);
            var step = ((int) args[2]);
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
        public static readonly Int64Function Instance = new();

        private Int64Function() : base(
            Id, FunnyType.ArrayOf(FunnyType.Int64), FunnyType.Int64, FunnyType.Int64,
            FunnyType.Int64)
        { }

        public override object Calc(object[] args)
        {
            var start = ((long) args[0]);
            var end = ((long) args[1]);
            var step = ((long) args[2]);
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
        public static readonly UInt8Function Instance = new();

        private UInt8Function() : base(
            Id, FunnyType.ArrayOf(FunnyType.UInt8), FunnyType.UInt8, FunnyType.UInt8,
            FunnyType.UInt8)
        { }

        public override object Calc(object[] args)
        {
            var start = ((byte) args[0]);
            var end = ((byte) args[1]);
            var step = ((byte) args[2]);
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
        public static readonly UInt16Function Instance = new();

        private UInt16Function() : base(
            Id, FunnyType.ArrayOf(FunnyType.UInt16), FunnyType.UInt16, FunnyType.UInt16,
            FunnyType.UInt16)
        { }

        public override object Calc(object[] args)
        {
            var start = ((ushort) args[0]);
            var end = ((ushort) args[1]);
            var step = ((ushort) args[2]);
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
        public static readonly UInt32Function Instance = new();

        private UInt32Function() : base(
            Id, FunnyType.ArrayOf(FunnyType.UInt32), FunnyType.UInt32, FunnyType.UInt32,
            FunnyType.UInt32)
        { }

        public override object Calc(object[] args)
        {
            var start = ((UInt32) args[0]);
            var end = ((UInt32) args[1]);
            var step = ((uint) args[2]);
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
        public static readonly UInt64Function Instance = new();

        private UInt64Function() : base(
            Id, FunnyType.ArrayOf(FunnyType.UInt64), FunnyType.UInt64, FunnyType.UInt64,
            FunnyType.UInt64)
        { }

        public override object Calc(object[] args)
        {
            var start = ((ulong) args[0]);
            var end = ((ulong) args[1]);
            var step = ((ulong) args[2]);
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

    class RealDoubleFunction : FunctionWithManyArguments {
        public static readonly RealDoubleFunction Instance = new();

        private RealDoubleFunction() : base(
            Id, FunnyType.ArrayOf(FunnyType.Real), FunnyType.Real, FunnyType.Real,
            FunnyType.Real)
        { }

        public override object Calc(object[] args)
        {
            var start = ((double) args[0]);
            var end = ((double) args[1]);
            var step = ((double) args[2]);
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


    class RealDecimalFunction : FunctionWithManyArguments {
        public static readonly RealDecimalFunction Instance = new();

        private RealDecimalFunction() : base(
            Id, FunnyType.ArrayOf(FunnyType.Real), FunnyType.Real, FunnyType.Real,
            FunnyType.Real)
        { }

        public override object Calc(object[] args)
        {
            var start = ((decimal) args[0]);
            var end = ((decimal) args[1]);
            var step = ((decimal) args[2]);
            if (step <= 0)
                throw new FunnyRuntimeException("Step has to be positive");

            var result = new List<decimal>();
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

}