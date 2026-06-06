using System;

namespace NFun.Tic.SolvingStates;

/// <summary>
/// Unified TIC state for ALL lang-mode positional ordered single-arg collections.
/// Data-driven via <see cref="ConstructorKind"/> discriminator — one C# class
/// covers <c>list&lt;T&gt;</c>, <c>fixedArray&lt;T&gt;</c>, <c>array&lt;T&gt;</c>,
/// <c>set&lt;T&gt;</c>, future <c>queue&lt;T&gt;</c>, <c>stack&lt;T&gt;</c>.
///
/// Rationale: per-collection-type subclasses create combinatorial blow-up in
/// <see cref="Stages.IStateFunction"/> (one Apply overload per class) and
/// <see cref="Stages.StagesExtension"/> (one switch arm per class). A single
/// data-discriminated class collapses N classes to one, N×M Apply overloads to
/// 1, and adding a new single-arg collection kind becomes "new
/// <see cref="ConstructorKind"/> enum value" rather than "new C# class + N
/// integration files."
///
/// <para><b>Shape boundary.</b> This class is for collections whose structural
/// shape is "one invariant element argument". Two-arg collections
/// (<c>map&lt;K,V&gt;</c>) live in a separate state class because their shape
/// differs structurally (key-value pair, not single element). The same applies
/// to hypothetical tuples / records — different shape ⇒ different state class.</para>
///
/// <para><b>Pattern matching.</b> Consumers cannot dispatch via
/// <c>state is StateList</c> any longer — replace with
/// <c>state is StateCollection c when c.Constructor == ConstructorKind.List</c>
/// or pattern-match on <see cref="Constructor"/> after a single
/// <c>StateCollection</c> case.</para>
///
/// All instances are <see cref="Variance.Invariant"/> in their element argument
/// per the Stage 0 uniform-invariance rule.
///
/// Rejected ConstructorKinds:
///   • <see cref="ConstructorKind.Any"/> — universal top, never instantiated.
///   • <see cref="ConstructorKind.Enumerable"/> — abstract constraint-only
///     (per <see cref="ConstructorLattice.IsConstraintOnly"/>); cannot be a value.
///   • <see cref="ConstructorKind.Map"/> — two-arg shape, separate state class.
/// </summary>
public sealed class StateCollection : StateComposite {

    private const int CycleGuard = -57600;

    public StateCollection(ConstructorKind constructor, TicNode elementNode) {
        if (constructor == ConstructorKind.Any
            || constructor == ConstructorKind.Enumerable
            || constructor == ConstructorKind.Map)
            throw new ArgumentException(
                $"StateCollection cannot represent {constructor} — not a single-arg concrete collection",
                nameof(constructor));
        Constructor = constructor;
        ElementNode = elementNode;
        Arguments = new[] { new CompositeArg(elementNode, Variance.Invariant) };
    }

    public static StateCollection Of(ConstructorKind kind, ITicNodeState state) =>
        state switch {
            ITypeState type     => Of(kind, type),
            StateRefTo refTo    => Of(kind, refTo.Node),
            ConstraintsState c  => new StateCollection(kind, TicNode.CreateInvisibleNode(c)),
            _ => throw new InvalidOperationException($"StateCollection cannot have state {state}")
        };

    public static StateCollection Of(ConstructorKind kind, TicNode node)    => new(kind, node);
    public static StateCollection Of(ConstructorKind kind, ITypeState type) => new(kind, TicNode.CreateTypeVariableNode(type));

    // Convenience factories — preserve readable call-sites like StateCollection.OfList(I32).
    public static StateCollection OfList(ITicNodeState s)         => Of(ConstructorKind.List, s);
    public static StateCollection OfFixedArray(ITicNodeState s)   => Of(ConstructorKind.FixedArray, s);
    public static StateCollection OfMutableArray(ITicNodeState s) => Of(ConstructorKind.Array, s);
    public static StateCollection OfSet(ITicNodeState s)          => Of(ConstructorKind.Set, s);

    // TicNode-direct overloads (for cycle-guard / shared-node scenarios).
    // TicNode does NOT implement ITicNodeState — these are unambiguous, the compiler
    // routes `OfList(someTicNode)` here, not to the ITicNodeState path.
    public static StateCollection OfList(TicNode node)         => new(ConstructorKind.List, node);
    public static StateCollection OfFixedArray(TicNode node)   => new(ConstructorKind.FixedArray, node);
    public static StateCollection OfMutableArray(TicNode node) => new(ConstructorKind.Array, node);
    public static StateCollection OfSet(TicNode node)          => new(ConstructorKind.Set, node);

    public TicNode ElementNode { get; }
    public ITicNodeState Element => ElementNode.State;

    public override ConstructorKind Constructor { get; }
    public override CompositeArg[] Arguments { get; }

    public override ICompositeState GetNonReferenced() => Of(Constructor, ElementNode.GetNonReference());

    /// <summary>True for <see cref="ConstructorKind.List"/> /
    /// <see cref="ConstructorKind.Array"/> / <see cref="ConstructorKind.FixedArray"/> —
    /// the Stage 0 lattice's `Array`-branch members that all flow into the
    /// legacy <see cref="StateArray"/> slot. Set sits on a separate branch.</summary>
    private static bool IsArrayBranchKind(ConstructorKind k) =>
        k == ConstructorKind.List || k == ConstructorKind.Array || k == ConstructorKind.FixedArray;

    /// <summary>
    /// Pure LCA: same Constructor + concrete-equal elements → return self;
    /// cross-Constructor StateCollection pairs collapse to Any per uniform
    /// invariance (Stage 0 simplification); special case
    /// <c>StateCollection(List) × StateArray</c> widens to the array (Stage 0
    /// hierarchy `List ⊆ Array`). Returns <c>null</c> when either element is
    /// unresolved (CS / RefTo) — the caller chooses whether to defer or share
    /// identity. <seealso cref="LcaOrShareIdentity"/>.
    /// </summary>
    public override ITypeState GetLastCommonAncestorOrNull(ITypeState otherType) {
        // Cross-family with legacy StateArray: any Array-branch kind widens to T[].
        if (otherType is StateArray arr && IsArrayBranchKind(Constructor))
        {
            if (Element is not ITypeState myElem || arr.Element is not ITypeState arrElem)
                return null;
            var elemLca = myElem.GetLastCommonAncestorOrNull(arrElem);
            return elemLca == null ? StatePrimitive.Any : StateArray.Of(elemLca);
        }
        if (otherType is not StateCollection other || other.Constructor != Constructor)
            return StatePrimitive.Any;
        if (Element is not ITypeState a || other.Element is not ITypeState b)
            return null;
        return a.Equals(b) ? (ITypeState)this : StatePrimitive.Any;
    }

    /// <summary>
    /// LCA + identity-sharing for same-kind StateCollections with unresolved
    /// elements (CS or RefTo). Mutates the graph via <c>MergeInplace</c> on the
    /// element nodes so future constraints land on the same identity. The
    /// non-null result is one of the two input states.
    ///
    /// Used by <see cref="NFun.Tic.Algebra.StateExtensions.Lca"/> when a pure
    /// LCA would otherwise widen to Any. Stage 0 uniform invariance still
    /// holds: once both sides resolve to concrete primitives, the shared
    /// element node makes them equal (or TIC raises an error).
    /// </summary>
    internal ITypeState LcaOrShareIdentity(ITicNodeState otherType) {
        var pure = otherType is ITypeState ts ? GetLastCommonAncestorOrNull(ts) : null;
        if (pure != null) return pure;
        if (otherType is StateCollection other && other.Constructor == Constructor)
        {
            if (!ReferenceEquals(ElementNode, other.ElementNode))
                Tic.SolvingFunctions.MergeInplace(ElementNode, other.ElementNode);
            return this;
        }
        return null;
    }

    public override bool Equals(object obj) =>
        obj is StateCollection other
        && other.Constructor == Constructor
        && InvariantSingleArgComposite.EqualsWithCycleGuard(ElementNode, other.Element, CycleGuard);

    public override int GetHashCode() => 0;

    public override string PrintState(int depth) {
        if (depth > 100) return $"{KindName}(...REQ...)";
        return $"{KindName}({Element.PrintState(depth + 1)})";
    }

    public override string ToString() =>
        ElementNode.IsSolved
            ? $"{KindName}({ElementNode})"
            : $"{KindName}({ElementNode.Name})";

    private string KindName => Constructor switch {
        ConstructorKind.List       => "list",
        ConstructorKind.FixedArray => "fixedArray",
        ConstructorKind.Array      => "mutArr",
        ConstructorKind.Set        => "set",
        _ => Constructor.ToString(),
    };
}
