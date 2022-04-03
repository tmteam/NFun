namespace NFun.Exceptions {

internal static class AssertChecks {
    public static void Panic(string message) => throw new NFunImpossibleException(message);
}
}