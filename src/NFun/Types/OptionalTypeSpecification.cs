namespace NFun.Types;

internal class OptionalTypeSpecification {
    public readonly FunnyType ElementType;

    public OptionalTypeSpecification(FunnyType elementType) => ElementType = elementType;

    private bool Equals(OptionalTypeSpecification other) => ElementType.Equals(other.ElementType);

    public override bool Equals(object obj)
    {
        if (obj == null) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != this.GetType()) return false;
        return Equals((OptionalTypeSpecification)obj);
    }

    public override int GetHashCode() => ElementType.GetHashCode();
}
