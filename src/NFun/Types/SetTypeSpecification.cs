namespace NFun.Types;

/// <summary>
/// Payload for <see cref="BaseFunnyType.Set"/> — carries the element type of a
/// lang-mode <c>set&lt;T&gt;</c>. Parallels the other single-arg collection
/// specs; kept distinct so pattern matches on container kind stay unambiguous.
/// </summary>
public class SetTypeSpecification {
    public readonly FunnyType FunnyType;

    public SetTypeSpecification(FunnyType funnyType) => FunnyType = funnyType;

    private bool Equals(SetTypeSpecification other) => FunnyType.Equals(other.FunnyType);

    public override bool Equals(object obj)
    {
        if (obj == null) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != GetType()) return false;
        return Equals((SetTypeSpecification)obj);
    }

    public override int GetHashCode() => FunnyType.GetHashCode();
}
