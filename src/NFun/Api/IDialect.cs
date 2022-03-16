using NFun.Interpretation.Functions;
using NFun.Types;

namespace NFun {

public static class Dialects {
    public static DialectSettings Origin => DialectSettings.Default;
    public static DialectSettings ModifyOrigin(
        IfExpressionSetup ifExpressionSetup = IfExpressionSetup.IfIfElse,
        IntegerPreferredType integerPreferredType = IntegerPreferredType.I32,
        RealClrType realClrType = RealClrType.IsDouble)
        => new(ifExpressionSetup, integerPreferredType,
            realClrType == RealClrType.IsDouble
                ? TypeBehaviour.RealIsDouble
                : TypeBehaviour.RealIsDecimal);
}

public sealed class DialectSettings {
    internal static DialectSettings Default { get; } =
        new(IfExpressionSetup.IfIfElse, IntegerPreferredType.I32, Types.TypeBehaviour.RealIsDouble);

    public DialectSettings(IfExpressionSetup ifExpressionSetup, IntegerPreferredType integerPreferredType, TypeBehaviour typeBehaviour) {
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

}