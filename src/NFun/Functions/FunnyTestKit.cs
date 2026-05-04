namespace NFun.Functions;

/// <summary>
/// Test toolkit for NFun scripts. Registers assert/assertEqual/assertType functions.
/// Usage: Funny.Hardcore.WithTestKit().BuildLang(script)
/// </summary>
public static class FunnyTestKit {
    /// <summary>Add test functions: assert, assertEqual, assertNotEqual, assertType.</summary>
    public static HardcoreBuilder WithTestKit(this HardcoreBuilder builder) =>
        builder
            .WithFunction(AssertFunction.Instance)
            .WithFunction(AssertWithMessageFunction.Instance)
            .WithFunction(new AssertEqualFunction())
            .WithFunction(new AssertNotEqualFunction())
            .WithFunction(new AssertTypeFunction());
}
