using System;
using System.Collections.Generic;

namespace NFun.Tic.SolvingStates;

/// <summary>
/// Composite-constraint interval state — peer of <see cref="ConstraintsState"/> over
/// the <see cref="ConstructorLattice"/>.
///
/// <para>Carries an interval <c>[Descendant..Ancestor]</c> over
/// <see cref="ConstructorKind"/> plus an element TIC node (invariant) plus
/// <see cref="IsOptional"/>. The 4-field state is sufficient because container-shape
/// predicates collapse to interval refinements (<c>IsIndexedMutable ≡ Anc ≤ Array</c>,
/// <c>Enumerable ≡ Anc = Enumerable</c>) and element-axis predicates
/// (Hashable) propagate to the element node where they live as
/// <see cref="ConstraintsState"/> predicates.</para>
///
/// <para><b>Immutability.</b> Fields are <c>readonly</c>. Mutation is via copy-on-write
/// <see cref="WithAncestor"/>, <see cref="WithDescendant"/>, <see cref="WithIsOptional"/>
/// which return a new instance (or a collapsed <see cref="StateCollection"/> when
/// the interval becomes a point, or <c>null</c> on contradiction). This closes the
/// v4-audit "partial mutation observable" hole structurally.</para>
///
/// <para><b>Spec.</b> See <c>Specs/Tic/Algebra_CompositeConstraints.md</c> v6.1 — §1
/// for the state shape, §3 for the Layer-0 symmetric content operators (LCA/GCD/Unify),
/// §4 for the Layer-2 directional Apply cells.</para>
///
/// <para>C.1 scope: class skeleton + factory + With* + TryCollapseToPoint +
/// SimplifyOrNull + cycle-guard infrastructure. Algebra operators (LCA / GCD / Unify /
/// Concretest / Abstractest) land in C.2. Apply cells land in C.3.</para>
/// </summary>
public sealed class StateCompositeConstraints : ITicNodeState {

    // ── Cycle-guard mark constants (Specs/Tic/Algebra_CompositeConstraints.md §3.8.1)
    // Range -59000..-59600 reserved for StateCompositeConstraints (Stage C.0).
    // Same-class GCD shares range with cross-class because §3.2 same-class delegates to §3.3 Unify.
    public const int CompCsLcaMark         = -59000;
    public const int CompCsGcdMark         = -59100;
    public const int CompCsUnifyMark       = -59200;
    public const int CompCsConcretestMark  = -59300;
    public const int CompCsAbstractestMark = -59400;
    public const int CompCsXCollMark       = -59500;
    public const int CompCsXArrayMark      = -59600;

    // ── Cycle-guard HashSet harnesses (lazy ThreadStatic init; established pattern in TIC).
    // Existing precedent: ConstraintsState._fitStructBoundInProgress at :142, similar usage in
    // StateExtensions.{Lca,Gcd}.cs.
    [ThreadStatic] internal static HashSet<(StateCompositeConstraints, StateCompositeConstraints)> _compCsLcaInProgress;
    [ThreadStatic] internal static HashSet<(StateCompositeConstraints, StateCompositeConstraints)> _compCsUnifyInProgress;
    [ThreadStatic] internal static HashSet<(StateCompositeConstraints, StateCollection)> _compCsXCollLcaInProgress;
    [ThreadStatic] internal static HashSet<(StateCompositeConstraints, StateCollection)> _compCsXCollGcdInProgress;
    [ThreadStatic] internal static HashSet<(StateCompositeConstraints, StateCollection)> _compCsXCollUnifyInProgress;
    [ThreadStatic] internal static HashSet<(StateCompositeConstraints, StateArray)> _compCsXArrayLcaInProgress;
    [ThreadStatic] internal static HashSet<(StateCompositeConstraints, StateArray)> _compCsXArrayGcdInProgress;
    [ThreadStatic] internal static HashSet<(StateCompositeConstraints, StateArray)> _compCsXArrayUnifyInProgress;
    [ThreadStatic] internal static HashSet<TicNode> _compCsConcretestInProgress;
    [ThreadStatic] internal static HashSet<TicNode> _compCsAbstractestInProgress;

    private readonly ConstructorKind? _ancestor;
    private readonly ConstructorKind? _descendant;
    private readonly bool _isOptional;

    private StateCompositeConstraints(
        TicNode elementNode,
        ConstructorKind? ancestor,
        ConstructorKind? descendant,
        bool isOptional) {
        ElementNode = elementNode ?? throw new ArgumentNullException(nameof(elementNode));
        _ancestor = ancestor;
        _descendant = descendant;
        _isOptional = isOptional;
    }

    /// <summary>Lattice upper bound. <c>null</c> ≡ "no cap". Symmetric to
    /// <see cref="ConstraintsState.Ancestor"/>.</summary>
    public ConstructorKind? Ancestor => _ancestor;
    public bool HasAncestor => _ancestor.HasValue;

    /// <summary>Lattice lower bound. <c>null</c> ≡ unconstrained.</summary>
    public ConstructorKind? Descendant => _descendant;
    public bool HasDescendant => _descendant.HasValue;

    /// <summary>Element TIC node. Always present. Invariance: identity on
    /// <see cref="StateExtensions.Concretest"/> / <see cref="StateExtensions.Abstractest"/>;
    /// merged via <see cref="SolvingFunctions.MergeInplace"/> on cross-state intersection.</summary>
    public TicNode ElementNode { get; }

    /// <summary>Optional-wrapping flag. LCA: OR. Unify: OR. (CS precedent.)</summary>
    public bool IsOptional => _isOptional;

    /// <summary>True when interval is fully unconstrained — no floor, no cap, no IsOpt.
    /// Mirrors <see cref="ConstraintsState.NoConstrains"/>.</summary>
    public bool NoConstraints => !HasAncestor && !HasDescendant && !_isOptional;

    // ── Factory: single entry point. SimplifyOrNull-enforced.

    /// <summary>
    /// Build a fresh <see cref="StateCompositeConstraints"/>. Returns <c>null</c> if the
    /// candidate is unsatisfiable (interval `Desc > Anc` in the lattice).
    /// </summary>
    public static StateCompositeConstraints Create(
        TicNode elementNode,
        ConstructorKind? ancestor = null,
        ConstructorKind? descendant = null,
        bool isOptional = false) {
        var candidate = new StateCompositeConstraints(elementNode, ancestor, descendant, isOptional);
        return SimplifyOrNull(candidate);
    }

    /// <summary>Convenience factory: empty interval (no constraints, fresh element).</summary>
    public static StateCompositeConstraints Empty(TicNode elementNode) =>
        new(elementNode, ancestor: null, descendant: null, isOptional: false);

    // ── Copy-on-write transformers. Return:
    //    - same instance when no change (no allocation).
    //    - new StateCompositeConstraints when accepted.
    //    - StateCollection (or StateOptional(SC)) on point-collapse.
    //    - null on contradiction.

    /// <summary>Returns a state with the new ancestor cap. May collapse to <see cref="StateCollection"/>
    /// when the interval becomes a point. Returns <c>null</c> on contradiction.</summary>
    public ITicNodeState WithAncestor(ConstructorKind value) =>
        With(ancestor: value, descendant: _descendant, isOptional: _isOptional);

    /// <summary>Returns a state with the new descendant floor. May collapse.
    /// Returns <c>null</c> on contradiction.</summary>
    public ITicNodeState WithDescendant(ConstructorKind value) =>
        With(ancestor: _ancestor, descendant: value, isOptional: _isOptional);

    /// <summary>Returns a state with the new IsOptional flag. Cannot reject (IsOptional doesn't
    /// affect interval validity). May still collapse if the underlying interval is already a point.</summary>
    public ITicNodeState WithIsOptional(bool value) =>
        With(ancestor: _ancestor, descendant: _descendant, isOptional: value);

    private ITicNodeState With(ConstructorKind? ancestor, ConstructorKind? descendant, bool isOptional) {
        // Identity shortcut: no field changed → return self (no allocation).
        if (_ancestor == ancestor && _descendant == descendant && _isOptional == isOptional)
            return this;
        var candidate = new StateCompositeConstraints(ElementNode, ancestor, descendant, isOptional);
        var simplified = SimplifyOrNull(candidate);
        if (simplified == null) return null;
        return simplified.TryCollapseToPoint() ?? (ITicNodeState)simplified;
    }

    /// <summary>
    /// Pure function — no mutation. Returns the collapsed state
    /// (<see cref="StateCollection"/>, possibly wrapped in <see cref="StateOptional"/>)
    /// when <c>Descendant == Ancestor</c>, otherwise <c>null</c>.
    /// </summary>
    public ITicNodeState TryCollapseToPoint() {
        if (!_descendant.HasValue || !_ancestor.HasValue) return null;
        if (_descendant.Value != _ancestor.Value) return null;
        var sc = new StateCollection(_descendant.Value, ElementNode);
        return _isOptional ? (ITicNodeState)StateOptional.Of(sc) : sc;
    }

    /// <summary>
    /// Interval validity check — the only invariant SimplifyOrNull enforces.
    /// Returns the input unchanged on success, <c>null</c> on empty interval.
    /// <para>What this does NOT do (vs v5):
    /// <list type="bullet">
    /// <item>No predicate→Ancestor refinement (no predicates).</item>
    /// <item>No element-axis push (hashability is a CS-on-ElementNode mechanism).</item>
    /// <item>No collapse (caller invokes <see cref="TryCollapseToPoint"/>).</item>
    /// </list></para>
    /// </summary>
    internal static StateCompositeConstraints SimplifyOrNull(StateCompositeConstraints s) {
        if (s._descendant.HasValue && s._ancestor.HasValue
            && !ConstructorLattice.IsSubtypeOf(s._descendant.Value, s._ancestor.Value))
            return null; // empty interval — `Desc > Anc` in the lattice
        return s;
    }

    // ── ITicNodeState implementation.

    /// <summary>Always true — interval state is by definition mutable (under refinement).</summary>
    public bool IsMutable => true;

    /// <summary>Always false — interval state is unresolved; resolution → <see cref="StateCollection"/>.</summary>
    public bool IsSolved => false;

    public string Description => StateDescription;

    public string StateDescription => PrintState(0);

    public string PrintState(int depth) {
        if (depth > 100) return "compCs(...REQ...)";
        var desc = _descendant.HasValue ? KindName(_descendant.Value) : "_";
        var anc = _ancestor.HasValue ? KindName(_ancestor.Value) : "_";
        var elem = ElementNode.IsSolved
            ? ElementNode.State.PrintState(depth + 1)
            : ElementNode.Name?.ToString() ?? "?";
        var opt = _isOptional ? "?" : "";
        return $"compCs[{desc}..{anc}, e={elem}]{opt}";
    }

    public override string ToString() => PrintState(0);

    public bool CanBePessimisticConvertedTo(StatePrimitive primitive) {
        // CompCS represents a composite shape. The only primitive that universally
        // accepts a composite descendant is Any. All others reject.
        return primitive == StatePrimitive.Any;
    }

    private static string KindName(ConstructorKind k) => k switch {
        ConstructorKind.Any        => "any",
        ConstructorKind.Enumerable => "enumerable",
        ConstructorKind.FixedArray => "fixedArray",
        ConstructorKind.Array      => "array",
        ConstructorKind.List       => "list",
        ConstructorKind.Set        => "set",
        ConstructorKind.Map        => "map",
        _ => k.ToString(),
    };
}
