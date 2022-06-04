using NFun.Interpretation.Functions;

namespace NFun.Functions; 

#region binaries

public class NotFunction : FunctionWithSingleArg {
    public NotFunction() : base(CoreFunNames.Not, FunnyType.Bool, FunnyType.Bool) { }
    public override object Calc(object a) => !(bool)a;
}

public class AndFunction : FunctionWithTwoArgs {
    public AndFunction() : base(CoreFunNames.And, FunnyType.Bool, FunnyType.Bool, FunnyType.Bool) { }
    public override object Calc(object a, object b) => (bool)a && (bool)b;
}

public class OrFunction : FunctionWithTwoArgs {
    public OrFunction() : base(CoreFunNames.Or, FunnyType.Bool, FunnyType.Bool, FunnyType.Bool) { }
    public override object Calc(object a, object b) => (bool)a || (bool)b;
}

public class XorFunction : FunctionWithTwoArgs {
    public XorFunction() : base(CoreFunNames.Xor, FunnyType.Bool, FunnyType.Bool, FunnyType.Bool) { }
    public override object Calc(object a, object b) => (bool)a ^ (bool)b;
}

#endregion