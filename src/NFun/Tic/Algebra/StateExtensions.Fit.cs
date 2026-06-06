namespace NFun.Tic.Algebra;

using System;
using SolvingStates;
using static SolvingStates.StatePrimitive;

public static partial class StateExtensions {
    public static bool FitsInto(this ITicNodeState target, ConstraintsState to) {
        if (to.HasAncestor && !target.CanBePessimisticConvertedTo(to.Ancestor))
            return false;
        if (to.IsComparable)
        {
            if (target is ICompositeState)
            {
                // the only comparable composite is arr(char)
                if (target is not StateArray a)
                    return false;
                if (a.Element != StatePrimitive.Char)
                    return false;
            }

            if (target is StatePrimitive tp)
                return tp.IsComparable;
        }
        return to.Descendant == null || CanBeFitConverted(to.Descendant, target);
    }


    public static bool FitsInto(this ITicNodeState target, ITicNodeState to) {
        if (to is StateRefTo toR)
            return target.FitsInto(toR.GetNonReference());
        if (to is ConstraintsState constrainsState)
            return target.FitsInto(constrainsState);

        // to is Optional: None ≤ Opt(T), T ≤ Opt(T) via implicit lift, Opt(A) ≤ Opt(B) covariant
        if (to is StateOptional optTo)
        {
            if (target == None)
                return true;
            if (target is StateOptional targetOpt)
                return targetOpt.Element.FitsInto(optTo.Element);
            // T ≤ Opt(U) iff T ≤ U (implicit lift: T ≤ U ≤ Opt(U))
            return target.FitsInto(optTo.Element);
        }

        // None fits into Opt(T) (handled above), None itself, or Any
        if (target == None)
            return to is StatePrimitive p && (p.Name == PrimitiveTypeName.None || p.Name == PrimitiveTypeName.Any);

        return target switch {
            // Opt(A) fits into Opt(B) (handled above) or Any
            StateOptional => to== Any,
            StateArray targetA => to is StateArray arrTo
                ? targetA.Element.FitsInto(arrTo.Element)
                : to== Any,
            // StateCollection: fits into another StateCollection of the SAME kind
            // with element fit, OR into a StateArray (per Stage 0 collections
            // hierarchy `List ⊆ Array`) with element fit, OR into Any. Cross-kind
            // collection / cross-family rejected.
            StateCollection targetC => to switch {
                StateCollection collTo =>
                    collTo.Constructor == targetC.Constructor
                    && targetC.Element.FitsInto(collTo.Element),
                StateArray arrTo when IsArrayBranchKind(targetC.Constructor) =>
                    targetC.Element.FitsInto(arrTo.Element),
                _ => to == Any,
            },
            StateFun targetF => to is StateFun funTo
                ? targetF.FitsInto(funTo)
                : to== Any,
            StateStruct targetS => to is StateStruct structTo
                ? targetS.FitsInto(structTo)
                : to== Any,
            StatePrimitive => to is StatePrimitive p && target.CanBePessimisticConvertedTo(p),
            ConstraintsState { HasDescendant: true } fc => fc.Descendant.FitsInto(to),
            ConstraintsState => true,
            StateRefTo targetR=> targetR.GetNonReference().FitsInto(to),
            _ => throw new NotImplementedException($"Type {target} :> {to} is not supported in FIT")
        };
    }

    private static bool FitsInto(this StateStruct from, StateStruct to) {
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
                var unified = fromField.State.UnifyOrNull(toField.Value.State);
                if (unified == null) return false;
            }
            else
            {
                // MutStruct → Struct (read-only view): covariant, use FitsInto
                // Struct → Struct: covariant, use FitsInto
                if (!fromField.State.FitsInto(toField.Value.State))
                    return false;
            }
        }
        return true;
    }

    private static bool FitsInto(this StateFun target, StateFun to) {
        if (target.ArgsCount != to.ArgsCount)
            return false;
        if (!target.ReturnType.FitsInto(to.ReturnType))
            return false;
        for (int i = 0; i < target.ArgsCount; i++)
        {
            if (!to.ArgNodes[i].State.FitsInto(target.ArgNodes[i].State))
                return false;
        }
        return true;
    }

    /// <summary>
    /// For any 'to' value, there exist 'desc' value, that can be pessimisticly converted to 'to'
    /// </summary>
    /// <summary>True for the Stage 0 lattice's `Array`-branch ConstructorKinds
    /// (List ⊆ Array ⊆ FixedArray) — all flow into legacy StateArray slots.</summary>
    private static bool IsArrayBranchKind(SolvingStates.ConstructorKind k) =>
        k == SolvingStates.ConstructorKind.List
        || k == SolvingStates.ConstructorKind.Array
        || k == SolvingStates.ConstructorKind.FixedArray;

    private static bool CanBeFitConverted(ITicNodeState desc, ITicNodeState to) {
        while (true)
        {
            if (to is StateRefTo rto)
            {
                to = rto.Element;
                continue;
            }

            if (to== Any) return true;

            return desc switch {
                StateRefTo descRef => CanBeFitConverted(descRef.Element, to),
                StatePrimitive descP => to switch {
                    StatePrimitive toP => descP.CanBePessimisticConvertedTo(toP),
                    ConstraintsState toC => toC.Descendant != null && descP.CanBeConvertedPessimisticTo(toC.Descendant),
                    _ => false
                },
                ConstraintsState fromDesc => fromDesc.Descendant == null || CanBeFitConverted(fromDesc.Descendant, to),
                ICompositeState comp => to switch {
                    ConstraintsState constrAnc => constrAnc.Descendant is ICompositeState toComposite && CanBeFitConverted(comp, toComposite),
                    ICompositeState composite => CanBeFitConverted(comp, composite),
                    _ => false
                },
                _ => throw new NotSupportedException($"CBFC does not support {desc} to {to}")
            };
        }
    }

    /// <summary>
    /// For any 'to' value, there exist 'desc' value, that can be pessimisticly converted to 'to'
    /// </summary>
    private static bool CanBeFitConverted(ICompositeState desc, ICompositeState to) {
        // MutStruct <: Struct, so treat both as struct family for dispatch
        if (desc.GetType() == to.GetType() || (desc is StateStruct && to is StateStruct))
        {
            return desc switch {
                StateArray descA => CanBeFitConverted(descA.Element, ((StateArray)to).Element),
                StateFun descF => CanBeFitConverted(descF, (StateFun)to),
                StateStruct descS when to is StateStruct toS => CanBeFitConverted(descS, toS),
                StateOptional descO => CanBeFitConverted(descO.Element, ((StateOptional)to).Element),
                // StateCollection: same kind required (invariant constructor).
                StateCollection descC when to is StateCollection toC =>
                    descC.Constructor == toC.Constructor
                    && CanBeFitConverted(descC.Element, toC.Element),
                _ => false
            };
        }

        // Stage 0 hierarchy: any Array-branch StateCollection (List / Array /
        // FixedArray) fits into legacy StateArray. Element fit is required.
        if (desc is StateCollection descColl
            && to is StateArray toArr
            && IsArrayBranchKind(descColl.Constructor))
            return CanBeFitConverted(descColl.Element, toArr.Element);

        // Implicit lift: T fits into opt(T) — lower bound T is satisfied by opt(T)
        if (to is StateOptional toOpt)
            return CanBeFitConverted(desc, toOpt.Element);
        return false;
    }

    /// <summary>
    /// desc ≤ to (width subtyping): desc must have all fields of to, each pessimistically convertible.
    /// Extra desc fields are OK (more fields = subtype).
    /// Struct cannot fit into MutStruct. MutStruct x MutStruct: invariant fields.
    /// </summary>
    private static bool CanBeFitConverted(StateStruct desc, StateStruct to) {
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
                if (descField.State.UnifyOrNull(toField.State) == null)
                    return false;
            }
            else if (!descField.State.CanBeConvertedPessimisticTo(toField.State))
                return false;
        }

        return true;
    }


    private static bool CanBeFitConverted(StateFun desc, StateFun to) {
        if (desc.ArgsCount != to.ArgsCount) return false;
        if (!CanBeFitConverted(desc.ReturnType, to.ReturnType))
            return false;
        for (int i = 0; i < to.ArgsCount; i++)
        {
            var descArg = desc.ArgNodes[i].State;
            var toArg = to.ArgNodes[i].State;
            if (!CanBeFitConverted(toArg, descArg))
                return false;
        }
        return true;
    }
}
