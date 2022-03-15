using System;
using NFun.Types;

namespace NFun.Exceptions {

public class FunnyInvalidUsageException : Exception {
    public static FunnyInvalidUsageException OutputTypeConstainsNoParameterlessCtor(Type type)
        => new($"Output type '{type.Name}' contains no parameterless constructor");

    public static FunnyInvalidUsageException DecimalTypeCannotBeUsedAsOutput()
        => new($"Decimal cannot be used as output if real maps to other type (it does not make sense). Use 'real is decimal' dialect settings");

    public static FunnyInvalidUsageException InputTypeCannotBeConverted(Type clrType, FunnyType type)
        => new($"Clr type {clrType.Name} cannot be used as input funny type {type}");

    private FunnyInvalidUsageException(string message) : base(message) { }
}

}