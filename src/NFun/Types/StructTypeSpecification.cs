using System;
using System.Collections.Generic;

namespace NFun.Types; 

internal class StructTypeSpecification: Dictionary<string, FunnyType>{
    public StructTypeSpecification(int capacity)
        : base(capacity, StringComparer.InvariantCultureIgnoreCase) =>
        _hashCode = new Lazy<int>(() =>
        {
            var hash = 17;
            foreach (var (key, value) in this)
            {
                hash ^= key.GetHashCode() * 23 + value.GetHashCode();
            }
            return hash;
        });


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