using System;
using System.Collections.Generic;
using System.Linq;
using NFun.Exceptions;
using NFun.Interpretation.Functions;
using NFun.Runtime.Arrays;
using NFun.Types;

//todo optimization
namespace NFun.Functions; 

public class MultiSumFunction : GenericFunctionBase {
    private const string Id = "sum";

    public MultiSumFunction() : base(
        Id, GenericConstrains.Arithmetical, FunnyType.Generic(0),
        FunnyType.ArrayOf(FunnyType.Generic(0)))
    { ArgProperties = FunArgProperty.FromNames("arr"); }

    public override IConcreteFunction CreateConcrete(FunnyType[] concreteTypes, IFunctionSelectorContext context) =>
        concreteTypes[0].BaseType switch
        {
            BaseFunnyType.UInt16 => context.AllowIntegerOverflow? UInt16Function.Instance: UInt16CheckedFunction.Instance ,
            BaseFunnyType.UInt32 => context.AllowIntegerOverflow? UInt32Function.Instance: UInt32CheckedFunction.Instance,
            BaseFunnyType.UInt64 => context.AllowIntegerOverflow? UInt64Function.Instance: UInt64CheckedFunction.Instance,
            BaseFunnyType.Int16  => context.AllowIntegerOverflow? Int16Function.Instance: Int16CheckedFunction.Instance,
            BaseFunnyType.Int32  => context.AllowIntegerOverflow? Int32Function.Instance: Int32CheckedFunction.Instance,
            BaseFunnyType.Int64  => context.AllowIntegerOverflow? Int64Function.Instance: Int64CheckedFunction.Instance,
            BaseFunnyType.Float32 => Float32Function.Instance,
            BaseFunnyType.Real   => context.RealTypeSelect<IConcreteFunction>(RealDoubleFunction.Instance, RealDecimalFunction.Instance),
            _                    => throw new NFunImpossibleException("Unsupported type for this function")
        };

    private class Float32Function : FunctionWithSingleArg {
        public static readonly Float32Function Instance = new();
        private Float32Function() : base(Id, FunnyType.Float32, FunnyType.ArrayOf(FunnyType.Float32)){ }
        public override object Calc(object a) => ((IFunnyArray) a).As<float>().Sum();
    }

    private class RealDoubleFunction : FunctionWithSingleArg {
        public static readonly RealDoubleFunction Instance = new();
        private RealDoubleFunction() : base(Id, FunnyType.Real, FunnyType.ArrayOf(FunnyType.Real)){ }
        public override object Calc(object a) => ((IFunnyArray) a).As<double>().Sum();
    }


    private class RealDecimalFunction : FunctionWithSingleArg {
        public static readonly RealDecimalFunction Instance = new();
        private RealDecimalFunction() : base(Id, FunnyType.Real, FunnyType.ArrayOf(FunnyType.Real))
        { }
        public override object Calc(object a) => ((IFunnyArray) a).As<decimal>().Sum();
    }


    private class Int16Function : FunctionWithSingleArg {
        public static readonly Int16Function Instance = new();
        private Int16Function() : base(Id, FunnyType.Int16, FunnyType.ArrayOf(FunnyType.Int16))
        { }
        public override object Calc(object a)
        {
            short answer = 0;
            foreach (var i in ((IFunnyArray)a).As<short>())
            {
                answer += i;
            }
            return answer;
        }
    }


    private class Int32Function : FunctionWithSingleArg {
        public static readonly Int32Function Instance = new();
        private Int32Function() : base(Id, FunnyType.Int32, FunnyType.ArrayOf(FunnyType.Int32))
        { }
        public override object Calc(object a) {
            int sum = 0;
            foreach (int i in ((IFunnyArray)a).As<int>())
            {
                sum += i;
            }
            return sum;
        }
    }
    
    private class Int64Function : FunctionWithSingleArg {
        public static readonly Int64Function Instance = new();
        private Int64Function() : base(Id, FunnyType.Int64, FunnyType.ArrayOf(FunnyType.Int64))
        { }
        public override object Calc(object a) {
            long sum = 0;
            foreach (long l in ((IFunnyArray)a).As<long>())
            {
                sum += l;
            }
            return sum;
        }
    }
    
    private class UInt16Function : FunctionWithSingleArg {
        public static readonly UInt16Function Instance = new();
        private UInt16Function() : base(Id, FunnyType.UInt16, FunnyType.ArrayOf(FunnyType.UInt16)){ }
        public override object Calc(object a) {
            ushort answer = 0;
            foreach (var i in ((IFunnyArray)a).As<ushort>())
                answer += i;
            return answer;
        }
    }
    
    private class UInt32Function : FunctionWithSingleArg {
        public static readonly UInt32Function Instance = new();
        private UInt32Function() : base(Id, FunnyType.UInt32, FunnyType.ArrayOf(FunnyType.UInt32))
        { }
        public override object Calc(object a)
        {
            uint answer = 0;
            foreach (var i in ((IFunnyArray) a).As<uint>())
                answer += i;
            return answer;
        }
    }
    
    private class UInt64Function : FunctionWithSingleArg {
        public static readonly UInt64Function Instance = new();
        private UInt64Function() : base(Id, FunnyType.UInt64, FunnyType.ArrayOf(FunnyType.UInt64))
        { }
        public override object Calc(object a)
        {
            ulong answer = 0;
            foreach (var i in ((IFunnyArray) a).As<ulong>())
                answer += i;
            return answer;
        }
    }
    
    private class Int16CheckedFunction : FunctionWithSingleArg {
        public static readonly Int16CheckedFunction Instance = new();
        private Int16CheckedFunction() : base(Id, FunnyType.Int16, FunnyType.ArrayOf(FunnyType.Int16))
        { }
        public override object Calc(object a)
        {
            checked
            {
                short answer = 0;
                foreach (var i in ((IFunnyArray)a).As<short>())
                    answer += i;
                return answer;
            }
        }
    }

    private class Int32CheckedFunction : FunctionWithSingleArg {
        public static readonly Int32CheckedFunction Instance = new();
        private Int32CheckedFunction() : base(Id, FunnyType.Int32, FunnyType.ArrayOf(FunnyType.Int32)) { }
        public override object Calc(object a) {
            checked
            {
                int answer = 0;
                foreach (var i in ((IFunnyArray)a).As<int>())
                    answer += i;
                return answer;
            }
        }
    }

    private class Int64CheckedFunction : FunctionWithSingleArg {
        public static readonly Int64CheckedFunction Instance = new();
        private Int64CheckedFunction() : base(Id, FunnyType.Int64, FunnyType.ArrayOf(FunnyType.Int64)) { }
        public override object Calc(object a) {
            checked
            {
                long answer = 0;
                foreach (var i in ((IFunnyArray)a).As<long>())
                    answer += i;
                return answer;
            }
        }
    }
    
    private class UInt16CheckedFunction : FunctionWithSingleArg {
        public static readonly UInt16CheckedFunction Instance = new();
        private UInt16CheckedFunction() : base(Id, FunnyType.UInt16, FunnyType.ArrayOf(FunnyType.UInt16)){ }
        public override object Calc(object a) {
            checked
            {
                ushort answer = 0;
                foreach (var i in ((IFunnyArray)a).As<ushort>())
                    answer += i;
                return answer;
            }
        }
    }
    
    private class UInt32CheckedFunction : FunctionWithSingleArg {
        public static readonly UInt32CheckedFunction Instance = new();
        private UInt32CheckedFunction() : base(Id, FunnyType.UInt32, FunnyType.ArrayOf(FunnyType.UInt32)){ }
        public override object Calc(object a) {
            checked
            {
                uint answer = 0;
                foreach (var i in ((IFunnyArray)a).As<uint>())
                    answer += i;
                return answer;
            }
        }
    }

    private class UInt64CheckedFunction : FunctionWithSingleArg {
        public static readonly UInt64CheckedFunction Instance = new();
        private UInt64CheckedFunction() : base(Id, FunnyType.UInt64, FunnyType.ArrayOf(FunnyType.UInt64)) { }
        public override object Calc(object a) {
            checked
            {
                ulong answer = 0;
                foreach (var i in ((IFunnyArray)a).As<ulong>())
                    answer += i;
                return answer;
            }
        }
    }
}

public class RangeFunction : GenericFunctionBase {
    public RangeFunction() : base(
        CoreFunNames.RangeName,
        GenericConstrains.Numbers,
        FunnyType.ArrayOf(FunnyType.Generic(0)), FunnyType.Generic(0), FunnyType.Generic(0))
    { ArgProperties = FunArgProperty.FromNames("from", "to"); }

    public override IConcreteFunction CreateConcrete(FunnyType[] concreteTypes, IFunctionSelectorContext context) =>
        concreteTypes[0].BaseType switch
        {
            BaseFunnyType.UInt8  => UInt8Function.Instance,
            BaseFunnyType.UInt16 => UInt16Function.Instance,
            BaseFunnyType.UInt32 => UInt32Function.Instance,
            BaseFunnyType.UInt64 => UInt64Function.Instance,
            BaseFunnyType.Int8   => Int8Function.Instance,
            BaseFunnyType.Int16  => Int16Function.Instance,
            BaseFunnyType.Int32  => Int32Function.Instance,
            BaseFunnyType.Int64  => Int64Function.Instance,
            BaseFunnyType.Float32 => Float32Function.Instance,
            BaseFunnyType.Real => context.RealTypeSelect<IConcreteFunction>(
                RealDoubleFunction.Instance,
                RealDecimalFunction.Instance),
            _ => throw new NotSupportedException()
        };

    private const string Id = "range";

    class Float32Function : FunctionWithTwoArgs {
        public static readonly Float32Function Instance = new();
        private Float32Function() : base(Id, FunnyType.ArrayOf(FunnyType.Float32), FunnyType.Float32, FunnyType.Float32) { }
        public override object Calc(object a, object b) {
            var start = (float)a;
            var end = (float)b;
            // Half-open enumeration matching int Range semantics: from..to inclusive,
            // step = ±1.0f, ascending if from < to else descending.
            var list = new List<float>();
            if (start <= end) for (float v = start; v <= end; v += 1f) list.Add(v);
            else for (float v = start; v >= end; v -= 1f) list.Add(v);
            return new ImmutableFunnyArray(list.ToArray(), FunnyType.Float32);
        }
    }

    class Int16Function : FunctionWithTwoArgs {
        public static readonly Int16Function Instance = new();

        private Int16Function() : base(Id, FunnyType.ArrayOf(FunnyType.Int16), FunnyType.Int16, FunnyType.Int16)
        { }

        public override object Calc(object a, object b)
        {
            var start = (short) a;
            var end = (short) b;
            short[] result;
            if (start < end)
            {
                // Count-based loop avoids post-increment past MaxValue. When end is the
                // type's MaxValue, `i += 1` after the final iteration overflows to MinValue,
                // condition `i <= end` becomes true, and the array gets indexed at a
                // huge negative offset → "Index out of bounds". (MR5Bug1.)
                int len = end - start + 1;
                result = new short[len];
                for (int c = 0; c < len; c++)
                    result[c] = (short)(start + c);
            }
            else
            {
                int len = start - end + 1;
                result = new short[len];
                for (int c = 0; c < len; c++)
                    result[c] = (short)(start - c);
            }
            return new ImmutableFunnyArray(result, FunnyType.Int16);
        }
    }

    class Int32Function : FunctionWithTwoArgs {
        public static readonly Int32Function Instance = new();

        private Int32Function() : base(Id, FunnyType.ArrayOf(FunnyType.Int32), FunnyType.Int32, FunnyType.Int32)
        { }

        public override object Calc(object a, object b)
        {
            var start = (int) a;
            var end = (int) b;
            int[] result;
            if (start < end)
            {
                // See Int16Function for the MaxValue overflow rationale.
                int len = end - start + 1;
                result = new int[len];
                for (int c = 0; c < len; c++)
                    result[c] = start + c;
            }
            else
            {
                int len = start - end + 1;
                result = new int[len];
                for (int c = 0; c < len; c++)
                    result[c] = start - c;
            }
            return new ImmutableFunnyArray(result);
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
            long[] result;
            if (start < end)
            {
                // See Int16Function for the MaxValue overflow rationale.
                long len = end - start + 1;
                result = new long[len];
                for (long c = 0; c < len; c++)
                    result[c] = start + c;
            }
            else
            {
                long len = start - end + 1;
                result = new long[len];
                for (long c = 0; c < len; c++)
                    result[c] = start - c;
            }
            return new ImmutableFunnyArray(result);
        }
    }

    class UInt8Function : FunctionWithTwoArgs {
        public static readonly UInt8Function Instance = new();

        private UInt8Function() : base(Id, FunnyType.ArrayOf(FunnyType.UInt8), FunnyType.UInt8, FunnyType.UInt8)
        { }

        public override object Calc(object a, object b)
        {
            var start = (byte) a;
            var end = (byte) b;

            byte[] result;
            if (start < end)
            {
                // See Int16Function for the MaxValue overflow rationale.
                int len = end - start + 1;
                result = new byte[len];
                for (int c = 0; c < len; c++)
                    result[c] = (byte)(start + c);
            }
            else
            {
                int len = start - end + 1;
                result = new byte[len];
                for (int c = 0; c < len; c++)
                    result[c] = (byte)(start - c);
            }
            return new ImmutableFunnyArray(result);
        }
    }

    class Int8Function : FunctionWithTwoArgs {
        public static readonly Int8Function Instance = new();

        private Int8Function() : base(Id, FunnyType.ArrayOf(FunnyType.Int8), FunnyType.Int8, FunnyType.Int8)
        { }

        public override object Calc(object a, object b)
        {
            var start = (sbyte) a;
            var end = (sbyte) b;
            sbyte[] result;
            if (start < end)
            {
                int len = end - start + 1;
                result = new sbyte[len];
                for (int c = 0; c < len; c++)
                    result[c] = (sbyte)(start + c);
            }
            else
            {
                int len = start - end + 1;
                result = new sbyte[len];
                for (int c = 0; c < len; c++)
                    result[c] = (sbyte)(start - c);
            }
            return new ImmutableFunnyArray(result, FunnyType.Int8);
        }
    }

    class UInt16Function : FunctionWithTwoArgs {
        public static readonly UInt16Function Instance = new();

        private UInt16Function() : base(Id, FunnyType.ArrayOf(FunnyType.UInt16), FunnyType.UInt16, FunnyType.UInt16)
        { }

        public override object Calc(object a, object b)
        {
            var start = (ushort) a;
            var end = (ushort) b;
            ushort[] result;
            if (start < end)
            {
                // See Int16Function for the MaxValue overflow rationale.
                int len = end - start + 1;
                result = new ushort[len];
                for (int c = 0; c < len; c++)
                    result[c] = (ushort)(start + c);
            }
            else
            {
                int len = start - end + 1;
                result = new ushort[len];
                for (int c = 0; c < len; c++)
                    result[c] = (ushort)(start - c);
            }
            return new ImmutableFunnyArray(result);
        }
    }

    class UInt32Function : FunctionWithTwoArgs {
        public static readonly UInt32Function Instance = new();

        private UInt32Function() : base(Id, FunnyType.ArrayOf(FunnyType.UInt32), FunnyType.UInt32, FunnyType.UInt32)
        { }

        public override object Calc(object a, object b)
        {
            var start = (uint) a;
            var end = (uint) b;
            uint[] result;
            if (start < end)
            {
                // See Int16Function for the MaxValue overflow rationale.
                uint len = end - start + 1;
                result = new uint[len];
                for (uint c = 0; c < len; c++)
                    result[c] = start + c;
            }
            else
            {
                uint len = start - end + 1;
                result = new uint[len];
                for (uint c = 0; c < len; c++)
                    result[c] = start - c;
            }
            return new ImmutableFunnyArray(result);
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
            ulong[] result;
            if (start < end)
            {
                // See Int16Function for the MaxValue overflow rationale.
                ulong len = end - start + 1;
                result = new ulong[len];
                for (ulong c = 0; c < len; c++)
                    result[c] = start + c;
            }
            else
            {
                ulong len = start - end + 1;
                result = new ulong[len];
                for (ulong c = 0; c < len; c++)
                    result[c] = start - c;
            }
            return new ImmutableFunnyArray(result);
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
            
            double[] result;
            if (start < end)
            {
                result = new double[(int)(end - start) + 1];
                int c = 0;
                for (var i = start; i <= end; i += 1)
                {
                    result[c] = i;
                    c++;
                }
            }
            else
            {
                result = new double[(int)(start - end) + 1];
                int c = 0;
                for (var i = start; i >= end; i -= 1)
                {
                    result[c] = i;
                    c++;
                }
            }
            return new ImmutableFunnyArray(result);
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
            
            decimal[] result;
            if (start < end)
            {
                result = new decimal[(int)(end - start) + 1];
                int c = 0;
                for (var i = start; i <= end; i += 1)
                {
                    result[c] = i;
                    c++;
                }
            }
            else
            {
                result = new decimal[(int)(start - end) + 1];
                int c = 0;
                for (var i = start; i >= end; i -= 1)
                {
                    result[c] = i;
                    c++;
                }
            }
            return new ImmutableFunnyArray(result);
        }
    }
}

public class RangeStepFunction : GenericFunctionBase {
    public RangeStepFunction() : base(
        CoreFunNames.RangeName,
        GenericConstrains.Numbers,
        FunnyType.ArrayOf(FunnyType.Generic(0)), FunnyType.Generic(0), FunnyType.Generic(0), FunnyType.Generic(0))
    { ArgProperties = FunArgProperty.FromNames("from", "to", "step"); }

    public override IConcreteFunction CreateConcrete(FunnyType[] concreteTypes, IFunctionSelectorContext context) =>
        concreteTypes[0].BaseType switch
        {
            BaseFunnyType.UInt8  => UInt8Function.Instance,
            BaseFunnyType.UInt16 => UInt16Function.Instance,
            BaseFunnyType.UInt32 => UInt32Function.Instance,
            BaseFunnyType.UInt64 => UInt64Function.Instance,
            BaseFunnyType.Int8   => Int8Function.Instance,
            BaseFunnyType.Int16  => Int16Function.Instance,
            BaseFunnyType.Int32  => Int32Function.Instance,
            BaseFunnyType.Int64  => Int64Function.Instance,
            BaseFunnyType.Float32 => Float32Function.Instance,
            BaseFunnyType.Real   =>context.RealTypeSelect<IConcreteFunction>(RealDoubleFunction.Instance,RealDecimalFunction.Instance),
            _                    => throw new NFunImpossibleException("Unsupported type for this function")
        };

    private const string Id = "rangeWithStep";

    class Float32Function : FunctionWithManyArguments {
        public static readonly Float32Function Instance = new();
        private Float32Function() : base(
            Id, FunnyType.ArrayOf(FunnyType.Float32), FunnyType.Float32, FunnyType.Float32, FunnyType.Float32) { }
        public override object Calc(object[] args) {
            var start = (float)args[0];
            var end = (float)args[1];
            var step = (float)args[2];
            if (step <= 0f) throw new FunnyRuntimeException("Step has to be positive");
            var list = new List<float>();
            if (start <= end) for (float v = start; v <= end; v += step) list.Add(v);
            else for (float v = start; v >= end; v -= step) list.Add(v);
            return new ImmutableFunnyArray(list.ToArray(), FunnyType.Float32);
        }
    }

    class Int16Function : FunctionWithManyArguments {
        public static readonly Int16Function Instance = new();

        private Int16Function() : base(
            Id, FunnyType.ArrayOf(FunnyType.Int16), FunnyType.Int16, FunnyType.Int16,
            FunnyType.Int16)
        { }

        public override object Calc(object[] args)
        {
            var start = (int) args[0];
            var end = (int) args[1];
            var step = (int) args[2];
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
            var start = (int) args[0];
            var end = (int) args[1];
            var step = (int) args[2];
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
            var start = (long) args[0];
            var end = (long) args[1];
            var step = (long) args[2];
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
            var start = (byte) args[0];
            var end = (byte) args[1];
            var step = (byte) args[2];
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

    class Int8Function : FunctionWithManyArguments {
        public static readonly Int8Function Instance = new();

        private Int8Function() : base(
            Id, FunnyType.ArrayOf(FunnyType.Int8), FunnyType.Int8, FunnyType.Int8,
            FunnyType.Int8)
        { }

        public override object Calc(object[] args)
        {
            var start = (sbyte) args[0];
            var end = (sbyte) args[1];
            var step = (sbyte) args[2];
            if (step <= 0)
                throw new FunnyRuntimeException("Step has to be positive");

            var result = new List<sbyte>();
            if (start < end)
                for (var i = start; i <= end; i = (sbyte)(i + step))
                    result.Add(i);
            else
                for (var i = start; i >= end; i = (sbyte)(i - step))
                    result.Add(i);
            return new ImmutableFunnyArray(result.ToArray(), FunnyType.Int8);
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
            var start = (ushort) args[0];
            var end = (ushort) args[1];
            var step = (ushort) args[2];
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
            var start = (UInt32) args[0];
            var end = (UInt32) args[1];
            var step = (uint) args[2];
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
            var start = (ulong) args[0];
            var end = (ulong) args[1];
            var step = (ulong) args[2];
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
            var start = (double) args[0];
            var end = (double) args[1];
            var step = (double) args[2];
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
            var start = (decimal) args[0];
            var end = (decimal) args[1];
            var step = (decimal) args[2];
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