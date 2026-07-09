namespace NFun.Tic.Algebra;

using SolvingStates;

public static partial class StateExtensions {
    /// <summary>
    /// ↓ₛA — Destruction-snapshot operator (RESOLUTION layer, Solve* family — NOT a pure
    /// projection; see Algebra.md §Слой резолюции and TicPreferred.md §5.4).
    ///
    /// Minimal representative of the state that PRESERVES resolution metadata (Preferred)
    /// where the pure projection ↓ (<see cref="Concretest"/>) would lose it. Used where the
    /// result is MATERIALIZED into stored graph state and later consumed by Destruction /
    /// Finalize (ConstraintsState.AddDescendant snapshots, LCA's stored one-sided
    /// descendant projections). Two arms differ from ↓ (extracted from it by debt #19):
    ///
    ///  1. optional CS with a fitting Preferred resolves to opt(Preferred) instead of
    ///     opt(↓D) — a resolution CHOICE by the hint;
    ///  2. array elements keep the CS[D, pref] hint-carrier (ancestor dropped, hint kept)
    ///     instead of collapsing to bare ↓D, so Preferred survives Destruction snapshots
    ///     of array descendants (float-family literals inside arrays).
    ///
    /// Contract: on Preferred-free states ↓ₛA ≡ ↓A (delegation law, pinned in
    /// ConcretestSnapshotTest); the interval part of the result always Fit-satisfies the
    /// source constraint (resolution never leaves the computed set).
    /// </summary>
    public static ITicNodeState ConcretestSnapshot(this ITicNodeState a) =>
        a switch {
            StatePrimitive => a,
            ConstraintsState cs => SnapshotConstraints(cs),
            StateRefTo aref => aref.Element.ConcretestSnapshot(),
            StateArray arr => StateArray.Of(SnapshotArrayElement(arr.Element)),
            StateOptional opt => SnapshotOptional(opt),
            StateFun f => SnapshotFun(f),
            StateStruct s => s.ConcretestStruct(),
            _ => a
        };

    /// <summary>
    /// Snapshot for array element: if element is CS with Preferred that differs from Desc,
    /// preserve the CS (with Preferred) instead of collapsing to bare Primitive.
    /// This ensures array Desc snapshots carry Preferred through Destruction
    /// (TicPreferred.md §5.4).
    /// </summary>
    private static ITicNodeState SnapshotArrayElement(ITicNodeState element) {
        if (element is ConstraintsState cs
            && cs.Preferred != null
            && cs.HasDescendant
            && cs.Descendant is StatePrimitive desc
            && !desc.Equals(cs.Preferred)
            && desc.CanBePessimisticConvertedTo(cs.Preferred)) {
            var result = ConstraintsState.Of(desc, isComparable: cs.IsComparable, isOptional: cs.IsOptional);
            result.Preferred = cs.Preferred;
            return result;
        }
        return element.ConcretestSnapshot();
    }

    private static ITicNodeState SnapshotConstraints(ConstraintsState cs) {
        var inner = cs.HasDescendant
            ? cs.Descendant.ConcretestSnapshot()
            : ConstraintsState.Of(isComparable: cs.IsComparable);
        if (cs.IsOptional) {
            // Resolution arm: use Preferred when a narrower solved Desc is available —
            // the snapshot resolves the point by the hint (NOT part of pure ↓).
            if (cs.Preferred != null && inner is StatePrimitive ip
                && ip.CanBePessimisticConvertedTo(cs.Preferred))
                inner = cs.Preferred;
            if (inner == StatePrimitive.Any)
                return StatePrimitive.Any;
            // Rule B (canonical Optional form) — SHARED with pure ↓ (debt #30):
            // the delegation law ↓ₛ ≡ ↓ on hint-free states is structural for this arm.
            return LiftOptional(cs, inner);
        }
        return inner;
    }

    private static ITicNodeState SnapshotOptional(StateOptional opt) {
        var inner = opt.Element.ConcretestSnapshot();
        return inner == StatePrimitive.Any ? StatePrimitive.Any : StateOptional.Of(inner);
    }

    private static ITicNodeState SnapshotFun(StateFun f) {
        var returnNode = TicNode.CreateInvisibleNode(f.ReturnType.ConcretestSnapshot());
        var argNodes = new TicNode[f.ArgsCount];
        for (int i = 0; i < f.ArgsCount; i++)
            argNodes[i] = TicNode.CreateInvisibleNode(f.ArgNodes[i].State.Abstractest());
        return StateFun.Of(argNodes, returnNode);
    }
}
