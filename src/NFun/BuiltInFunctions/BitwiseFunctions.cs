using NFun.Interpritation.Functions;
using NFun.Types;

namespace NFun.BuiltInFunctions
{
    
    public class BitShiftLeftInt32Function: FunctionBase
    {
        public BitShiftLeftInt32Function() : base(CoreFunNames.BitShiftLeft, VarType.Int32,VarType.Int32,VarType.Int32){}
        public override object Calc(object[] args) => args.Get<int>(0) << args.Get<int>(1);
    }
    
    public class BitShiftLeftInt64Function: FunctionBase
    {
        public BitShiftLeftInt64Function() : base(CoreFunNames.BitShiftLeft, VarType.Int64,VarType.Int64,VarType.Int32){}
        public override object Calc(object[] args) => args.Get<long>(0) << args.Get<int>(1);
    }
    
    public class BitShiftRightInt32Function: FunctionBase
    {
        public BitShiftRightInt32Function() : base(CoreFunNames.BitShiftRight, VarType.Int32,VarType.Int32,VarType.Int32){}
        public override object Calc(object[] args) => args.Get<int>(0) >> args.Get<int>(1);
    }
    public class BitShiftRightInt64Function: FunctionBase
    {
        public BitShiftRightInt64Function() : base(CoreFunNames.BitShiftRight, VarType.Int64,VarType.Int64,VarType.Int32){}
        public override object Calc(object[] args) => args.Get<long>(0) >> args.Get<int>(1);
    }
    #region or
    public class BitOrInt32Function : FunctionBase
    {
        public BitOrInt32Function() : base(CoreFunNames.BitOr, VarType.Int32,VarType.Int32,VarType.Int32){}
        public override object Calc(object[] args) => args.Get<int>(0) | args.Get<int>(1);
    }
    public class BitOrInt64Function : FunctionBase
    {
        public BitOrInt64Function() : base(CoreFunNames.BitOr, VarType.Int64,VarType.Int64,VarType.Int64){}
        public override object Calc(object[] args) => args.Get<int>(0) | args.Get<int>(1);
    }
    
    public class BitOrUInt8Function : FunctionBase
    {
        public BitOrUInt8Function() : base(CoreFunNames.BitOr, VarType.UInt8,VarType.UInt8,VarType.UInt8){}
        public override object Calc(object[] args) => (byte)(args.Get<byte>(0) | args.Get<byte>(1));
    }
    public class BitOrUInt16Function : FunctionBase
    {
        public BitOrUInt16Function() : base(CoreFunNames.BitOr, VarType.UInt16,VarType.UInt16,VarType.UInt16){}
        public override object Calc(object[] args) => (ushort)(args.Get<ushort>(0) | args.Get<ushort>(1));
    }
    
    public class BitOrUInt32Function : FunctionBase
    {
        public BitOrUInt32Function() : base(CoreFunNames.BitOr, VarType.UInt32,VarType.UInt32,VarType.UInt32){}
        public override object Calc(object[] args) => (uint)(args.Get<uint>(0) | args.Get<uint>(1));
    }
    public class BitOrUInt64Function : FunctionBase
    {
        public BitOrUInt64Function() : base(CoreFunNames.BitOr, VarType.UInt64,VarType.UInt64,VarType.UInt64){}
        public override object Calc(object[] args) => (ulong)(args.Get<ulong>(0) | args.Get<ulong>(1));
    }
    #endregion

    #region xor

    

    public class BitXorIntFunction : FunctionBase
    {
        public BitXorIntFunction() : base(CoreFunNames.BitXor, VarType.Int32,VarType.Int32,VarType.Int32){}
        public override object Calc(object[] args) => args.Get<int>(0) ^ args.Get<int>(1);
    }
    public class BitXorInt64Function : FunctionBase
    {
        public BitXorInt64Function() : base(CoreFunNames.BitXor, VarType.Int64,VarType.Int64,VarType.Int64){}
        public override object Calc(object[] args) => args.Get<long>(0) ^ args.Get<long>(1);
    }
    
    public class BitXorUInt8Function : FunctionBase
    {
        public BitXorUInt8Function() : base(CoreFunNames.BitXor, VarType.UInt8,VarType.UInt8,VarType.UInt8){}
        public override object Calc(object[] args) => (byte)(args.Get<byte>(0) ^ args.Get<byte>(1));
    }
    public class BitXorUInt16Function : FunctionBase
    {
        public BitXorUInt16Function() : base(CoreFunNames.BitXor, VarType.UInt16,VarType.UInt16,VarType.UInt16){}
        public override object Calc(object[] args) => (ushort)(args.Get<ushort>(0) ^ args.Get<ushort>(1));
    }
    
    public class BitXorUInt32Function : FunctionBase
    {
        public BitXorUInt32Function() : base(CoreFunNames.BitXor, VarType.UInt32,VarType.UInt32,VarType.UInt32){}
        public override object Calc(object[] args) => (uint)(args.Get<uint>(0) ^ args.Get<uint>(1));
    }
    public class BitXorUInt64Function : FunctionBase
    {
        public BitXorUInt64Function() : base(CoreFunNames.BitXor, VarType.UInt64,VarType.UInt64,VarType.UInt64){}
        public override object Calc(object[] args) => (ulong)(args.Get<ulong>(0) ^ args.Get<ulong>(1));
    }
    #endregion

    #region  and

    

    public class BitAndIntFunction: FunctionBase
    {
        public BitAndIntFunction() : base(CoreFunNames.BitAnd, VarType.Int32,VarType.Int32,VarType.Int32){}
        public override object Calc(object[] args) => args.Get<int>(0) & args.Get<int>(1);
    }
    public class BitAndInt64Function: FunctionBase
    {
        public BitAndInt64Function() : base(CoreFunNames.BitAnd, VarType.Int64,VarType.Int64,VarType.Int64){}
        public override object Calc(object[] args) => args.Get<long>(0) & args.Get<long>(1);
    }
        
    public class BitAndUInt8Function : FunctionBase
    {
        public BitAndUInt8Function() : base(CoreFunNames.BitAnd, VarType.UInt8,VarType.UInt8,VarType.UInt8){}
        public override object Calc(object[] args) => (byte)(args.Get<byte>(0) & args.Get<byte>(1));
    }
    public class BitAndUInt16Function : FunctionBase
    {
        public BitAndUInt16Function() : base(CoreFunNames.BitAnd, VarType.UInt16,VarType.UInt16,VarType.UInt16){}
        public override object Calc(object[] args) => (ushort)(args.Get<ushort>(0) & args.Get<ushort>(1));
    }
    
    public class BitAndUInt32Function : FunctionBase
    {
        public BitAndUInt32Function() : base(CoreFunNames.BitAnd, VarType.UInt32,VarType.UInt32,VarType.UInt32){}
        public override object Calc(object[] args) => (uint)(args.Get<uint>(0) & args.Get<uint>(1));
    }
    public class BitAndUInt64Function : FunctionBase
    {
        public BitAndUInt64Function() : base(CoreFunNames.BitAnd, VarType.UInt64,VarType.UInt64,VarType.UInt64){}
        public override object Calc(object[] args) => (ulong)(args.Get<ulong>(0) & args.Get<ulong>(1));
    }
    #endregion

    #region inverse

    

    public class BitInverseIntFunction: FunctionBase
    {
        public BitInverseIntFunction() : base(CoreFunNames.BitInverse, VarType.Int32,VarType.Int32){}
        public override object Calc(object[] args) => ~args.Get<int>(0) ;
    }
    public class BitInverseInt64Function: FunctionBase
    {
        public BitInverseInt64Function() : base(CoreFunNames.BitInverse, VarType.Int64,VarType.Int64){}
        public override object Calc(object[] args) => ~args.Get<long>(0) ;
    }
    
    public class BitInverseUInt8Function : FunctionBase
    {
        public BitInverseUInt8Function() : base(CoreFunNames.BitInverse,VarType.UInt8,VarType.UInt8){}
        public override object Calc(object[] args) => (byte)~args.Get<byte>(0);
    }
    public class BitInverseUInt16Function : FunctionBase
    {
        public BitInverseUInt16Function() : base(CoreFunNames.BitInverse,VarType.UInt16,VarType.UInt16){}
        public override object Calc(object[] args) => (ushort)~args.Get<ushort>(0);
    }
    
    public class BitInverseUInt32Function : FunctionBase
    {
        public BitInverseUInt32Function() : base(CoreFunNames.BitInverse,VarType.UInt32,VarType.UInt32){}
        public override object Calc(object[] args) => (uint)~args.Get<uint>(0);
    }
    public class BitInverseUInt64Function : FunctionBase
    {
        public BitInverseUInt64Function() : base(CoreFunNames.BitInverse,VarType.UInt64,VarType.UInt64){}
        public override object Calc(object[] args) => (ulong)~args.Get<ulong>(0);
    }
    #endregion
}