namespace NFun.Tic.Algebra;

using System;
using System.Collections.Generic;
using System.Linq;
using NFun.Tic.SolvingStates;
using static NFun.Tic.SolvingStates.StatePrimitive;

public static partial class StateExtensions {
    public static ITicNodeState Gcd(this ITicNodeState a, ITicNodeState b) {
        if (b is StateRefTo bref)
            return Gcd(a, bref.Element);
        if (a is StateRefTo aref)
            return Gcd(aref.Element, b);
        if (a is ConstraintsState ac)
            return ac.Ancestor != null ? Gcd(ac.Ancestor, b) : Abstractest(b);
        if (b is ConstraintsState bc)
            return bc.Ancestor != null ? Gcd(a, bc.Ancestor) : Abstractest(a);
        if (a is StatePrimitive ap)
            return b is StatePrimitive bp ? ap.GetFirstCommonDescendantOrNull(bp) :
                a.Equals(Any) ? Abstractest(b) : null;
        if (b.Equals(Any))
            return Abstractest(a);
        if (a.GetType() != b.GetType())
            return null;
        if (a is StateArray arrA)
            return Gcd(arrA, (StateArray)b);
        if (a is StateFun funA)
            return Gcd(funA, (StateFun)b);
        if (a is StateStruct astruct)
            return Gcd(astruct, (StateStruct)b);
        throw new NotSupportedException($"GCD is not supported for types {a} and {b}");
    }

    private static ITicNodeState Gcd(this StateArray arrA, StateArray arrB) {
        var lcd = Gcd(arrA.Element, arrB.Element);
        if (lcd == null)
            return null;
        return StateArray.Of(lcd);
    }

    private static ITicNodeState Gcd(this StateStruct a, StateStruct b) {
        var nodes = new Dictionary<string, TicNode>();
        // GCD of structs = union of all fields. For shared fields, compute GCD of field types.
        foreach (var (name, aFieldNode) in a.Fields)
        {
            var bField = b.GetFieldOrNull(name);
            if (bField == null)
                nodes.Add(name, aFieldNode);
            else
            {
                var fieldGcd = Gcd(aFieldNode.State, bField.State);
                if (fieldGcd == null)
                    return null;
                nodes.Add(name, TicNode.CreateInvisibleNode(fieldGcd));
            }
        }
        // Add fields only in b
        foreach (var (name, bFieldNode) in b.Fields)
        {
            if (a.GetFieldOrNull(name) == null)
                nodes.Add(name, bFieldNode);
        }

        return new StateStruct(nodes, isFrozen: true);
    }

    private static ITicNodeState Gcd(this StateFun funA, StateFun funB) {
        if (funA.ArgsCount != funB.ArgsCount)
            return null;
        var args = new ITicNodeState[funA.ArgsCount];
        for (int i = 0; i < funA.ArgsCount; i++)
            args[i] = Lca(funA.ArgNodes[i].State, funB.ArgNodes[i].State);

        var retType = Gcd(funA.ReturnType, funB.ReturnType);
        if (retType == null)
            return null;
        return StateFun.Of(args, retType);
    }
}
