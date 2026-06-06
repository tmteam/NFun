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
        // StateCompositeConstraints — Stage C.2. LCA is symmetric, so reverse-direction routes
        // through LcaCompCs as well (handler dispatches by RHS type).
        if (a is StateCompositeConstraints acompcs)
            return acompcs.LcaCompCs(b);
        if (b is StateCompositeConstraints bcompcs)
            return bcompcs.LcaCompCs(a);
        if (a is ConstraintsState ac && b is ConstraintsState bc)
        {
            // Both are constraints — compute LCA of their concretest forms
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
            // Propagate Preferred hint through LCA (not algebraic, just a resolution hint).
            // Rule: if both agree, keep; if only one has it, keep; if both differ, keep null.
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
            if (!comparable && !isOptional && preferred == null && lcaDesc is ITypeState { IsSolved: true } and (ICompositeState or StatePrimitive { Name: PrimitiveTypeName.Any }))
                return lcaDesc;
            var cs = ConstraintsState.Of(lcaDesc, null, comparable, isOptional);
            cs.Preferred = preferred;
            return cs;
        }
        if (b is ConstraintsState bc2) {
            var inner = bc2.HasDescendant ? a.Lca(bc2.Descendant) : a.Concretest();
            // Propagate IsOptional: LCA(T, C[.., opt=true]) = C[T.., opt=true]
            if (bc2.IsOptional && !inner.Equals(Any))
                return ConstraintsState.Of(inner, isOptional: true);
            // Preserve Preferred through LCA when result is Optional wrapping
            // a primitive narrower than Preferred. Resolves the Optional's element
            // to Preferred so snapshots in AddDescendant carry the resolution hint.
            // Example: LCA(opt(empty), CS[U8..Re,P=I32]) = opt(U8) → opt(I32).
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
            return b.Lca(a); // symmetric: hits bc2 branch above

        // None: LCA(None, T) = Opt(T), LCA(None, None) = None, LCA(None, Opt(T)) = Opt(T)
        if (a == None)
            return LcaWithNone(b);
        if (b == None)
            return LcaWithNone(a);
        // Optional: covariant wrapper
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
                // Stage 0 hierarchy: any Array-branch StateCollection (List /
                // Array / FixedArray) widens to T[] with element LCA. Lets a
                // variable holding both an array result and a lang-collection
                // literal stay typed as T[] instead of widening to Any.
                StateCollection bcoll
                    when bcoll.Constructor == ConstructorKind.List
                      || bcoll.Constructor == ConstructorKind.Array
                      || bcoll.Constructor == ConstructorKind.FixedArray =>
                    StateArray.Of(aarr.Element.Lca(bcoll.Element)),
                _ => Any
            },
            StateFun af => b is StateFun bf ? af.Lca(bf) : Any,
            StateStruct astruct => b is StateStruct bstruct ? astruct.Lca(bstruct) : Any,
            // StateCollection: uniform invariance for cross-Constructor; cross-family
            // edge to StateArray handled inside GetLastCommonAncestorOrNull.
            // `LcaOrShareIdentity` adds the identity-sharing fallback (with
            // explicit side-effect contract) for same-kind unresolved elements
            // — needed for nested `list<list<T>>` literals so the outer-LCA
            // doesn't widen to Any.
            StateCollection acoll => acoll.LcaOrShareIdentity(b) ?? Any,
            // StateMap: same identity-sharing pattern as StateCollection — needed
            // for nested `__mkMap({k=…, v=__mkMap(…)})` so the outer factory's
            // value-LCA doesn't widen to Any when both inner instances are still
            // CS-typed.
            StateMap amap => amap.LcaOrShareIdentity(b) ?? Any,
            _ => Any
        };
    }

    private static ITicNodeState LcaWithNone(ITicNodeState other) {
        if (other.Equals(None))
            return None;
        if (other.Equals(Any))
            return Any; // None ≤ Any → LCA = Any
        if (other is StateOptional opt)
            return opt.Element.Equals(Any) ? Any : other; // None ≤ Opt(T) → LCA = Opt(T); opt(any) = any
        // None ^ T = Opt(T) for non-Any types
        return StateOptional.Of(other);
    }

    private static ITicNodeState LcaWithOptional(StateOptional opt, ITicNodeState other) {
        ITicNodeState inner;
        if (other is StateOptional otherOpt)
            inner = opt.Element.Lca(otherOpt.Element);
        else
            inner = opt.Element.Lca(other);
        // opt(any) = any (collapses)
        if (inner.Equals(Any))
            return Any;
        return StateOptional.Of(inner);
    }

    // Cycle guard for recursive struct LCA. Two layers:
    //   1. Named-type short-circuit: if both sides have the same TypeName, the μ-cycle
    //      reached itself through Lca/Gcd interleaving (Lca creates new struct snapshots
    //      every level, so ref-equality misses). Coinductively return one side.
    //   2. Reference-equals fallback for anonymous structs (no TypeName).
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
        // Anonymous struct ref-equality guard (pre-existing).
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

        // MutStruct x MutStruct: try invariant Unify per field; if any fails, downgrade to immutable Struct.
        // MutStruct x Struct (or vice versa): always upcast to immutable Struct with covariant LCA.
        // Struct x Struct: covariant LCA per field (existing behavior).
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
                // Invariant: try Unify first
                fieldType = aState.UnifyOrNull(bState);
                if (fieldType == null)
                {
                    // Unify failed — downgrade entire result to immutable Struct, use LCA
                    unifyFailed = true;
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

        // Both MutStruct and all fields unified → result is MutStruct.
        // Otherwise → immutable Struct (mixed mutability or Unify failure).
        if (bothMutable && !unifyFailed)
            return new StateMutableStruct(nodes, true);
        // Note: IsOptionalSourced is NOT propagated through Lca. Lca produces
        // the structurally-narrowest common ancestor — its identity should not
        // inherit a marker that originated only on one side. The marker
        // matters for cycle-rescue gating, where Pull/Push merges share the
        // identity (handled in PullConstraintsFunctions.Apply(Struct,Struct)).
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
