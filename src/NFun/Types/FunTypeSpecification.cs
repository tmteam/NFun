namespace NFun.Types; 

public class FunTypeSpecification {
    private bool Equals(FunTypeSpecification other)
    {
        if (Output == null) return other.Output == null;
        if (other.Output == null) return false;
        if (!Output.Equals(other.Output)) return false;
        if (Inputs.Length != other.Inputs.Length) return false;
        for (int i = 0; i < Inputs.Length; i++)
        {
            if (!Inputs[i].Equals(other.Inputs[i]))
                return false;
        }
        return true;
    }

    public override bool Equals(object obj)
    {
        if (obj==null) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != this.GetType()) return false;
        return Equals((FunTypeSpecification)obj);
    }

    private int CalculateHashCode()
    {
        unchecked
        {
            int hash = 17;

            // get hash code for all items in array
            foreach (var item in Inputs)
            {
                hash = hash * 23 + item.GetHashCode();
            }
            
            return (Output.GetHashCode() * 397) ^ hash;
        }
    }
    public override int GetHashCode() => _hashCode;

    public readonly FunnyType Output;
    public readonly FunnyType[] Inputs;
    private readonly int _hashCode;

    public FunTypeSpecification(FunnyType output, FunnyType[] inputs) {
        Output = output;
        Inputs = inputs;
        _hashCode = CalculateHashCode();
    }
}