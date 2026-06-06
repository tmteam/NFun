namespace NFun.Types;

/// <summary>
/// Payload for <see cref="BaseFunnyType.Map"/> — carries the key and value
/// types of a lang-mode <c>map&lt;K, V&gt;</c>. Both invariant.
/// </summary>
public class MapTypeSpecification {
    public readonly FunnyType KeyType;
    public readonly FunnyType ValueType;

    public MapTypeSpecification(FunnyType keyType, FunnyType valueType) {
        KeyType = keyType;
        ValueType = valueType;
    }

    private bool Equals(MapTypeSpecification other) =>
        KeyType.Equals(other.KeyType) && ValueType.Equals(other.ValueType);

    public override bool Equals(object obj)
    {
        if (obj == null) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != GetType()) return false;
        return Equals((MapTypeSpecification)obj);
    }

    public override int GetHashCode() {
        unchecked {
            return (KeyType.GetHashCode() * 397) ^ ValueType.GetHashCode();
        }
    }
}
