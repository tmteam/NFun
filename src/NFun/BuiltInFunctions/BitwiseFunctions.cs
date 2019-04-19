using NFun.Interpritation.Functions;
using NFun.Types;

namespace NFun.BuiltInFunctions
{
    public class BitShiftLeftFunction: FunctionBase
    {
        public BitShiftLeftFunction() : base(CoreFunNames.BitShiftLeft, VarType.Int32,VarType.Int32,VarType.Int32){}
        public override object Calc(object[] args) => args.Get<int>(0) << args.Get<int>(1);
    }
    public class BitShiftRightFunction: FunctionBase
    {
        public BitShiftRightFunction() : base(CoreFunNames.BitShiftRight, VarType.Int32,VarType.Int32,VarType.Int32){}
        public override object Calc(object[] args) => args.Get<int>(0) >> args.Get<int>(1);
    }
    public class BitOrIntFunction : FunctionBase
    {
        public BitOrIntFunction() : base(CoreFunNames.BitOr, VarType.Int32,VarType.Int32,VarType.Int32){}
        public override object Calc(object[] args) => args.Get<int>(0) | (int)(args[1]);
    }
    
    public class BitXorIntFunction : FunctionBase
    {
        public BitXorIntFunction() : base(CoreFunNames.BitXor, VarType.Int32,VarType.Int32,VarType.Int32){}
        public override object Calc(object[] args) => args.Get<int>(0) ^ args.Get<int>(1);
    }
    
    public class BitAndIntFunction: FunctionBase
    {
        public BitAndIntFunction() : base(CoreFunNames.BitAnd, VarType.Int32,VarType.Int32,VarType.Int32){}
        public override object Calc(object[] args) => args.Get<int>(0) & (int)(args[1]);
    }
    public class BitInverseIntFunction: FunctionBase
    {
        public BitInverseIntFunction() : base(CoreFunNames.BitInverse, VarType.Int32,VarType.Int32){}
        public override object Calc(object[] args) => ~args.Get<int>(0) ;
    }
}