namespace NFun.Types;

/// <summary>
/// Payload for <see cref="BaseFunnyType.List"/> — carries the element type of
/// a lang-mode <c>list&lt;T&gt;</c>.
///
/// Parallels <see cref="ArrayTypeSpecification"/> (which carries the element
/// type of the legacy ee-mode <c>T[]</c>). A separate type is used rather than
/// reusing ArrayTypeSpecification because:
///   • The two have different semantics (covariant immutable array vs invariant
///     mutable list).
///   • Type-printing differs (<c>int[]</c> vs <c>list&lt;int&gt;</c>).
///   • Pattern matching at runtime ("is this an array spec or list spec?")
///     stays unambiguous.
/// </summary>
public class ListTypeSpecification {
    public readonly FunnyType FunnyType;

    public ListTypeSpecification(FunnyType funnyType) => FunnyType = funnyType;

    private bool Equals(ListTypeSpecification other) => FunnyType.Equals(other.FunnyType);

    public override bool Equals(object obj)
    {
        if (obj == null) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != GetType()) return false;
        return Equals((ListTypeSpecification)obj);
    }

    public override int GetHashCode() => FunnyType.GetHashCode();
}
