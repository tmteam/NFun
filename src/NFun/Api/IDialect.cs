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
        TryCatchSupport tryCatchSupport = TryCatchSupport.Enabled,
        ExtensionFunctionsSeparation extensionFunctionsSeparation = ExtensionFunctionsSeparation.Disabled,
        FloatFamilySupport floatFamilySupport = FloatFamilySupport.None
        ) {
        // Reject incompatible combo: Decimal-backed Real + IEEE float family.
        if (realClrType == RealClrType.IsDecimal && floatFamilySupport == FloatFamilySupport.Float32AndFloat64)
            throw new System.ArgumentException(
                "FloatFamilySupport.Float32AndFloat64 is incompatible with RealClrType.IsDecimal. " +
                "Either set FloatFamilySupport=None to keep decimal Real, or use RealClrType.IsDouble " +
                "to enable IEEE float32/float64 keywords.");

        var converter = (realClrType, floatFamilySupport) switch {
            (RealClrType.IsDouble,  FloatFamilySupport.None)              => FunnyConverter.RealIsDouble,
            (RealClrType.IsDouble,  FloatFamilySupport.Float32AndFloat64) => FunnyConverter.RealIsDoubleWithFloatFamily,
            (RealClrType.IsDecimal, FloatFamilySupport.None)              => FunnyConverter.RealIsDecimal,
            _ => throw new System.InvalidOperationException("Unreachable")
        };

        return new(
            ifExpressionSetup,
            integerPreferredType,
            converter,
            integerOverflow == IntegerOverflow.Unchecked,
            allowUserFunctions,
            optionalTypesSupport,
            allowNewlineInStrings,
            namedTypesSupport,
            tryCatchSupport,
            extensionFunctionsSeparation,
            floatFamilySupport);
    }
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
        TryCatchSupport tryCatchSupport = TryCatchSupport.Enabled,
        ExtensionFunctionsSeparation extensionFunctionsSeparation = ExtensionFunctionsSeparation.Disabled,
        FloatFamilySupport floatFamilySupport = FloatFamilySupport.None) {
        IfExpressionSetup = ifExpressionSetup;
        IntegerPreferredType = integerPreferredType;
        Converter = funnyConverter;
        AllowIntegerOverflow = allowIntegerOverflow;
        AllowUserFunctions = allowUserFunctions;
        OptionalTypesSupport = optionalTypesSupport;
        AllowNewlineInStrings = allowNewlineInStrings;
        NamedTypesSupport = namedTypesSupport;
        TryCatchSupport = tryCatchSupport;
        ExtensionFunctionsSeparation = extensionFunctionsSeparation;
        FloatFamilySupport = floatFamilySupport;
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
    public ExtensionFunctionsSeparation ExtensionFunctionsSeparation { get; }
    public FloatFamilySupport FloatFamilySupport { get; }
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

/// <summary>
/// Extension function namespace separation.
/// When enabled, user functions defined with piped syntax (x.f() = expr) are extension functions
/// and can only be called via piped syntax (5.f()), while regular functions (f(x) = expr) can
/// only be called directly (f(5)). Built-in functions are unaffected — they always work via pipe.
/// </summary>
public enum ExtensionFunctionsSeparation {
    /// <summary>Piped calls and regular calls share the same namespace (default, current behavior)</summary>
    Disabled = 0,
    /// <summary>Extension functions (x.f() = expr) have a separate namespace from regular functions</summary>
    Enabled = 1
}

/// <summary>
/// Availability of the IEEE 754 float family keywords (`float32`, `float64`).
/// The `real` keyword is always available regardless of this setting.
/// </summary>
public enum FloatFamilySupport {
    /// <summary>`float32` and `float64` keywords are parse errors.</summary>
    None = 0,
    /// <summary>
    /// `float32` (System.Single) and `float64` (alias for `real`) keywords enabled.
    /// Incompatible with <see cref="RealClrType.IsDecimal"/>.
    /// </summary>
    Float32AndFloat64 = 1
}
