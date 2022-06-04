using System;
using System.Linq;
using NFun.Interpretation.Functions;
using NFun.Runtime.Arrays;

namespace NFun.Functions; 

public class AverageDoubleFunction : FunctionWithSingleArg {
    public AverageDoubleFunction() : base("avg", FunnyType.Real, FunnyType.ArrayOf(FunnyType.Real)) { }
    public override object Calc(object a) => ((IFunnyArray)a).As<double>().Average();
}

public class SqrtDoubleFunction : FunctionWithSingleArg {
    public SqrtDoubleFunction() : base("sqrt", FunnyType.Real, FunnyType.Real) { }
    public override object Calc(object a) => Math.Sqrt((double)a);
}

public class PowDoubleFunction : FunctionWithTwoArgs {
    public PowDoubleFunction() : base(CoreFunNames.Pow, FunnyType.Real, FunnyType.Real, FunnyType.Real) { }
    public override object Calc(object a, object b) => Math.Pow((double)a, (double)b);
}

public class DivideDoubleFunction : FunctionWithTwoArgs {
    public DivideDoubleFunction() : base(CoreFunNames.DivideReal, FunnyType.Real, FunnyType.Real, FunnyType.Real) { }
    public override object Calc(object a, object b) => (double)a / (double)b;
}

public class CosDoubleFunction : FunctionWithSingleArg {
    public CosDoubleFunction() : base("cos", FunnyType.Real, FunnyType.Real) { }
    public override object Calc(object a) => Math.Cos((double)a);
}

public class SinDoubleFunction : FunctionWithSingleArg {
    public SinDoubleFunction() : base("sin", FunnyType.Real, FunnyType.Real) { }

    public override object Calc(object a) => Math.Sin((double)a);
}

public class TanDoubleFunction : FunctionWithSingleArg {
    public TanDoubleFunction() : base("tan", FunnyType.Real, FunnyType.Real) { }
    public override object Calc(object a) => Math.Tan((double)a);
}

public class Atan2DoubleFunction : FunctionWithTwoArgs {
    public Atan2DoubleFunction() : base("atan2", FunnyType.Real, FunnyType.Real, FunnyType.Real) { }
    public override object Calc(object a, object b) => Math.Atan2((double)a, (double)b);
}

public class AtanDoubleFunction : FunctionWithSingleArg {
    public AtanDoubleFunction() : base("atan", FunnyType.Real, FunnyType.Real) { }
    public override object Calc(object a) => Math.Atan((double)a);
}

public class AsinDoubleFunction : FunctionWithSingleArg {
    public AsinDoubleFunction() : base("asin", FunnyType.Real, FunnyType.Real) { }
    public override object Calc(object a) => Math.Asin((double)a);
}

public class AcosDoubleFunction : FunctionWithSingleArg {
    public AcosDoubleFunction() : base("acos", FunnyType.Real, FunnyType.Real) { }
    public override object Calc(object a) => Math.Acos((double)a);
}

public class ExpDoubleFunction : FunctionWithSingleArg {
    public ExpDoubleFunction() : base("exp", FunnyType.Real, FunnyType.Real) { }
    public override object Calc(object a) => Math.Exp((double)a);
}

public class LogEDoubleFunction : FunctionWithSingleArg {
    public LogEDoubleFunction() : base("log", FunnyType.Real, FunnyType.Real) { }
    public override object Calc(object a) => Math.Log((double)a);
}

public class LogDoubleFunction : FunctionWithTwoArgs {
    public LogDoubleFunction() : base("log", FunnyType.Real, FunnyType.Real, FunnyType.Real) { }
    public override object Calc(object a, object b) => Math.Log((double)a, (double)b);
}

public class Log10DoubleFunction : FunctionWithSingleArg {
    public Log10DoubleFunction() : base("log10", FunnyType.Real, FunnyType.Real) { }
    public override object Calc(object a) => Math.Log10((double)a);
}

public class RoundToDoubleFunction : FunctionWithTwoArgs {
    public RoundToDoubleFunction() : base("round", FunnyType.Real, FunnyType.Real, FunnyType.Int32) { }
    public override object Calc(object a, object b) => Math.Round((double)a, (int)b);
}