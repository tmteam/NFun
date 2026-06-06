using System.Collections.Generic;
using NFun.Types;

namespace NFun.Runtime.Lists;

/// <summary>
/// Marker interface uniting NFun runtime containers that can be iterated by
/// lang-mode collection consumers (currently <see cref="IFunnyList"/> and
/// <see cref="NFun.Runtime.Arrays.IFunnyArray"/>). Lets <c>for x in xs:</c>
/// and similar callers dispatch by structural type instead of by a chain of
/// <c>is</c>-checks ending in a permissive <see cref="IEnumerable"/> fallback —
/// the fallback used to capture stray CLR collections that bypassed NFun's
/// own input converters.
///
/// <para><b>Stage C.4b — ElementType pull-up.</b> The element type of the
/// container, previously declared independently by every implementor, is now
/// part of the parent contract. Generic Enumerable&lt;T&gt; functions read this
/// to dispatch on the actual element shape without an <c>is</c>-cast cascade.</para>
///
/// <para><b>Binary-compat note.</b> External implementors of
/// <see cref="IFunnyEnumerable"/> (host applications providing custom enumerable
/// types) MUST add <see cref="ElementType"/> when upgrading past Stage C.
/// All in-repo implementations (<c>IFunnyArray</c>, <c>IFunnyMutableArray</c>,
/// <c>IFunnyFixedArray</c>) already declared this property — the pull-up is
/// non-breaking in-repo.</para>
/// </summary>
public interface IFunnyEnumerable : IEnumerable<object> {
    int Count { get; }

    /// <summary>Element type of the container, as observed at construction.</summary>
    FunnyType ElementType { get; }
}
