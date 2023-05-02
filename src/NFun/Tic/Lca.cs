namespace NFun.Tic;

using System;
using SolvingStates;
using static SolvingStates.StatePrimitive;

public static class Lca {
    public static ITicNodeState MinFcd(ITicNodeState a, ITicNodeState b) {
        if (b is StateRefTo bref)
            return MinFcd(a, bref.Element);
        if (a is StateRefTo aref)
            return MinFcd(aref.Element, b);
        if (a is ConstrainsState ac)
            return ac.Ancestor != null ? MinFcd(ac.Ancestor, b) : GetMinType(b);
        if(b is ConstrainsState bc)
            return bc.Ancestor != null ? MinFcd(a, bc.Ancestor) : GetMinType(a);
        if (a is StatePrimitive ap)
            return b is StatePrimitive bp ? ap.GetFirstCommonDescendantOrNull(bp) :
                a.Equals(Any) ? GetMinType(b) : null;
        if(b.Equals(Any))
            return GetMinType(a);
        if (a.GetType() != b.GetType())
            return null;
        if (a is StateArray arrA)
        {
            var arrB = (StateArray)b;
            var lcd = MinFcd(arrA.Element, arrB.Element);
            if (lcd == null)
                return null;
            return StateArray.Of(lcd);
        }

        if (a is StateFun funA)
        {
            var funB = (StateFun)b;
            if (funA.ArgsCount != funB.ArgsCount)
                return null;
            var args = new ITicNodeState[funA.ArgsCount];
            for (int i = 0; i < funA.ArgsCount; i++)
                args[i] = MaxLca(funA.ArgNodes[i].State, funB.ArgNodes[i].State);

            var retType = MinFcd(funA.ReturnType, funB.ReturnType);
            if (retType == null)
                return null;
            return StateFun.Of(args, retType);
        }

        throw new NotImplementedException($"{a} fcd {b} is not implemented yet");
    }

    public static ITicNodeState MaxLca(ITicNodeState a, ITicNodeState b) {
        if (a is StateRefTo aref)
            return MaxLca(aref.Element, b);
        if (b is StateRefTo bref)
            return MaxLca(a, bref.Element);
        if (b is ConstrainsState bc)
            return bc.HasDescendant ? MaxLca(a, bc.Descendant) : GetMaxType(a);
        if (a is ConstrainsState)
            return MaxLca(b, a);
        if (a is StatePrimitive ap)
            return b is StatePrimitive bp ? ap.GetLastCommonPrimitiveAncestor(bp) : Any;
        if (b is StatePrimitive)
            return Any;
        if (a is StateArray aarr)
            return b is StateArray barr ? StateArray.Of(MaxLca(aarr.Element, barr.Element)) : Any;
        if (a is StateFun af)
        {
            if (!(b is StateFun bf))
                return Any;
            if (af.ArgsCount != bf.ArgsCount)
                return Any;
            var returnState = MaxLca(af.ReturnType, bf.ReturnType);
            var argNodes = new TicNode[af.ArgsCount];

            for (var i = 0; i < af.ArgNodes.Length; i++)
            {
                var aNode = af.ArgNodes[i];
                var bNode = bf.ArgNodes[i];
                var fcd = MinFcd(aNode.State, bNode.State);
                if (fcd == null)
                    return Any;
                argNodes[i] = TicNode.CreateInvisibleNode(fcd);
            }

            return StateFun.Of(argNodes, TicNode.CreateInvisibleNode(returnState));
        }

        if (a is StateStruct)
            throw new NotImplementedException($"Todo BottomLca type5 for {a}");
        return Any;
    }

    /*
     * Returns most concrete type, or [..] if there is no concreteness
     */
    public static ITicNodeState GetMaxType(ITicNodeState a) =>
        a switch {
            StatePrimitive => a,
            ConstrainsState cs => cs.HasDescendant ? cs.Descendant : new ConstrainsState(isComparable: cs.IsComparable),
            StateArray arr =>  StateArray.Of(GetMaxType(GetMaxType(arr.Element))),
            StateRefTo aref => GetMaxType(aref.Element),
            StateFun f =>  GetMaxType(f),
            StateStruct s => GetMaxType(s),
            _ => a
        };

    private static ITicNodeState GetMaxType(StateFun f) {
        // return type is classic, but arg type is contravariant
        var returnNode = TicNode.CreateInvisibleNode(GetMaxType(f.ReturnType));
        var argNodes = new TicNode[f.ArgsCount];
        var i = 0;
        foreach (var node in f.ArgNodes)
        {
            argNodes[i] = TicNode.CreateInvisibleNode(GetMinType(node.State));
            i++;
        }
        return StateFun.Of(argNodes, returnNode);
    }

    private static ITicNodeState GetMaxType(StateStruct a) {
        if (a.IsSolved)
            return a;
        throw new NotImplementedException($"Todo GetBottom type3 for {a}");
    }

    /*
     * Returns most abstract possible type
     */
    public static ITicNodeState GetMinType(ITicNodeState a) =>
     a switch {
         StateRefTo aref => GetMinType(aref.Element),
         ConstrainsState cs => cs.HasAncestor ? cs.Ancestor : Any,
         StatePrimitive => a,
         StateArray arr =>  StateArray.Of(GetMinType(GetMaxType(arr.Element))),
         StateFun f => GetMinType(f),
         StateStruct s => GetMinType(s),
         _ => a
     };

    private static ITicNodeState GetMinType(StateFun f) {
        if (f.IsSolved)
            return f;
        throw new NotImplementedException($"Todo GetBottom type2 for {f}");
    }

    private static ITicNodeState GetMinType(StateStruct s) {
        if (s.IsSolved)
            return s;
        throw new NotImplementedException($"Todo GetBottom type1 for {s}");
    }
}
