using System;
using NFun.Interpretation.Functions;
using NFun.Types;

namespace NFun.VM;

/// <summary>
/// delayed(milliseconds:int, value) → value after delay.
/// In VM context: suspends current fiber for the specified duration.
/// In tree-walker context: Thread.Sleep fallback.
/// </summary>
public class DelayedFunction : FunctionWithTwoArgs {
    public static readonly DelayedFunction Instance = new();

    public DelayedFunction() : base("delayed", FunnyType.Any, FunnyType.Int32, FunnyType.Any) { }

    public override object Calc(object a, object b) {
        var ms = (int)a;
        // In tree-walker context: blocking sleep
        System.Threading.Thread.Sleep(ms);
        return b;
    }
}

/// <summary>
/// Register VM-aware custom functions.
/// </summary>
public static class VMFunctions {
    /// <summary>Add delayed() and other VM functions to the builder.</summary>
    public static HardcoreBuilder WithVMFunctions(this HardcoreBuilder builder) =>
        builder.WithFunction(DelayedFunction.Instance);
}
