namespace NFun.Types; 

internal class ArrayTypeSpecification {
    public readonly FunnyType FunnyType;

    public ArrayTypeSpecification(FunnyType funnyType) => FunnyType = funnyType;

    private bool Equals(ArrayTypeSpecification other) => FunnyType.Equals(other.FunnyType);

    public override bool Equals(object obj)
    {
        if (obj == null) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != this.GetType()) return false;
        return Equals((ArrayTypeSpecification)obj);
    }

    public override int GetHashCode() => FunnyType.GetHashCode();
}