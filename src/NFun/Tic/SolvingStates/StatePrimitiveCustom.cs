using System;

namespace NFun.Tic.SolvingStates;

/// <summary>
/// Custom type as a TIC primitive. Inherits StatePrimitive so all existing
/// `is StatePrimitive` checks work transparently. Overrides LCA/GCD/Convert
/// to behave as an isolated type: unifies only with itself, converts only to Any.
/// </summary>
public class StatePrimitiveCustom : StatePrimitive {
    private static readonly PrimitiveTypeName DummyName = (PrimitiveTypeName)(-1 << 6);

    public string CustomName { get; }
    internal FunnyType OriginalFunnyType { get; }

    public StatePrimitiveCustom(string customName, FunnyType originalFunnyType)
        : base(DummyName) {
        CustomName = customName ?? throw new ArgumentNullException(nameof(customName));
        OriginalFunnyType = originalFunnyType;
    }

    public override bool CanBePessimisticConvertedTo(StatePrimitive type) {
        if (type is StatePrimitiveCustom other)
            return Equals(other);
        return type.Name == PrimitiveTypeName.Any;
    }

    public override StatePrimitive GetFirstCommonDescendantOrNull(StatePrimitive other) {
        if (other is StatePrimitiveCustom oc && Equals(oc))
            return this;
        if (other.Name == PrimitiveTypeName.Any)
            return this;
        return null;
    }

    public override StatePrimitive GetLastCommonPrimitiveAncestor(StatePrimitive other) {
        if (other is StatePrimitiveCustom oc && Equals(oc))
            return this;
        return Any;
    }

    public override ITypeState GetLastCommonAncestorOrNull(ITypeState otherType) {
        if (otherType is StatePrimitiveCustom oc && Equals(oc))
            return this;
        return Any;
    }

    public override bool Equals(object obj) =>
        obj is StatePrimitiveCustom other
        && string.Equals(CustomName, other.CustomName, StringComparison.OrdinalIgnoreCase);

    public override int GetHashCode() => StringComparer.OrdinalIgnoreCase.GetHashCode(CustomName);

    public override string ToString() => CustomName;
}
