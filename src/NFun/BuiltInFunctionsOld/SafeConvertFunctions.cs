using System;
using System.Collections;
using System.Globalization;
using System.Linq;
using System.Text;
using NFun.Interpritation.Functions;
using NFun.Runtime;
using NFun.Runtime.Arrays;
using NFun.Types;

namespace NFun.BuiltInFunctions
{
    public class ToTextFunction : FunctionWithSingleArg
    {
        public ToTextFunction() : base(CoreFunNames.ToText, VarType.Text, VarType.Anything) { }

        public override object Calc(object a) => new TextFunArray(TypeHelper.GetFunText(a));
    }
    /*
    public class ToInt16FromInt16Function : FunctionBase
    {
        public ToInt16FromInt16Function() : base("toInt16", VarType.Int16, VarType.Int16){}
        public override object Calc(object[] args) => ((short)args[0]);
    }
    public class ToInt32FromInt32Function : FunctionBase
    {
        public ToInt32FromInt32Function(string name ="toInt32"):base(name, VarType.Int32, VarType.Int32){}
        public override object Calc(object[] args) => ((int)args[0]);
    }
    
    public class ToInt64FromInt64Function : FunctionBase
    {
        public ToInt64FromInt64Function() : base("toInt64", VarType.Int64, VarType.Int64){}
        public override object Calc(object[] args) => ((long)args[0]);
    }
    
    public class ToUint16FromUint16Function : FunctionBase
    {
        public ToUint16FromUint16Function() : base("toUint16", VarType.UInt16, VarType.UInt16){}
        public override object Calc(object[] args) => ((ushort)args[0]);
    }
    public class ToUint32FromUint32Function : FunctionBase
    {
        public ToUint32FromUint32Function() : base("toUint32", VarType.UInt32, VarType.UInt32){}
        public override object Calc(object[] args) => ((UInt32)args[0]);
    }
    
    public class ToUint64FromUint64Function : FunctionBase
    {
        public ToUint64FromUint64Function() : base("toUint64", VarType.UInt64, VarType.UInt64){}
        public override object Calc(object[] args) => ((ulong)args[0]);
    } 
    
    public class ToRealFromRealFunction : FunctionBase
    {
        public ToRealFromRealFunction() : base("toReal", VarType.Real, VarType.Real){}
        public override object Calc(object[] args) => ((double)args[0]);
    }
    
    public class ToUtf8Function : FunctionBase
    {
        public ToUtf8Function() : base("toUtf8", VarType.ArrayOf(VarType.Int32), VarType.Text){}
        public override object Calc(object[] args) => ImmutableFunArray.By(
            Encoding.UTF8.GetBytes( args.Get<object>(0).ToString()).Select(c=> (object)Convert.ToInt32(c)));
    }
    public class ToUnicodeFunction : FunctionBase
    {
        public ToUnicodeFunction() : base("toUnicode", VarType.ArrayOf(VarType.Int32), VarType.Text){}
        public override object Calc(object[] args) => ImmutableFunArray.By(
            Encoding.Unicode.GetBytes(args.Get<object>(0).ToString()).Select(c=> (object)Convert.ToInt32(c)));
    }
    public class ToBitsFromIntFunction : FunctionBase
    {
        public ToBitsFromIntFunction() : base("toBits", VarType.ArrayOf(VarType.Bool), VarType.Int32){}
        public override object Calc(object[] args) => ImmutableFunArray.By(
            new BitArray(BitConverter.GetBytes(((int)args[0]))).Cast<bool>().Cast<object>());
    }
    public class ToBytesFromIntFunction : FunctionBase
    {
        public ToBytesFromIntFunction() 
            : base("toBytes", VarType.ArrayOf(VarType.Int32), VarType.Int32){}
        public override object Calc(object[] args) => ImmutableFunArray.By(
            BitConverter.GetBytes(((int)args[0])).Select(c=> (object)Convert.ToInt32(c)));
    }
    
    
    */
}