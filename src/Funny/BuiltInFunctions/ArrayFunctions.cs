using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Funny.Interpritation.Functions;
using Funny.Runtime;
using Funny.Types;

namespace Funny.BuiltInFunctions
{
    public class RangeWithStepIntFunction: FunctionBase{
        public const string Id = "range";

        public RangeWithStepIntFunction() : base(Id,
                VarType.ArrayOf(VarType.Int), 
                VarType.Int,VarType.Int, VarType.Int)
        {
            
        }

        public override object Calc(object[] args)
        {
            var start = (int) args[0];
            var end = (int) args[1];
            var step = (int) args[2];
            if(step<=0)
                throw  new FunRuntimeException("Step has to be positive");

            var result = new List<int>();
            if(start<end)
                for (int i = start; i <= end; i+= step)
                    result.Add(i);
            else
                for (int i = start; i >= end; i-= step)
                    result.Add(i);

            return result;
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
            var start = (double) args[0];
            var end = (double) args[1];
            var step = (double) args[2];
            if(step<=0)
                throw  new FunRuntimeException("Step has to be positive");
            var result = new List<double>();
            if(start<end)
                for (var i = start; i <= end; i+= step)
                    result.Add(i);
            else 
                for (var i = start; i >= end; i-= step)
                    result.Add(i);

            return result;
        }
            
    }

    public class RangeIntFunction: FunctionBase{
        public RangeIntFunction() : base(Id,VarType.ArrayOf(VarType.Int), VarType.Int, VarType.Int)
        {
            
        }

        public const string Id = "range";
        public override object Calc(object[] args)
        {
            var start = (int) args[0];
            var end = (int) args[1];
            var result = new List<int>();

            if (start < end)
                for (int i = start; i <= end; i += 1)
                    result.Add(i);
            else
                for (int i = start; i >= end; i -= 1)
                    result.Add(i);
            return result;
        }
    }
    
    public class MedianRealFunction: FunctionBase{
        public MedianRealFunction() : base("median",VarType.Real, VarType.ArrayOf(VarType.Real))
        {
            
        }
        
        public override object Calc(object[] args) 
            => GetMedian((args[0] as IEnumerable).Cast<double>());
        
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
        public MedianIntFunction() : base("median",VarType.Int, VarType.ArrayOf(VarType.Int))
        {
            
        }

        public override object Calc(object[] args) 
            => GetMedian((args[0] as IEnumerable).Cast<int>());
        
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
            => (args[0] as IEnumerable).Cast<double>().Min();
    }
    public class MultiMinIntFunction: FunctionBase{
        public MultiMinIntFunction() : base("min",VarType.Int, VarType.ArrayOf(VarType.Int))
        {
            
        }

        public override object Calc(object[] args) 
            => (args[0] as IEnumerable).Cast<int>().Min();
    }
    public class MultiSumRealFunction: FunctionBase{
        public MultiSumRealFunction() : base("sum",VarType.Real, VarType.ArrayOf(VarType.Real))
        {
            
        }
        public override object Calc(object[] args) 
            => (args[0] as IEnumerable).Cast<double>().Sum();
    }
    public class MultiSumIntFunction: FunctionBase{
        public MultiSumIntFunction() : base("sum",VarType.Int, VarType.ArrayOf(VarType.Int))
        {
            
        }

        public override object Calc(object[] args) 
            => (args[0] as IEnumerable).Cast<int>().Sum();
    }
    public class MultiMaxRealFunction: FunctionBase{
        public MultiMaxRealFunction() : base("max",VarType.Real, VarType.ArrayOf(VarType.Real))
        {
            
        }

        public override object Calc(object[] args) 
            => (args[0] as IEnumerable).Cast<double>().Max();
    }
    public class MultiMaxIntFunction: FunctionBase{
        public MultiMaxIntFunction() : base("max",VarType.Int, VarType.ArrayOf(VarType.Int))
        {
            
        }

        public override object Calc(object[] args) 
            => (args[0] as IEnumerable).Cast<int>().Max();
    }

    public class SortIntFunction : FunctionBase
    {
        public SortIntFunction() : base("sort", VarType.ArrayOf(VarType.Int), VarType.ArrayOf(VarType.Int)){}

        public override object Calc(object[] args)
        {
            var arr = (args[0] as IEnumerable).Cast<int>().ToArray();
            Array.Sort(arr);
            return arr;
        }
    }
    
    public class SortTextFunction : FunctionBase
    {
        public SortTextFunction() : base("sort", VarType.ArrayOf(VarType.Text), VarType.ArrayOf(VarType.Text)){}

        public override object Calc(object[] args)
        {
            var arr = (args[0] as IEnumerable).Cast<string>().ToArray();
            Array.Sort(arr, StringComparer.InvariantCulture);
            return arr;
        }
    }
    
    public class SortRealFunction : FunctionBase
    {
        public SortRealFunction() : base("sort", VarType.ArrayOf(VarType.Real), VarType.ArrayOf(VarType.Real)){}

        public override object Calc(object[] args)
        {
            var arr = (args[0] as IEnumerable).Cast<object>().ToArray();
            Array.Sort(arr);
            return arr;
        }
    }

    public class AverageFunction : FunctionBase
    {
        public AverageFunction(): base("avg", VarType.Real, VarType.ArrayOf(VarType.Real)){}
        public override object Calc(object[] args) => (args[0] as IEnumerable).Cast<double>().Average();
    }
    
    public class AnyFunction : FunctionBase
    {
        public AnyFunction(): base("any", VarType.Bool, VarType.ArrayOf(VarType.Anything))
        {
            
        }

        public override object Calc(object[] args) 
            => (args[0] as IEnumerable).Cast<object>().Any();
    }
    
    public class CountFunction : FunctionBase
    {
        public CountFunction(): base("count", VarType.Int, VarType.ArrayOf(VarType.Anything))
        {
            
        }

        public override object Calc(object[] args) 
            => (args[0] as IEnumerable).Cast<object>().Count();
    }
    
    
}