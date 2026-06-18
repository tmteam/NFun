using System;
using NFun.Tic;
using NFun.Tic.Algebra;

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
            || constructor == ConstructorKind.Enumerable)
            throw new ArgumentException(
                $"StateCollection cannot represent {constructor} — abstract constraint-only kind",
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

    /// <summary>
    /// Map factory — builds a frozen pair-struct <c>{key:K, value:V}</c> element
    /// node and wraps it as <c>StateCollection(Map, structNode)</c>. The struct's
    /// field nodes ARE the passed keyNode and valueNode — preserves positional
    /// generic identity from the caller's genericMap, so signature-level K and V
    /// continue to bind correctly through field-node identity even though the
    /// shape now goes through a single element slot.
    ///
    /// <para>Identity stability: the structNode is built ONCE per <c>OfMap</c>
    /// call; downstream Apply cells reuse the SAME node so K/V identities stay
    /// stable across merges. Clients that need access to K/V should pattern
    /// match on the element struct (no dedicated accessors — keeps the class
    /// data-driven by <see cref="Constructor"/>).</para>
    /// </summary>
    public static StateCollection OfMap(TicNode keyNode, TicNode valueNode) {
        var fields = new System.Collections.Generic.Dictionary<string, TicNode>(2, System.StringComparer.OrdinalIgnoreCase) {
            { "key",   keyNode   },
            { "value", valueNode },
        };
        var structNode = TicNode.CreateTypeVariableNode(new StateStruct(fields, isFrozen: true));
        return new StateCollection(ConstructorKind.Map, structNode);
    }

    public static StateCollection OfMap(ITicNodeState keyState, ITicNodeState valueState)
        => OfMap(WrapMapArg(keyState), WrapMapArg(valueState));

    private static TicNode WrapMapArg(ITicNodeState state) => state switch {
        ITypeState t        => TicNode.CreateTypeVariableNode(t),
        StateRefTo refTo    => refTo.Node,
        ConstraintsState c  => TicNode.CreateInvisibleNode(c),
        _ => throw new InvalidOperationException($"Map K/V cannot be {state}")
    };

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
        // Cross-Constructor within Array-branch (List ⊆ Array ⊆ FixedArray):
        // mirrors Stage 2 Liskov decision (Pull/Push at PullConstraintsFunctions.cs:416-430
        // already accept `IsSubtypeOrEqual(descendant.Constructor, ancestor.Constructor)`;
        // pinned by `Ambiguity_ListPassedWhereArrayExpected_Accepted`). LCA widens kind per
        // ConstructorLattice; element LCA recursive. Bug hunt round 6 #32.
        if (otherType is StateCollection xKindOther
            && xKindOther.Constructor != Constructor
            && IsArrayBranchKind(Constructor)
            && IsArrayBranchKind(xKindOther.Constructor))
        {
            if (Element is not ITypeState xElemA || xKindOther.Element is not ITypeState xElemB)
                return null;
            ITypeState elemLca = xElemA.Equals(xElemB)
                ? xElemA
                : xElemA.GetLastCommonAncestorOrNull(xElemB);
            if (elemLca == null || elemLca == StatePrimitive.Any) return StatePrimitive.Any;
            var widerKind = ConstructorLattice.Lca(Constructor, xKindOther.Constructor);
            return Of(widerKind, elemLca);
        }
        if (otherType is not StateCollection other || other.Constructor != Constructor)
            return StatePrimitive.Any;
        if (Element is not ITypeState a || other.Element is not ITypeState b)
            return null;
        if (a.Equals(b)) return this;
        // Element-wise LCA recursion — ONLY for composite elements (StateStruct,
        // nested StateCollection, etc.). Composite LCA naturally recurses into
        // sub-elements, where soft CS-typed fields (integer literals with
        // Preferred=I32) can be widened by their own LCA. This matches the
        // list-of-struct / struct-of-int+real precedent: without annotations
        // the inferred widening cascades through structural layers.
        //
        // Primitive elements keep strict invariance (return Any). Rationale:
        // when user writes `a:list<int>; b:list<real>`, those are two distinct
        // types — silently widening would break Stage 0 invariance and is
        // pinned by unit tests (PullPushTest.IfElse_DifferentLists_*). The
        // script case `[1,2,3] + [1.0]` doesn't hit this path: list element
        // stays CS (not ITypeState), the early `Element is not ITypeState`
        // bail-out falls through to LcaOrShareIdentity + MergeInplace.
        if (a is ICompositeState && b is ICompositeState) {
            var sameKindElemLca = a.GetLastCommonAncestorOrNull(b);
            if (sameKindElemLca == null || sameKindElemLca == StatePrimitive.Any) return StatePrimitive.Any;
            return Of(Constructor, sameKindElemLca);
        }
        return StatePrimitive.Any;
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
        // Cross-Constructor (Array-branch) with unresolved elements: identity-share
        // via MergeInplace + widen kind per ConstructorLattice. Bug hunt round 6 #32.
        //
        // ONLY when neither side's element is itself a StateCollection / StateArray /
        // StateStruct / StateOptional. If they were, MergeInplace would route through
        // `NarrowerArrayBranchOrNull` (intersection / unification semantics) which
        // returns the NARROWER constructor — opposite of LCA. Mixing widen-outer +
        // narrow-inner produces inconsistent types (0832 LeetCode regression).
        // Nested composite cases fall through to Any here; the resolved-element path
        // in GetLastCommonAncestorOrNull handles them recursively when both elements
        // are concrete.
        if (otherType is StateCollection xKindOther
            && xKindOther.Constructor != Constructor
            && IsArrayBranchKind(Constructor)
            && IsArrayBranchKind(xKindOther.Constructor))
        {
            var widerKind = ConstructorLattice.Lca(Constructor, xKindOther.Constructor);
            // Path (a) — both elements non-composite (primitive / RefTo to primitive
            // / CS). Identity-share via MergeInplace. Safe because MergeInplace on
            // primitive elements has no narrowing to invert. Bug hunt round 6 #32.
            if (Element is not ICompositeState && xKindOther.Element is not ICompositeState)
            {
                if (!ReferenceEquals(ElementNode, xKindOther.ElementNode))
                    Tic.SolvingFunctions.MergeInplace(ElementNode, xKindOther.ElementNode);
                return Of(widerKind, ElementNode.State);
            }
            // Path (b) — Bug hunt round 11 #55 / closes 2D depth of historical
            // debt #17. At least one side's element is a composite
            // (StateCollection / StateStruct / etc.). Bounded to 1-level depth
            // (the 2D surface): xKindOther's element must be a StateCollection
            // whose own element is non-composite. Deeper nesting (3D+) is
            // rejected here so the caller widens to Any. Unbounded path-(b)
            // recursion produces malformed CS shapes downstream (FU758 at the
            // literal). 3D+ residual is a worklist-Pull (debt #10) manifestation
            // per professorial review — first-time-entry recursion sees
            // physically distinct ElementNodes at each layer, so the principled
            // closure requires re-firing LCA after deep CS resolution.
            if (xKindOther.Element is StateCollection deeperOther
                && deeperOther.Element is ICompositeState)
                return null;
            var elemA = ElementNode.GetNonReference().State;
            var elemB = xKindOther.ElementNode.GetNonReference().State;
            var elemLca = elemA.Lca(elemB);
            if (elemLca is StatePrimitive { Name: PrimitiveTypeName.Any })
                return null;
            if (elemLca is not ITypeState elemTypeState)
                return null;
            // Identity coupling: when one side's element is CS, use that node as
            // canonical and seed it with the wider element type via AddDescendant.
            // This couples the literal's element node (V1) with the LCA result,
            // so downstream Push propagation finds matching identity.
            //
            // When neither side has a CS (both already-resolved composites), we
            // would need to invisibly synthesize a fresh canonical node — but
            // tests show this loses identity for downstream Push and causes
            // FU758 at deeper nesting (3D+). Fall through to null so the caller
            // widens to Any — preserves master's behavior for 3D+ instead of
            // regressing to a rejection. Closure tracked under debt #10.
            if (ElementNode.State is ConstraintsState csA)
            {
                csA.AddDescendant(elemTypeState);
                return Of(widerKind, ElementNode);
            }
            if (xKindOther.ElementNode.State is ConstraintsState csB)
            {
                csB.AddDescendant(elemTypeState);
                return Of(widerKind, xKindOther.ElementNode);
            }
            return null;
        }
        // Cross-family with legacy StateArray (receiver-side StateCollection direction).
        // Mirrors the StateArray receiver path at `StateExtensions.Lca.cs:96-100`.
        // Returns StateArray with merged-element identity so downstream Pull/Push
        // converge. Same Bug hunt round 6 #32 family; needed for text-concat narrowing
        // tests where `result = ''` (StateArray<char>) gets re-assigned with
        // `concat(...)` (StateCollection.FixedArray<char>) and the LCA dispatch
        // happens with StateCollection as receiver.
        if (otherType is StateArray legacyArr && IsArrayBranchKind(Constructor)
            && Element is not ICompositeState
            && legacyArr.Element is not ICompositeState)
        {
            if (!ReferenceEquals(ElementNode, legacyArr.ElementNode))
                Tic.SolvingFunctions.MergeInplace(ElementNode, legacyArr.ElementNode);
            return StateArray.Of(ElementNode.State);
        }
        // Bug hunt round 11 #55 / TechnicalDebt #17 — falling through to null
        // (caller widens to Any) for cross-kind StateCollection with nested
        // composite elements. Algebraically the correct answer is
        // `SC(Lca_L(K₁,K₂), e₁ ∨ e₂)` via recursive Lca; the implementation
        // can't materialize it because (a) MergeInplace cross-kind composite
        // routes through NarrowerArrayBranchOrNull (intersection — opposite
        // of LCA, the 0832 LeetCode protective trap), and (b) at LCA-time
        // the literal-side element is typically still CS, so fresh-node
        // results don't propagate to the literal's element identity.
        // Workaround for users: bind the literal to an unannotated var first
        // (`tmp = [[…]]; x ?? tmp`). Pinned: Stage1InvariancePinTests.cs.
        // Proper fix: split LCA from identity-share, requires worklist Pull
        // (debt #10) for late-CS resolution.
        TraceLog.WriteLine($"    LCA widened to Any (Stage 1 invariance pin / debt #17): {PrintState(0)} ∨ {otherType}");
        return null;
    }

    public override bool Equals(object obj) =>
        obj is StateCollection other
        && other.Constructor == Constructor
        && InvariantSingleArgComposite.EqualsWithCycleGuard(ElementNode, other.Element, CycleGuard);

    public override int GetHashCode() => 0;

    public override string PrintState(int depth) {
        if (depth > 100) return $"{KindName}(...REQ...)";
        // Map prints as map(K, V) — extract from the inner pair-struct's fields
        // so the surface output stays the same as the legacy StateMap printer.
        if (Constructor == ConstructorKind.Map
            && ElementNode.GetNonReference().State is StateStruct ss
            && ss.GetFieldOrNull("key") is { } k
            && ss.GetFieldOrNull("value") is { } v) {
            return $"map({k.State.PrintState(depth + 1)},{v.State.PrintState(depth + 1)})";
        }
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
        ConstructorKind.Map        => "map",
        _ => Constructor.ToString(),
    };
}
