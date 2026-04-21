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
            return inner;
        }
        if (a is ConstraintsState)
            return b.Lca(a); // symmetric: hits bc2 branch above

        // None: LCA(None, T) = Opt(T), LCA(None, None) = None, LCA(None, Opt(T)) = Opt(T)
        if (a == StatePrimitive.None)
            return LcaWithNone(b);
        if (b == StatePrimitive.None)
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
            StateArray aarr => b is StateArray barr ? StateArray.Of(aarr.Element.Lca(barr.Element)) : Any,
            StateFun af => b is StateFun bf ? af.Lca(bf) : Any,
            StateStruct astruct => b is StateStruct bstruct ? astruct.Lca(bstruct) : Any,
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

    // Cycle guard for recursive struct LCA (e.g., tree = {children: tree[]}).
    // Uses ReferenceEquals — structural Equals would also infinitely recurse.
    [ThreadStatic] private static List<(StateStruct, StateStruct)> _structLcaInProgress;

    private static ITicNodeState Lca(this StateStruct a, StateStruct b) {
        // LCA(T, T) = T — on cycle, return as-is (structurally equivalent).
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
        // Struct LCA = intersection of field names, with covariant field type LCA.
        var nodes = new Dictionary<string, TicNode>(StringComparer.OrdinalIgnoreCase);
        foreach (var aField in a.Fields)
        {
            var bField = b.GetFieldOrNull(aField.Key);
            if (bField == null) continue;

            var aState = aField.Value.GetNonReference().State;
            var bState = bField.GetNonReference().State;

            ITicNodeState fieldType;
            if (aState is ConstraintsState && bState is ConstraintsState)
                fieldType = aState.UnifyOrNull(bState);
            else
                fieldType = aState.Lca(bState);

            if (fieldType == null) continue;
            nodes.Add(aField.Key, TicNode.CreateInvisibleNode(fieldType));
        }

        return new StateStruct(nodes, true);
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
