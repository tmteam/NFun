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

            // StateCompositeConstraints — Stage C.2 (Layer-0 algebra).
            if (a is StateCompositeConstraints acompcs)
                return acompcs.GcdCompCs(b);
            if (b is StateCompositeConstraints bcompcs)
                return bcompcs.GcdCompCs(a); // GCD is symmetric

            if (a is ConstraintsState ac)
            {
                // Unconstrained ⊓ b: if a has no upper bound, its meet with b is determined
                // entirely by b. Returning b.Abstractest() here was wrong — Abstractest
                // gives the SUPREMUM (lattice top) which is the LCA direction, not GCD's
                // meet/infimum. For two unconstrained CSs that produced Any here, the LCA
                // of two unconstrained fn args (via LCA(Fun,Fun) → Gcd on args) widened
                // to Any, which Push's contravariance later couldn't reconcile with a
                // narrower annotation — surfaced as FU719 on `arr:s[]=[{f=rule it*N},...]`
                // patterns. Returning `b` itself is the correct meet: b's own constraint
                // set IS the intersection (since a accepts everything). (MR5Bug3.)
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

            // None: GCD(None, Opt(T)) = None, GCD(None, Any) = None, GCD(None, T) = null
            if (a == None) return GcdWithNone(b);
            if (b == None) return GcdWithNone(a);

            // Optional: covariant GCD
            if (a is StateOptional aopt) return GcdWithOptional(aopt, b);
            if (b is StateOptional bopt) return GcdWithOptional(bopt, a);

            if (a is StatePrimitive ap)
                return b is StatePrimitive bp ? ap.GetFirstCommonDescendantOrNull(bp) :
                    a== Any ? b.Abstractest() : null;
            if (b== Any) return a.Abstractest();
            // MutStruct <: Struct — allow GCD across struct family
            if (a.GetType() != b.GetType() && !(a is StateStruct && b is StateStruct)) return null;
            return a switch {
                StateArray arrA => arrA.Gcd((StateArray)b),
                StateFun funA => funA.Gcd((StateFun)b),
                StateStruct structA when b is StateStruct structB => structA.Gcd(structB),
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
        if (other.Equals(Any))
            return opt.Element.Equals(Any) ? Any : opt;
        // GCD(Opt(A), B) = GCD(A, B) — for non-Any B (common desc can't be optional since None ≰ B)
        return opt.Element.Gcd(other);
    }

    private static ITicNodeState Gcd(this StateArray arrA, StateArray arrB) {
        var lcd = arrA.Element.Gcd(arrB.Element);
        if (lcd == null)
            return null;
        return StateArray.Of(lcd);
    }

    // Cycle guard for recursive struct GCD (e.g., type t = {a:t?, b:rule(t)->t?}).
    // GCD reaches itself via Lca(StateFun) → contravariant-args Gcd → struct Gcd → field
    // Gcd → here again. Each Lca/Gcd level builds a NEW struct snapshot, so ReferenceEquals
    // alone misses the cycle. Use TypeName as the structural key: when both sides carry the
    // same named-type identity, they're the same recursive μ-type — return one side
    // coinductively. Without this guard, multi-self-path recursive named types (a path
    // through `?` + a path through `rule(t)->...`) cause exponential snapshot expansion
    // until stack/memory exhaustion
    [ThreadStatic] private static List<string> _structGcdNamesInProgress;

    private static ITicNodeState Gcd(this StateStruct a, StateStruct b) {
        // Named-type cycle short-circuit.
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
        // GCD of structs = union of all fields. For shared fields, compute GCD of field types.
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
                    // MutStruct fields are invariant: GCD requires Unify (exact match).
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
        // Add fields only in b
        foreach (var (name, bFieldNode) in b.Fields)
        {
            if (a.GetFieldOrNull(name) == null)
                nodes.Add(name, bFieldNode);
        }

        // GCD(MutStruct, MutStruct) = MutStruct (both mutable, intersection preserves mutability).
        // GCD(Struct, MutStruct) = MutStruct (MutStruct is more specific/concrete).
        // GCD(MutStruct, Struct) = MutStruct (symmetric).
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
