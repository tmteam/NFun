using System;
using System.Collections.Generic;
using System.Linq;
using NFun.Interpritation.Functions;
using NFun.Runtime.Arrays;
using NFun.Types;

namespace NFun.BuiltInFunctions
{
    public class LastFunction : GenericFunctionBase
    {
        public LastFunction() : base("last",
            VarType.Generic(0),
            VarType.ArrayOf(VarType.Generic(0)))
        {
        }

        protected override object Calc(object[] args)
        {
            var arr = (IFunArray) args[0];
            var ans = arr.GetElementOrNull(arr.Count-1);
            return ans ?? throw new FunRuntimeException("Array is empty");

        }
    }
    public class FirstFunction : GenericFunctionBase
    {
        public FirstFunction() : base("first",
            VarType.Generic(0),
            VarType.ArrayOf(VarType.Generic(0)))
        {
        }

        protected override object Calc(object[] args)
        {
            var arr = (IFunArray)args[0];
            var ans =  arr.GetElementOrNull(0);
            return ans ?? throw new FunRuntimeException("Array is empty");
        }
    }

    public class CountFunction : GenericFunctionWithSingleArgument
    {
        public CountFunction() : base("count",
            VarType.Int32,
            VarType.ArrayOf(VarType.Generic(0)))
        {
        }

        protected override object Calc(object a)
            => ((IFunArray)a).Count;
    }
    public class MapFunction : GenericFunctionWithTwoArguments
    {
        public MapFunction() : base("map",
            VarType.ArrayOf(VarType.Generic(1)),
            VarType.ArrayOf(VarType.Generic(0)),
            VarType.Fun(VarType.Generic(1), VarType.Generic(0)))
        {
        }

        protected override object Calc(object a, object b)
        {
            var arr = (IFunArray)a;
            
            if(b is FunctionWithSingleArg mapFunc)
                return new EnumerableFunArray(arr.Select(e=>mapFunc.Calc(e)));
            
            var map = (IConcreteFunction)b;
                
            return new EnumerableFunArray(arr.Select(e => map.Calc(new[] { e })));
        }
    }
    public class IsInSingleGenericFunctionDefenition : GenericFunctionBase
    {
        public IsInSingleGenericFunctionDefenition() : base(CoreFunNames.In, 
            VarType.Bool,
            VarType.Generic(0), 
            VarType.ArrayOf(VarType.Generic(0)))
        {
        }

        protected override object Calc(object[] args)
        {
            var val = args[0];
            var arr = (IFunArray)args[1];
            return arr.Any(a => TypeHelper.AreEqual(a, val));
        }
    }
    public class SliceWithStepGenericFunctionDefenition : GenericFunctionBase
    {
        public SliceWithStepGenericFunctionDefenition() : base(CoreFunNames.SliceName, 
            VarType.ArrayOf(VarType.Generic(0)),
            VarType.ArrayOf(VarType.Generic(0)),
            VarType.Int32,
            VarType.Int32,
            VarType.Int32)
        {
        }

        protected override object Calc(object[] args)
        {
            var start = ((int)args[1]);
            if(start<0)
                throw new FunRuntimeException("Argument out of range");
            var end = ((int)args[2]);
            if(end<0)
                throw new FunRuntimeException("Argument out of range");
            if(end!=0 && start>end)
                throw new FunRuntimeException("Start cannot be more than end");
            var step = ((int)args[3]);
            if(step<0)
                throw new FunRuntimeException("Argument out of range");
            if (step == 0)
                step = 1;
            var arr = (IFunArray)args[0];
            return arr.Slice(start, end, step);
        }
    }
    public class SortFunction : GenericFunctionBase
    {
        public SortFunction() : base("sort", GenericConstrains.Comparable, VarType.ArrayOf(VarType.Generic(0)), VarType.ArrayOf(VarType.Generic(0))) { }

        protected override object Calc(object[] args)
        {
            var funArray =  (IFunArray) args[0];
            
            var arr = funArray.As<IComparable>().ToArray();
            Array.Sort(arr);
            return new ImmutableFunArray(arr);
        }
    }
    public class MedianFunction : GenericFunctionBase
    {
        public MedianFunction() : base("median", GenericConstrains.Comparable, VarType.Generic(0), VarType.ArrayOf(VarType.Generic(0)))
        {

        }

        protected override object Calc(object[] args)
            => GetMedian(((IFunArray)args[0]).As<IComparable>());

        private static IComparable GetMedian(IEnumerable<IComparable> source)
        {
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
    public class MaxElementFunction : GenericFunctionBase
    {
        public MaxElementFunction() : base("max", GenericConstrains.Comparable, VarType.Generic(0), VarType.ArrayOf(VarType.Generic(0))) {}

        protected override object Calc(object[] args)
        {
            var array = (IFunArray) args[0];
            return array.As<IComparable>().Max();
        }
    }
    public class MinElementFunction : GenericFunctionBase
    {
        public MinElementFunction() : base("min", GenericConstrains.Comparable, VarType.Generic(0), VarType.ArrayOf(VarType.Generic(0))) { }

        protected override object Calc(object[] args)
        {
            var array = (IFunArray)args[0];
            return array.As<IComparable>().Min();
        }
    }

    public class MultiSumFunction : GenericFunctionBase
    {
        public const string id = "sum";
        public MultiSumFunction() : base(id, GenericConstrains.Arithmetical, VarType.Generic(0), VarType.ArrayOf(VarType.Generic(0))){
        }


        public override IConcreteFunction CreateConcrete(VarType[] concreteTypes)
        {
            switch (concreteTypes[0].BaseType)
            {
                case BaseVarType.UInt16: return new UInt16Function();
                case BaseVarType.UInt32: return new UInt32Function();
                case BaseVarType.UInt64: return new UInt64Function();
                case BaseVarType.Int16: return new Int16Function();
                case BaseVarType.Int32: return new Int32Function();
                case BaseVarType.Int64: return new Int64Function();
                case BaseVarType.Real: return new RealFunction();
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public class RealFunction : FunctionWithManyArguments
        {
            public RealFunction() : base(id, VarType.Real, VarType.ArrayOf(VarType.Real)) { }

            public override object Calc(object[] args) => ((IFunArray) args[0]).As<double>().Sum();
        }
        public class Int16Function : FunctionWithManyArguments
        {
            public Int16Function() : base(id, VarType.Int16, VarType.ArrayOf(VarType.Int16)) { }
            public override object Calc(object[] args)
            {
                short answer = 0;
                foreach (var i in ((IFunArray)args[0]).As<short>())
                    answer += i;
                return answer;
            }
        }
        public class Int32Function : FunctionWithManyArguments
        {
            public Int32Function() : base(id, VarType.Int32, VarType.ArrayOf(VarType.Int32)) { }
            public override object Calc(object[] args) => ((IFunArray) args[0]).As<int>().Sum();
        }
        public class Int64Function : FunctionWithManyArguments
        {
            public Int64Function() : base(id, VarType.Int64, VarType.ArrayOf(VarType.Int64)) { }
            public override object Calc(object[] args) => ((IFunArray) args[0]).As<long>().Sum();
        }
        public class UInt16Function : FunctionWithManyArguments
        {
            public UInt16Function() : base(id, VarType.UInt16, VarType.ArrayOf(VarType.UInt16)) { }
            public override object Calc(object[] args)
            {
                ushort answer = 0;
                foreach (var i in ((IFunArray)args[0]).As<ushort>())
                    answer += i;
                return answer;
            }
        }
        public class UInt32Function : FunctionWithManyArguments
        {
            public UInt32Function() : base(id, VarType.UInt32, VarType.ArrayOf(VarType.UInt32)) { }
            public override object Calc(object[] args)
            {
                uint answer = 0;
                foreach (var i in ((IFunArray)args[0]).As<uint>())
                    answer += i;
                return answer;
            }
        }
        public class UInt64Function : FunctionWithManyArguments
        {
            public UInt64Function() : base(id, VarType.UInt64, VarType.ArrayOf(VarType.UInt64)) { }
            public override object Calc(object[] args)
            {
                ulong answer = 0;
                foreach (var i in ((IFunArray) args[0]).As<ulong>())
                    answer += i;
                return answer;
            }
        }
    }

   

    public class RangeFunction : GenericFunctionBase
    {
        public RangeFunction() : base(CoreFunNames.RangeName,
            GenericConstrains.Numbers,
            VarType.ArrayOf(VarType.Generic(0)), VarType.Generic(0), VarType.Generic(0))
        {
        }

        public override IConcreteFunction CreateConcrete(VarType[] concreteTypes)
        {
            switch (concreteTypes[0].BaseType)
            {
                case BaseVarType.UInt8:
                    return new UInt8Function();
                case BaseVarType.UInt16:
                    return new UInt16Function();
                case BaseVarType.UInt32:
                    return new UInt32Function();
                case BaseVarType.UInt64:
                    return new UInt64Function();
                case BaseVarType.Int16:
                    return new Int16Function();
                case BaseVarType.Int32:
                    return new Int32Function();
                case BaseVarType.Int64:
                    return new Int64Function();
                case BaseVarType.Real:
                    return new RealFunction();
                default:
                    throw new NotSupportedException();
            }
        }

        private const string id = "range";
        class Int16Function : FunctionWithManyArguments
        {
            public Int16Function() : base(id, VarType.ArrayOf(VarType.Int16), VarType.Int16, VarType.Int16) { }
            public override object Calc(object[] args)
            {
                var start = ((short)args[0]);
                var end = ((short)args[1]);
                var result = new List<short>();

                if (start < end)
                    for (var i = start; i <= end; i += 1)
                        result.Add(i);
                else
                    for (var i = start; i >= end; i -= 1)
                        result.Add(i);
                
                return new ImmutableFunArray(result.ToArray());
            }
        }
        class Int32Function : FunctionWithTwoArgs
        {
            public Int32Function() : base(id, VarType.ArrayOf(VarType.Int32), VarType.Int32, VarType.Int32) { }

            public override object Calc(object a, object b)
            {
                var start = ((int)a);
                var end = ((int)b);
                var result = new List<int>();

                if (start < end)
                    for (int i = start; i <= end; i += 1)
                        result.Add(i);
                else
                    for (int i = start; i >= end; i -= 1)
                        result.Add(i);
                return new ImmutableFunArray(result.ToArray());            }
        }
        class Int64Function : FunctionWithManyArguments
        {
            public Int64Function() : base(id, VarType.ArrayOf(VarType.Int64), VarType.Int64, VarType.Int64) { }

            public override object Calc(object[] args)
            {
                var start = (long)args[0];
                var end =   (long)args[1];
                var result = new List<long>();

                if (start < end)
                    for (var i = start; i <= end; i += 1)
                        result.Add(i);
                else
                    for (var i = start; i >= end; i -= 1)
                        result.Add(i);
                return new ImmutableFunArray(result.ToArray());
            }
        }
        class UInt8Function : FunctionWithManyArguments
        {
            public UInt8Function() : base(id, VarType.ArrayOf(VarType.UInt8), VarType.UInt8, VarType.UInt8) { }
            public override object Calc(object[] args)
            {
                var start = ((byte)args[0]);
                var end = ((byte)args[1]);
                var result = new List<byte>();

                if (start < end)
                    for (var i = start; i <= end; i += 1)
                        result.Add(i);
                else
                    for (var i = start; i >= end; i -= 1)
                        result.Add(i);
                return new ImmutableFunArray(result.ToArray());
            }
        }
        class UInt16Function : FunctionWithManyArguments
        {
            public UInt16Function() : base(id, VarType.ArrayOf(VarType.UInt16), VarType.UInt16, VarType.UInt16) { }
            public override object Calc(object[] args)
            {
                var start = ((ushort)args[0]);
                var end = ((ushort)args[1]);
                var result = new List<ushort>();

                if (start < end)
                    for (var i = start; i <= end; i += 1)
                        result.Add(i);
                else
                    for (var i = start; i >= end; i -= 1)
                        result.Add(i);
                return new ImmutableFunArray(result.ToArray());
            }
        }
        class UInt32Function : FunctionWithManyArguments
        {
            public UInt32Function() : base(id, VarType.ArrayOf(VarType.UInt32), VarType.UInt32, VarType.UInt32) { }
            public override object Calc(object[] args)
            {
                var start = ((uint)args[0]);
                var end = ((uint)args[1]);
                var result = new List<uint>();

                if (start < end)
                    for (var i = start; i <= end; i += 1)
                        result.Add(i);
                else
                    for (var i = start; i >= end; i -= 1)
                        result.Add(i);
                return new ImmutableFunArray(result.ToArray());
            }
        }
        class UInt64Function : FunctionWithManyArguments
        {
            public UInt64Function() : base(id, VarType.ArrayOf(VarType.UInt64), VarType.UInt64, VarType.UInt64) { }

            public override object Calc(object[] args)
            {
                var start = (ulong)args[0];
                var end =   (ulong)args[1];
                var result = new List<ulong>();

                if (start < end)
                    for (var i = start; i <= end; i += 1)
                        result.Add(i);
                else
                    for (var i = start; i >= end; i -= 1)
                        result.Add(i);
                return new ImmutableFunArray(result.ToArray());
            }
        }
        class RealFunction : FunctionWithManyArguments
        {
            public RealFunction() : base(id, VarType.ArrayOf(VarType.Real), VarType.Real, VarType.Real) { }

            public override object Calc(object[] args)
            {
                var start = (double)args[0];
                var end =   (double)args[1];
                
                var result = new List<double>();

                if (start < end)
                    for (var i = start; i <= end; i += 1)
                        result.Add(i);
                else
                    for (var i = start; i >= end; i -= 1)
                        result.Add(i);
                return new ImmutableFunArray(result.ToArray());
            }
        }
    }
    public class RangeStepFunction : GenericFunctionBase
    {
        public RangeStepFunction() : base(CoreFunNames.RangeName, 
            GenericConstrains.Numbers, 
            VarType.ArrayOf(VarType.Generic(0)), VarType.Generic(0), VarType.Generic(0), VarType.Generic(0))
        {
        }

        public override IConcreteFunction CreateConcrete(VarType[] concreteTypes)
        {
            switch (concreteTypes[0].BaseType)
            {
                case BaseVarType.UInt8: return new UInt8Function();
                case BaseVarType.UInt16: return new UInt16Function();
                case BaseVarType.UInt32: return new UInt32Function();
                case BaseVarType.UInt64: return new UInt64Function();
                case BaseVarType.Int16: return new Int16Function();
                case BaseVarType.Int32: return new Int32Function();
                case BaseVarType.Int64: return new Int64Function();
                case BaseVarType.Real: return new RealFunction();

                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private const string id = "rangeWithStep";
        class Int16Function : FunctionWithManyArguments
        {
            public Int16Function() : base(id, VarType.ArrayOf(VarType.Int16), VarType.Int16, VarType.Int16, VarType.Int16) { }
            public override object Calc(object[] args)
            {
                var start = ((int)args[0]);
                var end = ((int)args[1]);
                var step = ((int)args[2]);
                if (step <= 0)
                    throw new FunRuntimeException("Step has to be positive");

                var result = new List<int>();
                if (start < end)
                    for (int i = start; i <= end; i += step)
                        result.Add(i);
                else
                    for (int i = start; i >= end; i -= step)
                        result.Add(i);
                return new ImmutableFunArray(result.ToArray());
            }
        }
        class Int32Function : FunctionWithManyArguments
        {
            public Int32Function() : base(id, VarType.ArrayOf(VarType.Int32), VarType.Int32, VarType.Int32, VarType.Int32) { }

            public override object Calc(object[] args)
            {
                var start = ((int)args[0]);
                var end = ((int)args[1]);
                var step = ((int)args[2]);
                if (step <= 0)
                    throw new FunRuntimeException("Step has to be positive");

                var result = new List<int>();
                if (start < end)
                    for (int i = start; i <= end; i += step)
                        result.Add(i);
                else
                    for (int i = start; i >= end; i -= step)
                        result.Add(i);
                return new ImmutableFunArray(result.ToArray());

            }
        }
        class Int64Function : FunctionWithManyArguments
        {
            public Int64Function() : base(id, VarType.ArrayOf(VarType.Int64), VarType.Int64, VarType.Int64, VarType.Int64) { }

            public override object Calc(object[] args)
            {
                var start = ((long)args[0]);
                var end = ((long)args[1]);
                var step = ((long)args[2]);
                if (step <= 0)
                    throw new FunRuntimeException("Step has to be positive");

                var result = new List<long>();
                if (start < end)
                    for (var i = start; i <= end; i += step)
                        result.Add(i);
                else
                    for (var i = start; i >= end; i -= step)
                        result.Add(i);
                return new ImmutableFunArray(result.ToArray());

            }
        }
        class UInt8Function : FunctionWithManyArguments
        {
            public UInt8Function() : base(id, VarType.ArrayOf(VarType.UInt8), VarType.UInt8, VarType.UInt8, VarType.UInt8) { }
            public override object Calc(object[] args)
            {
                var start = ((byte)args[0]);
                var end = ((byte)args[1]);
                var step = ((byte)args[2]);
                if (step <= 0)
                    throw new FunRuntimeException("Step has to be positive");

                var result = new List<byte>();
                if (start < end)
                    for (var i = start; i <= end; i += step)
                        result.Add(i);
                else
                    for (var i = start; i >= end; i -= step)
                        result.Add(i);
                return new ImmutableFunArray(result.ToArray());
            }
        }
        class UInt16Function : FunctionWithManyArguments
        {
            public UInt16Function() : base(id, VarType.ArrayOf(VarType.UInt16), VarType.UInt16, VarType.UInt16, VarType.UInt16) { }
            public override object Calc(object[] args)
            {
                var start = ((ushort)args[0]);
                var end = ((ushort)args[1]);
                var step = ((ushort)args[2]);
                if (step <= 0)
                    throw new FunRuntimeException("Step has to be positive");

                var result = new List<UInt16>();
                if (start < end)
                    for (var i = start; i <= end; i += step)
                        result.Add(i);
                else
                    for (var i = start; i >= end; i -= step)
                        result.Add(i);
                return new ImmutableFunArray(result.ToArray());
            }
        }
        class UInt32Function : FunctionWithManyArguments
        {
            public UInt32Function() : base(id, VarType.ArrayOf(VarType.UInt32), VarType.UInt32, VarType.UInt32, VarType.UInt32) { }
            public override object Calc(object[] args)
            {
                var start = ((UInt32)args[0]);
                var end = ((UInt32)args[1]);
                var step = ((uint)args[2]);
                if (step <= 0)
                    throw new FunRuntimeException("Step has to be positive");

                var result = new List<UInt32>();
                if (start < end)
                    for (var i = start; i <= end; i += step)
                        result.Add(i);
                else
                    for (var i = start; i >= end; i -= step)
                        result.Add(i);
                return new ImmutableFunArray(result.ToArray());
            }
        }
        class UInt64Function : FunctionWithManyArguments
        {
            public UInt64Function() : base(id, VarType.ArrayOf(VarType.UInt64), VarType.UInt64, VarType.UInt64, VarType.UInt64) { }
            public override object Calc(object[] args)
            {
                var start = ((ulong)args[0]);
                var end = ((ulong)args[1]);
                var step = ((ulong)args[2]);
                if (step <= 0)
                    throw new FunRuntimeException("Step has to be positive");

                var result = new List<UInt64>();
                if (start < end)
                    for (var i = start; i <= end; i += step)
                        result.Add(i);
                else
                    for (var i = start; i >= end; i -= step)
                        result.Add(i);
                return new ImmutableFunArray(result.ToArray());
            }
        }
        class RealFunction : FunctionWithManyArguments
        {
            public RealFunction() : base(id, VarType.ArrayOf(VarType.Real), VarType.Real, VarType.Real, VarType.Real) { }
            public override object Calc(object[] args)
            {
                var start = ((double)args[0]);
                var end = ((double)args[1]);
                var step = ((double)args[2]);
                if (step <= 0)
                    throw new FunRuntimeException("Step has to be positive");

                var result = new List<double>();
                if (start < end)
                    for (var i = start; i <= end; i += step)
                        result.Add(i);
                else
                    for (var i = start; i >= end; i -= step)
                        result.Add(i);
                return new ImmutableFunArray(result.ToArray());
            }
        }
    }
    public class SliceGenericFunctionDefenition : GenericFunctionBase
    {
        public SliceGenericFunctionDefenition() : base(CoreFunNames.SliceName, 
            VarType.ArrayOf(VarType.Generic(0)),
            VarType.ArrayOf(VarType.Generic(0)),
            VarType.Int32,
            VarType.Int32)
        {
        }

        protected override object Calc(object[] args)
        {
            var start = ((int)args[1]);
            if(start<0)
                throw new FunRuntimeException("Argument out of range");

            var end = ((int)args[2]);
            if(end<0)
                throw new FunRuntimeException("Argument out of range");
                
            if(end!=0 && start>end)
                throw new FunRuntimeException("Start cannot be more than end");
       
            var arr = (IFunArray)args[0];
            return arr.Slice(start, (end==int.MaxValue?null:(int?)end), null);
        }
    }
    public class GetGenericFunctionDefenition : GenericFunctionWithTwoArguments
    {
        public GetGenericFunctionDefenition() : base(CoreFunNames.GetElementName, 
            VarType.Generic(0),
            VarType.ArrayOf(VarType.Generic(0)),
            VarType.Int32)
        {
        }

        protected override object Calc(object a, object b)
        {
            var index = ((int)b);
            if(index<0)
                throw new FunRuntimeException("Argument out of range");
                
            var arr = (IFunArray)a;
            var res =arr.GetElementOrNull(index);
            
            if(res==null)
                throw new FunRuntimeException("Argument out of range");
            return res;
        }
    }
    public class SetGenericFunctionDefenition : GenericFunctionBase
    {
        
        public SetGenericFunctionDefenition() : base("set", 
            VarType.ArrayOf(VarType.Generic(0)),
            VarType.ArrayOf(VarType.Generic(0)),
            VarType.Int32,
            VarType.Generic(0))
        {
        }

        protected override object Calc(object[] args)
        {
            var arr = (IFunArray)args[0];

            var index = ((int)args[1]);
            if(index<0)
                throw new FunRuntimeException("Argument out of range");
            if(index>arr.Count+1)
                throw new FunRuntimeException("Argument out of range");
            var val = args[2];

            var newArr = arr.ClrArray;
            newArr.SetValue(val, index);
            return new ImmutableFunArray(newArr);
        }
    }
    public class FindGenericFunctionDefenition : GenericFunctionBase
    {
        public FindGenericFunctionDefenition() : base("find", 
            VarType.Int32,
            VarType.ArrayOf(VarType.Generic(0)),
            VarType.Generic(0))
        {
        }

        protected override object Calc(object[] args)
        {
            var arr = (IFunArray)args[0];
            var factor = args[1] ;
            int i = 0;
            foreach (var element in arr)
            {
                if(TypeHelper.AreEqual(element, factor))
                    return i;
                i++;
            }
            return -1;
        }
    }
    public class ChunkGenericFunctionDefenition : GenericFunctionBase
    {
        public ChunkGenericFunctionDefenition() : base("chunk", 
            VarType.ArrayOf(VarType.ArrayOf(VarType.Generic(0))),
            VarType.ArrayOf(VarType.Generic(0)),
            VarType.Int32)
        {
        }

        protected override object Calc(object[] args)
        {
            var arr = (IFunArray)args[0];
            var chunkSize = ((int)args[1]);
            if(chunkSize<=0)
                throw new FunRuntimeException("Chunk size is "+chunkSize+". It has to be positive");

            var res = arr
                .Select((x, i) => new {Index = i, Value = x})
                .GroupBy(x => x.Index / chunkSize)
                .Select(x => new EnumerableFunArray(x.Select(v => v.Value)));
            return new EnumerableFunArray(res);
        }
    }
    public class FlatGenericFunctionDefenition : GenericFunctionBase
    {
        public FlatGenericFunctionDefenition() : base("flat", 
            VarType.ArrayOf(VarType.Generic(0)),
            VarType.ArrayOf(VarType.ArrayOf(VarType.Generic(0))))
        {
        }

        protected override object Calc(object[] args)
        {
            var arr = (IFunArray)args[0];
            return new EnumerableFunArray(arr.SelectMany(o => (IFunArray) o));
        }
    }

    public class FoldGenericFunctionDefenition : GenericFunctionWithTwoArguments
    {
        public FoldGenericFunctionDefenition() : base("fold", new[] {GenericConstrains.Any},
            VarType.Generic(0),
            VarType.ArrayOf(VarType.Generic(0)),
            VarType.Fun(VarType.Generic(0), VarType.Generic(0), VarType.Generic(0)))
        {
        }

        protected override object Calc(object arg1, object arg2)
        {
            var arr = (IFunArray) arg1;
            if (arr.Count == 0)
                throw new FunRuntimeException("Input array is empty");
            if (arg2 is FunctionWithTwoArgs fold2)
                return arr.Aggregate((a, b) => fold2.Calc(a, b));

            var fold = (IConcreteFunction) arg2;

            return arr.Aggregate((a, b) => fold.Calc(new[] {a, b}));
        }
    }

    public class foldWithDefaultsGenericFunctionDefenition : GenericFunctionBase
    {
        public foldWithDefaultsGenericFunctionDefenition() : base("fold", 
            returnType: VarType.Generic(1),
            VarType.ArrayOf(VarType.Generic(0)),
            VarType.Generic(1),
            VarType.Fun(
                    returnType: VarType.Generic(1), VarType.Generic(1), VarType.Generic(0)))
        {}

        protected override object Calc(object[] args)
        {
            var arr = (IFunArray)args[0];
            var defaultValue = args[1];

            var fold = (IConcreteFunction) args[2];

            if (fold is FunctionWithTwoArgs fold2)
                return arr.Aggregate(defaultValue, (a,b)=>fold2.Calc(a, b));
            else
                return arr.Aggregate(defaultValue, (a,b)=>fold.Calc(new []{a,b}));
        }
    }
    public class UniteGenericFunctionDefenition : GenericFunctionBase
    {
        public UniteGenericFunctionDefenition() : base("unite", 
            VarType.ArrayOf(VarType.Generic(0)),
            VarType.ArrayOf(VarType.Generic(0)),
            VarType.ArrayOf(VarType.Generic(0)))
        {
        }

        protected override object Calc(object[] args)
        {
            var arr1 = (IFunArray)args[0];
            var arr2 = (IFunArray)args[1];
            return new EnumerableFunArray(arr1.Union(arr2));
        }
    }
    public class UniqueGenericFunctionDefenition : GenericFunctionBase
    {
        public UniqueGenericFunctionDefenition() : base("unique", 
            VarType.ArrayOf(VarType.Generic(0)),
            VarType.ArrayOf(VarType.Generic(0)),
            VarType.ArrayOf(VarType.Generic(0)))
        {
        }

        protected override object Calc(object[] args)
        {
            var arr1 = (IFunArray)args[0];
            var arr2 = (IFunArray)args[1];
            return new EnumerableFunArray(arr1.Except(arr2).Concat(arr2.Except(arr1)));
        }
    }
    public class IntersectGenericFunctionDefenition : GenericFunctionBase
    {
        public IntersectGenericFunctionDefenition() : base("intersect", 
            VarType.ArrayOf(VarType.Generic(0)),
            VarType.ArrayOf(VarType.Generic(0)),
            VarType.ArrayOf(VarType.Generic(0)))
        {
        }

        protected override object Calc(object[] args)
        {
            var arr1 = (IFunArray)args[0];
            var arr2 = (IFunArray)args[1];
            return new EnumerableFunArray(arr1.Intersect(arr2));
        }
    }
    public class ConcatArraysGenericFunctionDefenition : GenericFunctionWithTwoArguments
    {
        public ConcatArraysGenericFunctionDefenition() : base("concat", 
            VarType.ArrayOf(VarType.Generic(0)),
            VarType.ArrayOf(VarType.Generic(0)),
            VarType.ArrayOf(VarType.Generic(0)))
        {
        }

        protected override object Calc(object a, object b)
        {
            var arr1 = (IFunArray)a;
            var arr2 = (IFunArray)b;
            var res = new EnumerableFunArray(arr1.Concat(arr2));
            return res;
        }
    }

    public class AppendGenericFunctionDefenition : GenericFunctionBase
    {
        public AppendGenericFunctionDefenition() : base("append",
            VarType.ArrayOf(VarType.Generic(0)),
            VarType.ArrayOf(VarType.Generic(0)),
            VarType.Generic(0))
        {
        }

        protected override object Calc(object[] args)
        {
            var arr1 = (IFunArray)args[0];
            var arr2 = args[1];
            var res = new EnumerableFunArray(arr1.Append(arr2));
            return res;
        }
    }

    public class SubstractArraysGenericFunctionDefenition : GenericFunctionBase
    {
        public SubstractArraysGenericFunctionDefenition() : base("except", 
            VarType.ArrayOf(VarType.Generic(0)),
            VarType.ArrayOf(VarType.Generic(0)),
            VarType.ArrayOf(VarType.Generic(0)))
        {
        }

        protected override object Calc(object[] args)
        {
            var arr1 = (IFunArray)args[0];
            var arr2 = (IFunArray)args[1];
            return new EnumerableFunArray(arr1.Except(arr2));
        }
    }

    public class CountOfGenericFunctionDefenition : GenericFunctionBase
    {
        public CountOfGenericFunctionDefenition() : base("count",
            VarType.Int32,
            VarType.ArrayOf(VarType.Generic(0)),
            VarType.Fun(VarType.Bool, VarType.Generic(0)))
        {
        }

        protected override object Calc(object[] args)
        {
            var arr = (IFunArray)args[0];
            var filter = (IConcreteFunction) args[1];

            return arr.Count(a => (bool)filter.Calc(new[] { a }));
        }
    }

    public class HasAnyGenericFunctionDefenition : GenericFunctionBase
    {
        public HasAnyGenericFunctionDefenition() : base("any",
            VarType.Bool,
            VarType.ArrayOf(VarType.Generic(0)))
        {
        }

        protected override object Calc(object[] args)
        {
            var arr = (IFunArray)args[0];
            return arr.Count>0;
        }
    }
    public class AnyGenericFunctionDefenition : GenericFunctionWithTwoArguments
    {
        public AnyGenericFunctionDefenition() : base("any", 
            VarType.Bool,
            VarType.ArrayOf(VarType.Generic(0)),
            VarType.Fun(VarType.Bool, VarType.Generic(0)))
        {
        }

        protected override object Calc(object a, object b)
        {
            var arr    = (IFunArray)a;

            if(b is FunctionWithSingleArg predicate)
                return arr.Any(e=>(bool)predicate.Calc(e));

            var filter = (IConcreteFunction) b;
            return arr.Any(e => (bool) filter.Calc(new[] {e}));
        }
    }
    public class AllGenericFunctionDefenition : GenericFunctionBase
    {
        public AllGenericFunctionDefenition() : base("all", 
            VarType.Bool,
            VarType.ArrayOf(VarType.Generic(0)),
            VarType.Fun(VarType.Bool, VarType.Generic(0)))
        {
        }

        protected override object Calc(object[] args)
        {
            var arr = (IFunArray)args[0];
            var filter = (IConcreteFunction) args[1];

            return arr.All(a => (bool) filter.Calc(new[] {a}));
        }
    }
    public class FilterGenericFunctionDefenition : GenericFunctionWithTwoArguments
    {
        public FilterGenericFunctionDefenition() : base("filter", 
            VarType.ArrayOf(VarType.Generic(0)),
            VarType.ArrayOf(VarType.Generic(0)),
            VarType.Fun(VarType.Bool, VarType.Generic(0)))
        {
        }

        protected override object Calc(object a, object b)
        {
            var arr    = (IFunArray)a;
            if(b is FunctionWithSingleArg predicate)
                return new EnumerableFunArray(arr.Where(e=>(bool)predicate.Calc(e)));
            var filter = (IConcreteFunction)b;
            
            return new EnumerableFunArray(arr.Where(e=>(bool)filter.Calc(new []{e})));
        }
    }
    public class RepeatGenericFunctionDefenition : GenericFunctionBase
    {
        public RepeatGenericFunctionDefenition() : base("repeat",
            VarType.ArrayOf(VarType.Generic(0)), 
            VarType.Generic(0), 
            VarType.Int32)
        {
        }

        protected override object Calc(object[] args)
        {
            var first = args[0];
            return new EnumerableFunArray(Enumerable.Repeat(first, (int)args[1]));
        }
    }
    public class ReverseGenericFunctionDefenition: GenericFunctionBase
    {
        public ReverseGenericFunctionDefenition() : base("reverse", 
            VarType.ArrayOf(VarType.Generic(0)), 
            VarType.ArrayOf(VarType.Generic(0)))
        {
        }

        protected override object Calc(object[] args)
        {
            var arr  = (IFunArray) args[0];
            return new EnumerableFunArray(arr.Reverse());
        }
    }
    public class TakeGenericFunctionDefenition: GenericFunctionBase
    {
        public TakeGenericFunctionDefenition() : base("take", 
            VarType.ArrayOf(VarType.Generic(0)), 
            VarType.ArrayOf(VarType.Generic(0)), 
            VarType.Int32)
        {
        }

        protected override object Calc(object[] args)
        {
            return ((IFunArray)args[0]).Slice(null,((int)args[1])-1,1);
        }
    }
    public class SkipGenericFunctionDefenition: GenericFunctionBase
    {
        public SkipGenericFunctionDefenition() : base("skip", 
            VarType.ArrayOf(VarType.Generic(0)), 
            VarType.ArrayOf(VarType.Generic(0)), 
            VarType.Int32)
        {
        }

        protected override object Calc(object[] args)
        {
            return ((IFunArray)args[0]).Slice(((int)args[1]),null,1);
        }
    }
}