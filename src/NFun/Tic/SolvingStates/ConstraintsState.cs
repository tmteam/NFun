using System;
using NFun.Tic.Algebra;

namespace NFun.Tic.SolvingStates;

using static StatePrimitive;

public class ConstraintsState : ITicNodeState {
    public StatePrimitive Ancestor { get; private set; }
    public ITicNodeState Descendant { get; private set; }
    public bool HasAncestor => Ancestor != null;
    public bool HasDescendant => Descendant != null;
    public bool IsSolved => false;
    public bool IsMutable => true;
    public StatePrimitive Preferred { get; set; }
    public bool IsComparable { get; }
    public bool NoConstrains => !HasDescendant && !HasAncestor && !IsComparable;

    public static ConstraintsState Empty => new(null, null, false);

    public static ConstraintsState Of(ITicNodeState desc = null, StatePrimitive anc = null, bool isComparable = false) =>
        new(desc, anc, isComparable);

    private ConstraintsState(ITicNodeState desc, StatePrimitive anc, bool isComparable) {
        Descendant = desc;
        Ancestor = anc;
        IsComparable = isComparable;
    }

    public ConstraintsState GetCopy() => new(Descendant, Ancestor, IsComparable) { Preferred = Preferred };


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
        if (!HasDescendant)
        {
            Descendant = type.Concretest();
            TraceLog.WriteLine($"    Constrains.AddDescendant: first={type} => concretest={Descendant}");
        }
        else
        {
            var prev = Descendant;
            Descendant = Descendant.Lca(type);
            TraceLog.WriteLine($"    Constrains.AddDescendant: LCA({prev}, {type}) => {Descendant}");
        }
    }

    /// <summary>
    /// Intersect two constraint intervals:
    ///   desc = LCA(this.Desc, other.Desc)
    ///   anc  = GCD(this.Anc,  other.Anc)
    ///   comp = this.IsComparable || other.IsComparable
    /// Returns null if ancestors are incompatible (GCD fails).
    /// Does NOT merge Preferred or collapse composites.
    /// </summary>
    public ConstraintsState IntersectIntervalsOrNull(ConstraintsState other) {
        var result = new ConstraintsState(Descendant, Ancestor, IsComparable || other.IsComparable);
        result.AddDescendant(other.Descendant);
        if (!result.TryAddAncestor(other.Ancestor))
            return null;
        return result;
    }

    /// <summary>
    /// Returns true if the interval [Descendant..Ancestor] can contain at least one type.
    /// Open intervals (no ancestor or no descendant) are trivially non-empty.
    /// When Ancestor == Descendant, checks that comparable constraint is satisfied.
    /// </summary>
    public bool IntervalIsNonEmpty() {
        if (!HasAncestor || !HasDescendant)
            return true;
        if (Ancestor.Equals(Descendant))
            return !IsComparable || Ancestor.IsComparable;
        if (Descendant is ITypeState typeState)
            return typeState.CanBePessimisticConvertedTo(Ancestor);
        return true;
    }

    public ITicNodeState MergeOrNull(ConstraintsState constraintsState) {
        var result = IntersectIntervalsOrNull(constraintsState);
        if (result == null)
            return null;

        if (result.HasAncestor && result.HasDescendant)
        {
            if (result.Ancestor.Equals(result.Descendant))
            {
                if (result.IsComparable && !result.Ancestor.IsComparable)
                    return null;
                return result.Ancestor;
            }

            if (Descendant != null && !Descendant.CanBePessimisticConvertedTo(result.Ancestor))
                return null;
        }

        if (Preferred == null)
            result.Preferred = constraintsState.Preferred;
        else if (constraintsState.Preferred == null)
            result.Preferred = Preferred;
        else if (constraintsState.Preferred.Equals(Preferred))
            result.Preferred = Preferred;
        else if (result.CanBeConvertedTo(Preferred) && !result.CanBeConvertedTo(constraintsState.Preferred))
            result.Preferred = Preferred;
        else
            result.Preferred = constraintsState.Preferred;

        if (result.Preferred != null)
            if (!result.CanBeConvertedTo(result.Preferred))
                result.Preferred = null;

        // If descendant is a fully solved struct/optional and there is no ancestor constraint,
        // the composite type IS the result. Struct and Optional don't participate in the
        // primitive type hierarchy (unlike Array/Fun whose elements can widen covariantly),
        // so [struct/opt..] without a primitive upper bound is equivalent to the type itself.
        // Exception: when Optional has a preferred type hint, keep as ConstraintsState
        // so finalization/converter can apply preferred to the inner element.
        if (!result.HasAncestor && !result.IsComparable &&
            result.Descendant is StateStruct { IsSolved: true } or StateOptional { IsSolved: true })
        {
            if (result.Descendant is StateOptional && result.Preferred != null)
            {
                // Don't collapse — let preferred type survive to runtime resolution
            }
            else
                return (ITicNodeState)result.Descendant;
        }

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

        if (Descendant is ICompositeState)
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
            var isDescComparable = Descendant is StatePrimitive { IsComparable: true }
                                   || (Descendant is StateArray a && a.Element.Equals(StatePrimitive.Char));
            if (!isDescComparable)
                return this;
        }

        return Descendant;
    }

    public ITicNodeState SimplifyOrNull() {
        if (!HasDescendant) return this;

        if (IsComparable)
        {
            switch (Descendant)
            {
                case StateArray a:
                {
                    if (a.Element.CanBeConvertedOptimisticTo(Char))
                        return StateArray.Of(Char);
                    else
                        return null;
                }
                case StatePrimitive primitive:
                {
                    if (primitive.Equals(Char)) //it is an endpoint
                        return Char;
                    if (primitive.IsNumeric)
                    {
                        if (!TryAddAncestor(Real)) return null;
                    }
                    else return null;

                    break;
                }
                case ICompositeState:
                    return null;
            }
        }

        if (HasAncestor)
        {
            var d = Descendant;
            if (Descendant is ConstraintsState { IsComparable: true } constrains && Ancestor.IsComparable)
            {
                d = constrains.Descendant;
                if (d == null)
                    return new ConstraintsState(null, Ancestor, false);
            }

            if (Ancestor.Equals(d))
                return Ancestor;
            if (!(d is ITypeState descendant))
                return this;
            if (!descendant.CanBePessimisticConvertedTo(Ancestor))
                return null;
        }
        else if (Descendant is ConstraintsState constrainsState)
        {
            if (constrainsState.IsComparable && !IsComparable)
                return this;
            return new ConstraintsState(constrainsState.Descendant, null, IsComparable);
        }
        else if (Descendant.Equals(Any))
            return Any;

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
        if (obj is not ConstraintsState constrainsState)
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
