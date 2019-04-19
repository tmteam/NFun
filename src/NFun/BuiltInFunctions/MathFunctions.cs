using System;
using System.Collections;
using System.Linq;
using System.Text;
using NFun.Interpritation.Functions;
using NFun.Runtime;
using NFun.Types;

namespace NFun.BuiltInFunctions
{
    public class InvertFunction : FunctionBase
    {
        public InvertFunction() : base(CoreFunNames.Not, VarType.Bool,VarType.Bool){}
        public override object Calc(object[] args) => !args.Get<bool>(0);
    }
    public class AndFunction: FunctionBase
    {
        public AndFunction() : base(CoreFunNames.And, VarType.Bool,VarType.Bool,VarType.Bool){}
        public override object Calc(object[] args) => args.Get<bool>(0) && args.Get<bool>(1);
    }
    public class OrFunction: FunctionBase
    {
        public OrFunction() : base(CoreFunNames.Or, VarType.Bool,VarType.Bool,VarType.Bool){}
        public override object Calc(object[] args) => args.Get<bool>(0) || args.Get<bool>(1);
    }
    public class XorFunction: FunctionBase
    {
        public XorFunction() : base(CoreFunNames.Xor, VarType.Bool,VarType.Bool,VarType.Bool){}
        public override object Calc(object[] args) =>  args.Get<bool>(0) != args.Get<bool>(1);
    }
   
    public class MoreIntFunction: FunctionBase
    {
        public MoreIntFunction() : base(CoreFunNames.More, VarType.Bool,VarType.Int32,VarType.Int32){}
        public override object Calc(object[] args) => args.Get<int>(0) > args.Get<int>(1);
    }
    public class MoreOrEqualIntFunction: FunctionBase
    {
        public MoreOrEqualIntFunction() : base(CoreFunNames.MoreOrEqual, VarType.Bool,VarType.Int32,VarType.Int32){}
        public override object Calc(object[] args) => args.Get<int>(0) >= args.Get<int>(1);
    }
    public class LessIntFunction: FunctionBase
    {
        public LessIntFunction() : base(CoreFunNames.Less, VarType.Bool,VarType.Int32,VarType.Int32){}
        public override object Calc(object[] args) => args.Get<int>(0) < args.Get<int>(1);
    }
    public class LessOrEqualIntFunction: FunctionBase
    {
        public LessOrEqualIntFunction() : base(CoreFunNames.LessOrEqual, VarType.Bool,VarType.Int32,VarType.Int32){}
        public override object Calc(object[] args) => args.Get<int>(0) <= args.Get<int>(1);
    }
    public class MoreRealFunction: FunctionBase
    {
        public MoreRealFunction() : base(CoreFunNames.More, VarType.Bool,VarType.Real,VarType.Real){}
        public override object Calc(object[] args) => args.Get<double>(0) > args.Get<double>(1);
    }
    public class MoreOrEqualRealFunction: FunctionBase
    {
        public MoreOrEqualRealFunction() : base(CoreFunNames.MoreOrEqual, VarType.Bool,VarType.Real,VarType.Real){}
        public override object Calc(object[] args) => args.Get<double>(0) >= args.Get<double>(1);
    }
    public class LessRealFunction: FunctionBase
    {
        public LessRealFunction() : base(CoreFunNames.Less, VarType.Bool,VarType.Real,VarType.Real){}
        public override object Calc(object[] args) => args.Get<double>(0) < args.Get<double>(1);
    }
    public class LessOrEqualRealFunction: FunctionBase
    {
        public LessOrEqualRealFunction() : base(CoreFunNames.LessOrEqual, VarType.Bool,VarType.Real,VarType.Real){}
        public override object Calc(object[] args) => args.Get<double>(0) <= args.Get<double>(1);
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
        public override object Calc(object[] args) => args.Get<double>(0) % args.Get<double>(1);
    }
    
    public class RemainderIntFunction: FunctionBase
    {
        public RemainderIntFunction() : base(CoreFunNames.Remainder, VarType.Int32,VarType.Int32,VarType.Int32){}
        public override object Calc(object[] args) => args.Get<int>(0) % args.Get<int>(1);
    }
    
        
    public class PowRealFunction: FunctionBase
    {
        public PowRealFunction() : base(CoreFunNames.Pow, VarType.Real,VarType.Real,VarType.Real){}
        public override object Calc(object[] args) 
            => Math.Pow(args.Get<double>(0), args.Get<double>(1));
    }
    
    public class DivideRealFunction: FunctionBase
    {
        public DivideRealFunction() : base(CoreFunNames.Divide, VarType.Real,VarType.Real,VarType.Real){}
        public override object Calc(object[] args) => args.Get<double>(0) / args.Get<double>(1);
    }
    
    
    
    public class MultiplyRealFunction: FunctionBase
    {
        public MultiplyRealFunction() : base(CoreFunNames.Multiply, VarType.Real,VarType.Real,VarType.Real){}
        public override object Calc(object[] args) => args.Get<double>(0) * args.Get<double>(1);
    }
    public class MultiplyIntFunction: FunctionBase
    {
        public MultiplyIntFunction() : base(CoreFunNames.Multiply, VarType.Int32,VarType.Int32,VarType.Int32){}
        public override object Calc(object[] args) => args.Get<int>(0) * args.Get<int>(1);
    }
    public class SubstractRealFunction: FunctionBase
    {
        public SubstractRealFunction() : base(CoreFunNames.Substract, VarType.Real,VarType.Real,VarType.Real){}
        public override object Calc(object[] args) => args.Get<double>(0) - args.Get<double>(1);
    }
    public class SubstractIntFunction: FunctionBase
    {
        public SubstractIntFunction() : base(CoreFunNames.Substract, VarType.Int32,VarType.Int32,VarType.Int32){}
        public override object Calc(object[] args) => args.Get<int>(0) - args.Get<int>(1);
    }
   
    public class AddRealFunction: FunctionBase
    {
        public AddRealFunction(string name) : base(name, VarType.Real,VarType.Real,VarType.Real){}

        public AddRealFunction() : this(CoreFunNames.Add){}

        public override object Calc(object[] args) => args.Get<double>(0) + args.Get<double>(1);
    }
    public class AddIntFunction: FunctionBase
    {
        public AddIntFunction(string name) : base(name, VarType.Int32, VarType.Int32, VarType.Int32)
        {
        }

        public AddIntFunction() : this(CoreFunNames.Add){}

        public override object Calc(object[] args) => args.Get<int>(0) + args.Get<int>(1);
    }
    public class AddTextFunction: FunctionBase
    {
        public AddTextFunction(string name) : base(name, 
            VarType.Text,VarType.Text,VarType.Anything){}

        public AddTextFunction() : this(CoreFunNames.Add){}
        public override object Calc(object[] args) => args[0] + ToStringSmart(args[1]);

        private static string ToStringSmart(object o)
        {
            if (o is IEnumerable en && !(o is string))
                return '['+string.Join(",", en.Cast<object>().Select(ToStringSmart)) + ']';
            return o.ToString();
        }
        
    }            
        
    public class AbsOfRealFunction : FunctionBase
    {
        public AbsOfRealFunction() : base("abs", VarType.Real,VarType.Real){}
        public override object Calc(object[] args)
        {
            var val = args.Get<double>(0);
            return val > 0 ? val : -val;
        }
    }
    public class AbsOfIntFunction : FunctionBase
    {
        public AbsOfIntFunction() : base("abs", VarType.Int32,VarType.Int32){}
        public override object Calc(object[] args)
        {
            var val = (int)(args[0]);
            return val > 0 ? val : -val;
        }
    }

  
    public class CosFunction : FunctionBase
    {
        public CosFunction() : base("cos", VarType.Real, VarType.Real){}

        public override object Calc(object[] args) => Math.Cos(args.Get<double>(0));
    }
    public class SinFunction : FunctionBase
    {
        public SinFunction() : base("sin", VarType.Real,VarType.Real){}

        public override object Calc(object[] args) => Math.Sin(args.Get<double>(0));
    }
    public class TanFunction : FunctionBase {
        public TanFunction() : base("tan", VarType.Real, VarType.Real){}
        public override object Calc(object[] args) => Math.Tan(args.Get<double>(0));
    }
    
    public class Atan2Function : FunctionBase {
        public Atan2Function () : base("atan2", VarType.Real, VarType.Real, VarType.Real){}
        public override object Calc(object[] args) => Math.Atan2(args.Get<double>(0),args.Get<double>(0));
    }
    public class AtanFunction : FunctionBase {
        public AtanFunction () : base("atan", VarType.Real, VarType.Real){}
        public override object Calc(object[] args) => Math.Atan(args.Get<double>(0));
    }
    public class AsinFunction : FunctionBase {
        public AsinFunction () : base("asin", VarType.Real, VarType.Real){}
        public override object Calc(object[] args) => Math.Asin(args.Get<double>(0));
    }
    public class AcosFunction : FunctionBase {
        public AcosFunction () : base("acos", VarType.Real, VarType.Real){}
        public override object Calc(object[] args) => Math.Acos(args.Get<double>(0));
    }
    public class ExpFunction : FunctionBase {
        public ExpFunction () : base("exp", VarType.Real, VarType.Real){}
        public override object Calc(object[] args) => Math.Exp(args.Get<double>(0));
    }
    public class LogEFunction : FunctionBase {
        public LogEFunction () : base("log", VarType.Real, VarType.Real){}
        public override object Calc(object[] args) => Math.Log(args.Get<double>(0));
    }
    
    public class LogFunction : FunctionBase {
        public LogFunction () : base("log", VarType.Real,VarType.Real, VarType.Real){}
        public override object Calc(object[] args) => Math.Log(args.Get<double>(0),args.Get<double>(1));
    }
    
    public class Log10Function : FunctionBase {
        public Log10Function () : base("log10", VarType.Real, VarType.Real){}
        public override object Calc(object[] args) => Math.Log10(args.Get<double>(0));
    }
    
    public class FloorFunction : FunctionBase {
        public FloorFunction () : base("floor", VarType.Int32, VarType.Real){}
        public override object Calc(object[] args) => Convert.ToInt32(Math.Floor(args.Get<double>(0)));
    }
    
    public class CeilFunction : FunctionBase {
        public CeilFunction () : base("ceil", VarType.Int32, VarType.Real){}
        public override object Calc(object[] args) => Convert.ToInt32(Math.Ceiling(args.Get<double>(0)));
    }
    public class RoundToIntFunction: FunctionBase {
        public RoundToIntFunction() : base("round", VarType.Int32,VarType.Real){}
        public override object Calc(object[] args) => (int)Math.Round(args.Get<double>(0));
    }
    public class RoundToRealFunction: FunctionBase {
        public RoundToRealFunction() : base("round", VarType.Real,VarType.Real,VarType.Int32){}
        public override object Calc(object[] args) => Math.Round(args.Get<double>(0),args.Get<int>(1));
    }
    public class SignFunction: FunctionBase {
        public SignFunction() : base("sign", VarType.Int32,VarType.Real){}
        public override object Calc(object[] args) =>  Math.Sign(args.Get<double>(0));
    }
    
    public class PiFunction : FunctionBase {
        public PiFunction() : base("pi", VarType.Real){}
        public override object Calc(object[] args) => Math.PI;
    }
    public class EFunction : FunctionBase {
        public EFunction() : base("e",  VarType.Real){}
        public override object Calc(object[] args) => Math.E;
    }

    public class MaxOfIntFunction : FunctionBase {
        public MaxOfIntFunction() : base("max", VarType.Int32, VarType.Int32, VarType.Int32) { }
        public override object Calc(object[] args) => Math.Max(args.Get<int>(0), args.Get<int>(1));
    }
    public class MaxOfRealFunction : FunctionBase {
        public MaxOfRealFunction () : base("max", VarType.Real, VarType.Real, VarType.Real){}
        public override object Calc(object[] args) => Math.Max(args.Get<double>(0), args.Get<double>(1));
    }
    
    public class MinOfIntFunction : FunctionBase {
        public MinOfIntFunction() : base("min", VarType.Int32, VarType.Int32, VarType.Int32){}
        public override object Calc(object[] args) => Math.Min(args.Get<int>(0), args.Get<int>(1));
    }
    public class MinOfRealFunction : FunctionBase
    {
        public MinOfRealFunction () : base("min", VarType.Real, VarType.Real, VarType.Real)
        {
        }

        public override object Calc(object[] args) 
            => Math.Min(args.Get<double>(0), args.Get<double>(1));
    }

  
    
}