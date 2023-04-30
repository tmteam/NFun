namespace NFun.Tic;

using System;
using SolvingStates;

public static class Lca {
    public static ITicNodeState BottomLca(ITicNodeState a, ITicNodeState b) {
        if (a is StateRefTo aref)
            return BottomLca(aref.Element, b);
        if (b is StateRefTo bref)
            return BottomLca(a, bref.Element);
        if (b is ConstrainsState bc)
            return bc.HasDescendant ? BottomLca(a, bc.Descendant) : GetMaxType(a);
        if (a is ConstrainsState)
            return BottomLca(b, a);
        if (a is StatePrimitive ap)
            return b is StatePrimitive bp ? ap.GetLastCommonPrimitiveAncestor(bp) : StatePrimitive.Any;
        if (b is StatePrimitive)
            return StatePrimitive.Any;
        if (a is StateArray aarr)
            return b is StateArray barr ? StateArray.Of(BottomLca(aarr.Element, barr.Element)) : StatePrimitive.Any;
        if (a is StateFun)
            throw new NotImplementedException($"Todo BottomLca type for {a}");
        if (a is StateStruct)
            throw new NotImplementedException($"Todo BottomLca type for {a}");
        return StatePrimitive.Any;
    }

    /*
     * Returns most concrete type, or [..] if there is no concreteness
     */
    public static ITicNodeState GetMaxType(ITicNodeState a) =>
        a switch {
            StateRefTo aref => GetMaxType(aref.Element),
            ConstrainsState cs => cs.HasDescendant ? cs.Descendant : new ConstrainsState(),
            StatePrimitive => a,
            StateArray arr =>  StateArray.Of(GetMaxType(GetMaxType(arr.Element))),
            StateFun => throw new NotImplementedException($"Todo GetBottom type for {a}"),
            StateStruct => throw new NotImplementedException($"Todo GetBottom type for {a}"),
            _ => a
        };

    /*
     * Returns most abstract possible type
     */
    public static ITicNodeState GetMinType(ITicNodeState a) => throw new NotImplementedException("MinType");
    // a switch {
    //     StateRefTo aref => GetMaxType(aref.Element),
    //     ConstrainsState cs => cs.HasDescendant ? cs.Descendant : new ConstrainsState(),
    //     StatePrimitive => a,
    //     StateArray arr =>  StateArray.Of(GetMaxType(GetMaxType(arr.Element))),
    //     StateFun => throw new NotImplementedException($"Todo GetBottom type for {a}"),
    //     StateStruct => throw new NotImplementedException($"Todo GetBottom type for {a}"),
    //     _ => a
    // };
}
