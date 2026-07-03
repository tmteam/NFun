using System;
using NFun.Tic;
using NFun.Tic.Algebra;

namespace NFun.Tic.SolvingStates;

/// <summary>Data-driven TIC state for lang-mode single-arg invariant collections (list /
/// fixedArray / array / set) and Map (carried as <c>{key,value}</c> pair-struct element).
/// See Specs/Collections.md §Scope and specs_tic/TicTypeSystem.md §ConstructorLattice.
/// Rejects abstract / top kinds (<see cref="ConstructorKind.Any"/>, <see cref="ConstructorKind.Enumerable"/>).</summary>
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

    public static StateCollection OfList(ITicNodeState s)         => Of(ConstructorKind.List, s);
    public static StateCollection OfFixedArray(ITicNodeState s)   => Of(ConstructorKind.FixedArray, s);
    public static StateCollection OfMutableArray(ITicNodeState s) => Of(ConstructorKind.Array, s);
    public static StateCollection OfSet(ITicNodeState s)          => Of(ConstructorKind.Set, s);

    // TicNode-direct overloads (TicNode is not ITicNodeState, so overloads are unambiguous).
    public static StateCollection OfList(TicNode node)         => new(ConstructorKind.List, node);
    public static StateCollection OfFixedArray(TicNode node)   => new(ConstructorKind.FixedArray, node);
    public static StateCollection OfMutableArray(TicNode node) => new(ConstructorKind.Array, node);
    public static StateCollection OfSet(TicNode node)          => new(ConstructorKind.Set, node);

    /// <summary>Map factory: wraps key/value nodes in a frozen <c>{key,value}</c> pair-struct.
    /// The struct's field nodes ARE the passed nodes — preserves K/V identity across merges.</summary>
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

    /// <summary>True for the Array-branch lattice members (List / Array / FixedArray).</summary>
    private static bool IsArrayBranchKind(ConstructorKind k) =>
        k == ConstructorKind.List || k == ConstructorKind.Array || k == ConstructorKind.FixedArray;

    /// <summary>Pure LCA. Null when either element is unresolved — caller chooses defer
    /// vs identity-share via <see cref="LcaOrShareIdentity"/>.</summary>
    public override ITypeState GetLastCommonAncestorOrNull(ITypeState otherType) {
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
        // Composite elements recurse; primitive elements keep strict invariance (→ Any).
        // Widening primitives would break invariance for `a:list<int>; b:list<real>`.
        if (a is ICompositeState && b is ICompositeState) {
            var sameKindElemLca = a.GetLastCommonAncestorOrNull(b);
            if (sameKindElemLca == null || sameKindElemLca == StatePrimitive.Any) return StatePrimitive.Any;
            return Of(Constructor, sameKindElemLca);
        }
        return StatePrimitive.Any;
    }

    /// <summary>LCA + identity-share via MergeInplace for same-/cross-kind collections with
    /// unresolved elements. Invariance preserved: once elements resolve, shared identity makes
    /// them equal or TIC raises.</summary>
    internal ITypeState LcaOrShareIdentity(ITicNodeState otherType) {
        var pure = otherType is ITypeState ts ? GetLastCommonAncestorOrNull(ts) : null;
        if (pure != null) return pure;
        if (otherType is StateCollection other && other.Constructor == Constructor)
        {
            if (!ReferenceEquals(ElementNode, other.ElementNode))
                Tic.SolvingFunctions.MergeInplace(ElementNode, other.ElementNode);
            return this;
        }
        // Cross-Constructor Array-branch path (a): both elements non-composite.
        // MergeInplace on composite elements would route through NarrowerArrayBranchOrNull
        // (intersection — opposite of LCA), so composite elements take path (b) below or
        // fall through to Any.
        if (otherType is StateCollection xKindOther
            && xKindOther.Constructor != Constructor
            && IsArrayBranchKind(Constructor)
            && IsArrayBranchKind(xKindOther.Constructor))
        {
            var widerKind = ConstructorLattice.Lca(Constructor, xKindOther.Constructor);
            if (Element is not ICompositeState && xKindOther.Element is not ICompositeState)
            {
                if (!ReferenceEquals(ElementNode, xKindOther.ElementNode))
                    Tic.SolvingFunctions.MergeInplace(ElementNode, xKindOther.ElementNode);
                return Of(widerKind, ElementNode.State);
            }
            // Path (b): composite element. Recurses to arbitrary depth via
            // elemA.Lca(elemB) — innermost layer fires MergeInplace in the "both
            // non-composite" branch above; outer layers AddDescendant CS-side.
            // Returns a FRESH element node carrying the concrete elemLca so the
            // result has no CS-references downstream (Push compares the wider
            // type structurally and cannot traverse through a CS-wrapped element
            // node). CS-side identity is preserved separately via AddDescendant
            // on the CS element node, and via Push from the literal structure
            // which still references the original CS node. Debt #17 3D residual
            // closed 2026-06-29.
            var elemA = ElementNode.GetNonReference().State;
            var elemB = xKindOther.ElementNode.GetNonReference().State;
            var elemLca = elemA.Lca(elemB);
            if (elemLca is StatePrimitive { Name: PrimitiveTypeName.Any })
                return null;
            if (elemLca is not ITypeState elemTypeState)
                return null;
            // Seed the CS-side element node with the wider concrete element so it
            // narrows to the right value during Pull/Push (identity-coupled with
            // the literal structure). Both sides may be CS at the same time only
            // in pathological self-merge — handled by the Same node short-circuit.
            if (ElementNode.State is ConstraintsState csA)
                csA.AddDescendant(elemTypeState);
            if (xKindOther.ElementNode.State is ConstraintsState csB
                && !ReferenceEquals(ElementNode, xKindOther.ElementNode))
                csB.AddDescendant(elemTypeState);
            return Of(widerKind, TicNode.CreateInvisibleNode(elemTypeState));
        }
        // Cross-family with legacy StateArray (receiver-side direction). Mirrors the
        // StateArray-receiver path; merged-element identity lets Pull/Push converge.
        if (otherType is StateArray legacyArr && IsArrayBranchKind(Constructor)
            && Element is not ICompositeState
            && legacyArr.Element is not ICompositeState)
        {
            if (!ReferenceEquals(ElementNode, legacyArr.ElementNode))
                Tic.SolvingFunctions.MergeInplace(ElementNode, legacyArr.ElementNode);
            return StateArray.Of(ElementNode.State);
        }
        // Falls through to null (caller widens to Any) for nested-composite cross-kind LCA;
        // see specs_tic/TechnicalDebt.md §Closed (#17).
        TraceLog.WriteLine($"    LCA widened to Any (debt #17): {PrintState(0)} ∨ {otherType}");
        return null;
    }

    public override bool Equals(object obj) =>
        obj is StateCollection other
        && other.Constructor == Constructor
        && InvariantSingleArgComposite.EqualsWithCycleGuard(ElementNode, other.Element, CycleGuard);

    public override int GetHashCode() => 0;

    public override string PrintState(int depth) {
        if (depth > 100) return $"{KindName}(...REQ...)";
        // Map prints as map(K, V) — surface form, extracted from the pair-struct element.
        if (Constructor == ConstructorKind.Map
            && ElementNode.GetNonReference().State is StateStruct ss
            && ss.GetFieldOrNull("key") is { } k
            && ss.GetFieldOrNull("value") is { } v) {
            return $"map({k.State.PrintState(depth + 1)},{v.State.PrintState(depth + 1)})";
        }
        return $"{KindName}({Element.PrintState(depth + 1)})";
    }

    // Delegate to the depth-guarded printer — μ-recursive element states would
    // recurse forever through TicNode.ToString.
    public override string ToString() =>
        ElementNode.IsSolved ? PrintState(0) : $"{KindName}({ElementNode.Name})";

    private string KindName => Constructor switch {
        ConstructorKind.List       => "list",
        ConstructorKind.FixedArray => "fixedArray",
        ConstructorKind.Array      => "mutArr",
        ConstructorKind.Set        => "set",
        ConstructorKind.Map        => "map",
        _ => Constructor.ToString(),
    };
}
