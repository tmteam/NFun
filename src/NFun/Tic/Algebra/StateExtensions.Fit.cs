namespace NFun.Tic.Algebra;

using System;
using SolvingStates;
using static SolvingStates.StatePrimitive;

public static partial class StateExtensions {
    /// <summary>
    /// THE comparable-domain membership predicate (Algebra_Fit.md §comparable cell,
    /// Algebra_Merge.md §cmp-правило; debt #31). The domain is {numeric primitives, Char,
    /// arr(Char)}; a state belongs — or can still come to belong after refinement — iff it
    /// is a comparable primitive, an array whose element can still become Char (≤-form,
    /// not exact equality: Char has no proper descendants, so on solved elements the two
    /// coincide), or a still-unsolved constraint (accepted conservatively).
    /// Resolution sites additionally require a solved POINT (see SolveContravariant).
    /// </summary>
    public static bool IsComparableDomain(this ITicNodeState state) =>
        state switch {
            StatePrimitive p => p.IsComparable,
            StateArray a => a.Element.CanBeConvertedOptimisticTo(StatePrimitive.Char),
            ConstraintsState => true, // unsolved — can still become comparable
            StateRefTo r => r.GetNonReference().IsComparableDomain(),
            _ => false // Optional/Fun/Struct are never comparable
        };

    /// <summary>
    /// THE authoritative satisfaction predicate `T satisfies C` (Algebra_Fit.md §Единый
    /// satisfaction-предикат): structural Fit core (interval + cmp cells) + Optional-lift arm
    /// + F-bound (StructBound) + the unsolved-target ancestor cell.
    /// <see cref="ConstraintsState.CanBeConvertedTo(ITypeState)"/> and the `T ⊓ CS` cell of
    /// UnifyOrNull delegate here.
    /// </summary>
    public static bool FitsInto(this ITicNodeState target, ConstraintsState to) =>
        target.FitsInto(to, null);

    // The Fit chain carries the coinductive context because its MutStruct invariant-field
    // cells re-enter UnifyOrNull — a bridge of the mutually-recursive algebra family.
    internal static bool FitsInto(this ITicNodeState target, ConstraintsState to, AlgebraCycleContext ctx) {
        // Ref-transparency (F3): dereference before cell classification.
        if (target is StateRefTo tRef)
            return tRef.GetNonReference().FitsInto(to, ctx);

        // Optional axis: T satisfies C? ⟺ T = None, or T = opt(X) and X satisfies the plain
        // cells, or T satisfies the plain cells itself (implicit lift T ≤ opt(T)).
        if (to.IsOptional)
        {
            if (target == None)
                return true;
            if (target is StateOptional tOpt)
                target = tOpt.Element is StateRefTo eRef ? eRef.GetNonReference() : tOpt.Element;
        }

        // S axis (F-bound): a solved target must structurally satisfy the bound; an unsolved
        // target defers the bound to the merge phase (GcdBound transports it).
        if (to.HasStructBound && target is ITypeState solvedTarget
            && !ConstraintsState.FitStructBound(solvedTarget, to.StructBound, ctx))
            return false;

        if (to.HasAncestor)
        {
            if (target is ConstraintsState tCs)
            {
                // Unsolved-target ancestor cell: an unsolved target's only committed lower
                // bound is its Descendant — the obligation is joint satisfiability:
                // its Descendant must sit under to.Ancestor; with no Descendant the upper
                // bounds must intersect (GCD(target.Anc ?? Any, to.Anc) ≠ ∅).
                if (tCs.HasDescendant)
                {
                    if (!tCs.Descendant.CanBePessimisticConvertedTo(to.Ancestor))
                        return false;
                }
                else if (tCs.HasAncestor
                         && tCs.Ancestor.GetFirstCommonDescendantOrNull(to.Ancestor) == null)
                    return false;
            }
            else if (!target.CanBePessimisticConvertedTo(to.Ancestor))
                return false;
        }
        if (to.IsComparable)
        {
            // Single domain rule (debt #31): comparable primitives, arr(element that can
            // still become Char), unsolved CS conservatively; other composites never.
            if (!target.IsComparableDomain())
                return false;
            // Comparable does NOT cancel the descendant check: the rule is conjunctive —
            // A ≤ [D..A′, cmp] ⟺ A ∈ Comparable ∧ D ≤c A ∧ A ≤c A′. Merge absorption
            // (x ≤ a⊓b ⟺ x ≤ a ∧ x ≤ b) requires the cmp cell to stay conjunctive.
        }
        return to.Descendant == null || CanBeFitConverted(to.Descendant, target, ctx);
    }


    public static bool FitsInto(this ITicNodeState target, ITicNodeState to) =>
        target.FitsInto(to, (AlgebraCycleContext)null);

    internal static bool FitsInto(this ITicNodeState target, ITicNodeState to, AlgebraCycleContext ctx) {
        if (to is StateRefTo toR)
            return target.FitsInto(toR.GetNonReference(), ctx);
        if (to is ConstraintsState constrainsState)
            return target.FitsInto(constrainsState, ctx);

        // to is Optional: None ≤ Opt(T), T ≤ Opt(T) via implicit lift, Opt(A) ≤ Opt(B) covariant
        if (to is StateOptional optTo)
        {
            if (target == None)
                return true;
            if (target is StateOptional targetOpt)
                return targetOpt.Element.FitsInto(optTo.Element, ctx);
            // T ≤ Opt(U) iff T ≤ U (implicit lift: T ≤ U ≤ Opt(U))
            return target.FitsInto(optTo.Element, ctx);
        }

        // None fits into Opt(T) (handled above), None itself, or Any
        if (target == None)
            return to is StatePrimitive p && (p.Name == PrimitiveTypeName.None || p.Name == PrimitiveTypeName.Any);

        return target switch {
            // Opt(A) fits into Opt(B) (handled above) or Any
            StateOptional => to== Any,
            StateArray targetA => to is StateArray arrTo
                ? targetA.Element.FitsInto(arrTo.Element, ctx)
                : to== Any,
            StateFun targetF => to is StateFun funTo
                ? targetF.FitsInto(funTo, ctx)
                : to== Any,
            StateStruct targetS => to is StateStruct structTo
                ? targetS.FitsInto(structTo, ctx)
                : to== Any,
            StatePrimitive => to is StatePrimitive p && target.CanBePessimisticConvertedTo(p),
            ConstraintsState { HasDescendant: true } fc => fc.Descendant.FitsInto(to, ctx),
            ConstraintsState => true,
            StateRefTo targetR=> targetR.GetNonReference().FitsInto(to, ctx),
            _ => throw new NotImplementedException($"Type {target} :> {to} is not supported in FIT")
        };
    }

    private static bool FitsInto(this StateStruct from, StateStruct to, AlgebraCycleContext ctx) {
        // Struct cannot fit into MutStruct (can't upgrade immutable to mutable)
        if (to is StateMutableStruct && from is not StateMutableStruct)
            return false;

        // 'from' has to have every field from 'to'
        foreach (var toField in to.Fields)
        {
            var fromField = from.GetFieldOrNull(toField.Key);
            if (fromField == null) return false;

            // MutStruct x MutStruct: fields are invariant (must Unify, not just FitsInto)
            if (from is StateMutableStruct && to is StateMutableStruct)
            {
                var unified = fromField.State.UnifyOrNull(toField.Value.State, ctx);
                if (unified == null) return false;
            }
            else
            {
                // MutStruct → Struct (read-only view): covariant, use FitsInto
                // Struct → Struct: covariant, use FitsInto
                if (!fromField.State.FitsInto(toField.Value.State, ctx))
                    return false;
            }
        }
        return true;
    }

    private static bool FitsInto(this StateFun target, StateFun to, AlgebraCycleContext ctx) {
        if (target.ArgsCount != to.ArgsCount)
            return false;
        if (!target.ReturnType.FitsInto(to.ReturnType, ctx))
            return false;
        for (int i = 0; i < target.ArgsCount; i++)
        {
            if (!to.ArgNodes[i].State.FitsInto(target.ArgNodes[i].State, ctx))
                return false;
        }
        return true;
    }

    /// <summary>
    /// For any 'to' value, there exist 'desc' value, that can be pessimisticly converted to 'to'
    /// </summary>
    private static bool CanBeFitConverted(ITicNodeState desc, ITicNodeState to, AlgebraCycleContext ctx) {
        while (true)
        {
            if (to is StateRefTo rto)
            {
                to = rto.Element;
                continue;
            }

            if (to== Any) return true;

            return desc switch {
                StateRefTo descRef => CanBeFitConverted(descRef.Element, to, ctx),
                StatePrimitive descP => to switch {
                    StatePrimitive toP => descP.CanBePessimisticConvertedTo(toP),
                    ConstraintsState toC => toC.Descendant != null && descP.CanBeConvertedPessimisticTo(toC.Descendant),
                    // Implicit lift D ≤ opt(X) ⟺ D ≤ X — same rule as the composite arm below.
                    StateOptional toOpt => CanBeFitConverted(descP, toOpt.Element, ctx),
                    _ => false
                },
                ConstraintsState fromDesc => fromDesc.Descendant == null || CanBeFitConverted(fromDesc.Descendant, to, ctx),
                ICompositeState comp => to switch {
                    ConstraintsState constrAnc => constrAnc.Descendant is ICompositeState toComposite && CanBeFitConverted(comp, toComposite, ctx),
                    ICompositeState composite => CanBeFitConverted(comp, composite, ctx),
                    _ => false
                },
                _ => throw new NotSupportedException($"CBFC does not support {desc} to {to}")
            };
        }
    }

    /// <summary>
    /// For any 'to' value, there exist 'desc' value, that can be pessimisticly converted to 'to'
    /// </summary>
    private static bool CanBeFitConverted(ICompositeState desc, ICompositeState to, AlgebraCycleContext ctx) {
        // MutStruct <: Struct, so treat both as struct family for dispatch
        if (desc.GetType() == to.GetType() || (desc is StateStruct && to is StateStruct))
        {
            return desc switch {
                StateArray descA => CanBeFitConverted(descA.Element, ((StateArray)to).Element, ctx),
                StateFun descF => CanBeFitConverted(descF, (StateFun)to, ctx),
                StateStruct descS when to is StateStruct toS => CanBeFitConverted(descS, toS, ctx),
                StateOptional descO => CanBeFitConverted(descO.Element, ((StateOptional)to).Element, ctx),
                _ => false
            };
        }

        // Implicit lift: T fits into opt(T) — lower bound T is satisfied by opt(T)
        if (to is StateOptional toOpt)
            return CanBeFitConverted(desc, toOpt.Element, ctx);
        return false;
    }

    /// <summary>
    /// desc ≤ to (width subtyping): desc must have all fields of to, each pessimistically convertible.
    /// Extra desc fields are OK (more fields = subtype).
    /// Struct cannot fit into MutStruct. MutStruct x MutStruct: invariant fields.
    /// </summary>
    private static bool CanBeFitConverted(StateStruct desc, StateStruct to, AlgebraCycleContext ctx) {
        // Struct cannot fit into MutStruct
        if (to is StateMutableStruct && desc is not StateMutableStruct)
            return false;

        foreach (var (toName, toField) in to.Fields)
        {
            var descField = desc.GetFieldOrNull(toName);
            if (descField == null)
                return false;
            // MutStruct x MutStruct: invariant fields
            if (desc is StateMutableStruct && to is StateMutableStruct)
            {
                if (descField.State.UnifyOrNull(toField.State, ctx) == null)
                    return false;
            }
            else if (!descField.State.CanBeConvertedPessimisticTo(toField.State))
                return false;
        }

        return true;
    }


    private static bool CanBeFitConverted(StateFun desc, StateFun to, AlgebraCycleContext ctx) {
        if (desc.ArgsCount != to.ArgsCount) return false;
        if (!CanBeFitConverted(desc.ReturnType, to.ReturnType, ctx))
            return false;
        for (int i = 0; i < to.ArgsCount; i++)
        {
            var descArg = desc.ArgNodes[i].State;
            var toArg = to.ArgNodes[i].State;
            if (!CanBeFitConverted(toArg, descArg, ctx))
                return false;
        }
        return true;
    }
}
