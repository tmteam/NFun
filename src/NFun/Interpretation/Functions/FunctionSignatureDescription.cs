using NFun.Types;

namespace NFun.Interpretation.Functions;

/// <summary>
/// Declarative description of a built-in function signature. Carried by the
/// descriptor-style constructor on <see cref="GenericFunctionBase"/> and
/// <see cref="FunctionWithManyArguments"/>. Designed to grow (future
/// metadata, additional dispatch hints) without churning every constructor.
/// </summary>
public sealed class FunctionSignatureDescription {
    public string Name { get; }
    public FunnyType OutputType { get; }
    public FunnyType[] InputTypes { get; }
    /// <summary>
    /// True when the function is reachable only via piped syntax (<c>arr.f()</c>)
    /// under <see cref="ExtensionFunctionsSeparation.Enabled"/>. Default
    /// <c>false</c> — function is bi-callable.
    /// </summary>
    public bool IsExtension { get; }
    /// <summary>
    /// Optional explicit generic constraints. When null, the base constructor
    /// auto-fills <see cref="GenericConstrains.Any"/> for every generic slot.
    /// Only meaningful for generic functions.
    /// </summary>
    public GenericConstrains[] Constrains { get; }
    /// <summary>
    /// Optional per-arg metadata: names, defaults, <c>IsParams</c> (varargs),
    /// <c>IsLazy</c>. When provided, must have <c>Length == InputTypes.Length</c>.
    /// Built-ins that need default values or varargs declare them here; simple
    /// extensions pass <see cref="ArgNames"/> instead for a name-only shortcut.
    /// </summary>
    public FunArgProperty[] ArgProperties { get; }

    public FunctionSignatureDescription(
        string name,
        FunnyType outputType,
        FunnyType[] inputTypes,
        bool isExtension = false,
        GenericConstrains[] constrains = null,
        FunArgProperty[] argProperties = null,
        string[] argNames = null) {
        Name = name;
        OutputType = outputType;
        InputTypes = inputTypes;
        IsExtension = isExtension;
        Constrains = constrains;
        // Resolve ArgProperties: explicit `argProperties` wins, else build from `argNames` shortcut.
        if (argProperties != null)
            ArgProperties = argProperties;
        else if (argNames != null)
            ArgProperties = FunArgProperty.FromNames(argNames);
    }
}
