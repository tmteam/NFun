namespace NFun.Tic.SolvingStates;

using System;
using Algebra;

public class ConstraintsState : ITicNodeState {
    public StatePrimitive Ancestor { get; private set; }
    private ITicNodeState _descendant;

    public ITicNodeState Descendant {
        get => _descendant;
        private set => _descendant = value;
    }

    public bool HasAncestor => Ancestor != null;
    public bool HasDescendant => _descendant != null;

    /// <summary>
    /// When true, this constraint represents an optional type: opt(inner).
    /// Set when None is added as a descendant. The actual StateOptional is
    /// materialized before Destruction, after all constraints are collected.
    /// </summary>
    public bool IsOptional { get; private set; }

    public bool IsSolved => false;
    public bool IsMutable => true;
    public StatePrimitive Preferred { get; set; }
    public bool IsComparable { get; }
    public bool NoConstrains => !HasDescendant && !HasAncestor && !IsComparable && !IsOptional;

    public static ConstraintsState Empty => new(null, null, false);

    public static ConstraintsState Of(ITicNodeState desc = null, StatePrimitive anc = null,
        bool isComparable = false, bool isOptional = false) =>
        new(desc, anc, isComparable) { IsOptional = isOptional };

    private ConstraintsState(ITicNodeState desc, StatePrimitive anc, bool isComparable) {
        Descendant = desc;
        Ancestor = anc;
        IsComparable = isComparable;
    }

    public ConstraintsState GetCopy() =>
        new(_descendant, Ancestor, IsComparable) { Preferred = Preferred, IsOptional = IsOptional };

    public bool CanBeConvertedTo(ITypeState type) {
        if (HasAncestor && !type.CanBePessimisticConvertedTo(Ancestor))
            return false;

        switch (type)
        {
            case StatePrimitive primitive:
            {
                if (HasDescendant && !Descendant.CanBePessimisticConvertedTo(primitive))
                    return false;
                if (IsComparable && !primitive.IsComparable)
                    return false;
                return true;
            }
            case ICompositeState:
            {
                if (IsComparable)
                    return type is StateArray a && a.Element== StatePrimitive.Char;
                if (!HasDescendant)
                    return true;
                if (!type.IsSolved || !Descendant.IsSolved)
                    return false;
                if (Descendant.GetType() != type.GetType())
                    return false;
                return true;
            }
            default:
                return false;
        }
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

        // None sets the IsOptional flag instead of being stored as a descendant.
        // This avoids stale snapshots: Optional is materialized before Destruction,
        // after all constraints are fully propagated.
        if (type.Equals(StatePrimitive.None)) {
            IsOptional = true;
            TraceLog.WriteLine($"    Constrains.AddDescendant: None => IsOptional=true");
            return;
        }

        if (!HasDescendant)
        {
            _descendant = type.Concretest();
            TraceLog.WriteLine($"    Constrains.AddDescendant: first={type} => concretest={_descendant}");
        }
        else
        {
            var prev = Descendant;
            // Concretest the incoming type so IsOptional flags are materialized
            // into StateOptional before LCA. Without this, LCA(U8, ConstraintsState{IsOptional=true})
            // would return U8 (losing the Optional), but LCA(U8, opt(empty)) returns opt(U8).
            _descendant = Descendant.Lca(type.Concretest());
            TraceLog.WriteLine($"    Constrains.AddDescendant: LCA({prev}, {type}) => {_descendant}");
        }

    }

    /// <summary>
    /// Intersect two constraint intervals:
    ///   desc = LCA(this.Desc, other.Desc)
    ///   anc  = GCD(this.Anc,  other.Anc)
    ///   comp = this.IsComparable || other.IsComparable
    ///   opt  = this.IsOptional || other.IsOptional
    /// Returns null if ancestors are incompatible (GCD fails).
    /// Does NOT merge Preferred or collapse composites.
    /// </summary>
    public ConstraintsState IntersectIntervalsOrNull(ConstraintsState other) {
        var result = new ConstraintsState(Descendant, Ancestor, IsComparable || other.IsComparable) {
            IsOptional = IsOptional || other.IsOptional
        };
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
                if (result.IsOptional)
                    return StateOptional.Of(result.Ancestor);
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
        // the composite type IS the result.
        if (!result.HasAncestor && !result.IsComparable &&
            result.Descendant is StateStruct { IsSolved: true } or StateOptional { IsSolved: true })
        {
            if (result.Descendant is StateOptional && result.Preferred != null)
            {
                // Don't collapse — let preferred type survive to runtime resolution
            }
            else if (result.IsOptional)
                return StateOptional.Of(result.Descendant);
            else
                return result.Descendant;
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
        // Resolve inner type (ignoring IsOptional), then wrap if needed
        ITicNodeState inner;
        if (!ignorePreferred && Preferred != null && CanBeConvertedTo(Preferred))
            inner = Preferred;
        else {
            var ancestor = Ancestor ?? StatePrimitive.Any;
            if (IsComparable) {
                if (!ancestor.IsComparable)
                    return this; // unresolved — keep as ConstraintsState
                inner = ancestor;
            } else if (Descendant is ICompositeState)
                inner = Descendant;
            else
                inner = ancestor;
        }
        return IsOptional ? WrapOptional(inner) : inner;
    }

    /// <summary>
    /// Try to infer most CONCRETE type if is possible
    /// Return self otherwise
    ///
    /// For most cases it means that descendant type will be used
    /// </summary>
    public ITicNodeState SolveContravariant() {
        // Resolve inner type (ignoring IsOptional), then wrap if needed
        ITicNodeState inner;
        if (Preferred != null && CanBeConvertedTo(Preferred))
            inner = Preferred;
        else if (!HasDescendant)
            return this; // unresolved
        else if (IsComparable) {
            var isDescComparable = Descendant is StatePrimitive { IsComparable: true }
                                   || (Descendant is StateArray a && a.Element== StatePrimitive.Char);
            if (!isDescComparable)
                return this; // unresolved
            inner = Descendant;
        } else
            inner = Descendant;
        return IsOptional ? WrapOptional(inner) : inner;
    }

    private static ITicNodeState WrapOptional(ITicNodeState inner) =>
        inner == StatePrimitive.Any ? StatePrimitive.Any : StateOptional.Of(inner);

    public ITicNodeState SimplifyOrNull() {
        if (!HasDescendant && !IsOptional) return this;

        if (IsComparable)
        {
            switch (Descendant)
            {
                case StateArray a:
                {
                    if (a.Element.CanBeConvertedOptimisticTo(StatePrimitive.Char))
                        return StateArray.Of(StatePrimitive.Char);
                    return null;
                }
                case StatePrimitive primitive:
                {
                    if (primitive== StatePrimitive.Char) //it is an endpoint
                        return StatePrimitive.Char;
                    if (primitive.IsNumeric)
                    {
                        if (!TryAddAncestor(StatePrimitive.Real)) return null;
                    }
                    else return null;

                    break;
                }
                case ICompositeState:
                    return null;
            }
        }

        if (!HasDescendant)
            return this; // IsOptional=true but no descendant yet — keep collecting constraints

        if (HasAncestor)
        {
            var d = Descendant;
            if (Descendant is ConstraintsState { IsComparable: true } constrains && Ancestor.IsComparable)
            {
                d = constrains.Descendant;
                if (d == null)
                    return new ConstraintsState(null, Ancestor, false) { IsOptional = IsOptional };
            }

            if (Ancestor.Equals(d))
            {
                // Abstract types (I96, I48, U48, etc.) cannot be concrete results.
                // If the interval collapses to an abstract point, reject.
                if (Ancestor.Name.HasFlag(PrimitiveTypeName._isAbstract))
                    return null;
                return IsOptional ? StateOptional.Of(Ancestor) : (ITicNodeState)Ancestor;
            }
            if (!(d is ITypeState descendant))
                return this;
            if (!descendant.CanBePessimisticConvertedTo(Ancestor))
                return null;
            // opt(T) is a composite — it can't satisfy a primitive ancestor (except Any).
            // {desc=U8, anc=Real, IsOptional=true} is contradictory: no opt(T) ≤ Real.
            if (IsOptional && Ancestor is StatePrimitive pa && pa != StatePrimitive.Any)
                return null;
        }
        else if (Descendant is ConstraintsState constrainsState)
        {
            if (constrainsState.IsComparable && !IsComparable)
                return this;
            return new ConstraintsState(constrainsState.Descendant, null, IsComparable) {
                IsOptional = IsOptional || constrainsState.IsOptional
            };
        }
        else if (Descendant== StatePrimitive.Any)
            return IsOptional ? StateOptional.Of(StatePrimitive.Any) : (ITicNodeState)StatePrimitive.Any;

        return this;
    }

    public override string ToString() {
        var res = IsOptional ? $"[{Descendant}..{Ancestor}]?" : $"[{Descendant}..{Ancestor}]";
        if (IsComparable)
            res += "<>";
        if (Preferred != null)
            res += Preferred + "!";
        return res;
    }

    public string Description => ToString();

    public bool CanBePessimisticConvertedTo(StatePrimitive primitive) =>
        primitive == StatePrimitive.Any || (Ancestor?.CanBePessimisticConvertedTo(primitive) ?? false);

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

        if (IsOptional != constrainsState.IsOptional)
            return false;

        return IsComparable == constrainsState.IsComparable;
    }

    public string StateDescription => PrintState(0);

    public string PrintState(int depth) {
        if (depth > 100)
            return "[..REQ..]";
        depth++;
        var res = IsOptional
            ? $"[{Descendant?.PrintState(depth)}..{Ancestor?.PrintState(depth)}]?"
            : $"[{Descendant?.PrintState(depth)}..{Ancestor?.PrintState(depth)}]";
        if (IsComparable)
            res += "<>";
        if (Preferred != null)
            res += Preferred + "!";
        return res;
    }
}
