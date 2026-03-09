using NFun.Exceptions;
using NFun.Interpretation.Functions;

namespace NFun.Functions;

/// <summary>
/// Null coalesce operator: (T?, T) -> T
/// Returns left if not none, otherwise returns right.
/// </summary>
public class NullCoalesceFunction : GenericFunctionBase {
    public NullCoalesceFunction()
        : base(
            CoreFunNames.NullCoalesce,
            GenericConstrains.Any,
            FunnyType.Generic(0),
            FunnyType.OptionalOf(FunnyType.Generic(0)), FunnyType.Generic(0)) { }

    protected override object Calc(object[] args) => args[0] ?? args[1];
}

/// <summary>
/// Force unwrap operator: (T?) -> T
/// Returns unwrapped value or throws if none.
/// </summary>
public class ForceUnwrapFunction : GenericFunctionBase {
    public ForceUnwrapFunction()
        : base(
            CoreFunNames.ForceUnwrap,
            GenericConstrains.Any,
            FunnyType.Generic(0),
            FunnyType.OptionalOf(FunnyType.Generic(0))) { }

    protected override object Calc(object[] args) =>
        args[0] ?? throw new FunnyRuntimeException("Force unwrap of none value");
}
