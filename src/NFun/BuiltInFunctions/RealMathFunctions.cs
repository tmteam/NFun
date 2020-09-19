using System;
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

    public class Atan2Function : FunctionWithTwoArgs
    {
        public Atan2Function() : base("atan2", VarType.Real, VarType.Real, VarType.Real) { }
        public override object Calc(object a, object b) => Math.Atan2((double)a, (double)b);
    }
    public class AtanFunction : FunctionWithSingleArg
    {
        public AtanFunction() : base("atan", VarType.Real, VarType.Real) { }
        public override object Calc(object a) => Math.Atan((double)a);
    }
    public class AsinFunction : FunctionWithSingleArg
    {
        public AsinFunction() : base("asin", VarType.Real, VarType.Real) { }
        public override object Calc(object a) => Math.Asin((double)a);
    }
    public class AcosFunction : FunctionWithSingleArg
    {
        public AcosFunction() : base("acos", VarType.Real, VarType.Real) { }
        public override object Calc(object a) => Math.Acos((double)a);
    }
    public class ExpFunction : FunctionWithSingleArg
    {
        public ExpFunction() : base("exp", VarType.Real, VarType.Real) { }
        public override object Calc(object a) => Math.Exp((double)a);
    }
    public class LogEFunction : FunctionWithSingleArg
    {
        public LogEFunction() : base("log", VarType.Real, VarType.Real) { }
        public override object Calc(object a) => Math.Log((double)a);
    }

    public class LogFunction : FunctionWithTwoArgs
    {
        public LogFunction() : base("log", VarType.Real, VarType.Real, VarType.Real) { }
        public override object Calc(object a, object b) => Math.Log((double)a, (double)b);
    }

    public class Log10Function : FunctionWithSingleArg
    {
        public Log10Function() : base("log10", VarType.Real, VarType.Real) { }
        public override object Calc(object a) => Math.Log10((double)a);
    }
    
    public class RoundToRealFunction: FunctionWithTwoArgs {
        public RoundToRealFunction() : base("round", VarType.Real,VarType.Real,VarType.Int32){}
        public override object Calc(object a, object b) => Math.Round((double)a,(int)b);
    }

    #endregion
}
