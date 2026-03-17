namespace NFun.Tic.Algebra;

using System;
using System.Collections.Generic;
using SolvingStates;
using static SolvingStates.StatePrimitive;

public static partial class StateExtensions {
    public static ITicNodeState UnifyOrNull(this ITicNodeState a, ITicNodeState b) {
        if (a.Equals(b))
            return a;

        if (a is StateRefTo ar)
            return ar.GetNonReference().UnifyOrNull(b);
        if (b is StateRefTo br)
            return a.UnifyOrNull(br.GetNonReference());

        // Any is top of ALL types: None ≤ Any, Opt(T) ≤ Any
        if (a.Equals(Any))
            return b;
        if (b.Equals(Any))
            return a;

        if (a is ConstraintsState ac)
        {
            if (b is ConstraintsState bc)
                return ac.UnifyOrNull(bc);
            else if (b.FitsInto(ac))
                return b;
            else
                return null;
        }

        if (b is ConstraintsState)
            return b.UnifyOrNull(a);
        // Optional: Unify(Opt(A), Opt(B)) = Opt(Unify(A,B)), Unify(Opt, None) = None
        if (a is StateOptional aOpt)
        {
            if (b is StateOptional bOpt)
            {
                var elem = aOpt.Element.UnifyOrNull(bOpt.Element);
                return elem == null ? null : StateOptional.Of(elem);
            }
            if (b is StatePrimitive { Name: PrimitiveTypeName.None })
                return b; // None ≤ Opt(T), intersection = None
            return null;
        }
        if (b is StateOptional)
            return b.UnifyOrNull(a);

        if (a.GetType() != b.GetType())
            return null;
        if (a is StatePrimitive)
            return null;
        if (b is StatePrimitive)
            return null;
        if (a is StateArray aArr)
            return aArr.UnifyOrNull(b as StateArray);
        if (a is StateFun aFun)
            return aFun.UnifyOrNull(b as StateFun);
        if (a is StateStruct aStr)
            return aStr.UnifyOrNull(b as StateStruct);
        throw new NotSupportedException($"Unitype({a}, {b})");
    }

    private static ITicNodeState UnifyOrNull(this ConstraintsState a, ConstraintsState b) {
        var comparable = a.IsComparable || b.IsComparable;
        ITicNodeState descendant = null;
        if (!a.HasDescendant)
            descendant = b.Descendant;
        else if (!b.HasDescendant)
            descendant = a.Descendant;
        else
            descendant = a.Descendant.Lca(b.Descendant);

        StatePrimitive ancestor = null;
        if (!a.HasAncestor)
            ancestor = b.Ancestor;
        else if (!b.HasAncestor)
            ancestor = a.Ancestor;
        else
        {
            ancestor = a.Ancestor.GetFirstCommonDescendantOrNull(b.Ancestor);
            if (ancestor == null)
                return null;
        }

        return ConstraintsState.Of(descendant, ancestor, comparable).SimplifyOrNull();
    }

    private static ITicNodeState UnifyOrNull(this StateArray a, StateArray b) {
        var uniElement = a.Element.UnifyOrNull(b.Element);
        return uniElement == null ? null : StateArray.Of(uniElement);
    }

    private static ITicNodeState UnifyOrNull(this StateFun a, StateFun b) {
        if (a.ArgsCount != b.ArgsCount)
            return null;
        var argNodes = new TicNode[a.ArgsCount];
        for (int i = 0; i < a.ArgsCount; i++)
        {
            var uniArg = a.ArgNodes[i].State.UnifyOrNull(b.ArgNodes[i].State);
            if (uniArg == null)
                return null;
            argNodes[i] = TicNode.CreateInvisibleNode(uniArg);
        }

        var retArg = a.ReturnType.UnifyOrNull(b.ReturnType);
        if (retArg == null)
            return null;
        return StateFun.Of(argNodes, TicNode.CreateInvisibleNode(retArg));
    }

    private static ITicNodeState UnifyOrNull(this StateStruct a, StateStruct b) {
        if (a.FieldsCount != b.FieldsCount)
            return null;
        var fields = new Dictionary<string, TicNode>();
        foreach (var aField in a.Fields)
        {
            var bField = b.GetFieldOrNull(aField.Key);
            if (bField == null)
                return null;
            var uniField = aField.Value.State.UnifyOrNull(bField.State);
            if (uniField == null)
                return null;
            fields.Add(aField.Key, TicNode.CreateInvisibleNode(uniField));
        }

        return new StateStruct(fields, true);
    }
}
