namespace NFun.Types;

/// <summary>
/// Payload for <see cref="BaseFunnyType.Mutable"/> — carries the element type
/// of a typeclass-bound generic <c>Mutable&lt;T&gt;</c>. Constraint-only:
/// satisfied by <c>list&lt;T&gt;</c>, <c>array&lt;T&gt;</c>, <c>set&lt;T&gt;</c>
/// (and future queue/stack), but NOT by <c>fixedArray&lt;T&gt;</c> or the
/// legacy ee-mode <c>T[]</c>.
/// </summary>
public class MutableTypeSpecification {
    public readonly FunnyType FunnyType;

    public MutableTypeSpecification(FunnyType funnyType) => FunnyType = funnyType;

    private bool Equals(MutableTypeSpecification other) => FunnyType.Equals(other.FunnyType);

    public override bool Equals(object obj)
    {
        if (obj == null) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != GetType()) return false;
        return Equals((MutableTypeSpecification)obj);
    }

    public override int GetHashCode() => FunnyType.GetHashCode();
}
