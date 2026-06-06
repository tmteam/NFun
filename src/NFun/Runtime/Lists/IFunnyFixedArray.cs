namespace NFun.Runtime.Lists;

/// <summary>
/// Lang-mode <c>fixedArray&lt;T&gt;</c> contract — immutable after construction.
/// Read by index, iterate, count. No <c>SetAt</c> (and no add/remove). Sits at
/// the top of the Array-branch lattice
/// (<c>List ⊆ Array ⊆ FixedArray ⊆ Enumerable</c>); list / array values flow
/// into a fixedArray parameter via subtype edges, the reverse requires an
/// explicit <c>.toList()</c> / <c>.toArray()</c> conversion.
/// </summary>
public interface IFunnyFixedArray : IFunnyEnumerable {
    FunnyType ElementType { get; }
    object GetElementOrNull(int index);
}
