using System;
using System.Collections.Generic;

namespace NFun.Runtime.Lists;

/// <summary>
/// Lang-mode <c>list&lt;T&gt;</c> runtime contract.
///
/// Sibling of <see cref="NFun.Runtime.Arrays.IFunnyArray"/> — separate hierarchy
/// because the two have different semantics:
///   • <c>list&lt;T&gt;</c> is invariant in T and mutable (Stage 3 will expose
///     <c>add/remove/clear/[i]=v</c>).
///   • <c>T[]</c> is covariant and immutable (legacy ee-mode).
/// Code paths that handle <c>IFunnyArray</c> intentionally do NOT pick up
/// <c>IFunnyList</c> — that would silently re-introduce covariance.
///
/// Stage 2.2 only exposes the read surface (Count, indexer, iteration). Mutation
/// arrives in Stage 3.
/// </summary>
public interface IFunnyList : IFunnyMutableArray {
    // ElementType / GetElementOrNull / SetAt inherited from IFunnyMutableArray.
    IEnumerable<T> As<T>();
}
