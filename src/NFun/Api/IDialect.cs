using NFun.Types;

namespace NFun; 

internal static class Dialects {
    public static DialectSettings Origin { get; } 
        = new(
            ifExpressionSetup: IfExpressionSetup.IfIfElse, 
            integerPreferredType: IntegerPreferredType.I32, 
            funnyConverter: FunnyConverter.RealIsDouble, 
            allowIntegerOverflow: false);
    
    public static DialectSettings ModifyOrigin(
        IfExpressionSetup ifExpressionSetup = IfExpressionSetup.IfIfElse,
        IntegerPreferredType integerPreferredType = IntegerPreferredType.I32,
        RealClrType realClrType = RealClrType.IsDouble, 
        IntegerOverflow integerOverflow = IntegerOverflow.Checked)
        => new(ifExpressionSetup, integerPreferredType,
            realClrType == RealClrType.IsDouble
                ? FunnyConverter.RealIsDouble
                : FunnyConverter.RealIsDecimal,
            integerOverflow == IntegerOverflow.Unchecked);
}


public interface IFunctionSelectorContext {
    T RealTypeSelect<T>(T ifIsDouble, T ifIsDecimal);
    FunnyConverter Converter { get; }
    bool AllowIntegerOverflow { get; }
}

public sealed class DialectSettings : IFunctionSelectorContext {
    internal DialectSettings(IfExpressionSetup ifExpressionSetup, IntegerPreferredType integerPreferredType, FunnyConverter funnyConverter, bool allowIntegerOverflow) {
        IfExpressionSetup = ifExpressionSetup;
        IntegerPreferredType = integerPreferredType;
        Converter = funnyConverter;
        AllowIntegerOverflow = allowIntegerOverflow;
    }
    public T RealTypeSelect<T>(T ifIsDouble, T ifIsDecimal) => Converter.TypeBehaviour.RealTypeSelect(ifIsDouble, ifIsDecimal);
    public FunnyConverter Converter { get; }
    public IfExpressionSetup IfExpressionSetup { get; }
    public IntegerPreferredType IntegerPreferredType { get; }
    public bool AllowIntegerOverflow { get; }
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