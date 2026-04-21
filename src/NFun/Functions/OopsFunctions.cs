using NFun.Exceptions;
using NFun.Interpretation.Functions;

namespace NFun.Functions;

public class OopsFunction0 : GenericFunctionBase {
    public OopsFunction0()
        : base("oops", GenericConstrains.Any, FunnyType.Generic(0)) { }

    protected override object Calc(object[] args) =>
        throw new FunnyRuntimeException("oops");
}

public class OopsFunction1 : GenericFunctionBase {
    public OopsFunction1()
        : base("oops", GenericConstrains.Any, FunnyType.Generic(0), FunnyType.Text) { }

    protected override object Calc(object[] args) =>
        throw new FunnyRuntimeException(args[0]?.ToString() ?? "oops");
}

public class OopsFunction2 : GenericFunctionBase {
    public OopsFunction2()
        : base("oops", GenericConstrains.Any, FunnyType.Generic(0), FunnyType.Text, FunnyType.Any) { }

    protected override object Calc(object[] args) =>
        throw new FunnyRuntimeException(args[0]?.ToString() ?? "oops", args[1]);
}
