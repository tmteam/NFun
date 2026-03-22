using NFun.Types;

namespace NFun;

internal static class Dialects {
    public static DialectSettings Origin { get; }
        = new(
            ifExpressionSetup: IfExpressionSetup.IfIfElse,
            integerPreferredType: IntegerPreferredType.I32,
            funnyConverter: FunnyConverter.RealIsDouble,
            allowIntegerOverflow: false,
            allowUserFunctions: AllowUserFunctions.AllowAll,
            optionalTypesSupport: OptionalTypesSupport.Disabled);

    public static DialectSettings ModifyOrigin(
        IfExpressionSetup ifExpressionSetup = IfExpressionSetup.IfIfElse,
        IntegerPreferredType integerPreferredType = IntegerPreferredType.I32,
        RealClrType realClrType = RealClrType.IsDouble,
        IntegerOverflow integerOverflow = IntegerOverflow.Checked,
        AllowUserFunctions allowUserFunctions = AllowUserFunctions.AllowAll,
        OptionalTypesSupport optionalTypesSupport = OptionalTypesSupport.Disabled,
        AllowNewlineInStrings allowNewlineInStrings = AllowNewlineInStrings.Allow
        )
        => new(
            ifExpressionSetup,
            integerPreferredType,
            realClrType == RealClrType.IsDouble
                ? FunnyConverter.RealIsDouble
                : FunnyConverter.RealIsDecimal,
            integerOverflow == IntegerOverflow.Unchecked,
            allowUserFunctions,
            optionalTypesSupport,
            allowNewlineInStrings);
}


public interface IFunctionSelectorContext {
    FunnyConverter Converter { get; }
    bool AllowIntegerOverflow { get; }
}

internal sealed class DialectSettings : IFunctionSelectorContext {
    internal DialectSettings(IfExpressionSetup ifExpressionSetup, IntegerPreferredType integerPreferredType, FunnyConverter funnyConverter, bool allowIntegerOverflow, AllowUserFunctions allowUserFunctions, OptionalTypesSupport optionalTypesSupport = OptionalTypesSupport.Disabled, AllowNewlineInStrings allowNewlineInStrings = AllowNewlineInStrings.Allow) {
        IfExpressionSetup = ifExpressionSetup;
        IntegerPreferredType = integerPreferredType;
        Converter = funnyConverter;
        AllowIntegerOverflow = allowIntegerOverflow;
        AllowUserFunctions = allowUserFunctions;
        OptionalTypesSupport = optionalTypesSupport;
        AllowNewlineInStrings = allowNewlineInStrings;
    }
    public FunnyConverter Converter { get; }
    public IfExpressionSetup IfExpressionSetup { get; }
    public IntegerPreferredType IntegerPreferredType { get; }
    public bool AllowIntegerOverflow { get; }
    public AllowUserFunctions AllowUserFunctions { get; }
    public OptionalTypesSupport OptionalTypesSupport { get; }
    public AllowNewlineInStrings AllowNewlineInStrings { get; }
}

public enum AllowUserFunctions {
    AllowAll,
    DenyRecursive,
    DenyUserFunctions
}

public enum OptionalTypesSupport {
    Disabled,
    ExperimentalEnabled
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
    /// AllowAll integer overflow
    /// </summary>
    Unchecked,
    /// <summary>
    /// Integer overflow causes runtime exception
    /// </summary>
    Checked
}

public enum AllowNewlineInStrings {
    /// <summary>
    /// Raw newline characters are allowed inside string literals (default, backward compatible)
    /// </summary>
    Allow,
    /// <summary>
    /// Raw newline characters in string literals cause a parse error. Escaped \n and \r are still allowed.
    /// </summary>
    Deny
}
