namespace NFun.Tic;

using SolvingStates;

public static class Lca {
    public static ITicNodeState Calculate(ITicNodeState a, ITicNodeState b) {
        if (a is StateRefTo aref)
            return Calculate(aref.Element, b);
        if (b is StateRefTo bref)
            return Calculate(a, bref.Element);
        if (b is ConstrainsState bc)
            return bc.HasDescendant ? Calculate(a, bc.Descendant) : a;
        if (a is ConstrainsState)
            return Calculate(b, a);
        if (a is StatePrimitive ap)
            return b is StatePrimitive bp ? ap.GetLastCommonPrimitiveAncestor(bp) : StatePrimitive.Any;
        if (b is StatePrimitive)
            return StatePrimitive.Any;
        if (a is StateArray aarr)
            return b is StateArray barr ? StateArray.Of(Calculate(aarr.Element, barr.Element)) : StatePrimitive.Any;
        return StatePrimitive.Any;
    }
}
