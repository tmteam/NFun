using System;
using System.Collections;
using System.Globalization;
using System.Linq;
using System.Text;
using NFun.Interpritation.Functions;
using NFun.Runtime;
using NFun.Types;

namespace NFun.BuiltInFunctions
{
    
    public class ToIntFromRealFunction : FunctionBase
    {
        public ToIntFromRealFunction(string name = "toInt32") : base(name, VarType.Int32, VarType.Real){}
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
    
    public class ToInt16FromInt64Function : FunctionBase
    {
        public ToInt16FromInt64Function() : base("toInt16", VarType.Int16, VarType.Int64){}
        public override object Calc(object[] args) => (Int16)args.Get<long>(0);
    }
    public class ToInt16FromUInt64Function : FunctionBase
    {
        public ToInt16FromUInt64Function() : base("toInt16", VarType.Int16, VarType.UInt64){}
        public override object Calc(object[] args) => (Int16)args.Get<ulong>(0);
    }
    public class ToInt32FromInt64Function : FunctionBase
    {
        public ToInt32FromInt64Function(string name = "toInt32") : base(name, VarType.Int32, VarType.Int64){}
        public override object Calc(object[] args) => (int)args.Get<long>(0);
    }
    public class ToInt32FromUInt64Function : FunctionBase
    {
        public ToInt32FromUInt64Function(string name = "toInt32") : base(name, VarType.Int32, VarType.UInt64){}

        public override object Calc(object[] args) => (int)args.Get<UInt64>(0);
    }
    public class ToInt64FromUInt64Function : FunctionBase
    {
        public ToInt64FromUInt64Function() : base("toInt64", VarType.Int64, VarType.UInt64){}
        public override object Calc(object[] args) => (long)args.Get<ulong>(0);
    }
    
    public class ToUint8FromUint64Function : FunctionBase
    {
        public ToUint8FromUint64Function(string name = "toUint8") : base(name, VarType.UInt8, VarType.UInt64){}
        public override object Calc(object[] args) => (byte) args.Get<ulong>(0);
    }
    public class ToUint8FromInt64Function : FunctionBase
    {
        public ToUint8FromInt64Function(string name ="toUint8") : base(name, VarType.UInt8, VarType.Int64){}
        public override object Calc(object[] args) => (byte) args.Get<long>(0);
    }
    
    public class ToUint16FromUint64Function : FunctionBase
    {
        public ToUint16FromUint64Function() : base("toUint16", VarType.UInt16, VarType.UInt64){}
        public override object Calc(object[] args) => (ushort) args.Get<ulong>(0);
    }
     public class ToUint16FromInt64Function : FunctionBase
        {
            public ToUint16FromInt64Function() : base("toUint16", VarType.UInt16, VarType.Int64){}
            public override object Calc(object[] args) => (ushort) args.Get<long>(0);
        }
    public class ToUint32FromUint64Function : FunctionBase
    {
        public ToUint32FromUint64Function() : base("toUint32", VarType.UInt32, VarType.UInt64){}
        public override object Calc(object[] args) => (uint)args.Get<ulong>(0);
    }
    
    public class ToUint32FromInt64Function : FunctionBase
    {
            public ToUint32FromInt64Function() : base("toUint32", VarType.UInt32, VarType.Int64){}
            public override object Calc(object[] args) => (uint)args.Get<long>(0);
    }
    public class ToUint64FromInt64Function : FunctionBase
    {
        public ToUint64FromInt64Function() : base("toUint64", VarType.UInt64, VarType.Int64){}
        public override object Calc(object[] args) => (ulong)args.Get<long>(0);
    }
    
    public class ToRealFromTextFunction : FunctionBase
    {
        public ToRealFromTextFunction() : base("toReal", VarType.Real, VarType.Text){}
        public override object Calc(object[] args)
        {
            try {
                return Double.Parse(args.GetTextOrThrow(0), CultureInfo.InvariantCulture);
            }
            catch (Exception e) {
                throw new FunRuntimeException($"Text '{args[0]}' cannot be parsed into real", e);
            }
        }
    }
    
    public class ToIntFromTextFunction : FunctionBase
    {
        public ToIntFromTextFunction(string name = "toInt32") : base(name, VarType.Int32, VarType.Text){}
        public override object Calc(object[] args)
        {
            try {
                return int.Parse(args.GetTextOrThrow(0));
            }
            catch (Exception e) {
                throw new FunRuntimeException($"Text '{args[0]}' cannot be parsed into int", e);
            }
        }
    }
    public class ToIntFromBytesFunction : FunctionBase
    {
        public ToIntFromBytesFunction() : base("toInt", VarType.Int32, VarType.ArrayOf(VarType.Int32)){}
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