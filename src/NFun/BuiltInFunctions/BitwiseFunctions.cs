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
    public class BitOrIntFunction : FunctionBase
    {
        public BitOrIntFunction() : base(CoreFunNames.BitOr, VarType.Int32,VarType.Int32,VarType.Int32){}
        public override object Calc(object[] args) => args.Get<int>(0) | args.Get<int>(1);
    }
    public class BitOrInt64Function : FunctionBase
    {
        public BitOrInt64Function() : base(CoreFunNames.BitOr, VarType.Int64,VarType.Int64,VarType.Int64){}
        public override object Calc(object[] args) => args.Get<int>(0) | args.Get<int>(1);
    }
    
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
}