namespace NFun {

public static class Dialects {
    public static DialectSettings Origin => DialectSettings.Default;

    public static DialectSettings ModifyOrigin(
        IfExpressionSetup ifExpressionSetup = IfExpressionSetup.IfIfElse,
        IntegerPreferredType integerPreferredType = IntegerPreferredType.I32)
        => new(ifExpressionSetup, integerPreferredType);
}

public sealed class DialectSettings {
    internal static DialectSettings Default { get; } =
        new(IfExpressionSetup.IfIfElse, IntegerPreferredType.I32);

    public DialectSettings(IfExpressionSetup ifExpressionSetup, IntegerPreferredType integerPreferredType) {
        IfExpressionSetup = ifExpressionSetup;
        IntegerPreferredType = integerPreferredType;
    }

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