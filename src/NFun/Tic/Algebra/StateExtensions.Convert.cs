namespace NFun.Tic.Algebra;

using System;
using NFun.Tic.SolvingStates;
using static NFun.Tic.SolvingStates.StatePrimitive;

public static partial class StateExtensions {
    /// <summary>
    /// `from` can be converted to `to` in SOME case
    /// </summary>
    public static bool CanBeConvertedOptimisticTo(this ITicNodeState from, ITicNodeState to) {
        if (from is StateRefTo fromRef)
            return CanBeConvertedOptimisticTo(fromRef.Element, to);
        if (to is StateRefTo toRef)
            return CanBeConvertedOptimisticTo(from, toRef.Element);

        // None can only optimistically convert to None or Optional targets
        if (from is StatePrimitive { Name: PrimitiveTypeName.None })
            return to is StatePrimitive { Name: PrimitiveTypeName.None }
                || (to is ICompositeState cs && CanBeConvertedOptimisticTo(from, cs));

        // Optional can only optimistically convert to Optional targets
        if (from is StateOptional)
            return to is ICompositeState cs2 && CanBeConvertedOptimisticTo(from, cs2);

        if (to.Equals(Any))
            return true;
        if (to is ICompositeState compositeState)
            return CanBeConvertedOptimisticTo(from, compositeState);
        if (from is StatePrimitive)
            return to is StatePrimitive top2
                ? from.CanBePessimisticConvertedTo(top2)
                : to is ConstraintsState toConstraints2 &&
                  (!toConstraints2.HasAncestor || from.CanBePessimisticConvertedTo(toConstraints2.Ancestor));
        if (from is ConstraintsState fromConstraints)
        {
            if (fromConstraints.NoConstrains)
                return true;
            if (to is ConstraintsState toConstraints)
            {
                var ancestor = toConstraints.Ancestor;
                // if there is no ancestor, than anything can be possibly converted to 'to'
                if (ancestor == null || Equals(ancestor, Any))
                    return true;
                //if there is ancestor, then either 'from.ancestor` either `from.desc` has to be converted to it
                if (fromConstraints.HasAncestor && CanBeConvertedOptimisticTo(fromConstraints.Ancestor, ancestor))
                    return true;
                if (fromConstraints.HasDescendant && CanBeConvertedOptimisticTo(fromConstraints.Descendant, ancestor))
                    return true;
            }

            if (to is StatePrimitive toP)
                return CanBeConvertedOptimisticTo(fromConstraints, toP);
        }

        if (from is ICompositeState)
        {
            if (to is ConstraintsState constrainsState)
                // if there is no ancestor, than anything can be possibly converted to 'to'
                return constrainsState.Ancestor == null || Equals(constrainsState.Ancestor, Any);
            return false;
        }

        if (to is StatePrimitive toPrimitive)
            return from.CanBePessimisticConvertedTo(toPrimitive);
        return false;
    }

    public static bool CanBeConvertedOptimisticTo(this ConstraintsState from, StatePrimitive to) {
        if (from.Ancestor?.CanBePessimisticConvertedTo(to) == true)
            return true;

        if (from.HasDescendant)
        {
            var concretest = from.Descendant.Concretest();
            if (concretest is ConstraintsState { HasAncestor : false, HasDescendant: false })
                return true;
            return concretest.CanBePessimisticConvertedTo(to);
        }

        if (from.HasAncestor)
            return to.CanBePessimisticConvertedTo(from.Ancestor);

        if (from.IsComparable)
            return to.IsComparable || to.Equals(Any);
        else
            return true;
    }

    /// <summary>
    /// `from` can be converted to `to` in SOME case
    /// </summary>
    public static bool CanBeConvertedOptimisticTo(this ITicNodeState from, ICompositeState to) {
        if (from is StateRefTo r)
            return CanBeConvertedOptimisticTo(r.Element, to);

        // to is Optional: implicit lift from any compatible type
        if (to is StateOptional optTo)
        {
            if (from is StatePrimitive { Name: PrimitiveTypeName.None })
                return true;
            if (from is StateOptional optFrom)
                return optFrom.Element.CanBeConvertedOptimisticTo(optTo.Element);
            return from.CanBeConvertedOptimisticTo(optTo.Element);
        }

        // from is Optional: can only convert to Optional target (handled above)
        if (from is StateOptional)
            return false;

        if (from is ConstraintsState constrainsState)
            return !constrainsState.HasDescendant;
        if (from.GetType() != to.GetType())
            return false;
        return from switch {
            StateArray arrayFrom => CanBeConvertedOptimisticTo(arrayFrom.Element, ((StateArray)to).Element),
            StateFun funFrom => CanBeConvertedOptimisticTo(funFrom, (StateFun)to),
            StateStruct structFrom => CanBeConvertedOptimisticTo(structFrom, (StateStruct)to),
            _ => throw new NotSupportedException($"{from} is not supported for CanBeConvertedOptimistic")
        };
    }

    public static bool CanBeConvertedOptimisticTo(this StateFun from, StateFun to) {
        if (from.ArgsCount != to.ArgsCount)
            return false;
        if (!from.ReturnType.CanBeConvertedOptimisticTo(to.ReturnType))
            return false;
        for (int i = 0; i < from.ArgsCount; i++)
        {
            var fromType = from.ArgNodes[i].State;
            var toType = to.ArgNodes[i].State;
            if (!toType.CanBeConvertedOptimisticTo(fromType))
                return false;
        }

        return true;
    }

    public static bool CanBeConvertedOptimisticTo(this StateStruct from, StateStruct to) {
        if (to.FieldsCount > from.FieldsCount)
            return false;
        foreach (var toField in to.Fields)
        {
            var fromField = from.GetFieldOrNull(toField.Key);
            if (fromField == null)
                return false;
            var unitype = UnifyOrNull(fromField.State, toField.Value.State);
            if (unitype == null)
                return false;
        }

        return true;
    }


    /// <summary>
    /// `from` can be converted to `to` in ANY case
    /// </summary>
    private static bool CanBeConvertedPessimisticTo(this ICompositeState from, ConstraintsState to) {
        if (to.NoConstrains)
            return true;
        if (to.Ancestor != null && !from.CanBePessimisticConvertedTo(to.Ancestor))
            return false;
        if (to.IsComparable)
            return from is StateArray array && CanBeConvertedPessimisticTo(from: StatePrimitive.Char, array.Element);
        // so state has to be converted to descendant, to allow this
        var toDescendant = to.Descendant;
        if (Equals(toDescendant, Any))
            return true;
        if (toDescendant is ICompositeState toComposite)
            return CanBeConvertedPessimisticTo(from, toComposite);
        return false;
    }

    /// <summary>
    /// `from` can be converted to `to` in ANY case
    /// </summary>
    public static bool CanBeConvertedPessimisticTo(this ITicNodeState from, ITicNodeState to) {
        if (to is StateRefTo ancRef)
            return CanBeConvertedPessimisticTo(from, ancRef.Element);

        // to is Optional: None → Opt(T) = true, Opt(A) → Opt(B) covariant, T → Opt(T) implicit lift
        if (to is StateOptional optTo)
        {
            if (from is StatePrimitive { Name: PrimitiveTypeName.None })
                return true;
            if (from is StateOptional optFrom)
                return CanBeConvertedPessimisticTo(optFrom.Element, optTo.Element);
            return CanBeConvertedPessimisticTo(from, optTo.Element);
        }

        // None and Optional can't pessimistically convert to non-optional targets
        if (from is StatePrimitive { Name: PrimitiveTypeName.None })
            return to is StatePrimitive { Name: PrimitiveTypeName.None };
        if (from is StateOptional)
            return false;

        if (to.Equals(Any))
            return true;

        return from switch {
            StateRefTo descRef => CanBeConvertedPessimisticTo(descRef.Element, to),
            StatePrimitive fromPrim => to switch {
                StatePrimitive p => fromPrim.CanBePessimisticConvertedTo(p),
                ConstraintsState c => (!c.IsComparable || fromPrim.IsComparable)
                                     && c.HasDescendant && CanBeConvertedPessimisticTo(fromPrim, c.Descendant),
                _ => false
            },
            ConstraintsState fromDesc => fromDesc.HasAncestor
                ? CanBeConvertedPessimisticTo(fromDesc.Ancestor, to)
                : CanBeConvertedPessimisticTo(Any, to),
            ICompositeState comp => to switch {
                ConstraintsState constrAnc => CanBeConvertedPessimisticTo(comp, constrAnc),
                ICompositeState composite => CanBeConvertedPessimisticTo(comp, composite),
                _ => false
            }
        };
    }

    private static bool CanBeConvertedPessimisticTo(ICompositeState from, ICompositeState to) =>
        from switch {
            StateArray arrayDesc => to switch {
                StateArray arrayAnc => CanBeConvertedPessimisticTo(arrayDesc.Element, arrayAnc.Element),
                _ => false
            },
            StateFun fromFun => to switch {
                StateFun toFun => CanBeConvertedPessimisticTo(fromFun, toFun),
                _ => false
            },
            StateStruct fromStr => to switch {
                StateStruct toStr => CanBeConvertedPessimisticTo(fromStr, toStr),
                _ => false
            },
            StateOptional fromOpt => to switch {
                StateOptional toOpt => CanBeConvertedPessimisticTo(fromOpt.Element, toOpt.Element),
                _ => false
            },
            _ => throw new NotSupportedException($"type {from} is not supported for pessimistic convertion")
        };

    private static bool CanBeConvertedPessimisticTo(StateFun from, StateFun to) {
        if (from.ArgsCount != to.ArgsCount)
            return false;
        if (!from.ReturnType.CanBeConvertedPessimisticTo(to.ReturnType))
            return false;
        for (int i = 0; i < from.ArgsCount; i++)
        {
            if (!to.ArgNodes[i].State.CanBeConvertedPessimisticTo(from.ArgNodes[i].State))
                return false;
        }

        return true;
    }

    private static bool CanBeConvertedPessimisticTo(this StateStruct from, StateStruct to) {
        if (to.FieldsCount > from.FieldsCount)
            return false;
        foreach (var toField in to.Fields)
        {
            if (!toField.Value.IsSolved)
                return false;
            var fromField = from.GetFieldOrNull(toField.Key);
            if (fromField == null || !fromField.IsSolved)
                return false;
            if (toField.Value.State.StateDescription != fromField.State.StateDescription)
                return false;
        }

        return true;
    }
}
