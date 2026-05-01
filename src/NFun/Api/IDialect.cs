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
        AllowNewlineInStrings allowNewlineInStrings = AllowNewlineInStrings.Allow,
        NamedTypesSupport namedTypesSupport = NamedTypesSupport.Disabled,
        TryCatchSupport tryCatchSupport = TryCatchSupport.Enabled
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
            allowNewlineInStrings,
            namedTypesSupport,
            tryCatchSupport);
}


public interface IFunctionSelectorContext {
    FunnyConverter Converter { get; }
    bool AllowIntegerOverflow { get; }
}

internal sealed class DialectSettings : IFunctionSelectorContext {
    internal DialectSettings(
        IfExpressionSetup ifExpressionSetup,
        IntegerPreferredType integerPreferredType,
        FunnyConverter funnyConverter,
        bool allowIntegerOverflow,
        AllowUserFunctions allowUserFunctions,
        OptionalTypesSupport optionalTypesSupport = OptionalTypesSupport.Disabled,
        AllowNewlineInStrings allowNewlineInStrings = AllowNewlineInStrings.Allow,
        NamedTypesSupport namedTypesSupport = NamedTypesSupport.Disabled,
        TryCatchSupport tryCatchSupport = TryCatchSupport.Enabled) {
        IfExpressionSetup = ifExpressionSetup;
        IntegerPreferredType = integerPreferredType;
        Converter = funnyConverter;
        AllowIntegerOverflow = allowIntegerOverflow;
        AllowUserFunctions = allowUserFunctions;
        OptionalTypesSupport = optionalTypesSupport;
        AllowNewlineInStrings = allowNewlineInStrings;
        NamedTypesSupport = namedTypesSupport;
        TryCatchSupport = tryCatchSupport;
    }
    public FunnyConverter Converter { get; }
    public IfExpressionSetup IfExpressionSetup { get; }
    public IntegerPreferredType IntegerPreferredType { get; }
    public bool AllowIntegerOverflow { get; }
    public AllowUserFunctions AllowUserFunctions { get; }
    public OptionalTypesSupport OptionalTypesSupport { get; }
    public AllowNewlineInStrings AllowNewlineInStrings { get; }
    public NamedTypesSupport NamedTypesSupport { get; }
    public TryCatchSupport TryCatchSupport { get; }
}

/// <summary>
/// User-defined and recursive functions support.
/// </summary>
public enum AllowUserFunctions {
    /// <summary>All user functions allowed, including recursive</summary>
    AllowAll,
    /// <summary>User functions allowed, but recursion is denied</summary>
    DenyRecursive,
    /// <summary>No user function definitions allowed</summary>
    DenyUserFunctions
}

/// <summary>
/// Optional types (T?, none, ??, ?., ?[, !) support.
/// Enables nullable value semantics with type narrowing.
/// </summary>
public enum OptionalTypesSupport {
    /// <summary>Optional types disabled — none literal and ?/?? operators are not available</summary>
    Disabled,
    /// <summary>Optional types enabled — T?, none, ??, ?., ?[, !, type narrowing</summary>
    Enabled
}

/// <summary>
/// Named type definitions (type aliases and struct types).
/// Enables: type age = int, type point = {x:int, y:int}, recursive types.
/// </summary>
public enum NamedTypesSupport {
    /// <summary>Named type definitions disabled</summary>
    Disabled,
    /// <summary>Named type definitions enabled — type aliases, struct types, recursive types</summary>
    Enabled
}

/// <summary>
/// Integer literal preferred resolution type.
/// Determines what type untyped integer literals (1, 42, etc.) resolve to
/// when not constrained by context.
/// </summary>
public enum IntegerPreferredType {
    /// <summary>Integer literals prefer Real (double) — widest numeric type</summary>
    Real,
    /// <summary>Integer literals prefer Int32 — default, matches C#/Java behavior</summary>
    I32,
    /// <summary>Integer literals prefer Int64</summary>
    I64
}

/// <summary>
/// If-expression syntax style.
/// </summary>
public enum IfExpressionSetup {
    /// <summary>If-expressions are not allowed</summary>
    Deny,
    /// <summary>if(c) a if(c2) b else c — elif chain style (default)</summary>
    IfIfElse,
    /// <summary>if(c) a elseif(c2) b else c — elseif keyword style</summary>
    IfElseIf
}

/// <summary>
/// Integer arithmetic overflow behavior.
/// </summary>
public enum IntegerOverflow {
    /// <summary>Integer overflow wraps silently (unchecked arithmetic)</summary>
    Unchecked,
    /// <summary>Integer overflow throws runtime exception (default)</summary>
    Checked
}

/// <summary>
/// Raw newline handling in string literals.
/// </summary>
public enum AllowNewlineInStrings {
    /// <summary>Raw newline characters allowed inside string literals (default)</summary>
    Allow,
    /// <summary>Raw newlines cause parse error. Escaped \n and \r still allowed.</summary>
    Deny
}

/// <summary>
/// Try-catch error handling support.
/// When disabled, try/catch expressions are parse errors.
/// </summary>
public enum TryCatchSupport {
    /// <summary>Try-catch expressions enabled (default)</summary>
    Enabled,
    /// <summary>Try-catch expressions disabled — parse error if used</summary>
    Disabled
}
