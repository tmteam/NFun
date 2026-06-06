namespace NFun.Types;

/// <summary>
/// Payload for <see cref="BaseFunnyType.MutableArray"/> — carries the element
/// type of a lang-mode <c>array&lt;T&gt;</c>. Parallels
/// <see cref="ListTypeSpecification"/> / <see cref="ArrayTypeSpecification"/>;
/// kept distinct so pattern matches on container kind stay unambiguous.
/// </summary>
public class MutableArrayTypeSpecification {
    public readonly FunnyType FunnyType;

    public MutableArrayTypeSpecification(FunnyType funnyType) => FunnyType = funnyType;

    private bool Equals(MutableArrayTypeSpecification other) => FunnyType.Equals(other.FunnyType);

    public override bool Equals(object obj)
    {
        if (obj == null) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != GetType()) return false;
        return Equals((MutableArrayTypeSpecification)obj);
    }

    public override int GetHashCode() => FunnyType.GetHashCode();
}
