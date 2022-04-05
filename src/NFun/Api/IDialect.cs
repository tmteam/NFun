using NFun.Interpretation.Functions;
using NFun.Types;

namespace NFun {

internal static class Dialects {
    public static DialectSettings Origin { get; } 
        = new(
            ifExpressionSetup: IfExpressionSetup.IfIfElse, 
            integerPreferredType: IntegerPreferredType.I32, 
            typeBehaviour: TypeBehaviour.RealIsDouble, 
            allowIntegerOverflow: false);
    
    public static DialectSettings ModifyOrigin(
        IfExpressionSetup ifExpressionSetup = IfExpressionSetup.IfIfElse,
        IntegerPreferredType integerPreferredType = IntegerPreferredType.I32,
        RealClrType realClrType = RealClrType.IsDouble, 
        IntegerOverflow integerOverflow = IntegerOverflow.Checked)
        => new(ifExpressionSetup, integerPreferredType,
            realClrType == RealClrType.IsDouble
                ? TypeBehaviour.RealIsDouble
                : TypeBehaviour.RealIsDecimal,
            integerOverflow == IntegerOverflow.Unchecked);
}


public interface IFunctionSelectorContext {
    T RealTypeSelect<T>(T ifIsDouble, T ifIsDecimal);
    TypeBehaviour TypeBehaviour { get; }
    bool AllowIntegerOverflow { get; }
}

public sealed class DialectSettings : IFunctionSelectorContext {
    internal DialectSettings(IfExpressionSetup ifExpressionSetup, IntegerPreferredType integerPreferredType, TypeBehaviour typeBehaviour, bool allowIntegerOverflow) {
        IfExpressionSetup = ifExpressionSetup;
        IntegerPreferredType = integerPreferredType;
        TypeBehaviour = typeBehaviour;
        AllowIntegerOverflow = allowIntegerOverflow;
    }
    public T RealTypeSelect<T>(T ifIsDouble, T ifIsDecimal) => TypeBehaviour.RealTypeSelect(ifIsDouble, ifIsDecimal);
    public TypeBehaviour TypeBehaviour { get; }
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

}