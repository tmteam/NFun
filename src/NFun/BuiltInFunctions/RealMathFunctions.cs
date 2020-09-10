using System;
using System.Collections.Generic;
using System.Text;
using NFun.Interpritation.Functions;
using NFun.Types;

namespace NFun.BuiltInFunctions
{
    #region strict math

    public class SqrtFunction : FunctionWithSingleArg
    {
        public SqrtFunction() : base("sqrt", VarType.Real, VarType.Real) { }
        public override object Calc(object a) => Math.Sqrt((double) a);
    }
    public class PowRealFunction : FunctionWithTwoArgs
    {
        public PowRealFunction() : base(CoreFunNames.Pow, VarType.Real, VarType.Real, VarType.Real) { }
        public override object Calc(object a, object b) => Math.Pow((double) a, (double) b);
    }

    public class DivideRealFunction : FunctionWithTwoArgs
    {
        public DivideRealFunction() : base(CoreFunNames.Divide, VarType.Real, VarType.Real, VarType.Real) { }
        public override object Calc(object a, object b) => (double) a / (double) b;
    }
    public class CosFunction : FunctionWithSingleArg
    {
        public CosFunction() : base("cos", VarType.Real, VarType.Real) { }
        public override object Calc(object a) => Math.Cos((double) a);
    }
    public class SinFunction : FunctionWithSingleArg
    {
        public SinFunction() : base("sin", VarType.Real, VarType.Real) { }

        public override object Calc(object a) => Math.Sin((double) a);
    }
    public class TanFunction : FunctionWithSingleArg
    {
        public TanFunction() : base("tan", VarType.Real, VarType.Real) { }
        public override object Calc(object a) => Math.Tan((double) a);
    }

    public class Atan2Function : FunctionWithManyArguments
    {
        public Atan2Function() : base("atan2", VarType.Real, VarType.Real, VarType.Real) { }
        public override object Calc(object[] args) => Math.Atan2((double)args[0], (double)args[0]);
    }
    public class AtanFunction : FunctionWithSingleArg
    {
        public AtanFunction() : base("atan", VarType.Real, VarType.Real) { }
        public override object Calc(object a) => Math.Atan((double)a);
    }
    public class AsinFunction : FunctionWithManyArguments
    {
        public AsinFunction() : base("asin", VarType.Real, VarType.Real) { }
        public override object Calc(object[] args) => Math.Asin((double)args[0]);
    }
    public class AcosFunction : FunctionWithManyArguments
    {
        public AcosFunction() : base("acos", VarType.Real, VarType.Real) { }
        public override object Calc(object[] args) => Math.Acos((double)args[0]);
    }
    public class ExpFunction : FunctionWithManyArguments
    {
        public ExpFunction() : base("exp", VarType.Real, VarType.Real) { }
        public override object Calc(object[] args) => Math.Exp((double)args[0]);
    }
    public class LogEFunction : FunctionWithManyArguments
    {
        public LogEFunction() : base("log", VarType.Real, VarType.Real) { }
        public override object Calc(object[] args) => Math.Log((double)args[0]);
    }

    public class LogFunction : FunctionWithManyArguments
    {
        public LogFunction() : base("log", VarType.Real, VarType.Real, VarType.Real) { }
        public override object Calc(object[] args) => Math.Log((double)args[0], (double)args[1]);
    }

    public class Log10Function : FunctionWithManyArguments
    {
        public Log10Function() : base("log10", VarType.Real, VarType.Real) { }
        public override object Calc(object[] args) => Math.Log10((double)args[0]);
    }

    #endregion
}
