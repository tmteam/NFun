using System;
using System.Collections.Generic;
using System.Linq;
using NFun.Exceptions;
using NFun.Interpretation.Functions;
using NFun.Runtime.Arrays;
using NFun.Types;

namespace NFun.BuiltInFunctions
{
    public class LastFunction : GenericFunctionBase
    {
        public LastFunction() : base("last",
            FunnyType.Generic(0),
            FunnyType.ArrayOf(FunnyType.Generic(0)))
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
            FunnyType.Generic(0),
            FunnyType.ArrayOf(FunnyType.Generic(0)))
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
            FunnyType.Int32,
            FunnyType.ArrayOf(FunnyType.Generic(0)))
        {
        }

        protected override object Calc(object a)
            => ((IFunArray)a).Count;
    }
    public class MapFunction : GenericFunctionBase
    {
        public MapFunction() : base("map",
            FunnyType.ArrayOf(FunnyType.Generic(1)),
            
            FunnyType.ArrayOf(FunnyType.Generic(0)),
            FunnyType.Fun(FunnyType.Generic(1), FunnyType.Generic(0)))
        {
        }
        public override IConcreteFunction CreateConcrete(FunnyType[] concreteTypesMap)
        {
            var res = new ConcreteMap
            {
                Name = Name,
                ArgTypes = new[]
                {
                    FunnyType.ArrayOf(concreteTypesMap[0]),
                    FunnyType.Fun(concreteTypesMap[1], concreteTypesMap[0])
                },
                ReturnType = FunnyType.ArrayOf(concreteTypesMap[1])
            };
            return res;
        }

        class ConcreteMap: FunctionWithTwoArgs
        {
            public override object Calc(object a, object b)
            {
                var arr = (IFunArray)a;
                var type = ReturnType.ArrayTypeSpecification.FunnyType;            
                if(b is FunctionWithSingleArg mapFunc)
                    return new EnumerableFunArray(arr.Select(e=>mapFunc.Calc(e)),type);
            
                var map = (IConcreteFunction)b;
                
                return new EnumerableFunArray(arr.Select(e => map.Calc(new[] { e })),type);
            }
        }
    }
    public class IsInSingleGenericFunctionDefinition : GenericFunctionBase
    {
        public IsInSingleGenericFunctionDefinition() : base(CoreFunNames.In, 
            FunnyType.Bool,
            FunnyType.Generic(0), 
            FunnyType.ArrayOf(FunnyType.Generic(0)))
        {
        }

        protected override object Calc(object[] args)
        {
            var val = args[0];
            var arr = (IFunArray)args[1];
            return arr.Any(a => TypeHelper.AreEqual(a, val));
        }
    }
    public class SliceWithStepGenericFunctionDefinition : GenericFunctionBase
    {
        public SliceWithStepGenericFunctionDefinition() : base(CoreFunNames.SliceName, 
            FunnyType.ArrayOf(FunnyType.Generic(0)),
            FunnyType.ArrayOf(FunnyType.Generic(0)),
            FunnyType.Int32,
            FunnyType.Int32,
            FunnyType.Int32)
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
        public SortFunction() : base("sort", GenericConstrains.Comparable,
            FunnyType.ArrayOf(FunnyType.Generic(0)), FunnyType.ArrayOf(FunnyType.Generic(0)))
        {
            
        }

        protected override object Calc(object[] args)
        {
            var funArray =  (IFunArray) args[0];
            
            var arr = funArray.As<IComparable>().ToArray();
            Array.Sort(arr);
            return new ImmutableFunArray(arr, funArray.ElementType);
        }
    }
    public class MedianFunction : GenericFunctionBase
    {
        public MedianFunction() : base("median", GenericConstrains.Comparable, FunnyType.Generic(0), FunnyType.ArrayOf(FunnyType.Generic(0)))
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
        public MaxElementFunction() : base("max", GenericConstrains.Comparable, FunnyType.Generic(0), FunnyType.ArrayOf(FunnyType.Generic(0))) {}

        protected override object Calc(object[] args)
        {
            var array = (IFunArray) args[0];
            return array.As<IComparable>().Max();
        }
    }
    public class MinElementFunction : GenericFunctionBase
    {
        public MinElementFunction() : base("min", GenericConstrains.Comparable, FunnyType.Generic(0), FunnyType.ArrayOf(FunnyType.Generic(0))) { }

        protected override object Calc(object[] args)
        {
            var array = (IFunArray)args[0];
            return array.As<IComparable>().Min();
        }
    }

    public class MultiSumFunction : GenericFunctionBase
    {
        private const string Id = "sum";
        public MultiSumFunction() : base(Id, GenericConstrains.Arithmetical, FunnyType.Generic(0), FunnyType.ArrayOf(FunnyType.Generic(0))){
        }


        public override IConcreteFunction CreateConcrete(FunnyType[] concreteTypes)
        {
            switch (concreteTypes[0].BaseType)
            {
                case BaseFunnyType.UInt16: return new UInt16Function();
                case BaseFunnyType.UInt32: return new UInt32Function();
                case BaseFunnyType.UInt64: return new UInt64Function();
                case BaseFunnyType.Int16: return new Int16Function();
                case BaseFunnyType.Int32: return new Int32Function();
                case BaseFunnyType.Int64: return new Int64Function();
                case BaseFunnyType.Real: return new RealFunction();
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public class RealFunction : FunctionWithSingleArg
        {
            public RealFunction() : base(Id, FunnyType.Real, FunnyType.ArrayOf(FunnyType.Real)) { }

            public override object Calc(object a) => ((IFunArray)a).As<double>().Sum();
        }
        public class Int16Function : FunctionWithSingleArg
        {
            public Int16Function() : base(Id, FunnyType.Int16, FunnyType.ArrayOf(FunnyType.Int16)) { }
            public override object Calc(object a)
            {
                short answer = 0;
                foreach (var i in ((IFunArray)a).As<short>())
                    answer += i;
                return answer;
            }
        }
        public class Int32Function : FunctionWithSingleArg
        {
            public Int32Function() : base(Id, FunnyType.Int32, FunnyType.ArrayOf(FunnyType.Int32)) { }
            public override object Calc(object a) => ((IFunArray) a).As<int>().Sum();
        }
        public class Int64Function : FunctionWithSingleArg
        {
            public Int64Function() : base(Id, FunnyType.Int64, FunnyType.ArrayOf(FunnyType.Int64)) { }
            public override object Calc(object a) => ((IFunArray) a).As<long>().Sum();
        }
        public class UInt16Function : FunctionWithSingleArg
        {
            public UInt16Function() : base(Id, FunnyType.UInt16, FunnyType.ArrayOf(FunnyType.UInt16)) { }
            public override object Calc(object a)
            {
                ushort answer = 0;
                foreach (var i in ((IFunArray)a).As<ushort>())
                    answer += i;
                return answer;
            }
        }
        public class UInt32Function : FunctionWithSingleArg
        {
            public UInt32Function() : base(Id, FunnyType.UInt32, FunnyType.ArrayOf(FunnyType.UInt32)) { }
            public override object Calc(object a)
            {
                uint answer = 0;
                foreach (var i in ((IFunArray)a).As<uint>())
                    answer += i;
                return answer;
            }
        }
        public class UInt64Function : FunctionWithSingleArg
        {
            public UInt64Function() : base(Id, FunnyType.UInt64, FunnyType.ArrayOf(FunnyType.UInt64)) { }
            public override object Calc(object a)
            {
                ulong answer = 0;
                foreach (var i in ((IFunArray) a).As<ulong>())
                    answer += i;
                return answer;
            }
        }
    }

   

    public class RangeFunction : GenericFunctionBase
    {
        public RangeFunction() : base(CoreFunNames.RangeName,
            GenericConstrains.Numbers,
            FunnyType.ArrayOf(FunnyType.Generic(0)), FunnyType.Generic(0), FunnyType.Generic(0))
        {
        }

        public override IConcreteFunction CreateConcrete(FunnyType[] concreteTypes)
        {
            switch (concreteTypes[0].BaseType)
            {
                case BaseFunnyType.UInt8:
                    return new UInt8Function();
                case BaseFunnyType.UInt16:
                    return new UInt16Function();
                case BaseFunnyType.UInt32:
                    return new UInt32Function();
                case BaseFunnyType.UInt64:
                    return new UInt64Function();
                case BaseFunnyType.Int16:
                    return new Int16Function();
                case BaseFunnyType.Int32:
                    return new Int32Function();
                case BaseFunnyType.Int64:
                    return new Int64Function();
                case BaseFunnyType.Real:
                    return new RealFunction();
                default:
                    throw new NotSupportedException();
            }
        }

        private const string Id = "range";
        class Int16Function : FunctionWithTwoArgs
        {
            public Int16Function() : base(Id, FunnyType.ArrayOf(FunnyType.Int16), FunnyType.Int16, FunnyType.Int16) { }
            public override object Calc(object a, object b)
            {
                var start = ((short)a);
                var end = ((short)b);
                var result = new List<short>();

                if (start < end)
                    for (var i = start; i <= end; i += 1)
                        result.Add(i);
                else
                    for (var i = start; i >= end; i -= 1)
                        result.Add(i);
                
                return new ImmutableFunArray(result.ToArray(), FunnyType.Int16);
            }
        }
        class Int32Function : FunctionWithTwoArgs
        {
            public Int32Function() : base(Id, FunnyType.ArrayOf(FunnyType.Int32), FunnyType.Int32, FunnyType.Int32) { }

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
                return new ImmutableFunArray(result.ToArray());            
            }
        }
        class Int64Function : FunctionWithTwoArgs
        {
            public Int64Function() : base(Id, FunnyType.ArrayOf(FunnyType.Int64), FunnyType.Int64, FunnyType.Int64) { }

            public override object Calc(object a, object b)
            {
                var start = (long)a;
                var end =   (long)b;
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
        class UInt8Function : FunctionWithTwoArgs
        {
            public UInt8Function() : base(Id, FunnyType.ArrayOf(FunnyType.UInt8), FunnyType.UInt8, FunnyType.UInt8) { }
            public override object Calc(object a, object b)
            {
                var start = ((byte)a);
                var end = ((byte)b);
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
        class UInt16Function : FunctionWithTwoArgs
        {
            public UInt16Function() : base(Id, FunnyType.ArrayOf(FunnyType.UInt16), FunnyType.UInt16, FunnyType.UInt16) { }
            public override object Calc(object a, object b)
            {
                var start = ((ushort)a);
                var end = ((ushort)b);
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
        class UInt32Function : FunctionWithTwoArgs
        {
            public UInt32Function() : base(Id, FunnyType.ArrayOf(FunnyType.UInt32), FunnyType.UInt32, FunnyType.UInt32) { }
            public override object Calc(object a, object b)
            {
                var start = ((uint)a);
                var end = ((uint)b);
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
        class UInt64Function : FunctionWithTwoArgs
        {
            public UInt64Function() : base(Id, FunnyType.ArrayOf(FunnyType.UInt64), FunnyType.UInt64, FunnyType.UInt64) { }

            public override object Calc(object a, object b)
            {
                var start = (ulong)a;
                var end =   (ulong)b;
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
        class RealFunction : FunctionWithTwoArgs
        {
            public RealFunction() : base(Id, FunnyType.ArrayOf(FunnyType.Real), FunnyType.Real, FunnyType.Real) { }

            public override object Calc(object a, object b)
            {
                var start = (double)a;
                var end =   (double)b;
                
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
            FunnyType.ArrayOf(FunnyType.Generic(0)), FunnyType.Generic(0), FunnyType.Generic(0), FunnyType.Generic(0))
        {
        }

        public override IConcreteFunction CreateConcrete(FunnyType[] concreteTypes)
        {
            switch (concreteTypes[0].BaseType)
            {
                case BaseFunnyType.UInt8: return new UInt8Function();
                case BaseFunnyType.UInt16: return new UInt16Function();
                case BaseFunnyType.UInt32: return new UInt32Function();
                case BaseFunnyType.UInt64: return new UInt64Function();
                case BaseFunnyType.Int16: return new Int16Function();
                case BaseFunnyType.Int32: return new Int32Function();
                case BaseFunnyType.Int64: return new Int64Function();
                case BaseFunnyType.Real: return new RealFunction();

                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private const string Id = "rangeWithStep";
        class Int16Function : FunctionWithManyArguments
        {
            public Int16Function() : base(Id, FunnyType.ArrayOf(FunnyType.Int16), FunnyType.Int16, FunnyType.Int16, FunnyType.Int16) { }
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
            public Int32Function() : base(Id, FunnyType.ArrayOf(FunnyType.Int32), FunnyType.Int32, FunnyType.Int32, FunnyType.Int32) { }

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
            public Int64Function() : base(Id, FunnyType.ArrayOf(FunnyType.Int64), FunnyType.Int64, FunnyType.Int64, FunnyType.Int64) { }

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
            public UInt8Function() : base(Id, FunnyType.ArrayOf(FunnyType.UInt8), FunnyType.UInt8, FunnyType.UInt8, FunnyType.UInt8) { }
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
            public UInt16Function() : base(Id, FunnyType.ArrayOf(FunnyType.UInt16), FunnyType.UInt16, FunnyType.UInt16, FunnyType.UInt16) { }
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
            public UInt32Function() : base(Id, FunnyType.ArrayOf(FunnyType.UInt32), FunnyType.UInt32, FunnyType.UInt32, FunnyType.UInt32) { }
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
            public UInt64Function() : base(Id, FunnyType.ArrayOf(FunnyType.UInt64), FunnyType.UInt64, FunnyType.UInt64, FunnyType.UInt64) { }
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
            public RealFunction() : base(Id, FunnyType.ArrayOf(FunnyType.Real), FunnyType.Real, FunnyType.Real, FunnyType.Real) { }
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
    public class SliceGenericFunctionDefinition : GenericFunctionBase
    {
        public SliceGenericFunctionDefinition() : base(CoreFunNames.SliceName, 
            FunnyType.ArrayOf(FunnyType.Generic(0)),
            FunnyType.ArrayOf(FunnyType.Generic(0)),
            FunnyType.Int32,
            FunnyType.Int32)
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
    public class GetGenericFunctionDefinition : GenericFunctionWithTwoArguments
    {
        public GetGenericFunctionDefinition() : base(CoreFunNames.GetElementName, 
            FunnyType.Generic(0),
            FunnyType.ArrayOf(FunnyType.Generic(0)),
            FunnyType.Int32)
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
    public class SetGenericFunctionDefinition : GenericFunctionBase
    {
        
        public SetGenericFunctionDefinition() : base("set", 
            FunnyType.ArrayOf(FunnyType.Generic(0)),
            FunnyType.ArrayOf(FunnyType.Generic(0)),
            FunnyType.Int32,
            FunnyType.Generic(0))
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

            var newArr = new object[arr.ClrArray.Length];
             arr.ClrArray.CopyTo(newArr,0);
            newArr.SetValue(val, index);
            return new ImmutableFunArray(newArr,arr.ElementType);
        }
    }
    public class FindGenericFunctionDefinition : GenericFunctionWithTwoArguments
    {
        public FindGenericFunctionDefinition() : base("find", 
            FunnyType.Int32,
            FunnyType.ArrayOf(FunnyType.Generic(0)),
            FunnyType.Generic(0))
        {
        }

        protected override object Calc(object  a, object b)
        {
            var arr = (IFunArray)a;
            var factor = b;
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
    public class ChunkGenericFunctionDefinition : GenericFunctionWithTwoArguments
    {
        public ChunkGenericFunctionDefinition() : base("chunk", 
            FunnyType.ArrayOf(FunnyType.ArrayOf(FunnyType.Generic(0))),
            FunnyType.ArrayOf(FunnyType.Generic(0)),
            FunnyType.Int32)
        {
        }

        protected override object Calc(object a, object b)
        {
            var arr = (IFunArray)a;
            var chunkSize = ((int)b);
            if(chunkSize<=0)
                throw new FunRuntimeException("Chunk size is "+chunkSize+". It has to be positive");
            
            var originInputType = FunnyType.ArrayOf(arr.ElementType);
            
            var res = arr
                .Select((x, i) => new {Index = i, Value = x})
                .GroupBy(x => x.Index / chunkSize)
                .Select(x => new EnumerableFunArray(x.Select(v => v.Value),originInputType));
            return new EnumerableFunArray(res, FunnyType.ArrayOf(originInputType));
        }
    }
    public class FlatGenericFunctionDefinition : GenericFunctionWithSingleArgument
    {
        public FlatGenericFunctionDefinition() : base("flat", 
            FunnyType.ArrayOf(FunnyType.Generic(0)),
            FunnyType.ArrayOf(FunnyType.ArrayOf(FunnyType.Generic(0))))
        {
        }

        protected override object Calc(object a)
        {
            var arr = (IFunArray)a;
            var originInputType = arr.ElementType.ArrayTypeSpecification.FunnyType;

            return new EnumerableFunArray(arr.SelectMany(o => (IFunArray) o),originInputType);
        }
    }

    public class FoldGenericFunctionDefinition : GenericFunctionWithTwoArguments
    {
        public FoldGenericFunctionDefinition() : base("fold", new[] {GenericConstrains.Any},
            FunnyType.Generic(0),
            FunnyType.ArrayOf(FunnyType.Generic(0)),
            FunnyType.Fun(FunnyType.Generic(0), FunnyType.Generic(0), FunnyType.Generic(0)))
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

    public class FoldWithDefaultsGenericFunctionDefinition : GenericFunctionBase
    {
        public FoldWithDefaultsGenericFunctionDefinition() : base("fold", 
            returnType: FunnyType.Generic(1),
            FunnyType.ArrayOf(FunnyType.Generic(0)),
            FunnyType.Generic(1),
            FunnyType.Fun(
                    returnType: FunnyType.Generic(1), FunnyType.Generic(1), FunnyType.Generic(0)))
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
    public class UniteGenericFunctionDefinition : GenericFunctionWithTwoArguments
    {
        public UniteGenericFunctionDefinition() : base("unite", 
            FunnyType.ArrayOf(FunnyType.Generic(0)),
            FunnyType.ArrayOf(FunnyType.Generic(0)),
            FunnyType.ArrayOf(FunnyType.Generic(0)))
        {
        }

        protected override object Calc(object a, object b)
        {
            var arr1 = (IFunArray)a;
            var arr2 = (IFunArray)b;
            return new EnumerableFunArray(arr1.Union(arr2),arr1.ElementType);
        }
    }
    public class UniqueGenericFunctionDefinition : GenericFunctionWithTwoArguments
    {
        public UniqueGenericFunctionDefinition() : base("unique", 
            FunnyType.ArrayOf(FunnyType.Generic(0)),
            FunnyType.ArrayOf(FunnyType.Generic(0)),
            FunnyType.ArrayOf(FunnyType.Generic(0)))
        {
        }

        protected override object Calc(object a, object b)
        {
            var arr1 = (IFunArray)a;
            var arr2 = (IFunArray)b;
            return new EnumerableFunArray(arr1.Except(arr2).Concat(arr2.Except(arr1)),arr1.ElementType);
        }
    }
    public class IntersectGenericFunctionDefinition : GenericFunctionWithTwoArguments
    {
        public IntersectGenericFunctionDefinition() : base("intersect", 
            FunnyType.ArrayOf(FunnyType.Generic(0)),
            FunnyType.ArrayOf(FunnyType.Generic(0)),
            FunnyType.ArrayOf(FunnyType.Generic(0)))
        {
        }

        protected override object Calc(object a, object b)
        {
            var arr1 = (IFunArray)a;
            var arr2 = (IFunArray)b;
            return new EnumerableFunArray(arr1.Intersect(arr2),arr1.ElementType);
        }
    }
    public class ConcatArraysGenericFunctionDefinition : GenericFunctionWithTwoArguments
    {
        public ConcatArraysGenericFunctionDefinition() : base("concat", 
            FunnyType.ArrayOf(FunnyType.Generic(0)),
            FunnyType.ArrayOf(FunnyType.Generic(0)),
            FunnyType.ArrayOf(FunnyType.Generic(0)))
        {
        }

        protected override object Calc(object a, object b)
        {
            var arr1 = (IFunArray)a;
            var arr2 = (IFunArray)b;
            var res = new EnumerableFunArray(arr1.Concat(arr2),arr1.ElementType);
            return res;
        }
    }

    public class AppendGenericFunctionDefinition : GenericFunctionWithTwoArguments
    {
        public AppendGenericFunctionDefinition() : base("append",
            FunnyType.ArrayOf(FunnyType.Generic(0)),
            FunnyType.ArrayOf(FunnyType.Generic(0)),
            FunnyType.Generic(0))
        {
        }

        protected override object Calc(object a, object b)
        {
            var arr1 = (IFunArray)a;
            var arr2 = b;
            var res = new EnumerableFunArray(arr1.Append(arr2),arr1.ElementType);
            return res;
        }
    }

    public class SubstractArraysGenericFunctionDefinition : GenericFunctionWithTwoArguments
    {
        public SubstractArraysGenericFunctionDefinition() : base("except", 
            FunnyType.ArrayOf(FunnyType.Generic(0)),
            FunnyType.ArrayOf(FunnyType.Generic(0)),
            FunnyType.ArrayOf(FunnyType.Generic(0)))
        {
        }

        protected override object Calc(object a, object b)
        {
            var arr1 = (IFunArray)a;
            var arr2 = (IFunArray)b;
            return new EnumerableFunArray(arr1.Except(arr2),arr1.ElementType);
        }
    }

    public class CountOfGenericFunctionDefinition : GenericFunctionWithTwoArguments
    {
        public CountOfGenericFunctionDefinition() : base("count",
            FunnyType.Int32,
            FunnyType.ArrayOf(FunnyType.Generic(0)),
            FunnyType.Fun(FunnyType.Bool, FunnyType.Generic(0)))
        {
        }

        protected override object Calc(object a, object b)
        {
            var arr = (IFunArray)a;
            var filter = (IConcreteFunction) b;

            return arr.Count(arg => (bool)filter.Calc(new[] { arg }));
        }
    }

    public class HasAnyGenericFunctionDefinition : GenericFunctionWithSingleArgument
    {
        public HasAnyGenericFunctionDefinition() : base("any",
            FunnyType.Bool,
            FunnyType.ArrayOf(FunnyType.Generic(0)))
        {
        }

        protected override object Calc(object a) 
            => ((IFunArray)a).Count>0;
    }
    public class AnyGenericFunctionDefinition : GenericFunctionWithTwoArguments
    {
        public AnyGenericFunctionDefinition() : base("any", 
            FunnyType.Bool,
            FunnyType.ArrayOf(FunnyType.Generic(0)),
            FunnyType.Fun(FunnyType.Bool, FunnyType.Generic(0)))
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
    public class AllGenericFunctionDefinition : GenericFunctionWithTwoArguments
    {
        public AllGenericFunctionDefinition() : base("all", 
            FunnyType.Bool,
            FunnyType.ArrayOf(FunnyType.Generic(0)),
            FunnyType.Fun(FunnyType.Bool, FunnyType.Generic(0)))
        {
        }

        protected override object Calc(object a, object b)
        {
            var arr = (IFunArray)a;
            var filter = (IConcreteFunction) b;

            return arr.All(e => (bool) filter.Calc(new[] {e}));
        }
    }
    public class FilterGenericFunctionDefinition : GenericFunctionWithTwoArguments
    {
        public FilterGenericFunctionDefinition() : base("filter", 
            FunnyType.ArrayOf(FunnyType.Generic(0)),
            FunnyType.ArrayOf(FunnyType.Generic(0)),
            FunnyType.Fun(FunnyType.Bool, FunnyType.Generic(0)))
        {
        }

        protected override object Calc(object a, object b)
        {
            var arr    = (IFunArray)a;
            if(b is FunctionWithSingleArg predicate)
                return new EnumerableFunArray(arr.Where(e=>(bool)predicate.Calc(e)),arr.ElementType);
            var filter = (IConcreteFunction)b;
            
            return new EnumerableFunArray(arr.Where(e=>(bool)filter.Calc(new []{e})),arr.ElementType);
        }
    }
    public class RepeatGenericFunctionDefinition : GenericFunctionBase
    {
        public RepeatGenericFunctionDefinition() : base("repeat",
            FunnyType.ArrayOf(FunnyType.Generic(0)), 
            FunnyType.Generic(0), 
            FunnyType.Int32)
        {
        }

        public override IConcreteFunction CreateConcrete(FunnyType[] concreteTypesMap)
        {
            var res = new ConcreteRepeat
            {
                Name = Name,
                ArgTypes = new[] {concreteTypesMap[0], FunnyType.Int32},
                ReturnType = FunnyType.ArrayOf(concreteTypesMap[0])
            };
            return res;
        }

        class ConcreteRepeat: FunctionWithTwoArgs
        {
            public override object Calc(object a, object b) 
                => new EnumerableFunArray(Enumerable.Repeat(a, (int)b), this.ArgTypes[0]);
        }
    }
    public class ReverseGenericFunctionDefinition: GenericFunctionWithSingleArgument
    {
        public ReverseGenericFunctionDefinition() : base("reverse", 
            FunnyType.ArrayOf(FunnyType.Generic(0)), 
            FunnyType.ArrayOf(FunnyType.Generic(0)))
        {
        }

        protected override object Calc(object a)
        {
            var arr  = (IFunArray) a;
            return new EnumerableFunArray(arr.Reverse(),arr.ElementType);
        }
    }
    public class TakeGenericFunctionDefinition: GenericFunctionWithTwoArguments
    {
        public TakeGenericFunctionDefinition() : base("take", 
            FunnyType.ArrayOf(FunnyType.Generic(0)), 
            FunnyType.ArrayOf(FunnyType.Generic(0)), 
            FunnyType.Int32)
        {
        }

        protected override object Calc(object a, object b) 
            => ((IFunArray)a).Slice(null,((int)b)-1,1);
    }
    public class SkipGenericFunctionDefinition: GenericFunctionWithTwoArguments
    {
        public SkipGenericFunctionDefinition() : base("skip", 
            FunnyType.ArrayOf(FunnyType.Generic(0)), 
            FunnyType.ArrayOf(FunnyType.Generic(0)), 
            FunnyType.Int32)
        {
        }

        protected override object Calc(object a, object b) 
            => ((IFunArray)a).Slice(((int)b),null,1);
    }
}