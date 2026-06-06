namespace NFun.Types;

/// <summary>
/// Payload for <see cref="BaseFunnyType.Enumerable"/> — carries the element type of
/// a generic-constrained <c>Enumerable&lt;V&gt;</c> argument in a function
/// signature.
///
/// <para>Unlike <see cref="ListTypeSpecification"/> / <see cref="ArrayTypeSpecification"/>,
/// <c>Enumerable</c> is <b>constraint-only</b>: no user-side value has this type at
/// runtime. It exists in function signatures (e.g.
/// <c>count&lt;V&gt;(xs: Enumerable&lt;V&gt;): int</c>) to express "any container
/// shape whose <see cref="Tic.SolvingStates.ConstructorKind"/> caps at
/// <c>Enumerable</c>".</para>
///
/// <para>At TIC graph-build time, <c>EnumerableOf(V)</c> in an argument position
/// is converted to a <see cref="Tic.SolvingStates.StateCompositeConstraints"/>
/// node with <c>Ancestor=Enumerable, Descendant=null, ElementNode=V's resolved
/// node</c>. Pull from concrete <c>StateCollection</c> / <c>StateArray</c>
/// arguments refines the interval per <c>Specs/Tic/Algebra_CompositeConstraints.md</c>
/// §4.1.1. Destruction picks a concrete kind via <c>Concretest</c>.</para>
/// </summary>
public class EnumerableTypeSpecification {
    public readonly FunnyType FunnyType;

    public EnumerableTypeSpecification(FunnyType funnyType) => FunnyType = funnyType;

    private bool Equals(EnumerableTypeSpecification other) => FunnyType.Equals(other.FunnyType);

    public override bool Equals(object obj) {
        if (obj == null) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != GetType()) return false;
        return Equals((EnumerableTypeSpecification)obj);
    }

    public override int GetHashCode() => FunnyType.GetHashCode();
}
