namespace NFun.Tic.Algebra;

using System;
using System.Collections.Generic;
using NFun.Tic.SolvingStates;
using static NFun.Tic.SolvingStates.StatePrimitive;

public static partial class StateExtensions {
    public static ITicNodeState UnifyOrNull(this ITicNodeState a, ITicNodeState b) {
        if (a.Equals(b))
            return a;

        if (a is StateRefTo ar)
            return UnifyOrNull(ar.GetNonReference(), b);
        if (b is StateRefTo br)
            return UnifyOrNull(a, br.GetNonReference());

        // Any is the universal ancestor — any type is convertible to Any
        if (a.Equals(Any)) return Any;
        if (b.Equals(Any)) return Any;

        if (a is ConstraintsState ac)
        {
            if (b is ConstraintsState bc)
                return UnifyOrNull(ac, bc);
            else if (b.FitsInto(ac))
                return b;
            else
                return null;
        }

        if (b is ConstraintsState)
            return UnifyOrNull(b, a);
        if (a.GetType() != b.GetType())
            return null;
        if (a is StatePrimitive)
            return null;
        if (b is StatePrimitive)
            return null;
        if (a is StateArray aArr)
            return UnifyOrNull(aArr, b as StateArray);
        if (a is StateFun aFun)
            return UnifyOrNull(aFun, b as StateFun);
        if (a is StateStruct aStr)
            return UnifyOrNull(aStr, b as StateStruct);
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
            descendant = Lca(a.Descendant, b.Descendant);

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
        var uniElement = UnifyOrNull(a.Element, b.Element);
        return uniElement == null ? null : StateArray.Of(uniElement);
    }

    private static ITicNodeState UnifyOrNull(this StateFun a, StateFun b) {
        if (a.ArgsCount != b.ArgsCount)
            return null;
        var argNodes = new TicNode[a.ArgsCount];
        for (int i = 0; i < a.ArgsCount; i++)
        {
            var uniArg = UnifyOrNull(a.ArgNodes[i].State, b.ArgNodes[i].State);
            if (uniArg == null)
                return null;
            argNodes[i] = TicNode.CreateInvisibleNode(uniArg);
        }

        var retArg = UnifyOrNull(a.ReturnType, b.ReturnType);
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
            var uniField = UnifyOrNull(aField.Value.State, bField.State);
            if (uniField == null)
                return null;
            fields.Add(aField.Key, TicNode.CreateInvisibleNode(uniField));
        }

        return new StateStruct(fields, true);
    }
}
