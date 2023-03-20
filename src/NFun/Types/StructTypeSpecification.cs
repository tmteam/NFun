using System;
using System.Collections.Generic;

namespace NFun.Types;

public interface IStructTypeSpecification : IReadOnlyDictionary<string, FunnyType> {
    bool IsFrozen { get; }
    public bool AllowDefaultValues { get; }
}

internal class StructTypeSpecification : Dictionary<string, FunnyType>, IStructTypeSpecification {

    public bool IsFrozen { get; }

    public bool AllowDefaultValues { get; }

    public StructTypeSpecification(int capacity, bool isFrozen, bool allowDefaultValues)
        : base(capacity, StringComparer.InvariantCultureIgnoreCase) {
        IsFrozen = isFrozen;
        AllowDefaultValues = allowDefaultValues;
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

    public override bool Equals(object obj) {
        var res = obj as StructTypeSpecification;
        if (res == null)
            return false;
        //todo - equals should check that!
        //if (AllowDefaultValues != res.AllowDefaultValues)
        //    return false;
        return this.ValueEquals(res);
    }
}
