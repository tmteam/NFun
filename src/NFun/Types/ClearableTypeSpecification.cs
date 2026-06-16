namespace NFun.Types;

/// <summary>
/// Payload for <see cref="BaseFunnyType.Clearable"/> — carries the element type
/// of a typeclass-bound generic <c>Mutable&lt;T&gt;</c>. Constraint-only:
/// satisfied by <c>list&lt;T&gt;</c>, <c>array&lt;T&gt;</c>, <c>set&lt;T&gt;</c>
/// (and future queue/stack), but NOT by <c>fixedArray&lt;T&gt;</c> or the
/// legacy ee-mode <c>T[]</c>.
/// </summary>
public class ClearableTypeSpecification {
    public readonly FunnyType FunnyType;

    public ClearableTypeSpecification(FunnyType funnyType) => FunnyType = funnyType;

    private bool Equals(ClearableTypeSpecification other) => FunnyType.Equals(other.FunnyType);

    public override bool Equals(object obj)
    {
        if (obj == null) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != GetType()) return false;
        return Equals((ClearableTypeSpecification)obj);
    }

    public override int GetHashCode() => FunnyType.GetHashCode();
}
