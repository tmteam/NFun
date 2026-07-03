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

            if (a is StateCompositeConstraints acompcs)
                return acompcs.GcdCompCs(b);
            if (b is StateCompositeConstraints bcompcs)
                return bcompcs.GcdCompCs(a);

            if (a is ConstraintsState ac)
            {
                // Unconstrained a ⊓ b = b: a accepts everything so b's constraint set IS the meet.
                if (ac.Ancestor == null) return b;
                a = ac.Ancestor;
                continue;

            }

            if (b is ConstraintsState bc)
            {
                if (bc.Ancestor == null) return a;
                b = bc.Ancestor;
                continue;

            }

            if (a == None) return GcdWithNone(b);
            if (b == None) return GcdWithNone(a);

            if (a is StateOptional aopt) return GcdWithOptional(aopt, b);
            if (b is StateOptional bopt) return GcdWithOptional(bopt, a);

            if (a is StatePrimitive ap)
                return b is StatePrimitive bp ? ap.GetFirstCommonDescendantOrNull(bp) :
                    a== Any ? b.Abstractest() : null;
            if (b== Any) return a.Abstractest();
            // MutStruct <: Struct → cross-kind GCD allowed.
            if (a.GetType() != b.GetType() && !(a is StateStruct && b is StateStruct)) return null;
            return a switch {
                StateArray arrA => arrA.Gcd((StateArray)b),
                StateFun funA => funA.Gcd((StateFun)b),
                StateStruct structA when b is StateStruct structB => structA.Gcd(structB),
                StateCollection collA when b is StateCollection collB => collA.Gcd(collB),
                _ => throw new NotSupportedException($"GCD is not supported for types {a} and {b}")
            };
        }
    }

    private static ITicNodeState GcdWithNone(ITicNodeState other) =>
        other switch {
            StatePrimitive { Name: PrimitiveTypeName.None } => None,
            StatePrimitive { Name: PrimitiveTypeName.Any } => None,
            StateOptional => None,
            _ => null
        };

    private static ITicNodeState GcdWithOptional(StateOptional opt, ITicNodeState other) {
        if (other is StateOptional otherOpt)
        {
            var innerGcd = opt.Element.Gcd(otherOpt.Element);
            return innerGcd == null ? null : StateOptional.Of(innerGcd);
        }
        // Opt(T) ≤ Any.
        if (other.Equals(Any))
            return opt.Element.Equals(Any) ? Any : opt;
        // Non-Any B: opt-wrapper doesn't affect meet (None ≰ B).
        return opt.Element.Gcd(other);
    }

    private static ITicNodeState Gcd(this StateArray arrA, StateArray arrB) {
        var lcd = arrA.Element.Gcd(arrB.Element);
        if (lcd == null)
            return null;
        return StateArray.Of(lcd);
    }

    /// <summary>
    /// Gcd(SC(Ka, ea), SC(Kb, eb)) = SC(KindGcd(Ka, Kb), Unify(ea, eb)).
    /// Kinds meet via the ConstructorLattice; elements are INVARIANT, so they unify —
    /// same discipline as the Unify SC arm and GcdStructFields invariant fields.
    /// </summary>
    private static ITicNodeState Gcd(this StateCollection a, StateCollection b) {
        var kind = ConstructorLattice.Gcd(a.Constructor, b.Constructor);
        if (kind == null)
            return null;
        var element = a.Element.UnifyOrNull(b.Element);
        if (element == null)
            return null;
        return StateCollection.Of(kind.Value, element);
    }

    // Cycle guard for recursive named struct types: each Lca/Gcd level allocates a fresh
    // snapshot, so ref-equality misses the μ-type. TypeName is the structural key.
    [ThreadStatic] private static List<string> _structGcdNamesInProgress;

    private static ITicNodeState Gcd(this StateStruct a, StateStruct b) {
        if (a.TypeName != null && a.TypeName == b.TypeName) {
            var guard = _structGcdNamesInProgress ??= new();
            for (int i = 0; i < guard.Count; i++)
                if (guard[i] == a.TypeName)
                    return a;
            guard.Add(a.TypeName);
            try {
                return GcdStructFields(a, b);
            } finally {
                guard.RemoveAt(guard.Count - 1);
            }
        }
        return GcdStructFields(a, b);
    }

    private static ITicNodeState GcdStructFields(StateStruct a, StateStruct b) {
        bool bothMutable = a is StateMutableStruct && b is StateMutableStruct;
        bool eitherMutable = a is StateMutableStruct || b is StateMutableStruct;

        var nodes = new Dictionary<string, TicNode>();
        foreach (var (name, aFieldNode) in a.Fields)
        {
            var bField = b.GetFieldOrNull(name);
            if (bField == null)
                nodes.Add(name, aFieldNode);
            else
            {
                ITicNodeState fieldResult;
                if (eitherMutable)
                {
                    // Invariant fields → Unify.
                    fieldResult = aFieldNode.State.UnifyOrNull(bField.State);
                }
                else
                {
                    fieldResult = aFieldNode.State.Gcd(bField.State);
                }
                if (fieldResult == null)
                    return null;
                nodes.Add(name, TicNode.CreateInvisibleNode(fieldResult));
            }
        }
        foreach (var (name, bFieldNode) in b.Fields)
        {
            if (a.GetFieldOrNull(name) == null)
                nodes.Add(name, bFieldNode);
        }

        // MutStruct is the narrower of the two — meet of any pair containing it stays MutStruct.
        if (eitherMutable)
            return new StateMutableStruct(nodes, isFrozen: true);
        return new StateStruct(nodes, isFrozen: true) {
            IsOptionalSourced = StateStruct.MergedIsOptionalSourced(a.IsOptionalSourced, b.IsOptionalSourced),
            TypeName = StateStruct.MergedTypeName(a.TypeName, b.TypeName),
        };
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
