namespace NFun.Tic.SolvingStates;

using System;
using System.Collections.Generic;
using Algebra;

public class ConstraintsState : ITicNodeState {
    public StatePrimitive Ancestor { get; private set; }
    private ITicNodeState _descendant;

    public ITicNodeState Descendant {
        get => _descendant;
        private set => _descendant = value;
    }

    /// <summary>
    /// Clears the Descendant slot. Used by CS-internal slot promotion when the descendant struct
    /// is rebound as the F-bound (StructBound). The descendant obligation is subsumed by the
    /// F-bound (`T &lt;: S`); leaving Descendant set would over-constrain `T = S` exactly,
    /// defeating F-bounded polymorphism.
    /// </summary>
    public void ClearDescendant() {
        _descendant = null;
    }

    public bool HasAncestor => Ancestor != null;
    public bool HasDescendant => _descendant != null;

    /// <summary>
    /// When true, this constraint represents an optional type: opt(inner).
    /// Set when None is added as a descendant. The actual StateOptional is
    /// materialized before Destruction, after all constraints are collected.
    /// </summary>
    public bool IsOptional { get; private set; }

    /// <summary>
    /// F-bounded recursive upper bound: `T &lt;: τ(T)` — T is a fixed-point of the body τ
    /// (Pierce TAPL §20.2 iso-recursive types). The body is currently always a
    /// <see cref="StateStruct"/> whose fields may carry <see cref="StateRefTo"/> back to the
    /// owning ConstraintsState — that's the F-bound self-reference. Contractivity
    /// (Cardelli-Mitchell '89 §3) and covariance positions MUST hold for any such self-ref.
    ///
    /// Third independent dimension on CS, peer to IsComparable/IsOptional. Owned by exactly one
    /// ConstraintsState — never aliased.
    /// </summary>
    public StateStruct StructBound { get; set; }

    /// <summary>
    /// True iff this CS carries an F-bound. Single source of truth for "is this CS the holder
    /// of a recursive bound" — read-side analogue of the <see cref="StructBound"/> slot. Phase
    /// 3 of #108 migrates the source of truth to a new representation; this predicate is the
    /// stable read API while that migration happens.
    /// </summary>
    public bool HasStructBound => StructBound != null;

    public bool IsSolved => false;
    public bool IsMutable => true;
    public StatePrimitive Preferred { get; set; }
    public bool IsComparable { get; set; }
    public bool NoConstrains => !HasDescendant && !HasAncestor && !IsComparable && !IsOptional && !HasStructBound;

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
        new(_descendant, Ancestor, IsComparable) {
            Preferred = Preferred,
            IsOptional = IsOptional,
            // StructBound copied by reference: defensive deep-copy is only needed when the
            // copy participates in a merge that could mutate either side. Read-after-lift is
            // correct with reference-copy.
            StructBound = StructBound,
        };

    public bool CanBeConvertedTo(ITypeState type) {
        if (HasAncestor && !type.CanBePessimisticConvertedTo(Ancestor))
            return false;

        // F-bound check: T ≤ CS{S} requires T:Struct, Fields(T) ⊇ Fields(S), and pointwise
        // covariant ≤ on shared fields. F-bound self-references are guarded by FitStructBound
        // (cycle-aware).
        if (HasStructBound && !FitStructBound(type, StructBound))
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

    /// <summary>
    /// Structural F-bound check: candidate type <c>T</c> satisfies bound <c>S</c>
    /// (a <c>StateStruct</c>) iff:
    ///   1. <c>T</c> is a <c>StateStruct</c> (or wraps one through a single
    ///      <c>StateRefTo</c>),
    ///   2. <c>Fields(T) ⊇ Fields(S)</c> by name (width subtyping),
    ///   3. for each shared field, <c>T.fᵢ ≤ S.fᵢ</c> covariantly.
    ///
    /// Cycle guard: F-bound self-references (<c>S.fᵢ</c> may be a <c>RefTo</c>
    /// back to the owning CS, whose <c>StructBound</c> is <c>S</c> itself —
    /// when we recurse on <c>T.fᵢ</c> against that CS we'd hit <c>FitStructBound</c>
    /// again with the same pair). We track in-progress (T, S) pairs and return
    /// <c>true</c> on hit (Amadio–Cardelli equirecursive subtyping coinductive
    /// rule — assume the recursive subgoal holds).
    /// </summary>
    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
    private static bool FitStructBound(ITypeState candidate, StateStruct bound) {
        var guard = _fitStructBoundInProgress ??= new();
        return FitStructBoundInner(candidate, bound, guard);
    }

    [ThreadStatic] private static List<(ITypeState, StateStruct)> _fitStructBoundInProgress;

    private static bool FitStructBoundInner(ITypeState candidate, StateStruct bound, List<(ITypeState, StateStruct)> guard) {
        // Coinductive cycle break for F-bound self-refs.
        for (int i = 0; i < guard.Count; i++)
            if (ReferenceEquals(guard[i].Item1, candidate) && ReferenceEquals(guard[i].Item2, bound))
                return true;

        // Candidate must be a struct (possibly wrapped in StateRefTo).
        StateStruct candStruct = candidate switch {
            StateStruct s => s,
            _ => null,
        };
        if (candStruct == null) return false;

        // Width subtyping: Fields(candidate) ⊇ Fields(bound).
        if (candStruct.FieldsCount < bound.FieldsCount) return false;
        foreach (var boundField in bound.Fields)
        {
            if (candStruct.GetFieldOrNull(boundField.Key) == null)
                return false;
        }

        guard.Add((candidate, bound));
        try
        {
            // Covariant check on shared fields: T.fᵢ ≤ S.fᵢ for every f ∈ S.
            foreach (var boundField in bound.Fields)
            {
                var candField = candStruct.GetFieldOrNull(boundField.Key);
                var candFieldState = candField.GetNonReference().State;
                var boundFieldState = boundField.Value.GetNonReference().State;

                // S.fᵢ may be a CS with StructBound (F-bound self-ref via the
                // owning CS, or a generic structural constraint).
                if (boundFieldState is ConstraintsState boundCs)
                {
                    if (candFieldState is ITypeState candTypeState)
                    {
                        if (!boundCs.CanBeConvertedTo(candTypeState)) return false;
                    }
                    else if (candFieldState is ConstraintsState candCs)
                    {
                        // Both unsolved — assume coinductively, defer to merge phase.
                        continue;
                    }
                    else return false;
                }
                else if (boundFieldState is ITypeState boundTypeState)
                {
                    // Bound field is concrete — candidate field must Fit it.
                    if (candFieldState is ITypeState candTypeState)
                    {
                        // Reuse existing FitsInto logic from Algebra layer.
                        if (!candTypeState.CanBePessimisticConvertedTo(boundTypeState as StatePrimitive ?? StatePrimitive.Any)
                            && !boundTypeState.Equals(candTypeState))
                        {
                            // Composite vs composite — only accept exact match or primitive subtype.
                            if (boundTypeState is ICompositeState && candTypeState is ICompositeState)
                            {
                                // Defer to existing structural rules — accept if shapes match.
                                if (boundTypeState.GetType() != candTypeState.GetType())
                                    return false;
                                // shallow accept; deep check left to merge phase
                            }
                            else return false;
                        }
                    }
                    else return false;
                }
            }
            return true;
        }
        finally
        {
            guard.RemoveAt(guard.Count - 1);
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
        if (type == StatePrimitive.None) {
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
            // Short-circuit: if incoming is REFERENCE-equal to current Descendant, LCA is a no-op.
            // (Cannot use structural Equals — StateStruct.Equals short-circuits on TypeName, so
            // `n{v=1}` and `n{v=2}` would compare equal despite different field values, skipping
            // necessary LCA propagation.) Reference equality safely captures the common case where
            // multiple call sites share the same TIC node state for a named recursive arg type
            // (GH #128 TreeSum 3-op regression).
            if (ReferenceEquals(Descendant, type)) return;
            var prev = Descendant;
            var incoming = type is ConstraintsState ? type.Concretest() : type;
            // Row polymorphism: open structs (from SetFieldAccess) use field UNION,
            // closed structs (from literals/LCA) use field INTERSECTION (standard LCA).
            // Open struct = "at least these fields". Combining two open constraints:
            // "at least {a}" AND "at least {b}" = "at least {a,b}" (field union).
            if (Descendant is StateStruct descS && incoming is StateStruct incS
                && (descS.IsOpen || incS.IsOpen))
            {
                _descendant = UnionStructFields(descS, incS);
                TraceLog.WriteLine($"    Constrains.AddDescendant: Union({prev}, {type}) => {_descendant}");
            }
            else
            {
                _descendant = Descendant.Lca(incoming);
                TraceLog.WriteLine($"    Constrains.AddDescendant: LCA({prev}, {type}) => {_descendant}");
            }
        }
    }

    /// <summary>
    /// Union two struct field sets (row polymorphism).
    /// Creates a NEW struct with all fields from both. For shared fields, keep the existing node.
    /// Result is open only if BOTH are open; closed absorbs open.
    /// IMPORTANT: must NOT mutate inputs — struct states may be shared in the graph.
    /// </summary>
    private static StateStruct UnionStructFields(StateStruct existing, StateStruct incoming) {
        var fields = new Dictionary<string, TicNode>(StringComparer.OrdinalIgnoreCase);
        // All fields from existing
        foreach (var field in existing.Fields)
            fields[field.Key] = field.Value;
        // Add fields from incoming that aren't in existing
        foreach (var field in incoming.Fields)
            fields.TryAdd(field.Key, field.Value);
        return new StateStruct(fields, isFrozen: false, isOpen: existing.IsOpen && incoming.IsOpen) {
            IsOptionalSourced = StateStruct.MergedIsOptionalSourced(existing.IsOptionalSourced, incoming.IsOptionalSourced),
            TypeName = StateStruct.MergedTypeName(existing.TypeName, incoming.TypeName),
        };
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
            IsOptional = IsOptional || other.IsOptional,
        };
        result.AddDescendant(other.Descendant);
        if (!result.TryAddAncestor(other.Ancestor))
            return null;
        // Satisfiability check: if the merged interval [Desc..Anc] is empty (Desc wider
        // than Anc — e.g. intersecting [I16..I24] with [U32..U48] produces [I48..U16]),
        // no type satisfies BOTH constraints. The function's name promises null on
        // failure; without this check we'd return a "non-empty" CS whose interval is
        // mathematical garbage, forcing every caller to remember to call
        // IntervalIsNonEmpty separately. Found via bug-regression unit-test discovery
        // of finding #3 — the API name lied about the contract.
        if (!result.IntervalIsNonEmpty())
            return null;
        // Preserve Preferred through interval intersection.
        // Both sides may carry Preferred from integer constants (P=I32).
        // Take first non-null; if both exist and differ, keep the one that fits.
        result.Preferred = Preferred ?? other.Preferred;
        if (Preferred != null && other.Preferred != null && !Preferred.Equals(other.Preferred))
            result.Preferred = result.CanBeConvertedTo(Preferred) ? Preferred : other.Preferred;
        if (result.Preferred != null && !result.CanBeConvertedTo(result.Preferred))
            result.Preferred = null;
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
        // F-bound materialization. When the only constraint
        // present is StructBound (no Ancestor/Descendant/Preferred), the
        // covariant resolution is the bound ITSELF — return the structural
        // shape rather than collapsing to Any. Iso-recursive packing
        // fold[μX.S] (Pierce TAPL §20.2) wrapped in NFun's equirecursive
        // RefTo encoding: the bound's self-references already point back to
        // the owning CS, and after this materialization they semantically
        // point into the result struct.
        if (HasStructBound
            && Ancestor == null
            && !HasDescendant
            && Preferred == null
            && !IsComparable)
        {
            TraceLog.WriteLine($"  F-bound materialized: {StructBound.PrintState(0)}");
            return IsOptional ? WrapOptional(StructBound) : StructBound;
        }

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
            else if (Ancestor == null && Descendant is StatePrimitive { IsAbstract: true } abstractDesc)
                // Open interval [abstract..null]: resolve to narrowest concrete ancestor.
                // Abstract types (I48, I24, I96, U48, U24, U12) are TIC-internal and must be
                // concretized for output. Constrained intervals [abstract..abstract] are already
                // rejected by SimplifyOrNull during Pull/Push (e.g., bitwise U64 & I64).
                inner = abstractDesc.ConcreteAncestor;
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
        // F-bound is also the narrowest contravariant resolution when it's the sole constraint —
        // the bound IS the most specific shape that satisfies the F-bound predicate.
        if (HasStructBound
            && Ancestor == null
            && !HasDescendant
            && Preferred == null
            && !IsComparable)
        {
            TraceLog.WriteLine($"  F-bound materialized (contra): {StructBound.PrintState(0)}");
            return IsOptional ? WrapOptional(StructBound) : StructBound;
        }
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
        // Three-way non-emptiness check on (D, A, S). F-bound StructBound is a third independent
        // dimension; its presence imposes additional constraints on which D and A are compatible.
        // Structs cannot be Comparable; D=primitive is incompatible with S=struct; non-Any
        // primitive A rejects S.
        if (HasStructBound) {
            if (IsComparable) return null;                     // structs aren't Comparable
            if (HasAncestor && Ancestor != StatePrimitive.Any) // primitive upper bound rejects struct
                return null;
            if (HasDescendant) {
                // D must be a struct compatible with S, OR an Optional whose
                // element is, OR a CS whose StructBound is compatible.
                if (Descendant is StatePrimitive descPrim && descPrim != StatePrimitive.None)
                    return null;
                if (Descendant is StateStruct ds) {
                    // Width subtype: D's fields must be a SUPERSET of S's fields.
                    foreach (var (k, _) in StructBound.Fields)
                        if (ds.GetFieldOrNull(k) == null) return null;
                }
                // Other Descendant kinds (StateOptional, ConstraintsState) are accepted
                // conservatively here.
            }
        }

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

        if (!HasDescendant) {
            // IsOptional=true but no comparable descendant → None alone is not comparable → reject
            if (IsComparable && IsOptional)
                return null;
            return this; // IsOptional=true but no descendant yet — keep collecting constraints
        }

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
                IsOptional = IsOptional || constrainsState.IsOptional,
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

        if (IsComparable != constrainsState.IsComparable)
            return false;

        // Structural equality on StructBound with coinductive cycle guard
        // (Amadio–Cardelli equirecursive subtyping bisimulation, 1993; Pierce TAPL §21).
        // Reference equality is unsafe because Gcd-style merges produce
        // structurally-identical-but-reference-distinct bounds. We DON'T delegate to
        // StateStruct.Equals because that path lacks a cycle guard for anonymous bounds
        // (it relies on TypeName short-circuit, which lifted F-bounds may not have).
        return StructBoundsEqual(StructBound, constrainsState.StructBound);
    }

    /// <summary>
    /// Structural equality on F-bound StructBound, cycle-aware via
    /// coinductive in-progress guard. Two bounds are equal iff:
    /// 1. both null, or both non-null with same field-name set + arity;
    /// 2. for every shared field, the recursive value-state compares equal.
    /// Cycle guard: when (S₁,S₂) is on the in-progress stack we return true
    /// (assume equal — equirecursive bisimulation rule); the actual
    /// counter-example would surface elsewhere in the field walk before
    /// the cycle closes.
    /// </summary>
    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
    private static bool StructBoundsEqual(StateStruct a, StateStruct b) {
        if (ReferenceEquals(a, b)) return true; // fast path; covers both-null
        if (a == null || b == null) return false;
        var guard = _structBoundEqInProgress ??= new();
        return StructBoundsEqualInner(a, b, guard);
    }

    [ThreadStatic] private static List<(StateStruct, StateStruct)> _structBoundEqInProgress;

    private static bool StructBoundsEqualInner(StateStruct a, StateStruct b, List<(StateStruct, StateStruct)> guard) {
        // Coinductive break: assume equal on cycle close (Amadio–Cardelli).
        for (int i = 0; i < guard.Count; i++)
            if ((ReferenceEquals(guard[i].Item1, a) && ReferenceEquals(guard[i].Item2, b))
             || (ReferenceEquals(guard[i].Item1, b) && ReferenceEquals(guard[i].Item2, a)))
                return true;
        if (a.FieldsCount != b.FieldsCount) return false;
        // Field-name set check first (cheap rejection).
        foreach (var (k, _) in a.Fields)
            if (b.GetFieldOrNull(k) == null) return false;
        guard.Add((a, b));
        try {
            foreach (var (k, valA) in a.Fields) {
                var valB = b.GetFieldOrNull(k);
                var sA = valA.GetNonReference().State;
                var sB = valB.GetNonReference().State;
                if (sA is ConstraintsState csA && sB is ConstraintsState csB) {
                    // Both unsolved; compare by structural CS equality but
                    // RECURSE into StructBound coinductively if both have one
                    // (avoids re-entering full ConstraintsState.Equals which
                    // would lose the in-progress guard's identity).
                    if (csA.HasStructBound || csB.HasStructBound) {
                        if (!StructBoundsEqualInner(csA.StructBound, csB.StructBound, guard))
                            return false;
                    } else if (!csA.Equals(csB)) {
                        return false;
                    }
                } else if (sA is StateStruct ssA && sB is StateStruct ssB) {
                    // Nested struct field — recurse with the same guard.
                    if (!StructBoundsEqualInner(ssA, ssB, guard)) return false;
                } else if (!sA.Equals(sB)) {
                    return false;
                }
            }
            return true;
        } finally {
            guard.RemoveAt(guard.Count - 1);
        }
    }

    public override int GetHashCode() {
        // Structural-aligned hash. Field NAMES + arity, NOT field types — recursing into types
        // would cycle on F-bound self-refs. Equals is the source of truth for full equality;
        // hash only ensures bucket spread for HashSet/Dictionary.
        unchecked {
            int h = 17;
            h = h * 31 + (Ancestor?.Name.GetHashCode() ?? 0);
            // Use type-tag of Descendant (cheap, cycle-safe) — full structural
            // equality lives in Equals.
            h = h * 31 + (HasDescendant ? Descendant.GetType().GetHashCode() : 0);
            h = h * 31 + IsOptional.GetHashCode();
            h = h * 31 + IsComparable.GetHashCode();
            if (HasStructBound) {
                h = h * 31 + StructBound.FieldsCount;
                foreach (var f in StructBound.Fields)
                    h = h * 31 + StringComparer.OrdinalIgnoreCase.GetHashCode(f.Key);
            }
            return h;
        }
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
        if (HasStructBound)
            res += $"⊆μ"; // F-bound marker; full shape would self-recurse
        return res;
    }
}
