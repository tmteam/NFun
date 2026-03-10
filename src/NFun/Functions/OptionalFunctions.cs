using NFun.Exceptions;
using NFun.Interpretation.Functions;
using NFun.Types;

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

    protected override object Calc(object[] args) =>
        args[0] is FunnyNone ? args[1] : args[0];
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
        args[0] is FunnyNone
            ? throw new FunnyRuntimeException("Force unwrap of none value")
            : args[0];
}
