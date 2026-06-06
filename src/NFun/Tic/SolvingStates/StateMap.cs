using System;

namespace NFun.Tic.SolvingStates;

/// <summary>
/// TIC state for lang-mode <c>map&lt;K, V&gt;</c> — two invariant element
/// arguments (key + value). Separate from <see cref="StateCollection"/>
/// because the structural shape differs: collections have one element node,
/// Map has two.
///
/// Constructor is fixed to <see cref="ConstructorKind.Map"/>. Variance: both
/// arguments are invariant (per Stage 0 uniform-invariance).
/// </summary>
public sealed class StateMap : StateComposite {

    public StateMap(TicNode keyNode, TicNode valueNode) {
        KeyNode = keyNode;
        ValueNode = valueNode;
        Arguments = new[] {
            new CompositeArg(keyNode, Variance.Invariant),
            new CompositeArg(valueNode, Variance.Invariant),
        };
    }

    public static StateMap Of(ITicNodeState key, ITicNodeState value) =>
        new(WrapNode(key), WrapNode(value));

    public static StateMap Of(TicNode keyNode, TicNode valueNode) =>
        new(keyNode, valueNode);

    private static TicNode WrapNode(ITicNodeState state) => state switch {
        ITypeState t        => TicNode.CreateTypeVariableNode(t),
        StateRefTo refTo    => refTo.Node,
        ConstraintsState c  => TicNode.CreateInvisibleNode(c),
        _ => throw new InvalidOperationException($"StateMap cannot have state {state}")
    };

    public TicNode KeyNode { get; }
    public TicNode ValueNode { get; }

    public ITicNodeState KeyState   => KeyNode.State;
    public ITicNodeState ValueState => ValueNode.State;

    public override ConstructorKind Constructor => ConstructorKind.Map;
    public override CompositeArg[] Arguments { get; }

    public override ICompositeState GetNonReferenced() =>
        new StateMap(KeyNode.GetNonReference(), ValueNode.GetNonReference());

    public override ITypeState GetLastCommonAncestorOrNull(ITypeState otherType) {
        if (otherType is not StateMap other) return StatePrimitive.Any;
        if (KeyState is not ITypeState ak || ValueState is not ITypeState av) return null;
        if (other.KeyState is not ITypeState bk || other.ValueState is not ITypeState bv) return null;
        // Invariant in both args — equality required, else widens to Any.
        return ak.Equals(bk) && av.Equals(bv) ? (ITypeState)this : StatePrimitive.Any;
    }

    /// <summary>
    /// LCA + identity-sharing for two StateMaps with unresolved key or value
    /// nodes (CS / RefTo). Mirror of <see cref="StateCollection.LcaOrShareIdentity"/>
    /// — when pure LCA would widen to Any (because element nodes aren't yet
    /// solved), merge node identities so future constraints land on the same
    /// nodes. Stage 0 uniform invariance still holds: shared identity makes
    /// the maps equal once concretised.
    /// </summary>
    internal ITypeState LcaOrShareIdentity(ITicNodeState otherType) {
        var pure = otherType is ITypeState ts ? GetLastCommonAncestorOrNull(ts) : null;
        if (pure != null) return pure;
        if (otherType is StateMap other)
        {
            if (!ReferenceEquals(KeyNode, other.KeyNode))
                Tic.SolvingFunctions.MergeInplace(KeyNode, other.KeyNode);
            if (!ReferenceEquals(ValueNode, other.ValueNode))
                Tic.SolvingFunctions.MergeInplace(ValueNode, other.ValueNode);
            return this;
        }
        return null;
    }

    public override bool Equals(object obj) =>
        obj is StateMap other
        && ReferenceEquals(KeyNode.GetNonReference(), other.KeyNode.GetNonReference())
        && ReferenceEquals(ValueNode.GetNonReference(), other.ValueNode.GetNonReference());

    public override int GetHashCode() => 0;

    public override string PrintState(int depth) {
        if (depth > 100) return "map(...REQ...)";
        return $"map({KeyState.PrintState(depth + 1)},{ValueState.PrintState(depth + 1)})";
    }

    public override string ToString() =>
        (KeyNode.IsSolved && ValueNode.IsSolved)
            ? $"map({KeyNode},{ValueNode})"
            : $"map({KeyNode.Name},{ValueNode.Name})";
}
