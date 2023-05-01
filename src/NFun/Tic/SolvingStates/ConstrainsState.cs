using System;

namespace NFun.Tic.SolvingStates;

public class ConstrainsState : ITicNodeState {
    public StatePrimitive Ancestor { get; private set; }
    public ITicNodeState Descendant { get; private set; }
    public bool HasAncestor => Ancestor != null;
    public bool HasDescendant => Descendant != null;
    public bool IsSolved => false;
    public bool IsMutable => true;
    public StatePrimitive Preferred { get; set; }
    public bool IsComparable { get; }
    public bool NoConstrains => !HasDescendant && !HasAncestor && !IsComparable;

    public ConstrainsState(ITicNodeState desc = null, StatePrimitive anc = null, bool isComparable = false) {
        Descendant = desc;
        Ancestor = anc;
        IsComparable = isComparable;
    }

    public ConstrainsState GetCopy() =>
        new(Descendant, Ancestor, IsComparable) {
            Preferred = Preferred,
        };

    public bool Fits(ITicNodeState type) => CanBeFitConverted(Lca.GetMaxType(this), Lca.GetMaxType(type));

    public bool Fits(ICompositeState type) {
        if (HasAncestor && !type.CanBePessimisticConvertedTo(Ancestor))
            return false;
        if (IsComparable)
        {
            // the only comparable composite is arr(char)
            if (!(type is StateArray a))
                return false;
            if (!a.Element.Equals(StatePrimitive.Char))
                return false;
        }

        if (!HasDescendant)
            return true;
        if (Descendant.GetType() != type.GetType())
            return false;
        // Descendant can be converted to type, in some scenarious
        if (Descendant is StateArray stateArray)
        {
            var typeArray = (StateArray)type;
            var bottomDesc = Lca.GetMaxType(stateArray);
            var bottomAnc = Lca.GetMaxType(typeArray);
            return CanBeFitConverted(from: bottomDesc, to: bottomAnc);
        }

        if (Descendant is StateFun stateFun)
        {
            var typeFun = (StateFun)type;
            if (stateFun.ArgsCount != typeFun.ArgsCount)
                return false;
            var returnBottomDesc = Lca.GetMaxType(stateFun.ReturnType);
            var returnBottomAnc = Lca.GetMaxType(typeFun.ReturnType);
            if (!CanBeFitConverted(from: returnBottomDesc, to: returnBottomAnc))
                return false;
            for (int i = 0; i < stateFun.ArgsCount; i++)
            {
                var descArg = stateFun.ArgNodes[i].State;
                var ancArg = typeFun.ArgNodes[i].State;
                //todo - not sure about it. It is a sketch
                var dmin = Lca.GetMinType(descArg);
                var amin = Lca.GetMinType(ancArg);
                if (!CanBeFitConverted(from: dmin, to: amin))
                    return false;
            }
            return true;
        }
        else
            throw new NotImplementedException($"Type {type} is not supported yet in FIT");
    }

    private bool CanBeFitConverted(ITicNodeState from, ITicNodeState to) =>
        //todo - it is not done. Just superficial implementation
        from switch {
            StateArray arrFrom => to is StateArray arrTo
                ? CanBeFitConverted(arrFrom.Element, arrTo.Element)
                : to.Equals(StatePrimitive.Any),
            StatePrimitive => to is ConstrainsState st
                ? st.HasDescendant && CanBeFitConverted(from, st.Descendant)
                : to is StatePrimitive p && from.CanBePessimisticConvertedTo(p),
            ConstrainsState { HasDescendant: true } fc => CanBeFitConverted(fc.Descendant, to),
            ConstrainsState => true,
            _ => throw new NotImplementedException($"Type {from}-> {to} is not supported yet in FIT")
        };

    public bool CanBeConvertedTo(ITypeState type) {
        if (HasAncestor && !type.CanBePessimisticConvertedTo(Ancestor))
            return false;

        if (type is StatePrimitive primitive)
        {
            if (HasDescendant && !Descendant.CanBePessimisticConvertedTo(primitive))
                return false;
            if (IsComparable && !primitive.IsComparable)
                return false;
            return true;
        }
        else if (type is ICompositeState)
        {
            if (IsComparable)
                return type is StateArray a && a.Element.Equals(StatePrimitive.Char);
            if (!HasDescendant)
                return true;
            if (!type.IsSolved || !Descendant.IsSolved)
                return false;
            if (Descendant.GetType() != type.GetType())
                return false;
            return true;
        }
        else
            return false;
    }

    public bool TryAddAncestor(StatePrimitive type) {
        if (type == null)
            return true;

        if (Ancestor == null)
            Ancestor = type;
        else
        {
            var res = Ancestor.GetFirstCommonDescendantOrNull(type);
            if (res == null)
                return false;
            Ancestor = res;
        }

        return true;
    }

    public void AddAncestor(StatePrimitive type) {
        if (!TryAddAncestor(type))
            throw new InvalidOperationException();
    }

    public void AddDescendant(ITicNodeState type) {
        if (type == null)
            return;
        Descendant = !HasDescendant
            ? Lca.GetMaxType(type)
            : Lca.BottomLca(Descendant, type);
    }

    public ITicNodeState MergeOrNull(ConstrainsState constrainsState) {
        var result = new ConstrainsState(Descendant, Ancestor, IsComparable || constrainsState.IsComparable);

        result.AddDescendant(constrainsState.Descendant);

        if (!result.TryAddAncestor(constrainsState.Ancestor))
            return null;

        if (result.HasAncestor && result.HasDescendant)
        {
            var anc = result.Ancestor;
            var des = result.Descendant;
            if (anc.Equals(des))
            {
                if (result.IsComparable && !anc.IsComparable)
                    return null;
                return anc;
            }

            if (Descendant!=null && !Descendant.CanBePessimisticConvertedTo(anc))
                return null;
        }

        if (Preferred == null)
            result.Preferred = constrainsState.Preferred;
        else if (constrainsState.Preferred == null)
            result.Preferred = Preferred;
        else if (constrainsState.Preferred.Equals(Preferred))
            result.Preferred = Preferred;
        else if (result.CanBeConvertedTo(Preferred) && !result.CanBeConvertedTo(constrainsState.Preferred))
            result.Preferred = Preferred;
        else
            result.Preferred = constrainsState.Preferred;

        if (result.Preferred != null)
            if (!result.CanBeConvertedTo(result.Preferred))
                result.Preferred = null;

        return result;
    }

    /// <summary>
    /// Try to infer most generic type if is possible
    /// Return self otherwise
    ///
    /// For most cases it means that ancestor type will be used
    /// </summary>
    public ITicNodeState SolveCovariant(bool ignorePreferred = false) {
        if (!ignorePreferred && Preferred != null && CanBeConvertedTo(Preferred))
            return Preferred;
        var ancestor = Ancestor ?? StatePrimitive.Any;
        if (IsComparable)
        {
            if (ancestor.IsComparable)
                return ancestor;
            else
                return this;
        }

        if (Descendant is StateArray)
            return Descendant;
        return ancestor;
    }

    /// <summary>
    /// Try to infer most CONCRETE type if is possible
    /// Return self otherwise
    ///
    /// For most cases it means that descendant type will be used
    /// </summary>
    public ITicNodeState SolveContravariant() {
        if (Preferred != null && CanBeConvertedTo(Preferred))
            return Preferred;
        if (!HasDescendant)
            return this;

        if (IsComparable)
        {
            //todo
            //char[] is comparable!
            if (Descendant is not StatePrimitive { IsComparable: true })
                return this;
        }

        return Descendant;
    }

    public ITicNodeState GetOptimizedOrNull() {
        if (IsComparable)
        {
            if(Descendant is StateArray ar && ar.Element.CanBePessimisticConvertedTo(StatePrimitive.Char))
                return StateArray.Of(StatePrimitive.Char);

            if (Descendant is ICompositeState)
                return null;
            if (Descendant != null)
            {
                if (Descendant.Equals(StatePrimitive.Char))
                    return StatePrimitive.Char;

                if (Descendant is StatePrimitive primitive && primitive.IsNumeric)
                {
                    if (!TryAddAncestor(StatePrimitive.Real))
                        return null;
                }
                else if (Descendant is StateArray a && a.Element.Equals(StatePrimitive.Char))
                    return Descendant;
                else
                    return null;
            }
        }

        if (HasAncestor && HasDescendant)
        {
            if (Ancestor.Equals(Descendant))
                return Ancestor;
            if (!(Descendant is ITypeState descendant))
                return null;
            if (!descendant.CanBePessimisticConvertedTo(Ancestor))
                return null;
        }

        if (Descendant?.Equals(StatePrimitive.Any) == true)
            return StatePrimitive.Any;

        return this;
    }

    public override string ToString() {
        var res = $"[{Descendant}..{Ancestor}]";
        if (IsComparable)
            res += "<>";
        if (Preferred != null)
            res += Preferred + "!";
        return res;
    }

    public string Description => ToString();

    public bool CanBePessimisticConvertedTo(StatePrimitive primitive) =>
        Equals(primitive, StatePrimitive.Any) || (Ancestor?.CanBePessimisticConvertedTo(primitive) ?? false);

    public override bool Equals(object obj) {
        if (obj is not ConstrainsState constrainsState)
            return false;

        if (HasAncestor != constrainsState.HasAncestor)
            return false;
        if (HasAncestor && !constrainsState.Ancestor.Equals(Ancestor))
            return false;

        if (HasDescendant != constrainsState.HasDescendant)
            return false;
        if (HasDescendant && !constrainsState.Descendant.Equals(Descendant))
            return false;

        if (Preferred != null != (constrainsState.Preferred != null))
            return false;
        if (Preferred != null && !constrainsState.Preferred!.Equals(Preferred))
            return false;

        return IsComparable == constrainsState.IsComparable;
    }
}
