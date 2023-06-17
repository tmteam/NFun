using System;

namespace NFun.Tic.SolvingStates;

using static StatePrimitive;

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

    public static ConstrainsState Empty => new(null, null, false);

    public static ConstrainsState Of(ITicNodeState desc = null, StatePrimitive anc = null, bool isComparable = false) =>
        new(desc, anc, isComparable);

    private ConstrainsState(ITicNodeState desc, StatePrimitive anc, bool isComparable) {
        Descendant = desc;
        Ancestor = anc;
        IsComparable = isComparable;
    }

    public ConstrainsState GetCopy() => new(Descendant, Ancestor, IsComparable) { Preferred = Preferred };


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
                return type is StateArray a && a.Element.Equals(Char);
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
            ? type.Concretest()
            : Descendant.Lca(type);
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

            if (Descendant != null && !Descendant.CanBePessimisticConvertedTo(anc))
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
        var ancestor = Ancestor ?? Any;
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
            if (Descendant is StateArray ar && ar.Element.CanBeConvertedOptimisticTo(Char))
                return StateArray.Of(Char);
            if (Descendant is ICompositeState)
                return null;
            if (Descendant != null)
            {
                if (Descendant.Equals(Char))
                    return Char;
                else if (Descendant is StatePrimitive primitive && primitive.IsNumeric)
                {
                    if (!TryAddAncestor(Real))
                        return null;
                }
                else if (Descendant is StateArray a && a.Element.Equals(Char))
                    return Descendant;
            }
        }

        if (HasDescendant)
        {
            if (HasAncestor)
            {
                var d = Descendant;
                if (Descendant is ConstrainsState { IsComparable: true } constrains && Ancestor.IsComparable)
                {
                    d = constrains.Descendant;
                    if (d == null)
                        return new ConstrainsState(null, Ancestor, false);
                }

                if (IsComparable && d is StatePrimitive { IsComparable: false })
                    return null;
                if (Ancestor.Equals(d))
                    return Ancestor;
                if (!(d is ITypeState descendant))
                    return this;
                if (!descendant.CanBePessimisticConvertedTo(Ancestor))
                    return null;
            }
            else if (Descendant is ConstrainsState constrainsState)
            {
                if (constrainsState.IsComparable && !IsComparable)
                    return this;
                return new ConstrainsState(constrainsState.Descendant, null, IsComparable);
            }
            else if (Descendant.Equals(Any))
                return Any;
        }

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
        Equals(primitive, Any) || (Ancestor?.CanBePessimisticConvertedTo(primitive) ?? false);

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

    public string StateDescription => PrintState(0);

    public string PrintState(int depth) {
        if (depth > 100)
            return "[..REQ..]";
        depth++;
        var res = $"[{Descendant?.PrintState(depth)}..{Ancestor?.PrintState(depth)}]";
        if (IsComparable)
            res += "<>";
        if (Preferred != null)
            res += Preferred + "!";
        return res;
    }
}
