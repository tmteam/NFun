using NFun.Interpritation.Functions;
using NFun.Types;

namespace NFun.BuiltInFunctions
{
    #region binaries
    public class NotFunction : FunctionWithSingleArg
    {
        public NotFunction() : base(CoreFunNames.Not, VarType.Bool, VarType.Bool) { }
        public override object Calc(object a) => !(bool) a;
    }
    public class AndFunction : FunctionWithTwoArgs
    {
        public AndFunction() : base(CoreFunNames.And, VarType.Bool, VarType.Bool, VarType.Bool) { }
        public override object Calc(object a, object b) => (bool) a && (bool) b;
    }
    public class OrFunction : FunctionWithTwoArgs
    {
        public OrFunction() : base(CoreFunNames.Or, VarType.Bool, VarType.Bool, VarType.Bool) { }
        public override object Calc(object a, object b) => (bool) a || (bool) b;
    }
    public class XorFunction : FunctionWithTwoArgs
    {
        public XorFunction() : base(CoreFunNames.Xor, VarType.Bool, VarType.Bool, VarType.Bool) { }
        public override object Calc(object a, object b) => (bool) a ^ (bool) b;
    }
    #endregion
}
