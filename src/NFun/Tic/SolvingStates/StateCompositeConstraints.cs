using System;

namespace NFun.Tic.SolvingStates;

/// <summary>Composite-constraint interval state — peer of <see cref="ConstraintsState"/>
/// over the <see cref="ConstructorLattice"/>. Immutable: copy-on-write <see cref="WithAncestor"/>,
/// <see cref="WithDescendant"/>, <see cref="WithIsOptional"/>, <see cref="WithIsClearable"/>.
/// See specs_tic/Algebra/CompositeConstraints.md.</summary>
public sealed class StateCompositeConstraints : ITicNodeState {

    // Cycle-guard marks live in TicVisitMarks; pattern follows StateComposite.AllLeafTypes
    // (save-set-try/finally-restore). Marks are thread-isolated per the per-solver TicNode graph.

    private readonly ConstructorKind? _ancestor;
    private readonly ConstructorKind? _descendant;
    private readonly bool _isOptional;
    private readonly bool _isClearable;

    private StateCompositeConstraints(
        TicNode elementNode,
        ConstructorKind? ancestor,
        ConstructorKind? descendant,
        bool isOptional,
        bool isClearable) {
        ElementNode = elementNode ?? throw new ArgumentNullException(nameof(elementNode));
        _ancestor = ancestor;
        _descendant = descendant;
        _isOptional = isOptional;
        _isClearable = isClearable;
    }

    /// <summary>Lattice upper bound; <c>null</c> ≡ no cap.</summary>
    public ConstructorKind? Ancestor => _ancestor;
    public bool HasAncestor => _ancestor.HasValue;

    public ConstructorKind? Descendant => _descendant;
    public bool HasDescendant => _descendant.HasValue;

    /// <summary>Element TIC node. Always present.</summary>
    public TicNode ElementNode { get; }

    /// <summary>Optional-wrapping flag.</summary>
    public bool IsOptional => _isOptional;

    /// <summary>Clearable typeclass marker; resolution narrows to kinds where
    /// <see cref="ConstructorLattice.IsClearable"/> is true.</summary>
    public bool IsClearable => _isClearable;

    /// <summary>True when interval is fully unconstrained.</summary>
    public bool NoConstraints => !HasAncestor && !HasDescendant && !_isOptional && !_isClearable;

    /// <summary>Build a fresh state. Null if the interval is unsatisfiable.</summary>
    public static StateCompositeConstraints Create(
        TicNode elementNode,
        ConstructorKind? ancestor = null,
        ConstructorKind? descendant = null,
        bool isOptional = false,
        bool isClearable = false) {
        var candidate = new StateCompositeConstraints(elementNode, ancestor, descendant, isOptional, isClearable);
        return SimplifyOrNull(candidate);
    }

    public static StateCompositeConstraints Empty(TicNode elementNode) =>
        new(elementNode, ancestor: null, descendant: null, isOptional: false, isClearable: false);

    /// <summary>State with new ancestor; may collapse, null on contradiction.</summary>
    public ITicNodeState WithAncestor(ConstructorKind value) =>
        With(ancestor: value, descendant: _descendant, isOptional: _isOptional, isClearable: _isClearable);

    /// <summary>State with new descendant; may collapse, null on contradiction.</summary>
    public ITicNodeState WithDescendant(ConstructorKind value) =>
        With(ancestor: _ancestor, descendant: value, isOptional: _isOptional, isClearable: _isClearable);

    /// <summary>State with new IsOptional. Never null; may collapse.</summary>
    public ITicNodeState WithIsOptional(bool value) =>
        With(ancestor: _ancestor, descendant: _descendant, isOptional: value, isClearable: _isClearable);

    /// <summary>State with new IsClearable. Never null at construction (narrowing is at
    /// resolution time).</summary>
    public ITicNodeState WithIsClearable(bool value) =>
        With(ancestor: _ancestor, descendant: _descendant, isOptional: _isOptional, isClearable: value);

    private ITicNodeState With(ConstructorKind? ancestor, ConstructorKind? descendant, bool isOptional, bool isClearable) {
        if (_ancestor == ancestor && _descendant == descendant && _isOptional == isOptional && _isClearable == isClearable)
            return this;
        var candidate = new StateCompositeConstraints(ElementNode, ancestor, descendant, isOptional, isClearable);
        var simplified = SimplifyOrNull(candidate);
        if (simplified == null) return null;
        return simplified.TryCollapseToPoint() ?? (ITicNodeState)simplified;
    }

    /// <summary>Pure: collapse to <see cref="StateCollection"/> (possibly Optional-wrapped)
    /// when <c>Descendant == Ancestor</c>, else null.</summary>
    public ITicNodeState TryCollapseToPoint() {
        if (!_descendant.HasValue || !_ancestor.HasValue) return null;
        if (_descendant.Value != _ancestor.Value) return null;
        // IsClearable at collapse: K must satisfy IsClearable, else SimplifyOrNull is broken.
        if (_isClearable && !ConstructorLattice.IsClearable(_descendant.Value)) return null;
        var sc = new StateCollection(_descendant.Value, ElementNode);
        return _isOptional ? (ITicNodeState)StateOptional.Of(sc) : sc;
    }

    /// <summary>Interval validity check. Returns input unchanged on success, null on empty
    /// interval. Does NOT collapse — callers invoke <see cref="TryCollapseToPoint"/>.</summary>
    internal static StateCompositeConstraints SimplifyOrNull(StateCompositeConstraints s) {
        if (s._descendant.HasValue && s._ancestor.HasValue
            && !ConstructorLattice.IsSubtypeOf(s._descendant.Value, s._ancestor.Value))
            return null;
        // IsClearable narrows the type-set — pinned non-Clearable Descendant ⇒ empty.
        if (s._isClearable && s._descendant.HasValue
            && !ConstructorLattice.IsClearable(s._descendant.Value))
            return null;
        return s;
    }

    public bool IsMutable => true;
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
        var clr = _isClearable ? "@clearable" : "";
        return $"compCs[{desc}..{anc}, e={elem}]{opt}{clr}";
    }

    public override string ToString() => PrintState(0);

    public bool CanBePessimisticConvertedTo(StatePrimitive primitive) {
        return primitive == StatePrimitive.Any; // composite-shape descendant: only Any accepts
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
