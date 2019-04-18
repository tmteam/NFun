using System;
using System.Collections;
using System.Linq;
using System.Text;
using NFun.Interpritation.Functions;
using NFun.Runtime;
using NFun.Types;

namespace NFun.BuiltInFunctions
{
    public class ToIntFromRealFunction : FunctionBase
    {
        public ToIntFromRealFunction() : base("to_int", VarType.Int, VarType.Real){}
        public override object Calc(object[] args)
        {
            try {
                return Convert.ToInt32(args.Get<double>(0));
            }
            catch (Exception e) {
                throw new FunRuntimeException($"Number '{args[0]}' cannot be converted into int", e);
            }
        }
    }
    public class ToUtf8Function : FunctionBase
    {
        public ToUtf8Function() : base("to_utf8", VarType.ArrayOf(VarType.Int), VarType.Text){}
        public override object Calc(object[] args) => FunArray.By(
            Encoding.UTF8.GetBytes( args.Get<object>(0).ToString()).Select(c=> (object)Convert.ToInt32(c)));
    }
    public class ToUnicodeFunction : FunctionBase
    {
        public ToUnicodeFunction() : base("to_unicode", VarType.ArrayOf(VarType.Int), VarType.Text){}
        public override object Calc(object[] args) => FunArray.By(
            Encoding.Unicode.GetBytes(args.Get<object>(0).ToString()).Select(c=> (object)Convert.ToInt32(c)));
    }
    public class ToBitsFromIntFunction : FunctionBase
    {
        public ToBitsFromIntFunction() : base("to_bits", VarType.ArrayOf(VarType.Bool), VarType.Int){}
        public override object Calc(object[] args) => FunArray.By(
            new BitArray(BitConverter.GetBytes(args.Get<int>(0))).Cast<bool>().Cast<object>());
    }
    public class ToBytesFromIntFunction : FunctionBase
    {
        public ToBytesFromIntFunction() 
            : base("to_bytes", VarType.ArrayOf(VarType.Int), VarType.Int){}
        public override object Calc(object[] args) => FunArray.By(
            BitConverter.GetBytes(args.Get<int>(0)).Select(c=> (object)Convert.ToInt32(c)));
    }
    public class ToRealFromTextFunction : FunctionBase
    {
        public ToRealFromTextFunction() : base("to_real", VarType.Real, VarType.Text){}
        public override object Calc(object[] args)
        {
            try {
                return Double.Parse(args.Get<string>(0));
            }
            catch (Exception e) {
                throw new FunRuntimeException($"Text '{args[0]}' cannot be parsed into real", e);
            }
        }
    }
    public class ToRealFromIntFunction : FunctionBase
    {
        public ToRealFromIntFunction() : base("to_real", VarType.Real, VarType.Int){}
        public override object Calc(object[] args) 
            => Convert.ToDouble(args.Get<int>(0));
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
        public override object Calc(object[] args)
        {
            try {
                return int.Parse(args.Get<string>(0));
            }
            catch (Exception e) {
                throw new FunRuntimeException($"Text '{args[0]}' cannot be parsed into int", e);
            }
        }
    }
    public class ToIntFromBytesFunction : FunctionBase
    {
        public ToIntFromBytesFunction() : base("to_int", VarType.Int, VarType.ArrayOf(VarType.Int)){}
        public override object Calc(object[] args)
        {
            try {
                var val = ((IFunArray) args[0]);
                if(val.Count>4)
                    throw new FunRuntimeException("Array is too long");
                byte[] arr;
                if (val.Count == 4)
                    arr = val.Select(Convert.ToByte).ToArray();
                else
                    arr = val.Concat(new int[4 - val.Count].Cast<object>()).Select(Convert.ToByte).ToArray();
                return BitConverter.ToInt32(arr, 0);            }
            catch (Exception e) {
                throw new FunRuntimeException($"Array '{args[0]}' cannot be converted into int", e);
            }
            
        }
    }
}