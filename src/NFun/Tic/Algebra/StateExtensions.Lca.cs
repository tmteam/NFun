namespace NFun.Tic.Algebra;

using System.Collections.Generic;
using NFun.Tic.SolvingStates;
using static NFun.Tic.SolvingStates.StatePrimitive;

public static partial class StateExtensions {
    public static ITicNodeState Lca(this ITicNodeState a, ITicNodeState b) {
        if (a is StateRefTo aref)
            return Lca(aref.Element, b);
        if (b is StateRefTo bref)
            return Lca(a, bref.Element);
        if (a is ConstraintsState ac && b is ConstraintsState bc)
        {
            // Both are constraints — compute LCA of their concretest forms
            var descA = ac.HasDescendant ? ac.Descendant : null;
            var descB = bc.HasDescendant ? bc.Descendant : null;
            ITicNodeState lcaDesc = (descA, descB) switch {
                (null, null) => null,
                (null, _)    => Concretest(descB),
                (_, null)    => Concretest(descA),
                _            => Lca(descA, descB)
            };
            var comparable = ac.IsComparable && bc.IsComparable;
            if (lcaDesc == null)
                return ConstraintsState.Of(isComparable: comparable);
            // If result is a solved concrete type and no comparable constraint — return it directly
            if (lcaDesc is ITypeState { IsSolved: true } && !comparable)
                return lcaDesc;
            return ConstraintsState.Of(lcaDesc, null, comparable);
        }
        if (b is ConstraintsState bc2)
            return bc2.HasDescendant ? Lca(a, bc2.Descendant) : Concretest(a);
        if (a is ConstraintsState)
            return Lca(b, a);

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
        if (a is StateArray aarr)
            return b is StateArray barr ? StateArray.Of(Lca(aarr.Element, barr.Element)) : Any;
        if (a is StateFun af)
            return b is StateFun bf ? Lca(af, bf) : Any;
        if (a is StateStruct astruct)
            return b is StateStruct bstruct ? Lca(astruct, bstruct) : Any;
        return Any;
    }

    private static ITicNodeState LcaWithNone(ITicNodeState other) {
        if (other is StatePrimitive { Name: PrimitiveTypeName.None })
            return StatePrimitive.None;
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
            inner = Lca(opt.Element, otherOpt.Element);
        else
            inner = Lca(opt.Element, other);
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

            // When at least one field is solved, use covariant Lca.
            // When both are unsolved constraints, use UnifyOrNull to
            // correctly intersect constraint intervals.
            ITicNodeState fieldType;
            if (aField.Value.IsSolved || bField.IsSolved)
                fieldType = Lca(aField.Value.State, bField.State);
            else
                fieldType = UnifyOrNull(aField.Value.State, bField.State);

            if (fieldType == null) continue;
            nodes.Add(aField.Key, TicNode.CreateInvisibleNode(fieldType));
        }

        return new StateStruct(nodes, true);
    }

    private static ITicNodeState Lca(this StateFun a, StateFun b) {
        if (a.ArgsCount != b.ArgsCount)
            return Any;
        var returnState = Lca(a.ReturnType, b.ReturnType);
        var argNodes = new TicNode[a.ArgsCount];

        for (var i = 0; i < a.ArgNodes.Length; i++)
        {
            var aNode = a.ArgNodes[i];
            var bNode = b.ArgNodes[i];
            var gcd = Gcd(aNode.State, bNode.State);
            if (gcd == null)
                return Any;
            argNodes[i] = TicNode.CreateInvisibleNode(gcd);
        }

        return StateFun.Of(argNodes, TicNode.CreateInvisibleNode(returnState));
    }
}
