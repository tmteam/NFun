using NFun.Exceptions;
using NFun.Interpretation.Functions;

namespace NFun.Functions;

/// <summary>
/// Test-only function that always throws FunnyRuntimeException.
/// Used to verify lazy evaluation — if this function is never called,
/// no exception is thrown.
/// </summary>
public class ThrowErrorFunction : GenericFunctionBase {
    public ThrowErrorFunction()
        : base("___throwError", GenericConstrains.Any, FunnyType.Generic(0)) { }

    protected override object Calc(object[] args) =>
        throw new FunnyRuntimeException("___throwError() was called");
}
