namespace NFun.Tic;

using System;
using System.Collections.Generic;
using SolvingStates;
using static SolvingStates.StatePrimitive;

public static class StateExtensions {

    #region lca

    public static ITicNodeState Lca(this ITicNodeState a, ITicNodeState b) {
        if (a is StateRefTo aref)
            return Lca(aref.Element, b);
        if (b is StateRefTo bref)
            return Lca(a, bref.Element);
        if (b is ConstrainsState bc)
            return bc.HasDescendant ? Lca(a, bc.Descendant) : Concretest(a);
        if (a is ConstrainsState)
            return Lca(b, a);
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

    private static ITicNodeState Lca(this StateStruct a, StateStruct b) {
        var nodes = new Dictionary<string, TicNode>();
        foreach (var aField in a.Fields)
        {
            var bField = b.GetFieldOrNull(aField.Key);
            // if there is no bField - than resulting state got no this field
            if (bField == null) continue;

            var universalType = UniversalStateOrNull(aField.Value.State, bField.State);
            if (universalType == null) continue;
            nodes.Add(aField.Key, TicNode.CreateInvisibleNode(universalType));
        }
        //todo is it frozen or not?
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
            var fcd = Fcd(aNode.State, bNode.State);
            if (fcd == null)
                return Any;
            argNodes[i] = TicNode.CreateInvisibleNode(fcd);
        }

        return StateFun.Of(argNodes, TicNode.CreateInvisibleNode(returnState));
    }

    #endregion

    #region fcd

    private static ITicNodeState Fcd(this ITicNodeState a, ITicNodeState b) {
        if (b is StateRefTo bref)
            return Fcd(a, bref.Element);
        if (a is StateRefTo aref)
            return Fcd(aref.Element, b);
        if (a is ConstrainsState ac)
            return ac.Ancestor != null ? Fcd(ac.Ancestor, b) : Abstractest(b);
        if (b is ConstrainsState bc)
            return bc.Ancestor != null ? Fcd(a, bc.Ancestor) : Abstractest(a);
        if (a is StatePrimitive ap)
            return b is StatePrimitive bp ? ap.GetFirstCommonDescendantOrNull(bp) :
                a.Equals(Any) ? Abstractest(b) : null;
        if (b.Equals(Any))
            return Abstractest(a);
        if (a.GetType() != b.GetType())
            return null;
        if (a is StateArray arrA)
            return Fcd(arrA, (StateArray)b);
        if (a is StateFun funA)
            return Fcd(funA, (StateFun)b);
        if (a is StateStruct astruct)
            return Fcd(astruct, (StateStruct)b);
        throw new NotSupportedException($"FCD is not supported for types {a} and {b}");
    }

    private static ITicNodeState Fcd(this StateArray arrA, StateArray arrB) {
        var lcd = Fcd(arrA.Element, arrB.Element);
        if (lcd == null)
            return null;
        return StateArray.Of(lcd);
    }

    private static ITicNodeState Fcd(this StateStruct astruct, StateStruct bstruct) {
        //todo - just copy lca algorithm, but it is not fair
        // consider case of function with two itmes
        // should we merge fields with different names in the case?

        var nodes = new Dictionary<string, TicNode>();
        //todo - is it right? What about fields with unknown types?
        foreach (var aField in astruct.Fields)
        {
            if (!aField.Value.IsSolved)
                continue;
            var bField = bstruct.GetFieldOrNull(aField.Key);
            if (bField == null) continue;
            if (!bField.IsSolved)
                continue;
            if (!aField.Value.State.Equals(bField.State))
                continue;
            nodes.Add(aField.Key, aField.Value);
        }

        //todo is it frozen
        return new StateStruct(nodes, true);
    }

    private static ITicNodeState Fcd(this StateFun funA, StateFun funB) {
        if (funA.ArgsCount != funB.ArgsCount)
            return null;
        var args = new ITicNodeState[funA.ArgsCount];
        for (int i = 0; i < funA.ArgsCount; i++)
            args[i] = Lca(funA.ArgNodes[i].State, funB.ArgNodes[i].State);

        var retType = Fcd(funA.ReturnType, funB.ReturnType);
        if (retType == null)
            return null;
        return StateFun.Of(args, retType);
    }

    #endregion

    #region maxmin

    /// <summary>
    /// Returns most possible concrete type, that can be represented by current state (without convertion)
    /// </summary>
    public static ITicNodeState Concretest(this ITicNodeState a) =>
        a switch {
            StatePrimitive => a,
            ConstrainsState cs => cs.HasDescendant
                ? cs.Descendant.Concretest()
                : ConstrainsState.Of(isComparable: cs.IsComparable),
            StateArray arr => StateArray.Of(arr.Element.Concretest()),
            StateRefTo aref => aref.Element.Concretest(),
            StateFun f => f.Concretest(),
            StateStruct => a, //todo - exclude refs from struct
            _ => a
        };

    private static ITicNodeState Concretest(this StateFun f) {
        // return type is classic, but arg type is contravariant
        var returnNode = TicNode.CreateInvisibleNode(Concretest(f.ReturnType));
        var argNodes = new TicNode[f.ArgsCount];
        var i = 0;
        foreach (var node in f.ArgNodes)
        {
            argNodes[i] = TicNode.CreateInvisibleNode(node.State.Abstractest());
            i++;
        }

        return StateFun.Of(argNodes, returnNode);
    }

    /// <summary>
    /// Returns most possible abstract type, that can be represented by current state (without convertion)
    /// </summary>
    public static ITicNodeState Abstractest(this ITicNodeState a) =>
        a switch {
            StateRefTo aref => aref.Element.Abstractest(),
            ConstrainsState cs => cs.IsComparable? cs : cs.HasAncestor ? cs.Ancestor : Any,
            StatePrimitive => a,
            StateArray arr => StateArray.Of(arr.Element.Abstractest()),
            StateFun f => f.Abstractest(),
            StateStruct => a,
            _ => a
        };

    private static ITicNodeState Abstractest(this StateFun f) {
        var returnNode = TicNode.CreateInvisibleNode(Abstractest(f.ReturnType));
        var argNodes = new TicNode[f.ArgsCount];
        var i = 0;
        foreach (var node in f.ArgNodes)
        {
            argNodes[i] = TicNode.CreateInvisibleNode(node.State.Concretest());
            i++;
        }
        return StateFun.Of(argNodes, returnNode);
    }

    #endregion

    #region uni

    private static ITicNodeState UniversalStateOrNull(this ITicNodeState a, ITicNodeState b) {
        if (a.Equals(b))
            return a;

        if (a is StateRefTo ar)
            return UniversalStateOrNull(ar.GetNonReference(), b);
        if (b is StateRefTo br)
            return UniversalStateOrNull(a, br.GetNonReference());
        if (a is ConstrainsState ac)
        {
            if (b is ConstrainsState bc)
                return UniversalStateOrNull(ac, bc);
            else if (ac.Fits(b))
                return b;
            else
                return null;
        }

        if (b is ConstrainsState)
            return UniversalStateOrNull(b, a);
        if (a.GetType() != b.GetType())
            return null;
        if (a is StatePrimitive)
            return null;
        if (b is StatePrimitive)
            return null;
        if (a is StateArray aArr)
            return UniversalStateOrNull(aArr, b as StateArray);
        if (a is StateFun aFun)
            return UniversalStateOrNull(aFun, b as StateFun);
        if (a is StateStruct aStr)
            return UniversalStateOrNull(aStr, b as StateStruct);
        throw new NotSupportedException($"Unitype({a}, {b})");
    }

    private static ITicNodeState UniversalStateOrNull(this ConstrainsState a, ConstrainsState b) {
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

        return ConstrainsState.Of(descendant, ancestor, comparable).GetOptimizedOrNull();
    }

    private static ITicNodeState UniversalStateOrNull(this StateArray a, StateArray b) {
        var uniElement = UniversalStateOrNull(a.Element, b.Element);
        return uniElement == null ? null : StateArray.Of(uniElement);
    }

    private static ITicNodeState UniversalStateOrNull(this StateFun a, StateFun b) {
        if (a.ArgsCount != b.ArgsCount)
            return null;
        var argNodes = new TicNode[a.ArgsCount];
        for (int i = 0; i < a.ArgsCount; i++)
        {
            var uniArg = UniversalStateOrNull(a.ArgNodes[i].State, b.ArgNodes[i].State);
            if (uniArg == null)
                return null;
            argNodes[i] = TicNode.CreateInvisibleNode(uniArg);
        }

        var retArg = UniversalStateOrNull(a.ReturnType, b.ReturnType);
        if (retArg == null)
            return null;
        return StateFun.Of(argNodes, TicNode.CreateInvisibleNode(retArg));
    }

    private static ITicNodeState UniversalStateOrNull(this StateStruct a, StateStruct b) {
        if (a.FieldsCount != b.FieldsCount)
            return null;
        var fields = new Dictionary<string, TicNode>();
        foreach (var aField in a.Fields)
        {
            var bField = b.GetFieldOrNull(aField.Key);
            if (bField == null)
                return null;
            var uniField = UniversalStateOrNull(aField.Value.State, bField.State);
            if (uniField == null)
                return null;
            fields.Add(aField.Key, TicNode.CreateInvisibleNode(uniField));
        }

        return new StateStruct(fields, true);
    }

    #endregion

    #region convert

    /// <summary>
    /// `from` can be converted to `to` in SOME case
    /// </summary>
    public static bool CanBeConvertedOptimisticTo(this ITicNodeState from, ITicNodeState to) {
        if (from is StateRefTo fromRef)
            return CanBeConvertedOptimisticTo(fromRef.Element, to);
        if (to is StateRefTo toRef)
            return CanBeConvertedOptimisticTo(from, toRef.Element);
        if (to.Equals(Any))
            return true;
        if (to is ICompositeState compositeState)
            return CanBeConvertedOptimisticTo(from, compositeState);
        if (from is StatePrimitive)
            return to is StatePrimitive top2
                ? from.CanBePessimisticConvertedTo(top2)
                : to is ConstrainsState toConstraints2 &&
                  (!toConstraints2.HasAncestor || from.CanBePessimisticConvertedTo(toConstraints2.Ancestor));
        if (from is ConstrainsState fromConstraints)
        {
            if (fromConstraints.NoConstrains)
                return true;
            if (to is ConstrainsState toConstraints)
            {
                var ancestor = toConstraints.Ancestor;
                // if there is no ancestor, than anything can be possibly converted to 'to'
                if (ancestor == null || Equals(ancestor, Any))
                    return true;
                //if there is ancestor, then either 'from.ancestor` either `from.desc` has to be converted to it
                if (fromConstraints.HasAncestor && CanBeConvertedOptimisticTo(fromConstraints.Ancestor, ancestor))
                    return true;
                if (fromConstraints.HasDescendant && CanBeConvertedOptimisticTo(fromConstraints.Descendant, ancestor))
                    return true;
            }

            if (to is StatePrimitive toP)
            {
                if (fromConstraints.HasAncestor && fromConstraints.Ancestor.CanBePessimisticConvertedTo(toP))
                    return true;
                if (fromConstraints.HasDescendant && fromConstraints.Descendant.CanBePessimisticConvertedTo(toP))
                    return true;
            }
        }

        if (from is ICompositeState)
        {
            if (to is ConstrainsState constrainsState)
                // if there is no ancestor, than anything can be possibly converted to 'to'
                return constrainsState.Ancestor == null || Equals(constrainsState.Ancestor, Any);
            return false;
        }

        if (to is StatePrimitive toPrimitive)
            return from.CanBePessimisticConvertedTo(toPrimitive);
        return false;
    }

    /// <summary>
    /// `from` can be converted to `to` in SOME case
    /// </summary>
    public static bool CanBeConvertedOptimisticTo(this ITicNodeState from, ICompositeState to) {
        if (from is StateRefTo r)
            return CanBeConvertedOptimisticTo(r.Element, to);
        if (from is ConstrainsState constrainsState)
            return !constrainsState.HasDescendant;
        if (from.GetType() != to.GetType())
            return false;
        if (from is StateArray arrayFrom)
            return CanBeConvertedOptimisticTo(arrayFrom.Element, (to as StateArray).Element);
        throw new NotImplementedException($"{from} CanBeConvertedPessimistic Top");
    }

    /// <summary>
    /// `from` can be converted to `to` in ANY case
    /// </summary>
    private static bool CanBeConvertedPessimisticTo(this ICompositeState from, ConstrainsState to) {
        if (to.NoConstrains)
            return true;
        if (to.Ancestor != null && !from.CanBePessimisticConvertedTo(to.Ancestor))
            return false;
        if (to.IsComparable)
            return from is StateArray array && CanBeConvertedPessimisticTo(from: StatePrimitive.Char, array.Element);
        // so state has to be converted to descendant, to allow this
        if (Equals(to.Descendant, Any))
            return true;
        if (from is StateArray arrayDesc)
            return to.Descendant is StateArray arrayAnc &&
                   CanBeConvertedPessimisticTo(arrayDesc.Element, arrayAnc.Element);
        throw new NotImplementedException($"{from} CanBeConvertedPessimistic Typed");
    }

    /// <summary>
    /// `from` can be converted to `to` in ANY case
    /// </summary>
    public static bool CanBeConvertedPessimisticTo(this ITicNodeState from, ITicNodeState to) {
        if (to is StateRefTo ancRef)
            return CanBeConvertedPessimisticTo(from, ancRef.Element);
        return from switch {
            StateRefTo descRef => CanBeConvertedPessimisticTo(descRef.Element, to),
            StatePrimitive => to switch {
                StatePrimitive p => from.CanBePessimisticConvertedTo(p),
                ConstrainsState c => c.NoConstrains || (
                    c.HasDescendant
                        ? CanBeConvertedPessimisticTo(from, c.Descendant)
                        : CanBeConvertedPessimisticTo(from, c.Ancestor)),
                _ => false
            },
            ConstrainsState fromDesc => fromDesc.HasAncestor
                ? CanBeConvertedPessimisticTo(fromDesc.Ancestor, to) //todo support convertible
                : CanBeConvertedPessimisticTo(Any, to),
            StateArray arrayDesc => to switch {
                StateArray arrayAnc => CanBeConvertedPessimisticTo(arrayDesc.Element, arrayAnc.Element),
                ConstrainsState constrAnc => CanBeConvertedPessimisticTo(arrayDesc, constrAnc),
                _ => false
            },
            StateFun => throw new NotImplementedException($"{from} CanBeConvertedPessimistic "),
            StateStruct => throw new NotImplementedException($"{from} CanBeConvertedPessimistic "),
            _ => false
        };
    }

    #endregion
}
