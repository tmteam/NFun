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

    /// <summary>
    /// Like <see cref="NoConstrains"/>, but an Any upper bound counts as vacuous:
    /// Any is the lattice top, so Sat([∅..Any]) = Sat([∅..∅]) — the bound excludes
    /// nothing. Same treatment of a top bound as Theorem PT-F ("a non-Any primitive A
    /// rejects S", <see cref="StructBoundIsSatisfiable"/>). Such CS arise canonically
    /// in contravariant fun-arg positions (↑[∅..∅] = Any projected by ↓Fun, then
    /// re-minted as CS by TransformToFunOrNull). Used by the composite transforms,
    /// where only the EFFECTIVE constraints matter.
    /// </summary>
    public bool NoEffectiveConstrains =>
        !HasDescendant && !IsComparable && !IsOptional && !HasStructBound
        && (!HasAncestor || Ancestor == StatePrimitive.Any);

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

    /// <summary>
    /// `type satisfies this` — delegates to THE authoritative satisfaction predicate
    /// (debt #16): the CS-target cell of <see cref="StateExtensions.FitsInto(ITicNodeState, ConstraintsState)"/>
    /// (structural Fit core + Optional-lift arm + F-bound + unsolved-target ancestor cell).
    /// The old body was a shallow duplicate (composite check by constructor only, elements
    /// ignored; no Optional lift) — the merge fast path (SolvingFunctions.GetMergedStateOrNull)
    /// now gets the deep structural check through this delegation.
    /// </summary>
    public bool CanBeConvertedTo(ITypeState type) => ((ITicNodeState)type).FitsInto(this);

    // Coinductive-context bridge (see AlgebraCycleContext): keeps the assumption set of the
    // algebra family alive across the predicate hop.
    internal bool CanBeConvertedTo(ITypeState type, AlgebraCycleContext ctx) =>
        ((ITicNodeState)type).FitsInto(this, ctx);

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
    internal static bool FitStructBound(ITypeState candidate, StateStruct bound, AlgebraCycleContext ctx = null) {
        var guard = _fitStructBoundInProgress ??= new();
        return FitStructBoundInner(candidate, bound, guard, ctx);
    }

    [ThreadStatic] private static List<(ITypeState, StateStruct)> _fitStructBoundInProgress;

    private static bool FitStructBoundInner(ITypeState candidate, StateStruct bound,
        List<(ITypeState, StateStruct)> guard, AlgebraCycleContext ctx) {
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
                        if (!boundCs.CanBeConvertedTo(candTypeState, ctx)) return false;
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
                    // Bound field is concrete — candidate field must fit it covariantly,
                    // recursing through composites (Algebra_Fit §2 п.4). Re-entry into an
                    // F-bound self-ref goes through the S axis of FitsInto and lands back
                    // in FitStructBound, where the in-progress guard breaks the cycle
                    // coinductively.
                    if (candFieldState is ITypeState candTypeState)
                    {
                        if (!((ITicNodeState)candTypeState).FitsInto(boundTypeState, ctx))
                            return false;
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

    public void AddDescendant(ITicNodeState type) => AddDescendant(type, null);

    // Coinductive-context bridge (see AlgebraCycleContext): the Descendant join below
    // re-enters Lca, so a Merge running inside a struct-Lca arm must keep its assumption set.
    internal void AddDescendant(ITicNodeState type, AlgebraCycleContext ctx) {
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
            // Snapshot (NOT pure ↓): the stored descendant feeds Destruction/Finalize,
            // so resolution metadata (Preferred) must survive — debt #19.
            _descendant = type.ConcretestSnapshot();
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
            var incoming = type is ConstraintsState ? type.ConcretestSnapshot() : type;
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
                _descendant = Descendant.Lca(incoming, ctx);
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
    /// Interval core + axis transport of the ⊓ operator (Algebra_Merge.md):
    ///   desc = LCA(this.Desc, other.Desc)
    ///   anc  = GCD(this.Anc,  other.Anc)
    ///   comp = this.IsComparable || other.IsComparable
    ///   opt  = this.IsOptional || other.IsOptional
    ///   S    = S₁ ∪ S₂ (GcdBound field union — meet on the F-bound lattice, Theorem PT-F)
    ///   pref = commutative hint rule (equal → keep, one-sided → keep, differ → hint-LCA;
    ///          post-condition: the hint must fit the merged constraint, else dropped)
    /// Returns null if ancestors are incompatible (GCD fails), the merged interval is empty,
    /// or the merged S is incompatible with the interval (three-way (D, A, S) non-emptiness).
    /// Does NOT collapse composites (canonicalization lives in MergeOrNull).
    /// </summary>
    public ConstraintsState IntersectIntervalsOrNull(ConstraintsState other) =>
        IntersectIntervalsOrNull(other, null);

    internal ConstraintsState IntersectIntervalsOrNull(ConstraintsState other, AlgebraCycleContext ctx) {
        var result = new ConstraintsState(Descendant, Ancestor, IsComparable || other.IsComparable) {
            IsOptional = IsOptional || other.IsOptional,
        };
        result.AddDescendant(other.Descendant, ctx);
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
        // S axis (debt #12, Algebra_Merge.md §Оси): S = S₁ ∪ S₂ — field union, the meet on
        // the F-bound lattice (more required fields = stronger predicate, Theorem PT-F).
        // The union runs GcdBound in OWNERLESS mode: self-referential positions keep their
        // original node identity instead of being rewired — ownership transfer belongs to
        // the callers, which alias merged owner nodes (loser := RefTo(winner)), making old
        // back-edges transparent references to the merged variable.
        if (HasStructBound || other.HasStructBound)
        {
            var s = !HasStructBound ? other.StructBound
                : !other.HasStructBound ? StructBound
                : SolvingFunctions.GcdBound(StructBound, other.StructBound, null, null, ctx);
            if (s == null)
                return null; // bound conflict — Sat empty
            result.StructBound = s;
            // Three-way (D, A, S) non-emptiness (Theorem PT-F): a struct bound coexists
            // only with a struct-compatible D and an absent-or-Any A.
            if (!result.StructBoundIsSatisfiable())
                return null;
        }
        // Preferred is metadata (a resolution hint, Sat-neutral) — transported by the single
        // commutative rule (debt #14; Algebra_Merge.md §Preferred). Post-condition: the hint
        // must fit the merged constraint, otherwise it is dropped.
        result.Preferred = Algebra.StateExtensions.PreferredHintLcaOrNull(Preferred, other.Preferred);
        if (result.Preferred != null && !result.CanBeConvertedTo(result.Preferred, ctx))
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

    /// <summary>
    /// The single ⊓ operator on constraints (Merge, Algebra_Merge.md):
    /// interval core + axis transport (both in <see cref="IntersectIntervalsOrNull"/>),
    /// then the canonicalizing collapse rules (point collapse, solved-composite collapse).
    /// `Unify(CS, CS)` is DEFINED as this operator (Algebra_Unify.md).
    /// NOTE: <see cref="SimplifyOrNull"/> is deliberately NOT part of ⊓ — it is stage-level
    /// representation canonicalization that also does Sat-changing work (comparable
    /// narrowing, whole-value opt-vs-primitive-ancestor rejection), while the Merge
    /// collapse rules are the Sat-neutral subset (Algebra_Merge.md §Связь с Simplify).
    /// </summary>
    public ITicNodeState MergeOrNull(ConstraintsState constraintsState) =>
        MergeOrNull(constraintsState, null);

    internal ITicNodeState MergeOrNull(ConstraintsState constraintsState, AlgebraCycleContext ctx) {
        var result = IntersectIntervalsOrNull(constraintsState, ctx);
        if (result == null)
            return null;

        // Comparable canonicalization (Sat-neutral, mirrors SimplifyOrNull's cmp block):
        // a comparable satisfier above descendant D exists ⟺ D is in the comparable
        // domain (IsComparableDomain, debt #31): numeric (then bounded above by Real),
        // Char, arr(≤Char), or unsolved (passed through conservatively).
        if (result.IsComparable && result.HasDescendant)
        {
            if (result.Descendant is StatePrimitive { IsNumeric: true })
            {
                // TryAddAncestor failure = existing ancestor incompatible with Real
                // while the descendant is numeric — empty interval.
                if (!result.TryAddAncestor(StatePrimitive.Real))
                    return null;
            }
            else if (!result.Descendant.IsComparableDomain())
                return null; // no comparable type above this descendant
        }

        if (result.HasAncestor && result.HasDescendant)
        {
            // Point collapse [T..T] → T / Opt(T). The cmp-at-point contradiction
            // ([T..T, cmp] with T ∉ Comparable) is already rejected by
            // IntervalIsNonEmpty inside IntersectIntervalsOrNull.
            // WrapOptional enforces the canonical-form quotient opt(Any) = Any.
            if (result.Ancestor.Equals(result.Descendant))
            {
                if (result.IsOptional)
                    return WrapOptional(result.Ancestor);
                return result.Ancestor;
            }

            // Deviation M4 (Algebra_Merge.md, kept): receiver's OWN descendant re-checked
            // against the merged ancestor. Belt-and-suspenders for merged descendants that
            // are not ITypeState (IntervalIsNonEmpty passes them through); asymmetric — the
            // argument's descendant is not re-checked by this path.
            if (Descendant != null && !Descendant.CanBePessimisticConvertedTo(result.Ancestor))
                return null;
        }

        // Collapse of a solved composite: [D..∅] → D / Opt(D) for solved Struct/Optional D.
        // Preferred-survival exception: D = Optional with a hint must stay in flag form —
        // the hint has to live until resolution (TicPreferred.md), collapse would destroy it.
        // S-discharge exception: with a struct bound present, collapse pins the node to D,
        // which is legal only if D itself satisfies the bound (FitStructBound); otherwise
        // the constraint form is kept — skipping canonicalization is always Sat-neutral.
        // (A POINT interval [T..T] never coexists with S: T is a primitive and the
        // three-way check in IntersectIntervalsOrNull already rejected it.)
        if (!result.HasAncestor && !result.IsComparable &&
            result.Descendant is StateStruct { IsSolved: true } or StateOptional { IsSolved: true })
        {
            if (result.Descendant is StateOptional && result.Preferred != null)
            {
                // Don't collapse — let preferred type survive to runtime resolution
            }
            else if (result.HasStructBound
                     && !FitStructBound((ITypeState)result.Descendant, result.StructBound, ctx))
            {
                // Don't collapse — the bound is not discharged by D; keep [D..∅, S]
            }
            else if (result.IsOptional)
                // Canonical form Opt(Opt(T)) = Opt(T) (Algebra_Merge.md §Коллапс-правила,
                // FlattenNestedOptional): an already-Optional descendant absorbs the flag.
                return result.Descendant is StateOptional
                    ? result.Descendant
                    : StateOptional.Of(result.Descendant);
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
            // Resolution requires a comparable POINT: a solved member of the domain
            // (IsComparableDomain, debt #31). Unsolved shapes stay unresolved.
            var isDescComparable = Descendant is ITypeState { IsSolved: true }
                                   && Descendant.IsComparableDomain();
            if (!isDescComparable)
                return this; // unresolved
            inner = Descendant;
        } else
            inner = Descendant;
        return IsOptional ? WrapOptional(inner) : inner;
    }

    private static ITicNodeState WrapOptional(ITicNodeState inner) =>
        inner == StatePrimitive.Any ? StatePrimitive.Any : StateOptional.Of(inner);

    /// <summary>
    /// Three-way non-emptiness check on (D, A, S) — Theorem PT-F (Algebra.md). F-bound
    /// StructBound is a third independent dimension; its presence imposes additional
    /// constraints on which D and A are compatible: structs cannot be Comparable;
    /// D=primitive is incompatible with S=struct; a non-Any primitive A rejects S; a struct D
    /// must carry (at least by name — width) every field the bound requires. Unsolved and
    /// Optional descendants are accepted conservatively. True when no bound is present.
    /// Shared by <see cref="SimplifyOrNull"/> (stage canonicalization) and
    /// <see cref="IntersectIntervalsOrNull"/> (⊓ core).
    /// </summary>
    internal bool StructBoundIsSatisfiable() {
        if (!HasStructBound) return true;
        if (IsComparable) return false;                     // structs aren't Comparable
        if (HasAncestor && Ancestor != StatePrimitive.Any)  // primitive upper bound rejects struct
            return false;
        if (HasDescendant) {
            // D must be a struct compatible with S, OR an Optional whose
            // element is, OR a CS whose StructBound is compatible.
            if (Descendant is StatePrimitive descPrim && descPrim != StatePrimitive.None)
                return false;
            if (Descendant is StateStruct ds) {
                // Width subtype: D's fields must be a SUPERSET of S's fields.
                foreach (var (k, _) in StructBound.Fields)
                    if (ds.GetFieldOrNull(k) == null) return false;
            }
            // Other Descendant kinds (StateOptional, ConstraintsState) are accepted
            // conservatively here.
        }
        return true;
    }

    public ITicNodeState SimplifyOrNull() {
        if (!StructBoundIsSatisfiable())
            return null;

        if (!HasDescendant && !IsOptional) return this;

        if (IsComparable)
        {
            switch (Descendant)
            {
                case StateArray a:
                {
                    // Narrowing canonicalization: the only comparable composite is arr(Char) —
                    // domain membership per the single rule (IsComparableDomain, debt #31).
                    if (a.IsComparableDomain())
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
            // Symmetric clause of the line below (HasDescendant branch): opt(T) is a composite
            // and cannot satisfy a non-Any primitive Ancestor. Holds independently of whether
            // a Descendant has been added yet — no future Pull can rescue it (Pull only narrows
            // Ancestor via Gcd, and Gcd on a non-Any primitive cannot yield Any). Without this
            // clause, `none.toHexText()` (Ancestor=I64, Desc=null, IsOptional=true) resolves
            // to opt(int) and the runtime impl throws NFunImpossibleException. (MR2Bug3.)
            if (IsOptional && Ancestor is StatePrimitive pa && pa != StatePrimitive.Any)
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
                // WrapOptional enforces the canonical-form quotient opt(Any) = Any.
                return IsOptional ? WrapOptional(Ancestor) : Ancestor;
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
            // Canonical-form quotient opt(Any) = Any: [Any.., opt] collapses to Any, never opt(Any).
            return StatePrimitive.Any;

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
