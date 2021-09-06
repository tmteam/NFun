using System;
using System.Linq;
using NFun.Tic.Stages;

namespace NFun.Tic.SolvingStates {

public class ConstrainsState : ITicNodeState {
    public ConstrainsState(ITypeState desc = null, StatePrimitive anc = null) {
        Descendant = desc;
        Ancestor = anc;
    }

    public ConstrainsState GetCopy() =>
        new(Descendant, Ancestor) {
            IsComparable = this.IsComparable,
            Preferred = this.Preferred,
            _basicUnsolvedDescType = this._basicUnsolvedDescType
        };

    public bool Fits(ITypeState type) {
        if (HasAncestor)
        {
            if (!type.CanBeImplicitlyConvertedTo(Ancestor))
                return false;
        }

        if (type is StatePrimitive primitive)
        {
            if (HasDescendant)
            {
                if (!Descendant.CanBeImplicitlyConvertedTo(primitive))
                    return false;
            }

            if (IsComparable && !primitive.IsComparable)
                return false;
            return true;
        }

        if (type is ICompositeState)
        {
            if (IsComparable)
                return false;
            if (!HasDescendant)
                return true;
        }

        if (type is StateArray array)
        {
            if (Descendant is not StateArray descArray)
                return false;
            if (array.Element.Equals(descArray.Element))
                return true;
            if (!array.IsSolved || !descArray.IsSolved)
                return false;
            return false;
        }

        if (type is StateFun fun)
        {
            if (Descendant is not StateFun descfun)
                return false;
            if (fun.Members.SequenceEqual(descfun.Members))
                return true;
            if (!fun.IsSolved || !descfun.IsSolved)
                return false;
        }
        else if (type is StateStruct str)
        {
            //todo unit tests
            if (IsComparable)
                return false;
            if (!HasDescendant)
                return true;
            if (Descendant is not StateStruct descStruct)
                return false;
            if (descStruct.Members.All(str.Members.Contains))
                return true;
            if (!str.IsSolved || !descStruct.IsSolved)
                return false;
        }

        throw new NotSupportedException();
    }

    public bool Fits(StatePrimitive primitiveState) {
        if (HasAncestor)
        {
            if (!primitiveState.CanBeImplicitlyConvertedTo(Ancestor))
                return false;
        }

        if (HasDescendant)
        {
            if (!Descendant.CanBeImplicitlyConvertedTo(primitiveState))
                return false;
        }

        if (IsComparable && !primitiveState.IsComparable)
            return false;
        return true;
    }

    public StatePrimitive Ancestor { get; private set; }
    public ITypeState Descendant { get; private set; }

    public bool HasAncestor => Ancestor != null;
    public bool HasDescendant => (Descendant != null);

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

    public void AddDescendant(StatePrimitive type) {
        if (type == null)
            return;

        if (Descendant == null)
            Descendant = type;
        else
        {
            var ancestor = Descendant.GetLastCommonAncestorOrNull(type);
            if (ancestor != null)
                Descendant = ancestor;
        }
    }

    private enum BasicDescType {
        None,
        IsArray,
        IsFunction,
        IsStruct
    }

    private BasicDescType _basicUnsolvedDescType = BasicDescType.None;

    private static BasicDescType ToBasicDescType(ITicNodeState state) => state switch {
        StateRefTo refTo => ToBasicDescType(refTo.GetNonReference()),
        StateFun => BasicDescType.IsFunction,
        StateArray => BasicDescType.IsArray,
        StateStruct => BasicDescType.IsStruct,
        _ => BasicDescType.None
    };

    public void AddDescendant(ITypeState type) {
        if (type == null)
            return;

        if (!type.IsSolved)
        {
            var descType = ToBasicDescType(type);
            if (_basicUnsolvedDescType != BasicDescType.None && descType != _basicUnsolvedDescType)
                Descendant = StatePrimitive.Any;

            _basicUnsolvedDescType = descType;
            return;
        }

        if (Descendant == null)
            Descendant = type;
        else
        {
            var ancestor = Descendant.GetLastCommonAncestorOrNull(type);
            if (ancestor != null)
                Descendant = ancestor;
        }
    }

    public bool IsSolved => false;
    public bool IsMutable => true;
    public StatePrimitive Preferred { get; set; }
    public bool IsComparable { get; set; }
    public bool NoConstrains => !HasDescendant && !HasAncestor && !IsComparable;

    public ITicNodeState MergeOrNull(ConstrainsState constrainsState) {
        var result = new ConstrainsState(Descendant, Ancestor) {
            IsComparable = this.IsComparable || constrainsState.IsComparable
        };

        if (result._basicUnsolvedDescType == BasicDescType.None)
        {
            result._basicUnsolvedDescType = constrainsState._basicUnsolvedDescType;
        }
        else if (constrainsState._basicUnsolvedDescType == BasicDescType.None)
        {
            result._basicUnsolvedDescType = _basicUnsolvedDescType;
        }
        else if (constrainsState._basicUnsolvedDescType == this._basicUnsolvedDescType)
        {
            result._basicUnsolvedDescType = _basicUnsolvedDescType;
        }
        else if (constrainsState._basicUnsolvedDescType != _basicUnsolvedDescType)
        {
            result.AddDescendant(StatePrimitive.Any);
        }

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

            if (!des.CanBeImplicitlyConvertedTo(anc))
                return null;
        }

        if (Preferred == null)
            result.Preferred = constrainsState.Preferred;
        else if (constrainsState.Preferred == null)
            result.Preferred = Preferred;
        else if (constrainsState.Preferred.Equals(Preferred))
            result.Preferred = Preferred;
        else if (result.Fits(Preferred) && !result.Fits(constrainsState.Preferred))
            result.Preferred = Preferred;
        else
            result.Preferred = constrainsState.Preferred;

        if (result.Preferred != null)
            if (!result.Fits(result.Preferred))
                result.Preferred = null;

        return result;
    }

    /// <summary>
    /// Try to infer most generic type if is possible
    /// Return self otherwise
    ///
    /// For most cases it means that ancestor type will be used
    /// </summary>
    public ITicNodeState SolveCovariant() {
        if (Preferred != null && Fits(Preferred))
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
        if (Preferred != null && Fits(Preferred))
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
            if (_basicUnsolvedDescType != BasicDescType.None)
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

        if (HasDescendant && _basicUnsolvedDescType != BasicDescType.None)
        {
            //Workaround
            // We cannot determine situations, when several unsolved descs exist.
            // But if unsolved descs are from different families (like array vs struct)
            // it means that only one common possible ancestor is 'ANY'

            // Suitable for cases like [true, [1,2,3]] or [{it*2}, {x = 12}]

            if (ToBasicDescType(Descendant) != _basicUnsolvedDescType)
            {
                Descendant = StatePrimitive.Any;
            }
        }

        if (HasAncestor && HasDescendant)
        {
            if (Ancestor.Equals(Descendant))
                return Ancestor;
            if (!Descendant.CanBeImplicitlyConvertedTo(Ancestor))
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
        if (_basicUnsolvedDescType != BasicDescType.None)
            res += "(" + _basicUnsolvedDescType.ToString()[2] + ")";
        return res;
    }

    public string Description => ToString();

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

    public bool ApplyDescendant(
        IStateCombination2dimensionalVisitor visitor, TicNode ancestorNode,
        TicNode descendantNode) =>
        descendantNode.State.Apply(visitor, ancestorNode, descendantNode, this);

    public bool Apply(
        IStateCombination2dimensionalVisitor visitor, TicNode ancestorNode, TicNode descendantNode,
        StatePrimitive ancestor)
        => visitor.Apply(ancestor, this, ancestorNode, descendantNode);

    public bool Apply(
        IStateCombination2dimensionalVisitor visitor, TicNode ancestorNode, TicNode descendantNode,
        ConstrainsState ancestor)
        => visitor.Apply(ancestor, this, ancestorNode, descendantNode);

    public bool Apply(
        IStateCombination2dimensionalVisitor visitor, TicNode ancestorNode, TicNode descendantNode,
        ICompositeState ancestor)
        => visitor.Apply(ancestor, this, ancestorNode, descendantNode);
}

}