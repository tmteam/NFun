using NFun.Interpretation.Functions;
using NFun.Types;

namespace NFun {

internal static class Dialects {
    public static DialectSettings Origin => DialectSettings.Default;
    public static DialectSettings ModifyOrigin(
        IfExpressionSetup ifExpressionSetup = IfExpressionSetup.IfIfElse,
        IntegerPreferredType integerPreferredType = IntegerPreferredType.I32,
        RealClrType realClrType = RealClrType.IsDouble, 
        IntegerOverflow integerOverflow = IntegerOverflow.Unchecked)
        => new(ifExpressionSetup, integerPreferredType,
            realClrType == RealClrType.IsDouble
                ? TypeBehaviour.RealIsDouble(integerOverflow == IntegerOverflow.Unchecked)
                : TypeBehaviour.RealIsDecimal(integerOverflow == IntegerOverflow.Unchecked));
}

public sealed class DialectSettings {
    internal static DialectSettings Default { get; } =
        new(IfExpressionSetup.IfIfElse, IntegerPreferredType.I32, TypeBehaviour.Default);

    internal DialectSettings(IfExpressionSetup ifExpressionSetup, IntegerPreferredType integerPreferredType, TypeBehaviour typeBehaviour) {
        IfExpressionSetup = ifExpressionSetup;
        IntegerPreferredType = integerPreferredType;
        TypeBehaviour = typeBehaviour;
    }
    public TypeBehaviour TypeBehaviour { get; }
    public IfExpressionSetup IfExpressionSetup { get; }
    public IntegerPreferredType IntegerPreferredType { get; }
}

public enum IntegerPreferredType {
    Real,
    I32,
    I64
}

public enum IfExpressionSetup {
    Deny,
    IfIfElse,
    IfElseIf
}

public enum IntegerOverflow {
    /// <summary>
    /// Allow integer overflow
    /// </summary>
    Unchecked,
    /// <summary>
    /// Integer overflow causes runtime exception
    /// </summary>
    Checked
}

}