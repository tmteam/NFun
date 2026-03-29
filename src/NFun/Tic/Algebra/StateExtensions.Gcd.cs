namespace NFun.Tic.Algebra;

using System;
using System.Collections.Generic;
using System.Linq;
using SolvingStates;
using static SolvingStates.StatePrimitive;

public static partial class StateExtensions {
    public static ITicNodeState Gcd(this ITicNodeState a, ITicNodeState b) {
        while (true)
        {
            if (b is StateRefTo bref)
            {
                b = bref.Element;
                continue;
            }

            if (a is StateRefTo aref)
            {
                a = aref.Element;
                continue;
            }

            if (a is ConstraintsState ac)
            {
                if (ac.Ancestor == null) return b.Abstractest();
                a = ac.Ancestor;
                continue;

            }

            if (b is ConstraintsState bc)
            {
                if (bc.Ancestor == null) return a.Abstractest();
                b = bc.Ancestor;
                continue;

            }

            // None: GCD(None, Opt(T)) = None, GCD(None, Any) = None, GCD(None, T) = null
            if (a == StatePrimitive.None) return GcdWithNone(b);
            if (b == StatePrimitive.None) return GcdWithNone(a);

            // Optional: covariant GCD
            if (a is StateOptional aopt) return GcdWithOptional(aopt, b);
            if (b is StateOptional bopt) return GcdWithOptional(bopt, a);

            if (a is StatePrimitive ap)
                return b is StatePrimitive bp ? ap.GetFirstCommonDescendantOrNull(bp) :
                    a== Any ? b.Abstractest() : null;
            if (b== Any) return a.Abstractest();
            if (a.GetType() != b.GetType()) return null;
            return a switch {
                StateArray arrA => arrA.Gcd((StateArray)b),
                StateFun funA => funA.Gcd((StateFun)b),
                StateStruct structA => structA.Gcd((StateStruct)b),
                _ => throw new NotSupportedException($"GCD is not supported for types {a} and {b}")
            };
        }
    }

    private static ITicNodeState GcdWithNone(ITicNodeState other) =>
        other switch {
            StatePrimitive { Name: PrimitiveTypeName.None } => None,
            StatePrimitive { Name: PrimitiveTypeName.Any } => None, // None ≤ Any → meet = None
            StateOptional => None, // None ≤ Opt(T) → meet = None
            _ => null // None is not ≤ other concrete types
        };

    private static ITicNodeState GcdWithOptional(StateOptional opt, ITicNodeState other) {
        if (other is StateOptional otherOpt)
        {
            var innerGcd = opt.Element.Gcd(otherOpt.Element);
            return innerGcd == null ? null : StateOptional.Of(innerGcd);
        }
        // Opt(T) ≤ Any, so GCD(Opt(T), Any) = Opt(T)
        if (other== Any)
            return opt.Element== Any ? Any : opt;
        // GCD(Opt(A), B) = GCD(A, B) — for non-Any B (common desc can't be optional since None ≰ B)
        return opt.Element.Gcd(other);
    }

    private static ITicNodeState Gcd(this StateArray arrA, StateArray arrB) {
        var lcd = arrA.Element.Gcd(arrB.Element);
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
                var fieldGcd = aFieldNode.State.Gcd(bField.State);
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
            args[i] = funA.ArgNodes[i].State.Lca(funB.ArgNodes[i].State);

        var retType = funA.ReturnType.Gcd(funB.ReturnType);
        if (retType == null)
            return null;
        return StateFun.Of(args, retType);
    }
}
