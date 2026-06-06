namespace NFun.Runtime.Lists;

/// <summary>
/// Lang-mode <c>set&lt;T&gt;</c> contract — unordered collection of unique
/// elements with O(1) membership testing.
///
/// Sits on a separate branch from the List/Array/FixedArray chain in the
/// <c>ConstructorLattice</c>: its supertype is <c>Enumerable</c> directly
/// (cross-kind with the Array branch is rejected). Like <see cref="IFunnyList"/>,
/// the value semantics are by content — two sets with the same elements
/// compare equal.
/// </summary>
public interface IFunnyMutableSet : IFunnyEnumerable {
    /// <summary>True when <paramref name="value"/> is a member of this set.</summary>
    bool Contains(object value);

    /// <summary>
    /// Adds <paramref name="value"/>. Returns true if it was a new element,
    /// false if the value was already present.
    /// </summary>
    bool Add(object value);

    /// <summary>
    /// Removes <paramref name="value"/>. Returns true if it was present and
    /// removed, false otherwise.
    /// </summary>
    bool Remove(object value);

    /// <summary>Drops every element.</summary>
    void Clear();
}
