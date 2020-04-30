using System;
using System.Collections.Generic;
using System.Text;
using NFun.Interpritation.Functions;
using NFun.Types;

namespace NFun.BuiltInFunctions
{
    #region binaries
    public class NotFunction : FunctionBase
    {
        public NotFunction() : base(CoreFunNames.Not, VarType.Bool, VarType.Bool) { }
        public override object Calc(object[] args) => !args.Get<bool>(0);
    }
    public class AndFunction : FunctionBase
    {
        public AndFunction() : base(CoreFunNames.And, VarType.Bool, VarType.Bool, VarType.Bool) { }
        public override object Calc(object[] args) => args.Get<bool>(0) && args.Get<bool>(1);
    }
    public class OrFunction : FunctionBase
    {
        public OrFunction() : base(CoreFunNames.Or, VarType.Bool, VarType.Bool, VarType.Bool) { }
        public override object Calc(object[] args) => args.Get<bool>(0) || args.Get<bool>(1);
    }
    public class XorFunction : FunctionBase
    {
        public XorFunction() : base(CoreFunNames.Or, VarType.Bool, VarType.Bool, VarType.Bool) { }
        public override object Calc(object[] args) => args.Get<bool>(0) ^ args.Get<bool>(1);
    }
    #endregion
}
