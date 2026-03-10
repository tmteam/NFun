namespace NFun.Tic.Algebra;

using System;
using NFun.Tic.SolvingStates;
using static NFun.Tic.SolvingStates.StatePrimitive;

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
                if (!a.Element.Equals(StatePrimitive.Char))
                    return false;
            }

            if (target is StatePrimitive tp)
                return tp.IsComparable;
        }
        return to.Descendant == null || CanBeFitConverted(to.Descendant, target);
    }


    public static bool FitsInto(this ITicNodeState target, ITicNodeState to) {
        if (to is StateRefTo toR)
            return FitsInto(target, toR.GetNonReference());
        if (to is ConstraintsState constrainsState)
            return target.FitsInto(constrainsState);

        // to is Optional: None ≤ Opt(T), T ≤ Opt(T) via implicit lift, Opt(A) ≤ Opt(B) covariant
        if (to is StateOptional optTo)
        {
            if (target is StatePrimitive { Name: PrimitiveTypeName.None })
                return true;
            if (target is StateOptional targetOpt)
                return targetOpt.Element.FitsInto(optTo.Element);
            // T ≤ Opt(U) iff T ≤ U (implicit lift: T ≤ U ≤ Opt(U))
            return target.FitsInto(optTo.Element);
        }

        // None fits into Opt(T) (handled above), None itself, or Any
        if (target is StatePrimitive { Name: PrimitiveTypeName.None })
            return to is StatePrimitive p && (p.Name == PrimitiveTypeName.None || p.Name == PrimitiveTypeName.Any);

        return target switch {
            // Opt(A) fits into Opt(B) (handled above) or Any
            StateOptional => to.Equals(Any),
            StateArray targetA => to is StateArray arrTo
                ? targetA.Element.FitsInto(arrTo.Element)
                : to.Equals(Any),
            StateFun targetF => to is StateFun funTo
                ? targetF.FitsInto(funTo)
                : to.Equals(Any),
            StateStruct targetS => to is StateStruct structTo
                ? targetS.FitsInto(structTo)
                : to.Equals(Any),
            StatePrimitive => to is StatePrimitive p && target.CanBePessimisticConvertedTo(p),
            ConstraintsState { HasDescendant: true } fc => FitsInto(fc.Descendant, to),
            ConstraintsState => true,
            StateRefTo targetR=> FitsInto(targetR.GetNonReference(), to),
            _ => throw new NotImplementedException($"Type {target} :> {to} is not supported in FIT")
        };
    }

    private static bool FitsInto(this StateStruct from, StateStruct to) {
        // 'from' has to have every field from 'to'
        // every 'from' field has to fit into 'to' field
        foreach (var toField in to.Fields)
        {
            var fromField = from.GetFieldOrNull(toField.Key);
            if (fromField == null) return false;
            if (!FitsInto(fromField.State, toField.Value.State))
                return false;
        }
        return true;
    }

    private static bool FitsInto(this StateFun target, StateFun to) {
        if (target.ArgsCount != to.ArgsCount)
            return false;
        if (!FitsInto(target.ReturnType, to.ReturnType))
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
    private static bool CanBeFitConverted(ITicNodeState desc, ITicNodeState to) {
        if (to is StateRefTo rto)
            return CanBeFitConverted(desc, rto.Element);
        if (to.Equals(Any))
            return true;

        return desc switch {
            StateRefTo descRef => CanBeFitConverted(descRef.Element, to),
            StatePrimitive descP => to switch {
                StatePrimitive toP => descP.CanBePessimisticConvertedTo(toP),
                ConstraintsState toC => toC.Descendant!=null && descP.CanBeConvertedPessimisticTo(toC.Descendant),
                _ => false
            },
            ConstraintsState fromDesc => fromDesc.Descendant==null || CanBeFitConverted(fromDesc.Descendant, to),
            ICompositeState comp => to switch {
                ConstraintsState constrAnc => constrAnc.Descendant is ICompositeState toComposite && CanBeFitConverted(comp, toComposite),
                ICompositeState composite => CanBeFitConverted(comp, composite),
                _ => false
            },
            _ => throw new NotSupportedException($"CBFC does not support {desc} to {to}")
        };
    }

    /// <summary>
    /// For any 'to' value, there exist 'desc' value, that can be pessimisticly converted to 'to'
    /// </summary>
    private static bool CanBeFitConverted(ICompositeState desc, ICompositeState to) {
        if (desc.GetType() != to.GetType())
            return false;
        return desc switch {
            StateArray descA     => CanBeFitConverted(descA.Element, ((StateArray)to).Element),
            StateFun descF       => CanBeFitConverted(descF, (StateFun)to),
            StateStruct descS    => CanBeFitConverted(descS, (StateStruct)to),
            StateOptional descO  => CanBeFitConverted(descO.Element, ((StateOptional)to).Element),
            _ => false
        };
    }

    /// <summary>
    /// desc ≤ to (width subtyping): desc must have all fields of to, each pessimistically convertible.
    /// Extra desc fields are OK (more fields = subtype).
    /// </summary>
    private static bool CanBeFitConverted(StateStruct desc, StateStruct to) {
        foreach (var (toName, toField) in to.Fields)
        {
            var descField = desc.GetFieldOrNull(toName);
            if (descField == null)
                return false;
            if (!descField.State.CanBeConvertedPessimisticTo(toField.State))
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
