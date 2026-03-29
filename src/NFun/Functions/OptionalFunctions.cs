using NFun.Exceptions;
using NFun.Interpretation.Functions;
using NFun.Runtime.Arrays;
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
            FunnyType.OptionalOf(FunnyType.Generic(0)), FunnyType.Generic(0)) {
        ArgProperties = new[] {
            new FunArgProperty { Name = "a" },
            new FunArgProperty { Name = "b", IsLazy = true },
        };
    }

    protected override object Calc(object[] args) =>
        args[0] is FunnyNone ? ((ILazyFunnyValue)args[1]).Calc() : args[0];
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

/// <summary>
/// Safe array index: (T[]?, int) -> T?
/// Returns element if array is not none, otherwise returns none.
/// </summary>
public class SafeGetElementFunction : GenericFunctionBase {
    public SafeGetElementFunction()
        : base(
            CoreFunNames.SafeGetElementName,
            GenericConstrains.Any,
            FunnyType.OptionalOf(FunnyType.Generic(0)),
            FunnyType.OptionalOf(FunnyType.ArrayOf(FunnyType.Generic(0))),
            FunnyType.Int32) { }

    protected override object Calc(object[] args) {
        if (args[0] is FunnyNone)
            return FunnyNone.Instance;
        var arr = (IFunnyArray)args[0];
        var index = (int)args[1];
        if (index < 0 || index >= arr.Count)
            throw new FunnyRuntimeException("Argument out of range");
        return arr.GetElementOrNull(index) ?? throw new FunnyRuntimeException("Argument out of range");
    }
}
