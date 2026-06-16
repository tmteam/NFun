namespace NFun.Types;

public enum BaseFunnyType {
    Empty = 0,
    Char = 1,
    Bool = 2,
    UInt8 = 3,
    UInt16 = 4,
    UInt32 = 5,
    UInt64 = 6,
    Int16 = 7,
    Int32 = 8,
    Int64 = 9,
    Real = 10,
    Ip = 11,
    /// <summary>
    /// Expression-mode (legacy) immutable covariant array <c>T[]</c>. Backed by
    /// <c>System.Array</c>-based <c>ImmutableFunnyArray</c>. Stays covariant per
    /// existing ee-mode semantics.
    /// </summary>
    ArrayOf = 12,
    Fun = 13,
    Generic = 14,
    Any = 15,
    Struct = 16,
    Optional = 17,
    None = 18,
    Custom = 19,
    NamedStruct = 20,

    /// <summary>
    /// Lang-mode growable list <c>list&lt;T&gt;</c>. Invariant in element.
    /// Backed by <c>System.Collections.Generic.List&lt;T&gt;</c> via
    /// <c>MutableFunnyList&lt;T&gt;</c>. Distinct from <see cref="ArrayOf"/> —
    /// the ee-mode legacy array stays unchanged.
    ///
    /// Stage 2.2 introduces the enum value and the runtime backing. Stage 2.3
    /// wires parser (`list&lt;T&gt;` type syntax, `list(...)` factory). Stage 3
    /// adds mutation (`add`, `remove`, `clear`, `[i]=v`).
    /// </summary>
    List = 21,

    /// <summary>
    /// Lang-mode mutable array <c>array&lt;T&gt;</c>. Fixed length, mutable
    /// element (<c>a[i] = v</c>). Invariant in element. Backed by
    /// <c>object[]</c> via <c>MutableFunnyArray</c>. Sits above
    /// <see cref="List"/> in the Stage 0 lattice: every list can flow into an
    /// array parameter; not vice versa (array lacks <c>add</c>/<c>remove</c>).
    /// </summary>
    MutableArray = 22,

    /// <summary>
    /// Lang-mode immutable fixed-length array <c>fixedArray&lt;T&gt;</c>.
    /// Read-only after construction (<c>a[i] = v</c> rejected at TIC). Invariant
    /// in element. Backed by <c>object[]</c> via <c>FixedFunnyArray</c>. Sits
    /// above <see cref="MutableArray"/> in the Stage 0 lattice: list / array
    /// values flow into a fixedArray parameter; not vice versa.
    /// </summary>
    FixedArray = 23,

    /// <summary>
    /// Stage C — constraint-only top of the lang-mode collection lattice. Accepts
    /// any concrete collection kind (List / MutableArray / FixedArray / Set /
    /// Queue / Map / legacy ArrayOf) for read-only access via
    /// <c>IFunnyEnumerable</c>. Never instantiated as a value — used in generic
    /// constraints only (e.g. <c>fun count&lt;T&gt;(xs: Enumerable&lt;T&gt;)</c>).
    /// The Concretest operator resolves <c>Enumerable</c> to <see cref="List"/>
    /// per <c>ConstructorLattice.Concretest</c>. CLR input converter accepts any
    /// <c>System.Collections.IEnumerable</c>; output converter rejects (function
    /// must resolve to a concrete kind before runtime).
    /// </summary>
    Enumerable = 24,

    /// <summary>
    /// Lang-mode unordered hash-set <c>set&lt;T&gt;</c>. Invariant in element.
    /// Backed by <c>HashSet&lt;object&gt;</c> via <c>MutableFunnySet</c>. Sits
    /// on a separate branch from the List/Array/FixedArray chain — its
    /// supertype is <see cref="Enumerable"/> directly (cross-kind with the
    /// Array branch is rejected per ConstructorLattice).
    /// </summary>
    Set = 25,

    /// <summary>
    /// Constraint-only typeclass for containers that support `.clear()` —
    /// dropping ALL elements (length must change). Accepts <see cref="List"/>,
    /// <see cref="Set"/>, <see cref="Map"/>. **Rejects <see cref="MutableArray"/>**
    /// (lang-mode `int[]` — element-mutable but fixed length, so clear doesn't
    /// apply), <see cref="FixedArray"/> (immutable), legacy ee-mode
    /// <see cref="ArrayOf"/>, and <see cref="Enumerable"/> (abstract).
    /// Never instantiated as a value — used in generic constraints only
    /// (e.g. <c>fun clear&lt;T&gt;(xs: Clearable&lt;T&gt;)</c>). Concretest
    /// resolves <c>Clearable</c> to <see cref="List"/> per
    /// <c>ConstructorLattice.Concretest</c>.
    ///
    /// <para>Conceptually distinct from "Mutable" (which would include
    /// MutableArray, mutable struct, …). NFun has Clearable but no separate
    /// Mutable typeclass yet — if one is needed, it would be a strict
    /// superset of Clearable.</para>
    /// </summary>
    Clearable = 26,

    /// <summary>
    /// Lang-mode hash <c>map&lt;K, V&gt;</c> — unordered key→value mapping.
    /// Invariant in both arguments. Backed by
    /// <c>System.Collections.Generic.Dictionary&lt;object, object&gt;</c> via
    /// <see cref="NFun.Runtime.Lists.MutableFunnyMap"/>. Iteration yields
    /// <c>{key, value}</c> structs; runtime cardinality query is via the same
    /// <see cref="NFun.Runtime.Lists.IFunnyEnumerable.Count"/> contract as
    /// other collections.
    /// </summary>
    Map = 27,
}