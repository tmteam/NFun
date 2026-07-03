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

        if (to is StateOptional optTo)
        {
            if (target == None)
                return true;
            if (target is StateOptional targetOpt)
                return targetOpt.Element.FitsInto(optTo.Element);
            // Implicit lift: T ≤ U ⇒ T ≤ Opt(U).
            return target.FitsInto(optTo.Element);
        }

        if (target == None)
            return to is StatePrimitive p && (p.Name == PrimitiveTypeName.None || p.Name == PrimitiveTypeName.Any);

        return target switch {
            StateOptional => to== Any,
            StateArray targetA => to is StateArray arrTo
                ? targetA.Element.FitsInto(arrTo.Element)
                : to== Any,
            // SC ≤ SC requires same Constructor (invariant); SC ≤ T[] via Array-branch.
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
            // Two CompCs's with shared ElementNode identity are trivially compatible.
            StateCompositeConstraints targetCc => to switch {
                StatePrimitive p => p.Name == PrimitiveTypeName.Any,
                StateCompositeConstraints toCc =>
                    ReferenceEquals(targetCc.ElementNode.GetNonReference(), toCc.ElementNode.GetNonReference()),
                _ => false
            },
            _ => throw new NotImplementedException($"Type {target} :> {to} is not supported in FIT")
        };
    }

    private static bool FitsInto(this StateStruct from, StateStruct to) {
        if (to is StateMutableStruct && from is not StateMutableStruct)
            return false;

        foreach (var toField in to.Fields)
        {
            var fromField = from.GetFieldOrNull(toField.Key);
            if (fromField == null) return false;

            // MutStruct fields are invariant — Unify, not FitsInto.
            if (from is StateMutableStruct && to is StateMutableStruct)
            {
                var unified = fromField.State.UnifyOrNull(toField.Value.State);
                if (unified == null) return false;
            }
            else
            {
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

    /// <summary>True for Array-branch ConstructorKinds (List, Array, FixedArray) — all flow into StateArray slots.</summary>
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
                // Two CompCs's with shared ElementNode identity are trivially compatible.
                StateCompositeConstraints fromCc => to switch {
                    StateCompositeConstraints toCc =>
                        ReferenceEquals(fromCc.ElementNode.GetNonReference(), toCc.ElementNode.GetNonReference()),
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
        if (desc.GetType() == to.GetType() || (desc is StateStruct && to is StateStruct))
        {
            return desc switch {
                StateArray descA => CanBeFitConverted(descA.Element, ((StateArray)to).Element),
                StateFun descF => CanBeFitConverted(descF, (StateFun)to),
                StateStruct descS when to is StateStruct toS => CanBeFitConverted(descS, toS),
                StateOptional descO => CanBeFitConverted(descO.Element, ((StateOptional)to).Element),
                StateCollection descC when to is StateCollection toC =>
                    descC.Constructor == toC.Constructor
                    && CanBeFitConverted(descC.Element, toC.Element),
                _ => false
            };
        }

        // Array-branch SC fits into StateArray with element fit.
        if (desc is StateCollection descColl
            && to is StateArray toArr
            && IsArrayBranchKind(descColl.Constructor))
            return CanBeFitConverted(descColl.Element, toArr.Element);

        // Implicit lift: T ≤ opt(T).
        if (to is StateOptional toOpt)
            return CanBeFitConverted(desc, toOpt.Element);
        return false;
    }

    /// <summary>
    /// Width subtyping: desc has every field of to, each pessimistically convertible.
    /// MutStruct fields are invariant; Struct does not fit into MutStruct.
    /// </summary>
    private static bool CanBeFitConverted(StateStruct desc, StateStruct to) {
        if (to is StateMutableStruct && desc is not StateMutableStruct)
            return false;

        foreach (var (toName, toField) in to.Fields)
        {
            var descField = desc.GetFieldOrNull(toName);
            if (descField == null)
                return false;
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
