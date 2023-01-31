namespace NFun.Exceptions;

internal static class AssertChecks {
    public static void Panic(string message) => throw new NFunImpossibleException(message);

    public static T NotNull<T>(this T item, string message) where T : class {
        if(item==null)
            throw new NFunImpossibleException(message);
        return item;
    }
}
