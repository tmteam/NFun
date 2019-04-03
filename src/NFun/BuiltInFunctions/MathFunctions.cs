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
        public AddRealFunction(string name) : base(name, VarType.Real,VarType.Real,VarType.Real){}

        public AddRealFunction() : this(CoreFunNames.Add){}

        public override object Calc(object[] args) => Convert.ToDouble(args[0]) + Convert.ToDouble(args[1]);
    }
    public class AddIntFunction: FunctionBase
    {
        public AddIntFunction(string name) : base(name, VarType.Int, VarType.Int, VarType.Int)
        {
        }

        public AddIntFunction() : this(CoreFunNames.Add){}

        public override object Calc(object[] args) => (int)args[0] + (int)args[1];
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
    public class ToIntFromBytesFunction : FunctionBase
    {
        public ToIntFromBytesFunction() : base("to_int", VarType.Int, VarType.ArrayOf(VarType.Int)){}
        public override object Calc(object[] args)
        {
            var val = ((FunArray) args[0]);
            if(val.Count>4)
                throw new FunRuntimeException("Array is too long");
            byte[] arr;
            if (val.Count == 4)
                arr = val.Select(Convert.ToByte).ToArray();
            else
                arr = val.Concat(new int[4 - val.Count].Cast<object>()).Select(Convert.ToByte).ToArray();
            return BitConverter.ToInt32(arr, 0);
        }
    }
    public class ToIntFromRealFunction : FunctionBase
    {
        public ToIntFromRealFunction() : base("to_int", VarType.Int, VarType.Real){}
        public override object Calc(object[] args) => Convert.ToInt32(args[0]);
    }
    public class ToUtf8Function : FunctionBase
    {
        public ToUtf8Function() : base("to_utf8", VarType.ArrayOf(VarType.Int), VarType.Text){}
        public override object Calc(object[] args) => FunArray.By(
            Encoding.UTF8.GetBytes((string)args[0]).Select(c=> (object)Convert.ToInt32(c)));
    }
    public class ToUnicodeFunction : FunctionBase
    {
        public ToUnicodeFunction() : base("to_unicode", VarType.ArrayOf(VarType.Int), VarType.Text){}
        public override object Calc(object[] args) => FunArray.By(
            Encoding.Unicode.GetBytes((string)args[0]).Select(c=> (object)Convert.ToInt32(c)));
    }
    public class ToBitsFromIntFunction : FunctionBase
    {
        public ToBitsFromIntFunction() : base("to_bits", VarType.ArrayOf(VarType.Bool), VarType.Int){}
        public override object Calc(object[] args) => FunArray.By(
            new BitArray(BitConverter.GetBytes((int)args[0])).Cast<bool>().Cast<object>());
    }
    public class ToBytesFromIntFunction : FunctionBase
    {
        public ToBytesFromIntFunction() 
            : base("to_bytes", VarType.ArrayOf(VarType.Int), VarType.Int){}
        public override object Calc(object[] args) => FunArray.By(
            BitConverter.GetBytes((int)args[0]).Select(c=> (object)Convert.ToInt32(c)));
    }
    public class ToRealFromTextFunction : FunctionBase
    {
        public ToRealFromTextFunction() : base("to_real", VarType.Real, VarType.Text){}
        public override object Calc(object[] args) => Double.Parse((string)args[0]);
    }
    public class ToRealFromIntFunction : FunctionBase
    {
        public ToRealFromIntFunction() : base("to_real", VarType.Real, VarType.Int){}
        public override object Calc(object[] args) => Convert.ToDouble((int) args[0]);
    }
    public class ToTextFunction : FunctionBase
    {
        public ToTextFunction() : base("to_text", VarType.Text, VarType.Anything){}
        public override object Calc(object[] args) => ToText(args[0]);

        string ToText(object val)
        {
            if (val is FunArray f)
                return $"[{string.Join(",", f.Select(ToText))}]";
            else
                return val.ToString();
        }
    }
    public class ToIntFromTextFunction : FunctionBase
    {
        public ToIntFromTextFunction() : base("to_int", VarType.Int, VarType.Text){}
        public override object Calc(object[] args) => int.Parse((string)args[0]);
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
    public class TanFunction : FunctionBase {
        public TanFunction() : base("tan", VarType.Real, VarType.Real){}
        public override object Calc(object[] args) => Math.Tan(Convert.ToDouble(args[0]));
    }
    
    public class Atan2Function : FunctionBase {
        public Atan2Function () : base("atan2", VarType.Real, VarType.Real, VarType.Real){}
        public override object Calc(object[] args) => Math.Atan2((double)args[0],(double)args[0]);
    }
    public class AtanFunction : FunctionBase {
        public AtanFunction () : base("atan", VarType.Real, VarType.Real){}
        public override object Calc(object[] args) => Math.Atan((double)args[0]);
    }
    public class AsinFunction : FunctionBase {
        public AsinFunction () : base("asin", VarType.Real, VarType.Real){}
        public override object Calc(object[] args) => Math.Asin((double)args[0]);
    }
    public class AcosFunction : FunctionBase {
        public AcosFunction () : base("acos", VarType.Real, VarType.Real){}
        public override object Calc(object[] args) => Math.Acos((double)args[0]);
    }
    public class ExpFunction : FunctionBase {
        public ExpFunction () : base("exp", VarType.Real, VarType.Real){}
        public override object Calc(object[] args) => Math.Exp((double)args[0]);
    }
    public class LogEFunction : FunctionBase {
        public LogEFunction () : base("log", VarType.Real, VarType.Real){}
        public override object Calc(object[] args) => Math.Log((double)args[0]);
    }
    
    public class LogFunction : FunctionBase {
        public LogFunction () : base("log", VarType.Real,VarType.Real, VarType.Real){}
        public override object Calc(object[] args) => Math.Log((double)args[0],(double)args[1]);
    }
    
    public class Log10Function : FunctionBase {
        public Log10Function () : base("log10", VarType.Real, VarType.Real){}
        public override object Calc(object[] args) => Math.Log10((double)args[0]);
    }
    
    public class FloorFunction : FunctionBase {
        public FloorFunction () : base("floor", VarType.Int, VarType.Real){}
        public override object Calc(object[] args) => Convert.ToInt32(Math.Floor((double)args[0]));
    }
    
    public class CeilFunction : FunctionBase {
        public CeilFunction () : base("ceil", VarType.Int, VarType.Real){}
        public override object Calc(object[] args) => Convert.ToInt32(Math.Ceiling((double)args[0]));
    }
    public class RoundToIntFunction: FunctionBase {
        public RoundToIntFunction() : base("round", VarType.Int,VarType.Real){}
        public override object Calc(object[] args) => (int)Math.Round(Convert.ToDouble(args[0]));
    }
    public class RoundToRealFunction: FunctionBase {
        public RoundToRealFunction() : base("round", VarType.Real,VarType.Real,VarType.Int){}
        public override object Calc(object[] args) => Math.Round((double)args[0],(int)args[1]);
    }
    public class SignFunction: FunctionBase {
        public SignFunction() : base("sign", VarType.Int,VarType.Real){}
        public override object Calc(object[] args) =>  Math.Sign((double)args[0]);
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
        public MaxOfIntFunction() : base("max", VarType.Int, VarType.Int, VarType.Int) { }
        public override object Calc(object[] args) => Math.Max((int) args[0], (int) args[1]);
    }
    public class MaxOfRealFunction : FunctionBase {
        public MaxOfRealFunction () : base("max", VarType.Real, VarType.Real, VarType.Real){}
        public override object Calc(object[] args) => Math.Max((double) args[0], (double) args[1]);
    }
    
    public class MinOfIntFunction : FunctionBase {
        public MinOfIntFunction() : base("min", VarType.Int, VarType.Int, VarType.Int){}
        public override object Calc(object[] args) => Math.Min((int) args[0], (int) args[1]);
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