using System;
using System.Collections.Generic;
using System.Linq;
using NFun.Interpritation.Functions;
using NFun.Runtime;
using NFun.Runtime.Arrays;
using NFun.Types;

namespace NFun.BuiltInFunctions
{
    public class RangeWithStepIntFunction: FunctionBase{
        public const string Id = "range";

        public RangeWithStepIntFunction() : base(Id,
                VarType.ArrayOf(VarType.Int32), 
                VarType.Int32,VarType.Int32, VarType.Int32)
        {
            
        }
        public override object Calc(object[] args)
        {
            var start = args.Get<int>(0);
            var end = args.Get<int>(1);
            var step = args.Get<int>(2);
            if(step<=0)
                throw  new FunRuntimeException("Step has to be positive");

            var result = new List<int>();
            if(start<end)
                for (int i = start; i <= end; i+= step)
                    result.Add(i);
            else
                for (int i = start; i >= end; i-= step)
                    result.Add(i);

            return new ImmutableFunArray(result.ToArray());
        }
            
    }
    public class RangeWithStepRealFunction: FunctionBase{
        public const string Id = "range";

        public RangeWithStepRealFunction() : base(Id,
            VarType.ArrayOf(VarType.Real), 
            VarType.Real,VarType.Real, VarType.Real)
        {
            
        }

        public override object Calc(object[] args)
        {
            var start = args.Get<double>(0);
            var end = args.Get<double>(1);
            var step = args.Get<double>(2);
            if(step<=0)
                throw  new FunRuntimeException("Step has to be positive");
            var result = new List<double>();
            if(start<end)
                for (var i = start; i <= end; i+= step)
                    result.Add(i);
            else 
                for (var i = start; i >= end; i-= step)
                    result.Add(i);

            return new ImmutableFunArray(result.ToArray());
        }
            
    }

    public class RangeIntFunction: FunctionBase{
        public RangeIntFunction() : base(Id,VarType.ArrayOf(VarType.Int32), VarType.Int32, VarType.Int32)
        {
            
        }

        public const string Id = "range";
        public override object Calc(object[] args)
        {
            var start = args.Get<int>(0);
            var end = args.Get<int>(1);
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
    
    public class MedianRealFunction: FunctionBase{
        public MedianRealFunction() : base("median",VarType.Real, VarType.ArrayOf(VarType.Real))
        {
            
        }
        
        public override object Calc(object[] args) 
            => GetMedian(((IFunArray)args[0]).As<double>());
        
        public static double GetMedian(IEnumerable<double> source)
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
    
    public class MedianIntFunction: FunctionBase{
        public MedianIntFunction() : base("median",VarType.Int32, VarType.ArrayOf(VarType.Int32))
        {
            
        }

        public override object Calc(object[] args) 
            => GetMedian(((IFunArray)args[0]).As<int>());
        
        public static double GetMedian(IEnumerable<int> source)
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
    public class MultiMinRealFunction: FunctionBase{
        public MultiMinRealFunction() : base("min",VarType.Real, VarType.ArrayOf(VarType.Real))
        {
            
        }

        public override object Calc(object[] args) 
            => ((IFunArray)args[0]).As<double>().Min();
    }
    public class MultiMinIntFunction: FunctionBase{
        public MultiMinIntFunction() : base("min",VarType.Int32, VarType.ArrayOf(VarType.Int32))
        {
            
        }

        public override object Calc(object[] args) 
            => ((IFunArray)args[0]).As<int>().Min();
    }
    public class MultiSumRealFunction: FunctionBase{
        public MultiSumRealFunction() : base("sum",VarType.Real, VarType.ArrayOf(VarType.Real))
        {
            
        }
        public override object Calc(object[] args) 
            => ((IFunArray)args[0]).As<double>().Sum();
    }
    public class MultiSumIntFunction: FunctionBase{
        public MultiSumIntFunction() : base("sum",VarType.Int32, VarType.ArrayOf(VarType.Int32))
        {
            
        }

        public override object Calc(object[] args) 
            => ((IFunArray)args[0]).As<int>().Sum();
    }
    public class MultiMaxRealFunction: FunctionBase{
        public MultiMaxRealFunction() : base("max",VarType.Real, VarType.ArrayOf(VarType.Real))
        {
            
        }

        public override object Calc(object[] args) 
            => ((IFunArray)args[0]).As<double>().Max();
    }
    public class MultiMaxIntFunction: FunctionBase{
        public MultiMaxIntFunction() : base("max",VarType.Int32, VarType.ArrayOf(VarType.Int32))
        {
            
        }

        public override object Calc(object[] args) 
            => ((IFunArray)args[0]).As<int>().Max();
    }

    public class SortIntFunction : FunctionBase
    {
        public SortIntFunction() : base("sort", VarType.ArrayOf(VarType.Int32), VarType.ArrayOf(VarType.Int32)){}

        public override object Calc(object[] args)
        {
            var arr = ((IFunArray)args[0]).As<int>().ToArray();
            Array.Sort(arr);
            return new ImmutableFunArray(arr);
        }
    }
    
  
    
    public class SortRealFunction : FunctionBase
    {
        public SortRealFunction() : base("sort", VarType.ArrayOf(VarType.Real), VarType.ArrayOf(VarType.Real)){}

        public override object Calc(object[] args)
        {
            var arr = ((IFunArray)args[0]).As<double>().ToArray();
            Array.Sort(arr);
            return arr;
        }
    }

    public class AverageFunction : FunctionBase
    {
        public AverageFunction(): base("avg", VarType.Real, VarType.ArrayOf(VarType.Real)){}
        public override object Calc(object[] args) => 
            ((IFunArray)args[0]).As<double>().Average();
    }
    
    public class AnyFunction : FunctionBase
    {
        public AnyFunction(): base("any", VarType.Bool, VarType.ArrayOf(VarType.Anything))
        {
            
        }

        public override object Calc(object[] args) 
            => ((IFunArray)args[0]).Count>0;
    }
    
    public class CountFunction : FunctionBase
    {
        public CountFunction(): base("count", VarType.Int32, VarType.ArrayOf(VarType.Anything))
        {
            
        }

        public override object Calc(object[] args) 
            => ((IFunArray)args[0]).Count;
    }
}