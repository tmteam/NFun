namespace NFun.Tic;

using SolvingStates;

public static class Lca {
    public static ITicNodeState BottomLca(ITicNodeState a, ITicNodeState b) {
        if (a is StateRefTo aref)
            return BottomLca(aref.Element, b);
        if (b is StateRefTo bref)
            return BottomLca(a, bref.Element);
        if (b is ConstrainsState bc)
            return bc.HasDescendant ? BottomLca(a, bc.Descendant) : GetBottomType(a);
        if (a is ConstrainsState)
            return BottomLca(b, a);
        if (a is StatePrimitive ap)
            return b is StatePrimitive bp ? ap.GetLastCommonPrimitiveAncestor(bp) : StatePrimitive.Any;
        if (b is StatePrimitive)
            return StatePrimitive.Any;
        if (a is StateArray aarr)
            return b is StateArray barr ? StateArray.Of(BottomLca(aarr.Element, barr.Element)) : StatePrimitive.Any;
        return StatePrimitive.Any;
    }

    private static ITicNodeState GetBottomType(ITicNodeState a) =>
        a switch {
            StateRefTo aref => GetBottomType(aref.Element),
            ConstrainsState cs => cs.HasDescendant ? cs.Descendant : new ConstrainsState(),
            StatePrimitive => a,
            StateArray arr =>  StateArray.Of(GetBottomType(GetBottomType(arr.Element))),
            _ => a
        };
}
