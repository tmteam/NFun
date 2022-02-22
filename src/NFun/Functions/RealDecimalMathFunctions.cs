using System;
using System.Linq;
using NFun.Interpretation.Functions;
using NFun.Runtime.Arrays;

namespace NFun.Functions {

public class AverageDecimalFunction : FunctionWithSingleArg {
    public AverageDecimalFunction() : base("avg", FunnyType.Real, FunnyType.ArrayOf(FunnyType.Real)) { }
    public override object Calc(object a) => ((IFunnyArray)a).As<decimal>().Average();
}

public class SqrtDecimalFunction : FunctionWithSingleArg {
    public SqrtDecimalFunction() : base("sqrt", FunnyType.Real, FunnyType.Real) { }
    public override object Calc(object a) =>  (Decimal)Math.Sqrt((double)(decimal)a);
}

public class PowDecimalFunction : FunctionWithTwoArgs {
    public PowDecimalFunction() : base(CoreFunNames.Pow, FunnyType.Real, FunnyType.Real, FunnyType.Real) { }
    public override object Calc(object a, object b) => (decimal)Math.Pow((double)(decimal)a, (double)(decimal)b);
}

public class DivideDecimalFunction : FunctionWithTwoArgs {
    public DivideDecimalFunction() : base(CoreFunNames.DivideReal, FunnyType.Real, FunnyType.Real, FunnyType.Real) { }
    public override object Calc(object a, object b) => (decimal)a / (decimal)b;
}

public class CosDecimalFunction : FunctionWithSingleArg {
    public CosDecimalFunction() : base("cos", FunnyType.Real, FunnyType.Real) { }
    public override object Calc(object a) => (decimal)Math.Cos((double)(decimal)a);
}

public class SinDecimalFunction : FunctionWithSingleArg {
    public SinDecimalFunction() : base("sin", FunnyType.Real, FunnyType.Real) { }

    public override object Calc(object a) => (decimal)Math.Sin((double)(decimal)a);
}

public class TanDecimalFunction : FunctionWithSingleArg {
    public TanDecimalFunction() : base("tan", FunnyType.Real, FunnyType.Real) { }
    public override object Calc(object a) => (decimal)Math.Tan((double)(decimal)a);
}

public class Atan2DecimalFunction : FunctionWithTwoArgs {
    public Atan2DecimalFunction() : base("atan2", FunnyType.Real, FunnyType.Real, FunnyType.Real) { }
    public override object Calc(object a, object b) => (decimal)Math.Atan2((double)(decimal)a, (double)(decimal)b);
}

public class AtanDecimalFunction : FunctionWithSingleArg {
    public AtanDecimalFunction() : base("atan", FunnyType.Real, FunnyType.Real) { }
    public override object Calc(object a) => (decimal)Math.Atan((double)(decimal)a);
}

public class AsinDecimalFunction : FunctionWithSingleArg {
    public AsinDecimalFunction() : base("asin", FunnyType.Real, FunnyType.Real) { }
    public override object Calc(object a) =>(decimal)Math.Asin((double)(decimal)a);
}

public class AcosDecimalFunction : FunctionWithSingleArg {
    public AcosDecimalFunction() : base("acos", FunnyType.Real, FunnyType.Real) { }
    public override object Calc(object a) => (decimal)Math.Acos((double)(decimal)a);
}

public class ExpDecimalFunction : FunctionWithSingleArg {
    public ExpDecimalFunction() : base("exp", FunnyType.Real, FunnyType.Real) { }
    public override object Calc(object a) => (decimal)Math.Exp((double)(decimal)a);
}

public class LogEDecimalFunction : FunctionWithSingleArg {
    public LogEDecimalFunction() : base("log", FunnyType.Real, FunnyType.Real) { }
    public override object Calc(object a) => (decimal)Math.Log((double)(decimal)a);
}

public class LogDecimalFunction : FunctionWithTwoArgs {
    public LogDecimalFunction() : base("log", FunnyType.Real, FunnyType.Real, FunnyType.Real) { }
    public override object Calc(object a, object b) => (decimal)Math.Log((double)(decimal)a, (double)(decimal)b);
}

public class Log10DecimalFunction : FunctionWithSingleArg {
    public Log10DecimalFunction() : base("log10", FunnyType.Real, FunnyType.Real) { }
    public override object Calc(object a) => (decimal)Math.Log10((double)(decimal)a);
}

public class RoundToDecimalFunction : FunctionWithTwoArgs {
    public RoundToDecimalFunction() : base("round", FunnyType.Real, FunnyType.Real, FunnyType.Int32) { }
    public override object Calc(object a, object b) => Math.Round((decimal)a, (int)b);
}

}