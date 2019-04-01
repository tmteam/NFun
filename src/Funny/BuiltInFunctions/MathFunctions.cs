using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Funny.Interpritation;
using Funny.Interpritation.Functions;
using Funny.Runtime;
using Funny.Types;

namespace Funny.BuiltInFunctions
{
    public class InvertFunction : FunctionBase
    {
        public InvertFunction() : base(CoreFunNames.Not, VarType.Bool,VarType.Bool){}
        public override object Calc(object[] args) => !(bool)args[0];
    }
    public class AndFunction: FunctionBase
    {
        public AndFunction() : base(CoreFunNames.And, VarType.Bool,VarType.Bool,VarType.Bool){}
        public override object Calc(object[] args) => (bool)args[0] && (bool)args[1];
    }
    public class OrFunction: FunctionBase
    {
        public OrFunction() : base(CoreFunNames.Or, VarType.Bool,VarType.Bool,VarType.Bool){}
        public override object Calc(object[] args) => (bool)args[0] || (bool)args[1];
    }
    public class XorFunction: FunctionBase
    {
        public XorFunction() : base(CoreFunNames.Xor, VarType.Bool,VarType.Bool,VarType.Bool){}
        public override object Calc(object[] args) =>  (bool)args[0] != (bool)args[1];
    }
   
    public class MoreIntFunction: FunctionBase
    {
        public MoreIntFunction() : base(CoreFunNames.More, VarType.Bool,VarType.Int,VarType.Int){}
        public override object Calc(object[] args) => (int)args[0] > (int)args[1];
    }
    public class MoreOrEqualIntFunction: FunctionBase
    {
        public MoreOrEqualIntFunction() : base(CoreFunNames.MoreOrEqual, VarType.Bool,VarType.Int,VarType.Int){}
        public override object Calc(object[] args) => (int)args[0] >= (int)args[1];
    }
    public class LessIntFunction: FunctionBase
    {
        public LessIntFunction() : base(CoreFunNames.Less, VarType.Bool,VarType.Int,VarType.Int){}
        public override object Calc(object[] args) => (int)args[0] < (int)args[1];
    }
    public class LessOrEqualIntFunction: FunctionBase
    {
        public LessOrEqualIntFunction() : base(CoreFunNames.LessOrEqual, VarType.Bool,VarType.Int,VarType.Int){}
        public override object Calc(object[] args) => (int)args[0] <= (int)args[1];
    }
    public class MoreRealFunction: FunctionBase
    {
        public MoreRealFunction() : base(CoreFunNames.More, VarType.Bool,VarType.Real,VarType.Real){}
        public override object Calc(object[] args) => (double)args[0] > (double)args[1];
    }
    public class MoreOrEqualRealFunction: FunctionBase
    {
        public MoreOrEqualRealFunction() : base(CoreFunNames.MoreOrEqual, VarType.Bool,VarType.Real,VarType.Real){}
        public override object Calc(object[] args) => (double)args[0] >= (double)args[1];
    }
    public class LessRealFunction: FunctionBase
    {
        public LessRealFunction() : base(CoreFunNames.Less, VarType.Bool,VarType.Real,VarType.Real){}
        public override object Calc(object[] args) => (double)args[0] < (double)args[1];
    }
    public class LessOrEqualRealFunction: FunctionBase
    {
        public LessOrEqualRealFunction() : base(CoreFunNames.LessOrEqual, VarType.Bool,VarType.Real,VarType.Real){}
        public override object Calc(object[] args) => (double)args[0] <= (double)args[1];
    }

    public class EqualFunction: FunctionBase
    {
        public EqualFunction() : base(CoreFunNames.Equal, VarType.Bool,VarType.Anything,VarType.Anything){}
        public override object Calc(object[] args) => TypeHelper.AreEqual(args[0],args[1]);
    }
    public class NotEqualFunction: FunctionBase
    {
        public NotEqualFunction() : base(CoreFunNames.NotEqual, VarType.Bool,VarType.Anything,VarType.Anything){}
        public override object Calc(object[] args) => !TypeHelper.AreEqual(args[0],args[1]);
    }

    public class RemainderRealFunction: FunctionBase
    {
        public RemainderRealFunction() : base(CoreFunNames.Remainder, VarType.Real,VarType.Real,VarType.Real){}
        public override object Calc(object[] args) => (double)args[0] % (double)args[1];
    }
    
    public class RemainderIntFunction: FunctionBase
    {
        public RemainderIntFunction() : base(CoreFunNames.Remainder, VarType.Int,VarType.Int,VarType.Int){}
        public override object Calc(object[] args) => (int)args[0] % (int)args[1];
    }
    
        
    public class PowRealFunction: FunctionBase
    {
        public PowRealFunction() : base(CoreFunNames.Pow, VarType.Real,VarType.Real,VarType.Real){}
        public override object Calc(object[] args) 
            => Math.Pow(Convert.ToDouble(args[0]), Convert.ToDouble(args[1]));
    }
    
    public class DivideRealFunction: FunctionBase
    {
        public DivideRealFunction() : base(CoreFunNames.Divide, VarType.Real,VarType.Real,VarType.Real){}
        public override object Calc(object[] args) => Convert.ToDouble(args[0]) / Convert.ToDouble(args[1]);
    }
    
    public class MultiplyRealFunction: FunctionBase
    {
        public MultiplyRealFunction() : base(CoreFunNames.Multiply, VarType.Real,VarType.Real,VarType.Real){}
        public override object Calc(object[] args) => Convert.ToDouble(args[0]) * Convert.ToDouble(args[1]);
    }
    public class MultiplyIntFunction: FunctionBase
    {
        public MultiplyIntFunction() : base(CoreFunNames.Multiply, VarType.Int,VarType.Int,VarType.Int){}
        public override object Calc(object[] args) => (int)args[0] * (int)args[1];
    }
    public class SubstractRealFunction: FunctionBase
    {
        public SubstractRealFunction() : base(CoreFunNames.Substract, VarType.Real,VarType.Real,VarType.Real){}
        public override object Calc(object[] args) => Convert.ToDouble(args[0]) - Convert.ToDouble(args[1]);
    }
    public class SubstractIntFunction: FunctionBase
    {
        public SubstractIntFunction() : base(CoreFunNames.Substract, VarType.Int,VarType.Int,VarType.Int){}
        public override object Calc(object[] args) => (int)args[0] - (int)args[1];
    }
   
    
    public class AddRealFunction: FunctionBase
    {
        public AddRealFunction() : base(CoreFunNames.Add, VarType.Real,VarType.Real,VarType.Real){}
        public override object Calc(object[] args) => Convert.ToDouble(args[0]) + Convert.ToDouble(args[1]);
    }
    public class AddIntFunction: FunctionBase
    {
        public AddIntFunction() : base(CoreFunNames.Add, VarType.Int,VarType.Int,VarType.Int){}
        public override object Calc(object[] args) => (int)args[0] + (int)args[1];
    }
    public class AddTextFunction: FunctionBase
    {
        public AddTextFunction() : base(CoreFunNames.Add, 
            VarType.Text,VarType.Text,VarType.Anything){}
        public override object Calc(object[] args) => args[0].ToString() + ToStringSmart(args[1]);

        private static string ToStringSmart(object o)
        {
            if (o is IEnumerable en && !(o is string))
                return '['+string.Join(',', en.Cast<object>().Select(ToStringSmart)) + ']';
            return o.ToString();
        }
        
    }            
        
    public class AbsOfRealFunction : FunctionBase
    {
        public AbsOfRealFunction() : base("abs", VarType.Real,VarType.Real){}
        public override object Calc(object[] args)
        {
            var val = Convert.ToDouble(args[0]);
            return val > 0 ? val : -val;
        }
    }
    public class AbsOfIntFunction : FunctionBase
    {
        public AbsOfIntFunction() : base("abs", VarType.Int,VarType.Int){}
        public override object Calc(object[] args)
        {
            var val = (int)(args[0]);
            return val > 0 ? val : -val;
        }
    }
        
    public class CosFunction : FunctionBase
    {
        public CosFunction() : base("cos", VarType.Real, VarType.Real){}

        public override object Calc(object[] args) => Math.Cos(Convert.ToDouble(args[0]));
    }
    public class SinFunction : FunctionBase
    {
        public SinFunction() : base("sin", VarType.Real,VarType.Real){}

        public override object Calc(object[] args) => Math.Sin(Convert.ToDouble(args[0]));
    }
    public class TanFunction : FunctionBase
    {
        public TanFunction() : base("tan", VarType.Real, VarType.Real){}

        public override object Calc(object[] args) => Math.Tan(Convert.ToDouble(args[0]));
    }
    
    public class PiFunction : FunctionBase
    {
        public PiFunction() : base("pi", VarType.Real){}

        public override object Calc(object[] args) => Math.PI;
    }
    public class EFunction : FunctionBase
    {
        public EFunction() : base("e",  VarType.Real){}
        public override object Calc(object[] args) => Math.E;
    }

    public class MaxOfIntFunction : FunctionBase
    {
        public MaxOfIntFunction() : base("max", VarType.Int, VarType.Int, VarType.Int)
        {
        }

        public override object Calc(object[] args) 
            => Math.Max((int) args[0], (int) args[1]);
    }
    public class MaxOfRealFunction : FunctionBase
    {
        public MaxOfRealFunction () : base("max", VarType.Real, VarType.Real, VarType.Real)
        {
        }

        public override object Calc(object[] args) 
            => Math.Max((double) args[0], (double) args[1]);
    }
    
    public class MinOfIntFunction : FunctionBase
    {
        public MinOfIntFunction() : base("min", VarType.Int, VarType.Int, VarType.Int)
        {
        }

        public override object Calc(object[] args) 
            => Math.Min((int) args[0], (int) args[1]);
    }
    public class MinOfRealFunction : FunctionBase
    {
        public MinOfRealFunction () : base("min", VarType.Real, VarType.Real, VarType.Real)
        {
        }

        public override object Calc(object[] args) 
            => Math.Min((double) args[0], (double) args[1]);
    }

  
    
}