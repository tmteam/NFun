namespace NFun.Tic.Algebra;

using System;
using System.Collections.Generic;
using System.Linq;
using SolvingStates;
using static SolvingStates.StatePrimitive;

public static partial class StateExtensions {
    /// <summary>
    /// ∧ — meet. S axis (Algebra_GCD.md §StructBound): S = S₁ ∪ S₂ — GcdBound field
    /// union (ownerless mode — self-referential positions keep node identity), applied on top
    /// of the S-free interval core: a CS result carries the union (three-way (D,A,S)
    /// satisfiability enforced), a solved struct result absorbs it via the same GcdBound meet,
    /// Any becomes CS{S}, an Optional result unwraps (no struct satisfier is None), and any
    /// other solved result is null — a struct bound admits no satisfier under a non-struct.
    /// </summary>
    public static ITicNodeState Gcd(this ITicNodeState a, ITicNodeState b) => a.Gcd(b, null);

    // ∧ with an explicit coinductive context (see AlgebraCycleContext) threaded through the
    // whole mutually-recursive family — Gcd ↔ Lca (Fun args), struct fields → Unify → Merge,
    // GcdBound field meets.
    internal static ITicNodeState Gcd(this ITicNodeState a, ITicNodeState b, AlgebraCycleContext ctx) {
        var boundA = StructBoundOf(a);
        var boundB = StructBoundOf(b);
        if (boundA == null && boundB == null)
            return GcdCore(a, b, ctx);
        var s = boundA == null ? boundB
            : boundB == null ? boundA
            : SolvingFunctions.GcdBound(boundA, boundB, null, null, ctx);
        if (s == null)
            return null; // bound conflict — no common satisfier
        return ApplyBoundToMeet(GcdCore(a, b, ctx), s, ctx);
    }

    private static StateStruct StructBoundOf(ITicNodeState state) {
        while (state is StateRefTo r)
            state = r.Element;
        return state is ConstraintsState { HasStructBound: true } cs ? cs.StructBound : null;
    }

    private static ITicNodeState ApplyBoundToMeet(ITicNodeState core, StateStruct s, AlgebraCycleContext ctx) {
        switch (core) {
            case null:
                return null;
            case ConstraintsState cc: {
                // A CS core result is always one of the OPERANDS returned whole (neutral /
                // Any-identity branches), so its own bound ∈ {S₁, S₂, ∅} — subsumed by the
                // union s. Overwriting is exact; the operand itself is never mutated.
                if (ReferenceEquals(cc.StructBound, s))
                    return cc.StructBoundIsSatisfiable() ? cc : null;
                var copy = cc.GetCopy();
                copy.StructBound = s;
                return copy.StructBoundIsSatisfiable() ? copy : null;
            }
            case StateStruct st:
                // satisfier ≤ st ∧ satisfier fits S ⟺ satisfier fits GcdBound(st, S):
                // struct-≤ IS the F-bound predicate (width + covariant fields), so the solved
                // struct is just another bound contributor (same rule as Pull's Apply(CS, Struct)).
                return SolvingFunctions.GcdBound(st, s, null, null, ctx);
            case StateOptional opt:
                // No struct satisfier is None: meet(S, opt(X)) = meet(S, X).
                return ApplyBoundToMeet(opt.Element, s, ctx);
            case StatePrimitive { Name: PrimitiveTypeName.Any }: {
                // Any carries no information — the meet is exactly the bound constraint.
                var cs = ConstraintsState.Empty;
                cs.StructBound = s;
                return cs;
            }
            default:
                return null; // non-struct solved meet cannot satisfy a struct bound
        }
    }

    private static ITicNodeState GcdCore(ITicNodeState a, ITicNodeState b, AlgebraCycleContext ctx) {
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

            // A CS contributes only its Ancestor to the meet (Desc and flags are node
            // obligations, not an upper bound). A no-ancestor CS ([D..∅]) is therefore
            // NEUTRAL: [D..∅] ∧ x = x in BOTH orders — the other operand survives whole
            // (Desc, flags, Preferred included). Neutrality of both sides is checked
            // BEFORE raising either to its Ancestor — commutativity requires it — and
            // the neutral answer is the operand ITSELF, not its Abstractest
            // (↑ gives the supremum, the LCA direction, not GCD's meet).
            if (a is ConstraintsState { Ancestor: null }) return b;
            if (b is ConstraintsState { Ancestor: null }) return a;

            if (a is ConstraintsState ac)
            {
                a = ac.Ancestor;
                continue;
            }

            if (b is ConstraintsState bc)
            {
                b = bc.Ancestor;
                continue;
            }

            // None: GCD(None, Opt(T)) = None, GCD(None, Any) = None, GCD(None, T) = null
            if (a == None) return GcdWithNone(b);
            if (b == None) return GcdWithNone(a);

            // Optional: covariant GCD
            if (a is StateOptional aopt) return GcdWithOptional(aopt, b, ctx);
            if (b is StateOptional bopt) return GcdWithOptional(bopt, a, ctx);

            // Any is the lattice top: meet with top is identity — Any ∧ x = x, including
            // composites with unsolved innards. The former x.Abstractest() projection here
            // differed from x exactly on those unsolved innards (debt #18).
            if (a is StatePrimitive ap)
                return b is StatePrimitive bp ? ap.GetFirstCommonDescendantOrNull(bp) :
                    a== Any ? b : null;
            if (b== Any) return a;
            // MutStruct <: Struct — allow GCD across struct family
            if (a.GetType() != b.GetType() && !(a is StateStruct && b is StateStruct)) return null;
            return a switch {
                StateArray arrA => arrA.Gcd((StateArray)b, ctx),
                StateFun funA => funA.Gcd((StateFun)b, ctx),
                StateStruct structA when b is StateStruct structB => structA.Gcd(structB, ctx),
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

    private static ITicNodeState GcdWithOptional(StateOptional opt, ITicNodeState other, AlgebraCycleContext ctx) {
        if (other is StateOptional otherOpt)
        {
            var innerGcd = opt.Element.Gcd(otherOpt.Element, ctx);
            return innerGcd == null ? null : StateOptional.Of(innerGcd);
        }
        // Opt(T) ≤ Any, so GCD(Opt(T), Any) = Opt(T)
        if (other.Equals(Any))
            return opt.Element.Equals(Any) ? Any : opt;
        // GCD(Opt(A), B) = GCD(A, B) — for non-Any B (common desc can't be optional since None ≰ B)
        return opt.Element.Gcd(other, ctx);
    }

    private static ITicNodeState Gcd(this StateArray arrA, StateArray arrB, AlgebraCycleContext ctx) {
        var lcd = arrA.Element.Gcd(arrB.Element, ctx);
        if (lcd == null)
            return null;
        return StateArray.Of(lcd);
    }

    // Coinductive arm of recursive struct GCD (e.g., type t = {a:t?, b:rule(t)->t?}).
    // GCD reaches itself via Lca(StateFun) → contravariant-args Gcd → struct Gcd → field
    // Gcd → here again. Each Lca/Gcd level builds a NEW struct snapshot, so ReferenceEquals
    // alone misses the cycle. Use TypeName as the structural key: when both sides carry the
    // same named-type identity, they're the same recursive μ-type — return one side
    // coinductively. Without this arm, multi-self-path recursive named types (a path
    // through `?` + a path through `rule(t)->...`) cause exponential snapshot expansion
    // until stack/memory exhaustion.
    private static ITicNodeState Gcd(this StateStruct a, StateStruct b, AlgebraCycleContext ctx) {
        // Named-type cycle short-circuit.
        if (a.TypeName != null && a.TypeName == b.TypeName) {
            ctx ??= new AlgebraCycleContext();
            if (ctx.GcdNameInProgress(a.TypeName))
                return a;
            ctx.EnterGcdName(a.TypeName);
            try {
                return GcdStructFields(a, b, ctx);
            } finally {
                ctx.ExitGcdName();
            }
        }
        return GcdStructFields(a, b, ctx);
    }

    private static ITicNodeState GcdStructFields(StateStruct a, StateStruct b, AlgebraCycleContext ctx) {
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
                    fieldResult = aFieldNode.State.UnifyOrNull(bField.State, ctx);
                }
                else
                {
                    fieldResult = aFieldNode.State.Gcd(bField.State, ctx);
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

    private static ITicNodeState Gcd(this StateFun funA, StateFun funB, AlgebraCycleContext ctx) {
        if (funA.ArgsCount != funB.ArgsCount)
            return null;
        var args = new ITicNodeState[funA.ArgsCount];
        for (int i = 0; i < funA.ArgsCount; i++)
            args[i] = funA.ArgNodes[i].State.Lca(funB.ArgNodes[i].State, ctx);

        var retType = funA.ReturnType.Gcd(funB.ReturnType, ctx);
        if (retType == null)
            return null;
        return StateFun.Of(args, retType);
    }
}
