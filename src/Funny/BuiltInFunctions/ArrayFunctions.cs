using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Funny.Interpritation.Functions;
using Funny.Types;

namespace Funny.BuiltInFunctions
{
    
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
    public class AverageFunction : FunctionBase
    {
        public AverageFunction(): base("avg", VarType.Real, VarType.ArrayOf(VarType.Real)){}
        public override object Calc(object[] args) => (args[0] as IEnumerable).Cast<double>().Average();
    }
    public class CountFunction : FunctionBase
    {
        public CountFunction(): base("count", VarType.Int, VarType.ArrayOf(VarType.Any))
        {
            
        }

        public override object Calc(object[] args) 
            => (args[0] as IEnumerable).Cast<object>().Count();
    }
    
    
}