using System;
using System.Linq;
using NFun.Exceptions;
using NFun.Interpretation.Functions;
using NFun.Runtime.Arrays;
using NFun.Types;

namespace NFun.Functions;

// Math functions generic over the Floats constraint.
// Float32 → MathF.X. Real → Math.X (double) or double-cast (decimal) via RealTypeSelect.

public class SqrtFunction : PureGenericFunctionBase {
    public SqrtFunction() : base("sqrt", GenericConstrains.Floats, 1) { ArgProperties = FunArgProperty.FromNames("x"); }
    public override IConcreteFunction CreateConcrete(FunnyType[] t, IFunctionSelectorContext ctx) {
        FunctionWithSingleArg r = t[0].BaseType switch {
            BaseFunnyType.Float32 => F32.Instance,
            BaseFunnyType.Real    => ctx.RealTypeSelect<FunctionWithSingleArg>(F64.Instance, Decimal.Instance),
            _ => throw new NFunImpossibleException("Unsupported type for sqrt")
        };
        r.Name = "sqrt"; r.ArgTypes = t; r.ReturnType = t[0];
        return r;
    }
    sealed class F32 : FunctionWithSingleArg { public static readonly F32 Instance = new(); public override object Calc(object a) => MathF.Sqrt((float)a); }
    sealed class F64 : FunctionWithSingleArg { public static readonly F64 Instance = new(); public override object Calc(object a) => Math.Sqrt((double)a); }
    sealed class Decimal : FunctionWithSingleArg { public static readonly Decimal Instance = new(); public override object Calc(object a) => (decimal)Math.Sqrt((double)(decimal)a); }
}

public class SinFunction : PureGenericFunctionBase {
    public SinFunction() : base("sin", GenericConstrains.Floats, 1) { ArgProperties = FunArgProperty.FromNames("x"); }
    public override IConcreteFunction CreateConcrete(FunnyType[] t, IFunctionSelectorContext ctx) {
        FunctionWithSingleArg r = t[0].BaseType switch {
            BaseFunnyType.Float32 => F32.Instance,
            BaseFunnyType.Real    => ctx.RealTypeSelect<FunctionWithSingleArg>(F64.Instance, Decimal.Instance),
            _ => throw new NFunImpossibleException("Unsupported type for sin")
        };
        r.Name = "sin"; r.ArgTypes = t; r.ReturnType = t[0];
        return r;
    }
    sealed class F32 : FunctionWithSingleArg { public static readonly F32 Instance = new(); public override object Calc(object a) => MathF.Sin((float)a); }
    sealed class F64 : FunctionWithSingleArg { public static readonly F64 Instance = new(); public override object Calc(object a) => Math.Sin((double)a); }
    sealed class Decimal : FunctionWithSingleArg { public static readonly Decimal Instance = new(); public override object Calc(object a) => (decimal)Math.Sin((double)(decimal)a); }
}

public class CosFunction : PureGenericFunctionBase {
    public CosFunction() : base("cos", GenericConstrains.Floats, 1) { ArgProperties = FunArgProperty.FromNames("x"); }
    public override IConcreteFunction CreateConcrete(FunnyType[] t, IFunctionSelectorContext ctx) {
        FunctionWithSingleArg r = t[0].BaseType switch {
            BaseFunnyType.Float32 => F32.Instance,
            BaseFunnyType.Real    => ctx.RealTypeSelect<FunctionWithSingleArg>(F64.Instance, Decimal.Instance),
            _ => throw new NFunImpossibleException("Unsupported type for cos")
        };
        r.Name = "cos"; r.ArgTypes = t; r.ReturnType = t[0];
        return r;
    }
    sealed class F32 : FunctionWithSingleArg { public static readonly F32 Instance = new(); public override object Calc(object a) => MathF.Cos((float)a); }
    sealed class F64 : FunctionWithSingleArg { public static readonly F64 Instance = new(); public override object Calc(object a) => Math.Cos((double)a); }
    sealed class Decimal : FunctionWithSingleArg { public static readonly Decimal Instance = new(); public override object Calc(object a) => (decimal)Math.Cos((double)(decimal)a); }
}

public class TanFunction : PureGenericFunctionBase {
    public TanFunction() : base("tan", GenericConstrains.Floats, 1) { ArgProperties = FunArgProperty.FromNames("x"); }
    public override IConcreteFunction CreateConcrete(FunnyType[] t, IFunctionSelectorContext ctx) {
        FunctionWithSingleArg r = t[0].BaseType switch {
            BaseFunnyType.Float32 => F32.Instance,
            BaseFunnyType.Real    => ctx.RealTypeSelect<FunctionWithSingleArg>(F64.Instance, Decimal.Instance),
            _ => throw new NFunImpossibleException("Unsupported type for tan")
        };
        r.Name = "tan"; r.ArgTypes = t; r.ReturnType = t[0];
        return r;
    }
    sealed class F32 : FunctionWithSingleArg { public static readonly F32 Instance = new(); public override object Calc(object a) => MathF.Tan((float)a); }
    sealed class F64 : FunctionWithSingleArg { public static readonly F64 Instance = new(); public override object Calc(object a) => Math.Tan((double)a); }
    sealed class Decimal : FunctionWithSingleArg { public static readonly Decimal Instance = new(); public override object Calc(object a) => (decimal)Math.Tan((double)(decimal)a); }
}

public class AsinFunction : PureGenericFunctionBase {
    public AsinFunction() : base("asin", GenericConstrains.Floats, 1) { ArgProperties = FunArgProperty.FromNames("x"); }
    public override IConcreteFunction CreateConcrete(FunnyType[] t, IFunctionSelectorContext ctx) {
        FunctionWithSingleArg r = t[0].BaseType switch {
            BaseFunnyType.Float32 => F32.Instance,
            BaseFunnyType.Real    => ctx.RealTypeSelect<FunctionWithSingleArg>(F64.Instance, Decimal.Instance),
            _ => throw new NFunImpossibleException("Unsupported type for asin")
        };
        r.Name = "asin"; r.ArgTypes = t; r.ReturnType = t[0];
        return r;
    }
    sealed class F32 : FunctionWithSingleArg { public static readonly F32 Instance = new(); public override object Calc(object a) => MathF.Asin((float)a); }
    sealed class F64 : FunctionWithSingleArg { public static readonly F64 Instance = new(); public override object Calc(object a) => Math.Asin((double)a); }
    sealed class Decimal : FunctionWithSingleArg { public static readonly Decimal Instance = new(); public override object Calc(object a) => (decimal)Math.Asin((double)(decimal)a); }
}

public class AcosFunction : PureGenericFunctionBase {
    public AcosFunction() : base("acos", GenericConstrains.Floats, 1) { ArgProperties = FunArgProperty.FromNames("x"); }
    public override IConcreteFunction CreateConcrete(FunnyType[] t, IFunctionSelectorContext ctx) {
        FunctionWithSingleArg r = t[0].BaseType switch {
            BaseFunnyType.Float32 => F32.Instance,
            BaseFunnyType.Real    => ctx.RealTypeSelect<FunctionWithSingleArg>(F64.Instance, Decimal.Instance),
            _ => throw new NFunImpossibleException("Unsupported type for acos")
        };
        r.Name = "acos"; r.ArgTypes = t; r.ReturnType = t[0];
        return r;
    }
    sealed class F32 : FunctionWithSingleArg { public static readonly F32 Instance = new(); public override object Calc(object a) => MathF.Acos((float)a); }
    sealed class F64 : FunctionWithSingleArg { public static readonly F64 Instance = new(); public override object Calc(object a) => Math.Acos((double)a); }
    sealed class Decimal : FunctionWithSingleArg { public static readonly Decimal Instance = new(); public override object Calc(object a) => (decimal)Math.Acos((double)(decimal)a); }
}

public class AtanFunction : PureGenericFunctionBase {
    public AtanFunction() : base("atan", GenericConstrains.Floats, 1) { ArgProperties = FunArgProperty.FromNames("x"); }
    public override IConcreteFunction CreateConcrete(FunnyType[] t, IFunctionSelectorContext ctx) {
        FunctionWithSingleArg r = t[0].BaseType switch {
            BaseFunnyType.Float32 => F32.Instance,
            BaseFunnyType.Real    => ctx.RealTypeSelect<FunctionWithSingleArg>(F64.Instance, Decimal.Instance),
            _ => throw new NFunImpossibleException("Unsupported type for atan")
        };
        r.Name = "atan"; r.ArgTypes = t; r.ReturnType = t[0];
        return r;
    }
    sealed class F32 : FunctionWithSingleArg { public static readonly F32 Instance = new(); public override object Calc(object a) => MathF.Atan((float)a); }
    sealed class F64 : FunctionWithSingleArg { public static readonly F64 Instance = new(); public override object Calc(object a) => Math.Atan((double)a); }
    sealed class Decimal : FunctionWithSingleArg { public static readonly Decimal Instance = new(); public override object Calc(object a) => (decimal)Math.Atan((double)(decimal)a); }
}

public class Atan2Function : PureGenericFunctionBase {
    public Atan2Function() : base("atan2", GenericConstrains.Floats, 2) { ArgProperties = FunArgProperty.FromNames("y", "x"); }
    public override IConcreteFunction CreateConcrete(FunnyType[] t, IFunctionSelectorContext ctx) {
        FunctionWithTwoArgs r = t[0].BaseType switch {
            BaseFunnyType.Float32 => F32.Instance,
            BaseFunnyType.Real    => ctx.RealTypeSelect<FunctionWithTwoArgs>(F64.Instance, Decimal.Instance),
            _ => throw new NFunImpossibleException("Unsupported type for atan2")
        };
        r.Name = "atan2"; r.ArgTypes = new[] { t[0], t[0] }; r.ReturnType = t[0];
        return r;
    }
    sealed class F32 : FunctionWithTwoArgs { public static readonly F32 Instance = new(); public override object Calc(object a, object b) => MathF.Atan2((float)a, (float)b); }
    sealed class F64 : FunctionWithTwoArgs { public static readonly F64 Instance = new(); public override object Calc(object a, object b) => Math.Atan2((double)a, (double)b); }
    sealed class Decimal : FunctionWithTwoArgs { public static readonly Decimal Instance = new(); public override object Calc(object a, object b) => (decimal)Math.Atan2((double)(decimal)a, (double)(decimal)b); }
}

public class ExpFunction : PureGenericFunctionBase {
    public ExpFunction() : base("exp", GenericConstrains.Floats, 1) { ArgProperties = FunArgProperty.FromNames("x"); }
    public override IConcreteFunction CreateConcrete(FunnyType[] t, IFunctionSelectorContext ctx) {
        FunctionWithSingleArg r = t[0].BaseType switch {
            BaseFunnyType.Float32 => F32.Instance,
            BaseFunnyType.Real    => ctx.RealTypeSelect<FunctionWithSingleArg>(F64.Instance, Decimal.Instance),
            _ => throw new NFunImpossibleException("Unsupported type for exp")
        };
        r.Name = "exp"; r.ArgTypes = t; r.ReturnType = t[0];
        return r;
    }
    sealed class F32 : FunctionWithSingleArg { public static readonly F32 Instance = new(); public override object Calc(object a) => MathF.Exp((float)a); }
    sealed class F64 : FunctionWithSingleArg { public static readonly F64 Instance = new(); public override object Calc(object a) => Math.Exp((double)a); }
    sealed class Decimal : FunctionWithSingleArg { public static readonly Decimal Instance = new(); public override object Calc(object a) => (decimal)Math.Exp((double)(decimal)a); }
}

public class LogEFunction : PureGenericFunctionBase {
    public LogEFunction() : base("log", GenericConstrains.Floats, 1) { ArgProperties = FunArgProperty.FromNames("x"); }
    public override IConcreteFunction CreateConcrete(FunnyType[] t, IFunctionSelectorContext ctx) {
        FunctionWithSingleArg r = t[0].BaseType switch {
            BaseFunnyType.Float32 => F32.Instance,
            BaseFunnyType.Real    => ctx.RealTypeSelect<FunctionWithSingleArg>(F64.Instance, Decimal.Instance),
            _ => throw new NFunImpossibleException("Unsupported type for log")
        };
        r.Name = "log"; r.ArgTypes = t; r.ReturnType = t[0];
        return r;
    }
    sealed class F32 : FunctionWithSingleArg { public static readonly F32 Instance = new(); public override object Calc(object a) => MathF.Log((float)a); }
    sealed class F64 : FunctionWithSingleArg { public static readonly F64 Instance = new(); public override object Calc(object a) => Math.Log((double)a); }
    sealed class Decimal : FunctionWithSingleArg { public static readonly Decimal Instance = new(); public override object Calc(object a) => (decimal)Math.Log((double)(decimal)a); }
}

public class LogFunction : PureGenericFunctionBase {
    public LogFunction() : base("log", GenericConstrains.Floats, 2) { ArgProperties = FunArgProperty.FromNames("value", "newBase"); }
    public override IConcreteFunction CreateConcrete(FunnyType[] t, IFunctionSelectorContext ctx) {
        FunctionWithTwoArgs r = t[0].BaseType switch {
            BaseFunnyType.Float32 => F32.Instance,
            BaseFunnyType.Real    => ctx.RealTypeSelect<FunctionWithTwoArgs>(F64.Instance, Decimal.Instance),
            _ => throw new NFunImpossibleException("Unsupported type for log(value,newBase)")
        };
        r.Name = "log"; r.ArgTypes = new[] { t[0], t[0] }; r.ReturnType = t[0];
        return r;
    }
    sealed class F32 : FunctionWithTwoArgs { public static readonly F32 Instance = new(); public override object Calc(object a, object b) => MathF.Log((float)a, (float)b); }
    sealed class F64 : FunctionWithTwoArgs { public static readonly F64 Instance = new(); public override object Calc(object a, object b) => Math.Log((double)a, (double)b); }
    sealed class Decimal : FunctionWithTwoArgs { public static readonly Decimal Instance = new(); public override object Calc(object a, object b) => (decimal)Math.Log((double)(decimal)a, (double)(decimal)b); }
}

public class Log10Function : PureGenericFunctionBase {
    public Log10Function() : base("log10", GenericConstrains.Floats, 1) { ArgProperties = FunArgProperty.FromNames("x"); }
    public override IConcreteFunction CreateConcrete(FunnyType[] t, IFunctionSelectorContext ctx) {
        FunctionWithSingleArg r = t[0].BaseType switch {
            BaseFunnyType.Float32 => F32.Instance,
            BaseFunnyType.Real    => ctx.RealTypeSelect<FunctionWithSingleArg>(F64.Instance, Decimal.Instance),
            _ => throw new NFunImpossibleException("Unsupported type for log10")
        };
        r.Name = "log10"; r.ArgTypes = t; r.ReturnType = t[0];
        return r;
    }
    sealed class F32 : FunctionWithSingleArg { public static readonly F32 Instance = new(); public override object Calc(object a) => MathF.Log10((float)a); }
    sealed class F64 : FunctionWithSingleArg { public static readonly F64 Instance = new(); public override object Calc(object a) => Math.Log10((double)a); }
    sealed class Decimal : FunctionWithSingleArg { public static readonly Decimal Instance = new(); public override object Calc(object a) => (decimal)Math.Log10((double)(decimal)a); }
}

public class CeilFunction : PureGenericFunctionBase {
    public CeilFunction() : base("ceil", GenericConstrains.Floats, 1) { ArgProperties = FunArgProperty.FromNames("x"); }
    public override IConcreteFunction CreateConcrete(FunnyType[] t, IFunctionSelectorContext ctx) {
        FunctionWithSingleArg r = t[0].BaseType switch {
            BaseFunnyType.Float32 => F32.Instance,
            BaseFunnyType.Real    => ctx.RealTypeSelect<FunctionWithSingleArg>(F64.Instance, Decimal.Instance),
            _ => throw new NFunImpossibleException("Unsupported type for ceil")
        };
        r.Name = "ceil"; r.ArgTypes = t; r.ReturnType = t[0];
        return r;
    }
    sealed class F32 : FunctionWithSingleArg { public static readonly F32 Instance = new(); public override object Calc(object a) => MathF.Ceiling((float)a); }
    sealed class F64 : FunctionWithSingleArg { public static readonly F64 Instance = new(); public override object Calc(object a) => Math.Ceiling((double)a); }
    sealed class Decimal : FunctionWithSingleArg { public static readonly Decimal Instance = new(); public override object Calc(object a) => decimal.Ceiling((decimal)a); }
}

public class FloorFunction : PureGenericFunctionBase {
    public FloorFunction() : base("floor", GenericConstrains.Floats, 1) { ArgProperties = FunArgProperty.FromNames("x"); }
    public override IConcreteFunction CreateConcrete(FunnyType[] t, IFunctionSelectorContext ctx) {
        FunctionWithSingleArg r = t[0].BaseType switch {
            BaseFunnyType.Float32 => F32.Instance,
            BaseFunnyType.Real    => ctx.RealTypeSelect<FunctionWithSingleArg>(F64.Instance, Decimal.Instance),
            _ => throw new NFunImpossibleException("Unsupported type for floor")
        };
        r.Name = "floor"; r.ArgTypes = t; r.ReturnType = t[0];
        return r;
    }
    sealed class F32 : FunctionWithSingleArg { public static readonly F32 Instance = new(); public override object Calc(object a) => MathF.Floor((float)a); }
    sealed class F64 : FunctionWithSingleArg { public static readonly F64 Instance = new(); public override object Calc(object a) => Math.Floor((double)a); }
    sealed class Decimal : FunctionWithSingleArg { public static readonly Decimal Instance = new(); public override object Calc(object a) => decimal.Floor((decimal)a); }
}

public class RoundFunction : GenericFunctionBase {
    public RoundFunction() : base("round", GenericConstrains.Floats, FunnyType.Generic(0), FunnyType.Generic(0), FunnyType.Int32)
        { ArgProperties = FunArgProperty.FromNames("value", "digits"); }
    public override IConcreteFunction CreateConcrete(FunnyType[] t, IFunctionSelectorContext ctx) {
        // AwayFromZero per Bug FF — matches format-specifier rounding.
        FunctionWithTwoArgs r = t[0].BaseType switch {
            BaseFunnyType.Float32 => F32.Instance,
            BaseFunnyType.Real    => ctx.RealTypeSelect<FunctionWithTwoArgs>(F64.Instance, Decimal.Instance),
            _ => throw new NFunImpossibleException("Unsupported type for round")
        };
        r.Name = "round"; r.ArgTypes = new[] { t[0], FunnyType.Int32 }; r.ReturnType = t[0];
        return r;
    }
    sealed class F32 : FunctionWithTwoArgs { public static readonly F32 Instance = new(); public override object Calc(object a, object b) => MathF.Round((float)a, (int)b, MidpointRounding.AwayFromZero); }
    sealed class F64 : FunctionWithTwoArgs { public static readonly F64 Instance = new(); public override object Calc(object a, object b) => Math.Round((double)a, (int)b, MidpointRounding.AwayFromZero); }
    sealed class Decimal : FunctionWithTwoArgs { public static readonly Decimal Instance = new(); public override object Calc(object a, object b) => Math.Round((decimal)a, (int)b, MidpointRounding.AwayFromZero); }
}

public class AverageFunction : GenericFunctionBase {
    public AverageFunction() : base("avg", GenericConstrains.Floats, FunnyType.Generic(0), FunnyType.ArrayOf(FunnyType.Generic(0)))
        { ArgProperties = FunArgProperty.FromNames("arr"); }
    public override IConcreteFunction CreateConcrete(FunnyType[] t, IFunctionSelectorContext ctx) {
        FunctionWithSingleArg r = t[0].BaseType switch {
            BaseFunnyType.Float32 => F32.Instance,
            BaseFunnyType.Real    => ctx.RealTypeSelect<FunctionWithSingleArg>(F64.Instance, Decimal.Instance),
            _ => throw new NFunImpossibleException("Unsupported type for avg")
        };
        r.Name = "avg"; r.ArgTypes = new[] { FunnyType.ArrayOf(t[0]) }; r.ReturnType = t[0];
        return r;
    }
    sealed class F32 : FunctionWithSingleArg {
        public static readonly F32 Instance = new();
        public override object Calc(object a) {
            var arr = (IFunnyArray)a;
            if (arr.Count == 0) throw new FunnyRuntimeException("Array is empty");
            return arr.As<float>().Average();
        }
    }
    sealed class F64 : FunctionWithSingleArg {
        public static readonly F64 Instance = new();
        public override object Calc(object a) {
            var arr = (IFunnyArray)a;
            if (arr.Count == 0) throw new FunnyRuntimeException("Array is empty");
            return arr.As<double>().Average();
        }
    }
    sealed class Decimal : FunctionWithSingleArg {
        public static readonly Decimal Instance = new();
        public override object Calc(object a) {
            var arr = (IFunnyArray)a;
            if (arr.Count == 0) throw new FunnyRuntimeException("Array is empty");
            return arr.As<decimal>().Average();
        }
    }
}

public class DivideFunction : PureGenericFunctionBase {
    public DivideFunction() : base(CoreFunNames.DivideReal, GenericConstrains.Floats, 2) { }
    public override IConcreteFunction CreateConcrete(FunnyType[] t, IFunctionSelectorContext ctx) {
        FunctionWithTwoArgs r = t[0].BaseType switch {
            BaseFunnyType.Float32 => F32.Instance,
            BaseFunnyType.Real    => ctx.RealTypeSelect<FunctionWithTwoArgs>(F64.Instance, Decimal.Instance),
            _ => throw new NFunImpossibleException("Unsupported type for /")
        };
        r.Name = CoreFunNames.DivideReal; r.ArgTypes = new[] { t[0], t[0] }; r.ReturnType = t[0];
        return r;
    }
    sealed class F32 : FunctionWithTwoArgs { public static readonly F32 Instance = new(); public override object Calc(object a, object b) => (float)a / (float)b; }
    sealed class F64 : FunctionWithTwoArgs { public static readonly F64 Instance = new(); public override object Calc(object a, object b) => (double)a / (double)b; }
    sealed class Decimal : FunctionWithTwoArgs { public static readonly Decimal Instance = new(); public override object Calc(object a, object b) => (decimal)a / (decimal)b; }
}
