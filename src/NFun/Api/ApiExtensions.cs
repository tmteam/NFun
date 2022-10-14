namespace NFun; 

public static class ApiExtensions {
    public static T RealTypeSelect<T>(this IFunctionSelectorContext context, T ifIsDouble, T ifIsDecimal) 
        => context.Converter.TypeBehaviour.RealTypeSelect(ifIsDouble, ifIsDecimal);
}