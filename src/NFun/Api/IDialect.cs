namespace NFun
{
    public static class Dialects
    {
        public static ClassicDialectSettings Classic  => ClassicDialectSettings.Default;
        public static ClassicDialectSettings ModifyClassic(
            IfExpressionSetup ifExpressionSetup = IfExpressionSetup.IfIfElse, 
            IntegerPreferedType integerPreferedType = IntegerPreferedType.Real)
                =>  new (ifExpressionSetup, integerPreferedType);
    }
    public class ClassicDialectSettings
    {
        public static ClassicDialectSettings Default { get; } =
            new(IfExpressionSetup.IfIfElse, IntegerPreferedType.Real);

        public ClassicDialectSettings(IfExpressionSetup ifExpressionSetup, IntegerPreferedType integerPreferedType)
        {
            IfExpressionSetup = ifExpressionSetup;
            IntegerPreferedType = integerPreferedType;
        }

        public IfExpressionSetup IfExpressionSetup { get; }
        public IntegerPreferedType IntegerPreferedType { get; }
    }

    public enum IntegerPreferedType
    {
        Real,
        Int32,
        Int64
    }

    public enum IfExpressionSetup
    {
        Deny,
        IfIfElse,
        IfElseIf
    }
}