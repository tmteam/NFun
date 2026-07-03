namespace NFun.Types;

/// <summary>
/// Payload for <see cref="BaseFunnyType.FixedArray"/> — carries the element
/// type of a lang-mode <c>fixedArray&lt;T&gt;</c>. Parallels
/// <see cref="ListTypeSpecification"/> / <see cref="MutableArrayTypeSpecification"/> /
/// <see cref="ArrayTypeSpecification"/>; kept distinct so pattern matches on
/// container kind stay unambiguous.
/// </summary>
public class FixedArrayTypeSpecification {
    public readonly FunnyType FunnyType;

    public FixedArrayTypeSpecification(FunnyType funnyType) => FunnyType = funnyType;

    private bool Equals(FixedArrayTypeSpecification other) => FunnyType.Equals(other.FunnyType);

    public override bool Equals(object obj)
    {
        if (obj == null) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != GetType()) return false;
        return Equals((FixedArrayTypeSpecification)obj);
    }

    public override int GetHashCode() => FunnyType.GetHashCode();
}
