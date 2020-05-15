using NFun.Interpritation.Functions;
using NFun.Types;

namespace NFun.BuiltInFunctions
{
    #region binaries
    public class NotFunction : FunctionBase
    {
        public NotFunction() : base(CoreFunNames.Not, VarType.Bool, VarType.Bool) { }
        public override object Calc(object[] args) => !((bool)args[0]);
    }
    public class AndFunction : FunctionBase
    {
        public AndFunction() : base(CoreFunNames.And, VarType.Bool, VarType.Bool, VarType.Bool) { }
        public override object Calc(object[] args) => ((bool)args[0]) && ((bool)args[1]);
    }
    public class OrFunction : FunctionBase
    {
        public OrFunction() : base(CoreFunNames.Or, VarType.Bool, VarType.Bool, VarType.Bool) { }
        public override object Calc(object[] args) => ((bool)args[0]) || ((bool)args[1]);
    }
    public class XorFunction : FunctionBase
    {
        public XorFunction() : base(CoreFunNames.Xor, VarType.Bool, VarType.Bool, VarType.Bool) { }
        public override object Calc(object[] args) => ((bool)args[0]) ^ ((bool)args[1]);
    }
    #endregion
}
