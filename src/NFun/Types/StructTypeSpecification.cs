using System;
using System.Collections.Generic;

namespace NFun.Types;

public interface IStructTypeSpecification: IReadOnlyDictionary<string,FunnyType> {
    bool IsFrozen { get; }
}

internal class StructTypeSpecification:Dictionary<string, FunnyType>, IStructTypeSpecification{

    public bool IsFrozen { get; }

    public StructTypeSpecification(int capacity, bool isFrozen)
        : base(capacity, StringComparer.InvariantCultureIgnoreCase) {
        IsFrozen = isFrozen;
        _hashCode = new Lazy<int>(() => {
            var hash = 17;
            foreach (var (key, value) in this)
            {
                hash ^= key.GetHashCode() * 23 + value.GetHashCode();
            }

            return hash;
        });
    }


    private readonly Lazy<int> _hashCode;

    public override int GetHashCode() => _hashCode.Value;

    public override bool Equals(object obj)
    {
        var res = obj as StructTypeSpecification;
        if (res == null)
            return false;
        return this.ValueEquals(res);
    }

}
