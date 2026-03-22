namespace NFun.Types;

/// <summary>
/// User-defined custom type definition. Immutable singleton describing a type (not a value).
/// Implement this interface to register custom types with NFun.
/// </summary>
public interface IFunnyCustomTypeDefinition {
    /// <summary>Type name usable in scripts (e.g. "my_type")</summary>
    string Name { get; }

    /// <summary>Default non-null value for this type</summary>
    object DefaultValue { get; }

    /// <summary>Value equality check</summary>
    bool Equals(object a, object b);

    /// <summary>Text representation of a value</summary>
    string ToText(object value);
}
