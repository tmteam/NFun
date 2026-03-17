namespace NFun.Tic.Algebra;

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
            if (lcaDesc == null)
                return ConstraintsState.Of(isComparable: comparable);
            // Collapse when lcaDesc is a solved composite type (arrays, funs, structs, optionals
            // have no type hierarchy beyond Any) or when lcaDesc is Any itself.
            // Keep constraint wrapper for primitives where further widening is possible
            // (e.g., [I24..] means "at least I24" — could resolve to I32, I64, Real).
            if (!comparable && lcaDesc is ITypeState { IsSolved: true } and (ICompositeState or StatePrimitive { Name: PrimitiveTypeName.Any }))
                return lcaDesc;
            return ConstraintsState.Of(lcaDesc, null, comparable);
        }
        if (b is ConstraintsState bc2)
            return bc2.HasDescendant ? a.Lca(bc2.Descendant) : a.Concretest();
        if (a is ConstraintsState)
            return b.Lca(a);

        // None: LCA(None, T) = Opt(T), LCA(None, None) = None, LCA(None, Opt(T)) = Opt(T)
        if (a is StatePrimitive { Name: PrimitiveTypeName.None })
            return LcaWithNone(b);
        if (b is StatePrimitive { Name: PrimitiveTypeName.None })
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
        if (other is StatePrimitive { Name: PrimitiveTypeName.None })
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

    private static ITicNodeState Lca(this StateStruct a, StateStruct b) {
        // Struct LCA = intersection of field names, with covariant field type LCA.
        // Covariance is sound because NFun structs are immutable (read-only fields).
        var nodes = new Dictionary<string, TicNode>();
        foreach (var aField in a.Fields)
        {
            var bField = b.GetFieldOrNull(aField.Key);
            if (bField == null) continue;

            // Resolve refs to get actual field states
            var aState = aField.Value.GetNonReference().State;
            var bState = bField.GetNonReference().State;

            // When both are unsolved constraints, use UnifyOrNull to
            // correctly intersect constraint intervals.
            // Otherwise use covariant Lca (handles mixed ConstraintsState/composite cases).
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
