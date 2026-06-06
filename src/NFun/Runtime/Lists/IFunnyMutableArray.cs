namespace NFun.Runtime.Lists;

/// <summary>
/// Lang-mode <c>array&lt;T&gt;</c> contract — fixed length, mutable element
/// (<c>a[i] = v</c> works; <c>add</c>/<c>remove</c> do not).
///
/// Stage 0 hierarchy: <c>IFunnyList ≤ IFunnyMutableArray ≤ IFunnyFixedArray ≤ IFunnyEnumerable</c>.
/// Lists extend this interface because every mutable-array operation also
/// works on a list (the converse — <c>add</c>/<c>remove</c> on an
/// array — does not, which is why we don't share `add` between them).
/// </summary>
public interface IFunnyMutableArray : IFunnyEnumerable {
    FunnyType ElementType { get; }
    object GetElementOrNull(int index);
    /// <summary>Replaces the element at <paramref name="index"/>.
    /// Returns false when out-of-range; caller-side wiring surfaces that as a
    /// clean runtime exception.</summary>
    bool SetAt(int index, object value);
}
