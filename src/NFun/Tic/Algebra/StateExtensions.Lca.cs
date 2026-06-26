namespace NFun.Tic.Algebra;

using System;
using System.Collections.Generic;
using SolvingStates;
using static SolvingStates.StatePrimitive;

public static partial class StateExtensions {
    public static ITicNodeState Lca(this ITicNodeState a, ITicNodeState b) {
        if (a is StateRefTo aref)
            return aref.Element.Lca(b);
        if (b is StateRefTo bref)
            return a.Lca(bref.Element);
        // LCA is symmetric — both dispatches route into LcaCompCs.
        if (a is StateCompositeConstraints acompcs)
            return acompcs.LcaCompCs(b);
        if (b is StateCompositeConstraints bcompcs)
            return bcompcs.LcaCompCs(a);
        if (a is ConstraintsState ac && b is ConstraintsState bc)
        {
            var descA = ac.HasDescendant ? ac.Descendant : null;
            var descB = bc.HasDescendant ? bc.Descendant : null;
            var lcaDesc = (descA, descB) switch {
                (null, null) => null,
                (null, _)    => descB.Concretest(),
                (_, null)    => descA.Concretest(),
                _            => descA.Lca(descB)
            };
            var comparable = ac.IsComparable && bc.IsComparable;
            var isOptional = ac.IsOptional || bc.IsOptional;
            // Preferred is a resolution hint, not algebra: agree → keep; one-sided → keep; differ → drop.
            var preferred = (ac.Preferred, bc.Preferred) switch {
                (null, null)  => (StatePrimitive)null,
                (null, var p) => p,
                (var p, null) => p,
                var (pa, pb)  => pa.Equals(pb) ? pa : null
            };
            if (lcaDesc == null) {
                var result = ConstraintsState.Of(isComparable: comparable, isOptional: isOptional);
                result.Preferred = preferred;
                return result;
            }
            if (!comparable
                && !isOptional
                && preferred == null
                && lcaDesc is ITypeState { IsSolved: true } and (ICompositeState or StatePrimitive { Name: PrimitiveTypeName.Any }))
                return lcaDesc;
            var cs = ConstraintsState.Of(lcaDesc, null, comparable, isOptional);
            cs.Preferred = preferred;
            return cs;
        }
        if (b is ConstraintsState bc2) {
            var inner = bc2.HasDescendant ? a.Lca(bc2.Descendant) : a.Concretest();
            if (bc2.IsOptional && !inner.Equals(Any))
                return ConstraintsState.Of(inner, isOptional: true);
            // Resolve opt-element to Preferred so AddDescendant snapshots carry the hint.
            // Example: LCA(opt(empty), CS[U8..Re,P=I32]) = opt(I32).
            if (bc2.Preferred != null
                && inner is StateOptional optInner
                && optInner.IsSolved
                && optInner.Element is StatePrimitive elemP
                && !elemP.Equals(bc2.Preferred)
                && elemP.CanBePessimisticConvertedTo(bc2.Preferred)
                && bc2.CanBeConvertedTo(bc2.Preferred))
                return StateOptional.Of(bc2.Preferred);
            return inner;
        }
        if (a is ConstraintsState)
            return b.Lca(a); // symmetry routes to the bc2 branch

        if (a == None)
            return LcaWithNone(b);
        if (b == None)
            return LcaWithNone(a);
        if (a is StateOptional aopt)
            return LcaWithOptional(aopt, b);
        if (b is StateOptional bopt)
            return LcaWithOptional(bopt, a);

        if (a is StatePrimitive ap)
            return b is StatePrimitive bp ? ap.GetLastCommonPrimitiveAncestor(bp) : Any;
        if (b is StatePrimitive)
            return Any;
        return a switch {
            StateArray aarr => b switch {
                StateArray barr => StateArray.Of(aarr.Element.Lca(barr.Element)),
                // Array-branch SC ∨ StateArray = StateArray with element-LCA.
                StateCollection bcoll
                    when bcoll.Constructor == ConstructorKind.List
                      || bcoll.Constructor == ConstructorKind.Array
                      || bcoll.Constructor == ConstructorKind.FixedArray =>
                    StateArray.Of(aarr.Element.Lca(bcoll.Element)),
                _ => Any
            },
            StateFun af => b is StateFun bf ? af.Lca(bf) : Any,
            StateStruct astruct => b is StateStruct bstruct ? astruct.Lca(bstruct) : Any,
            // See specs_tic/Algebra/LcaOrShareIdentity.md for the identity-sharing fallback.
            StateCollection acoll => acoll.LcaOrShareIdentity(b) ?? Any,
            _ => Any
        };
    }

    private static ITicNodeState LcaWithNone(ITicNodeState other) {
        if (other.Equals(None))
            return None;
        if (other.Equals(Any))
            return Any;
        if (other is StateOptional opt)
            return opt.Element.Equals(Any) ? Any : other; // opt(any) collapses to any
        return StateOptional.Of(other);
    }

    private static ITicNodeState LcaWithOptional(StateOptional opt, ITicNodeState other) {
        ITicNodeState inner;
        if (other is StateOptional otherOpt)
            inner = opt.Element.Lca(otherOpt.Element);
        else
            inner = opt.Element.Lca(other);
        if (inner.Equals(Any))
            return Any;
        return StateOptional.Of(inner);
    }

    // Cycle guards: TypeName for named μ-types (snapshots break ref-equality), ref-equality for anonymous.
    [ThreadStatic] private static List<(StateStruct, StateStruct)> _structLcaInProgress;
    [ThreadStatic] private static List<string> _structLcaNamesInProgress;

    private static ITicNodeState Lca(this StateStruct a, StateStruct b) {
        if (a.TypeName != null && a.TypeName == b.TypeName) {
            var nameGuard = _structLcaNamesInProgress ??= new();
            for (int i = 0; i < nameGuard.Count; i++)
                if (nameGuard[i] == a.TypeName)
                    return a;
            nameGuard.Add(a.TypeName);
            try {
                return LcaStructFields(a, b);
            } finally {
                nameGuard.RemoveAt(nameGuard.Count - 1);
            }
        }
        var guard = _structLcaInProgress ??= new();
        for (int i = 0; i < guard.Count; i++)
            if (ReferenceEquals(guard[i].Item1, a) && ReferenceEquals(guard[i].Item2, b)
                || ReferenceEquals(guard[i].Item1, b) && ReferenceEquals(guard[i].Item2, a))
                return a;
        guard.Add((a, b));
        try {
            return LcaStructFields(a, b);
        } finally {
            guard.RemoveAt(guard.Count - 1);
        }
    }

    private static ITicNodeState LcaStructFields(StateStruct a, StateStruct b) {
        bool bothMutable = a is StateMutableStruct && b is StateMutableStruct;
        bool eitherMutable = a is StateMutableStruct || b is StateMutableStruct;

        var nodes = new Dictionary<string, TicNode>(StringComparer.OrdinalIgnoreCase);
        bool unifyFailed = false;
        foreach (var aField in a.Fields)
        {
            var bField = b.GetFieldOrNull(aField.Key);
            if (bField == null) continue;

            var aState = aField.Value.GetNonReference().State;
            var bState = bField.GetNonReference().State;

            ITicNodeState fieldType;
            if (bothMutable && !unifyFailed)
            {
                fieldType = aState.UnifyOrNull(bState);
                if (fieldType == null)
                {
                    unifyFailed = true; // downgrade entire result to immutable Struct
                    fieldType = aState.Lca(bState);
                }
            }
            else if (aState is ConstraintsState && bState is ConstraintsState)
                fieldType = aState.UnifyOrNull(bState);
            else
                fieldType = aState.Lca(bState);

            if (fieldType == null) continue;
            nodes.Add(aField.Key, TicNode.CreateInvisibleNode(fieldType));
        }

        if (bothMutable && !unifyFailed)
            return new StateMutableStruct(nodes, true);
        // IsOptionalSourced is one-sided identity metadata — not propagated through LCA.
        return new StateStruct(nodes, true) {
            TypeName = StateStruct.LcaTypeName(a.TypeName, b.TypeName),
        };
    }

    private static ITicNodeState Lca(this StateFun a, StateFun b) {
        if (a.ArgsCount != b.ArgsCount)
            return Any;
        var returnState = a.ReturnType.Lca(b.ReturnType);
        var argNodes = new TicNode[a.ArgsCount];

        for (var i = 0; i < a.ArgNodes.Length; i++)
        {
            var aNode = a.ArgNodes[i];
            var bNode = b.ArgNodes[i];
            var gcd = aNode.State.Gcd(bNode.State);
            if (gcd == null)
                return Any;
            argNodes[i] = TicNode.CreateInvisibleNode(gcd);
        }

        return StateFun.Of(argNodes, TicNode.CreateInvisibleNode(returnState));
    }
}
