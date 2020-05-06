using System;
using System.Collections.Generic;
using System.Text;
using NFun.Interpritation.Functions;
using NFun.Types;

namespace NFun.BuiltInFunctions
{
    #region strict math

    public class SqrtFunction : FunctionBase
    {
        public SqrtFunction() : base("sqrt", VarType.Real, VarType.Real) { }
        public override object Calc(object[] args) => Math.Sqrt(args.Get<double>(0));
    }
    public class PowRealFunction : FunctionBase
    {
        public PowRealFunction() : base(CoreFunNames.Pow, VarType.Real, VarType.Real, VarType.Real) { }
        public override object Calc(object[] args)
            => Math.Pow(args.Get<double>(0), args.Get<double>(1));
    }

    public class DivideRealFunction : FunctionBase
    {
        public DivideRealFunction() : base(CoreFunNames.Divide, VarType.Real, VarType.Real, VarType.Real) { }
        public override object Calc(object[] args) => args.Get<double>(0) / args.Get<double>(1);
    }
    public class CosFunction : FunctionBase
    {
        public CosFunction() : base("cos", VarType.Real, VarType.Real) { }

        public override object Calc(object[] args) => Math.Cos(args.Get<double>(0));
    }
    public class SinFunction : FunctionBase
    {
        public SinFunction() : base("sin", VarType.Real, VarType.Real) { }

        public override object Calc(object[] args) => Math.Sin(args.Get<double>(0));
    }
    public class TanFunction : FunctionBase
    {
        public TanFunction() : base("tan", VarType.Real, VarType.Real) { }
        public override object Calc(object[] args) => Math.Tan(args.Get<double>(0));
    }

    public class Atan2Function : FunctionBase
    {
        public Atan2Function() : base("atan2", VarType.Real, VarType.Real, VarType.Real) { }
        public override object Calc(object[] args) => Math.Atan2(args.Get<double>(0), args.Get<double>(0));
    }
    public class AtanFunction : FunctionBase
    {
        public AtanFunction() : base("atan", VarType.Real, VarType.Real) { }
        public override object Calc(object[] args) => Math.Atan(args.Get<double>(0));
    }
    public class AsinFunction : FunctionBase
    {
        public AsinFunction() : base("asin", VarType.Real, VarType.Real) { }
        public override object Calc(object[] args) => Math.Asin(args.Get<double>(0));
    }
    public class AcosFunction : FunctionBase
    {
        public AcosFunction() : base("acos", VarType.Real, VarType.Real) { }
        public override object Calc(object[] args) => Math.Acos(args.Get<double>(0));
    }
    public class ExpFunction : FunctionBase
    {
        public ExpFunction() : base("exp", VarType.Real, VarType.Real) { }
        public override object Calc(object[] args) => Math.Exp(args.Get<double>(0));
    }
    public class LogEFunction : FunctionBase
    {
        public LogEFunction() : base("log", VarType.Real, VarType.Real) { }
        public override object Calc(object[] args) => Math.Log(args.Get<double>(0));
    }

    public class LogFunction : FunctionBase
    {
        public LogFunction() : base("log", VarType.Real, VarType.Real, VarType.Real) { }
        public override object Calc(object[] args) => Math.Log(args.Get<double>(0), args.Get<double>(1));
    }

    public class Log10Function : FunctionBase
    {
        public Log10Function() : base("log10", VarType.Real, VarType.Real) { }
        public override object Calc(object[] args) => Math.Log10(args.Get<double>(0));
    }

    #endregion
}
