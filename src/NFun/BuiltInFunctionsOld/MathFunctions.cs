using System;
using System.Collections;
using System.Linq;
using NFun.Interpritation.Functions;
using NFun.Types;

namespace NFun.BuiltInFunctions
{
   /*
    public class XorFunction: FunctionBase
    {
        public XorFunction() : base(CoreFunNames.Xor, VarType.Bool,VarType.Bool,VarType.Bool){}
        public override object Calc(object[] args) =>  args.Get<bool>(0) != args.Get<bool>(1);
    }
   

#region remainder
    public class RemainderRealFunction: FunctionBase
    {
        public RemainderRealFunction() : base(CoreFunNames.Remainder, VarType.Real,VarType.Real,VarType.Real){}
        public override object Calc(object[] args) => args.Get<double>(0) % args.Get<double>(1);
    }
    public class RemainderInt16Function: FunctionBase
    {
        public RemainderInt16Function() : base(CoreFunNames.Remainder, VarType.Int16,VarType.Int16,VarType.Int16){}
        public override object Calc(object[] args) => (Int16)(args.Get<Int16>(0) % args.Get<Int16>(1));
    }
    public class RemainderInt32Function: FunctionBase
    {
        public RemainderInt32Function() : base(CoreFunNames.Remainder, VarType.Int32,VarType.Int32,VarType.Int32){}
        public override object Calc(object[] args) => args.Get<int>(0) % args.Get<int>(1);
    }
    
    public class RemainderInt64Function: FunctionBase
    {
        public RemainderInt64Function() : base(CoreFunNames.Remainder, VarType.Int64,VarType.Int64,VarType.Int64){}
        public override object Calc(object[] args) => (Int64)(args.Get<Int64>(0) % args.Get<Int64>(1));
    }
    
    public class RemainderUInt8Function: FunctionBase
    {
        public RemainderUInt8Function() : base(CoreFunNames.Remainder, VarType.UInt8,VarType.UInt8,VarType.UInt8){}
        public override object Calc(object[] args) => (byte)(args.Get<byte>(0) % args.Get<byte>(1));
    }
    public class RemainderUInt16Function: FunctionBase
    {
        public RemainderUInt16Function() : base(CoreFunNames.Remainder, VarType.UInt16,VarType.UInt16,VarType.UInt16){}
        public override object Calc(object[] args) => (UInt16)(args.Get<UInt16>(0) % args.Get<UInt16>(1));
    }
    public class RemainderUInt32Function: FunctionBase
    {
        public RemainderUInt32Function() : base(CoreFunNames.Remainder, VarType.UInt32,VarType.UInt32,VarType.UInt32){}
        public override object Calc(object[] args) => (uint)(args.Get<uint>(0) % args.Get<uint>(1));
    }
    
    public class RemainderUInt64Function: FunctionBase
    {
        public RemainderUInt64Function() : base(CoreFunNames.Remainder, VarType.UInt64,VarType.UInt64,VarType.UInt64){}
        public override object Calc(object[] args) => (UInt64)(args.Get<UInt64>(0) % args.Get<UInt64>(1));
    }
    #endregion
   

    #region multiply

    

    public class MultiplyRealFunction: FunctionBase
    {
        public MultiplyRealFunction() : base(CoreFunNames.Multiply, VarType.Real,VarType.Real,VarType.Real){}
        public override object Calc(object[] args) => args.Get<double>(0) * args.Get<double>(1);
    }

    public class MultiplyInt32Function: FunctionBase
    {
        public MultiplyInt32Function() : base(CoreFunNames.Multiply, VarType.Int32,VarType.Int32,VarType.Int32){}
        public override object Calc(object[] args) => args.Get<int>(0) * args.Get<int>(1);
    }
    public class MultiplyInt64Function: FunctionBase
    {
        public MultiplyInt64Function() : base(CoreFunNames.Multiply, VarType.Int64,VarType.Int64,VarType.Int64){}
        public override object Calc(object[] args) => args.Get<long>(0) * args.Get<long>(1);
    }
    
    public class MultiplyUInt8Function: FunctionBase
    {
        public MultiplyUInt8Function() : base(CoreFunNames.Multiply, VarType.UInt16,VarType.UInt8,VarType.UInt8){}
        public override object Calc(object[] args) => (ushort)(args.Get<byte>(0) * args.Get<byte>(1));
    }
    public class MultiplyUInt32Function: FunctionBase
    {
        public MultiplyUInt32Function() : base(CoreFunNames.Multiply, VarType.UInt32,VarType.UInt32,VarType.UInt32){}
        public override object Calc(object[] args) => (uint)(args.Get<uint>(0) * args.Get<uint>(1));
    }
    
    public class MultiplyUInt64Function: FunctionBase
    {
        public MultiplyUInt64Function() : base(CoreFunNames.Multiply, VarType.UInt64,VarType.UInt64,VarType.UInt64){}
        public override object Calc(object[] args) => (ulong)(args.Get<ulong>(0) * args.Get<ulong>(1));
    }
    
    #endregion

    
   
    public class AddTextFunction: FunctionBase
    {
        public AddTextFunction(string name) : base(name, 
            VarType.Text,VarType.Text,VarType.Anything){}

        public override object Calc(object[] args) => args.Get<object>(0) + ToStringSmart(args.Get<object>(1));

        private static string ToStringSmart(object o)
        {
            if (o is IEnumerable en && !(o is string))
                return '['+string.Join(",", en.Cast<object>().Select(ToStringSmart)) + ']';
            return o.ToString();
        }
        
    }

    public class NegateOfInt32Function : FunctionBase
    {
        public NegateOfInt32Function()
            : base(CoreFunNames.Negate, VarType.Int32, VarType.Int32){}
        public override object Calc(object[] args) => - args.Get<int>(0);
    }
    public class NegateOfUInt32Function : FunctionBase
    {
        public NegateOfUInt32Function()
            : base(CoreFunNames.Negate, VarType.Int64, VarType.UInt32){}
        public override object Calc(object[] args) => - args.Get<uint>(0);
    }
    public class NegateOfInt16Function : FunctionBase
    {
        public NegateOfInt16Function()
            : base(CoreFunNames.Negate, VarType.Int16, VarType.Int16){}
        public override object Calc(object[] args) => (short)(- args.Get<short>(0));
    }
    public class NegateOfUInt8Function : FunctionBase
    {
        public NegateOfUInt8Function()
            : base(CoreFunNames.Negate, VarType.Int16, VarType.UInt8){}
        public override object Calc(object[] args) => (short)(- args.Get<byte>(0));
    }
    public class NegateOfInt64Function : FunctionBase
    {
        public NegateOfInt64Function()
            : base(CoreFunNames.Negate, VarType.Int64, VarType.Int64){}
        public override object Calc(object[] args) => - args.Get<long>(0);
    }
    
    
    public class NegateOfRealFunction : FunctionBase
    {
        public NegateOfRealFunction()
            : base(CoreFunNames.Negate, VarType.Real, VarType.Real){}
        public override object Calc(object[] args) => - args.Get<double>(0);
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
    public class MaxOfInt64Function : FunctionBase {
        public MaxOfInt64Function() : base("max", VarType.Int64, VarType.Int64, VarType.Int64) { }
        public override object Calc(object[] args) => Math.Max(args.Get<long>(0), args.Get<long>(1));
    }
    public class MaxOfRealFunction : FunctionBase {
        public MaxOfRealFunction () : base("max", VarType.Real, VarType.Real, VarType.Real){}
        public override object Calc(object[] args) => Math.Max(args.Get<double>(0), args.Get<double>(1));
    }
    
    public class MinOfIntFunction : FunctionBase {
        public MinOfIntFunction() : base("min", VarType.Int32, VarType.Int32, VarType.Int32){}
        public override object Calc(object[] args) => Math.Min(args.Get<int>(0), args.Get<int>(1));
    }
    public class MinOfInt64Function : FunctionBase {
        public MinOfInt64Function() : base("min", VarType.Int64, VarType.Int64, VarType.Int64){}
        public override object Calc(object[] args) => Math.Min(args.Get<long>(0), args.Get<long>(1));
    }
    public class MinOfRealFunction : FunctionBase
    {
        public MinOfRealFunction () : base("min", VarType.Real, VarType.Real, VarType.Real)
        {
        }

        public override object Calc(object[] args) 
            => Math.Min(args.Get<double>(0), args.Get<double>(1));
    }
    */
  
    
}