namespace NFun.Interpretation.Functions;

/// <summary>Metadata for a single function parameter (name, default, params).</summary>
public readonly struct FunArgProperty {
    public string Name { get; init; }
    public bool HasDefault { get; init; }
    public object DefaultValue { get; init; }
    public bool IsParams { get; init; }
    /// <summary>When true, argument is passed as ILazyFunnyValue instead of computed value.</summary>
    public bool IsLazy { get; init; }

    /// <summary>Creates ArgProperties from argument names (no defaults, no params).</summary>
    public static FunArgProperty[] FromNames(params string[] names) {
        var result = new FunArgProperty[names.Length];
        for (int i = 0; i < names.Length; i++)
            result[i] = new FunArgProperty { Name = names[i] };
        return result;
    }
}
