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

    /// <summary>Clears Descendant when its obligation is subsumed by StructBound (`T &lt;: S`);
    /// leaving it set would over-constrain `T = S` and defeat F-bounded polymorphism.</summary>
    public void ClearDescendant() {
        _descendant = null;
    }

    public bool HasAncestor => Ancestor != null;
    public bool HasDescendant => _descendant != null;

    /// <summary>opt(inner) marker — set when None is added as descendant. StateOptional is
    /// materialized before Destruction, after constraint collection (avoids stale snapshots).</summary>
    public bool IsOptional { get; private set; }

    /// <summary>F-bounded recursive upper bound `T &lt;: τ(T)` (Pierce TAPL §20.2). Body is a
    /// StateStruct whose fields may RefTo this owning CS — contractivity + covariant positions
    /// required. Owned by exactly one CS; never aliased.</summary>
    public StateStruct StructBound { get; set; }

    public bool HasStructBound => StructBound != null;

    public bool IsSolved => false;
    public bool IsMutable => true;
    public StatePrimitive Preferred { get; set; }
    public bool IsComparable { get; set; }
    /// <summary>Clearable typeclass marker — CS must resolve to a Clearable kind (List/Set/Map).</summary>
    public bool IsClearable { get; set; }
    public bool NoConstrains => !HasDescendant && !HasAncestor && !IsComparable && !IsClearable && !IsOptional && !HasStructBound;

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
            IsClearable = IsClearable,
            // StructBound: reference copy is safe — only merges that could mutate either side
            // need deep copy, and lift→read paths don't mutate.
            StructBound = StructBound,
        };

    public bool CanBeConvertedTo(ITypeState type) {
        if (HasAncestor && !type.CanBePessimisticConvertedTo(Ancestor))
            return false;

        // F-bound subtyping with cycle-aware self-ref handling — see FitStructBound.
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

    /// <summary>Structural F-bound subtyping with coinductive cycle break
    /// (Amadio–Cardelli '93): T ⊆ Struct{S} iff T width-supersets S and shares
    /// covariantly. In-progress (T,S) pair on the stack ⇒ assume true.</summary>
    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
    private static bool FitStructBound(ITypeState candidate, StateStruct bound) {
        var guard = _fitStructBoundInProgress ??= new();
        return FitStructBoundInner(candidate, bound, guard);
    }

    [ThreadStatic] private static List<(ITypeState, StateStruct)> _fitStructBoundInProgress;

    private static bool FitStructBoundInner(ITypeState candidate, StateStruct bound, List<(ITypeState, StateStruct)> guard) {
        for (int i = 0; i < guard.Count; i++)
            if (ReferenceEquals(guard[i].Item1, candidate) && ReferenceEquals(guard[i].Item2, bound))
                return true;

        StateStruct candStruct = candidate switch {
            StateStruct s => s,
            _ => null,
        };
        if (candStruct == null) return false;

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
                    if (candFieldState is ITypeState candTypeState)
                    {
                        if (!candTypeState.CanBePessimisticConvertedTo(boundTypeState as StatePrimitive ?? StatePrimitive.Any)
                            && !boundTypeState.Equals(candTypeState))
                        {
                            if (boundTypeState is ICompositeState && candTypeState is ICompositeState)
                            {
                                if (boundTypeState.GetType() != candTypeState.GetType())
                                    return false;
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

        // None → IsOptional flag instead of Descendant: avoids stale-snapshot Optional wrapping.
        if (type == StatePrimitive.None) {
            IsOptional = true;
            TraceLog.WriteLine($"    Constrains.AddDescendant: None => IsOptional=true");
            return;
        }

        if (!HasDescendant)
        {
            // Live StateFun with unresolved non-signature nodes: preserve identity instead of
            // Concretest snapshot. Concretest fabricates fresh arg/ret nodes (arg→Abstractest,
            // ret→Concretest) which severs identity with the lambda's binder/body — later Push
            // of the LCA result over-widens the binder to Any.
            if (type is StateFun fn && IsLiveSnapshotableFun(fn))
            {
                _descendant = type;
                TraceLog.WriteLine($"    Constrains.AddDescendant: first={type} => preserve identity (live StateFun)");
            }
            else
            {
                _descendant = type.Concretest();
                TraceLog.WriteLine($"    Constrains.AddDescendant: first={type} => concretest={_descendant}");
            }
        }
        else
        {
            // Reference equality (not structural Equals): StateStruct.Equals short-circuits on
            // TypeName, so `n{v=1}` ≡ `n{v=2}` would skip needed LCA propagation.
            if (ReferenceEquals(Descendant, type)) return;
            var prev = Descendant;
            var incoming = type is ConstraintsState ? type.Concretest() : type;
            // Row polymorphism: open structs combine by field UNION ("at least {a}" ∧
            // "at least {b}" = "at least {a,b}"); closed structs use intersection (standard LCA).
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
    /// A StateFun is "live-snapshotable" when preserving its identity (instead of taking
    /// a Concretest copy with fresh nodes) is safe at the AddDescendant snapshot site:
    /// at least one component node is still unresolved (CS-typed), AND no component is
    /// a signature-param (snapshotting a signature-rigid node would let downstream
    /// constraints mutate the user function's contract).
    /// </summary>
    private static bool IsLiveSnapshotableFun(StateFun fn) {
        if (fn.RetNode.IsSignatureParam) return false;
        bool anyUnresolved = fn.RetNode.State is ConstraintsState;
        foreach (var arg in fn.ArgNodes)
        {
            if (arg.IsSignatureParam) return false;
            if (arg.State is ConstraintsState) anyUnresolved = true;
        }
        return anyUnresolved;
    }

    /// <summary>Row-polymorphism union of two struct field sets — returns NEW struct (input
    /// states may be shared across the graph; mutation would corrupt). Open only iff both open.</summary>
    private static StateStruct UnionStructFields(StateStruct existing, StateStruct incoming) {
        var fields = new Dictionary<string, TicNode>(StringComparer.OrdinalIgnoreCase);
        foreach (var field in existing.Fields)
            fields[field.Key] = field.Value;
        foreach (var field in incoming.Fields)
            fields.TryAdd(field.Key, field.Value);
        return new StateStruct(fields, isFrozen: false, isOpen: existing.IsOpen && incoming.IsOpen) {
            IsOptionalSourced = StateStruct.MergedIsOptionalSourced(existing.IsOptionalSourced, incoming.IsOptionalSourced),
            TypeName = StateStruct.MergedTypeName(existing.TypeName, incoming.TypeName),
        };
    }

    /// <summary>Intersect two constraint intervals — see specs_tic/Algebra/ConstraintsState.md.
    /// Null when ancestors are GCD-incompatible. Does NOT merge Preferred or collapse composites.</summary>
    public ConstraintsState IntersectIntervalsOrNull(ConstraintsState other,
        TicNode resultOwnerNode = null) {
        // Negative-skolem owners (IsForcedNonOptional, set at `??`/`!`'s U-node) reject the
        // implicit lift T ≤ opt(T): suppress IsOptional OR-fusion, else result becomes opt(opt(T))
        // (forbidden, INV-1). IsOptionalElement is too broad — fires on legitimate `wrap: T→opt(T)`.
        bool suppressOptionalOr = resultOwnerNode != null
            && resultOwnerNode.IsForcedNonOptional;
        // Mirror the IsOptional suppression on Descendant: peel one StateOptional layer
        // (negative-skolem absorbs opt(X) as X), otherwise the wrapped descendant re-introduces
        // nesting through MergeOrNull below.
        var selfDesc = Descendant;
        var otherDesc = other.Descendant;
        if (suppressOptionalOr) {
            if (selfDesc is StateOptional selfOpt) selfDesc = selfOpt.Element;
            if (otherDesc is StateOptional otherOpt) otherDesc = otherOpt.Element;
        }
        var result = new ConstraintsState(selfDesc, Ancestor, IsComparable || other.IsComparable) {
            IsOptional = !suppressOptionalOr && (IsOptional || other.IsOptional),
            IsClearable = IsClearable || other.IsClearable,
        };
        result.AddDescendant(otherDesc);
        if (!result.TryAddAncestor(other.Ancestor))
            return null;
        // Empty merged interval (Desc > Anc) → null, per function-name contract.
        if (!result.IntervalIsNonEmpty())
            return null;
        // Preferred survives intersection: take first non-null; on conflict keep the one that fits.
        result.Preferred = Preferred ?? other.Preferred;
        if (Preferred != null && other.Preferred != null && !Preferred.Equals(other.Preferred))
            result.Preferred = result.CanBeConvertedTo(Preferred) ? Preferred : other.Preferred;
        if (result.Preferred != null && !result.CanBeConvertedTo(result.Preferred))
            result.Preferred = null;
        return result;
    }

    /// <summary>True iff [Descendant..Ancestor] admits at least one type.</summary>
    public bool IntervalIsNonEmpty() {
        if (!HasAncestor || !HasDescendant)
            return true;
        if (Ancestor.Equals(Descendant))
            return !IsComparable || Ancestor.IsComparable;
        if (Descendant is ITypeState typeState)
            return typeState.CanBePessimisticConvertedTo(Ancestor);
        return true;
    }

    public ITicNodeState MergeOrNull(ConstraintsState constraintsState, TicNode resultOwnerNode = null) {
        var result = IntersectIntervalsOrNull(constraintsState, resultOwnerNode);
        if (result == null)
            return null;

        // SimplifyOrNull catches trans-axis typeclass × composite/primitive conflicts that the
        // [D..A] interval check misses (else MergeInplace silently accepts inconsistent CS).
        // Gated on flag+target presence so the common no-typeclass path stays cheap.
        if ((result.IsComparable || result.IsClearable || result.HasStructBound)
            && (result.HasDescendant || result.HasAncestor)) {
            var simplified = result.SimplifyOrNull();
            if (simplified == null) return null;
            if (simplified is not ConstraintsState csResult) return simplified;
            result = csResult;
        }

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

        if (!result.HasAncestor && !result.IsComparable &&
            result.Descendant is StateStruct { IsSolved: true } or StateOptional { IsSolved: true })
        {
            if (result.Descendant is StateOptional && result.Preferred != null)
            {
                // Don't collapse: let Preferred survive to runtime resolution.
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
        // Sole-constraint StructBound: bound IS the narrowest shape satisfying it.
        if (HasStructBound
            && Ancestor == null
            && !HasDescendant
            && Preferred == null
            && !IsComparable)
        {
            TraceLog.WriteLine($"  F-bound materialized (contra): {StructBound.PrintState(0)}");
            return IsOptional ? WrapOptional(StructBound) : StructBound;
        }
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
        // F-bound × (D,A) compatibility: structs aren't Comparable, primitive D is incompatible
        // with struct S, non-Any primitive A rejects S.
        if (HasStructBound) {
            if (IsComparable) return null;
            if (HasAncestor && Ancestor != StatePrimitive.Any) return null;
            if (HasDescendant) {
                if (Descendant is StatePrimitive descPrim && descPrim != StatePrimitive.None)
                    return null;
                if (Descendant is StateStruct ds) {
                    foreach (var (k, _) in StructBound.Fields)
                        if (ds.GetFieldOrNull(k) == null) return null;
                }
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
        // Clearable rejects primitive descendants only. Composite narrowing belongs to Push
        // apply cells — doing it here previously produced an Optional cycle (see TicTechnicalDebt).
        if (IsClearable && Descendant is StatePrimitive csp && csp != StatePrimitive.None)
            return null;

        if (!HasDescendant) {
            // None alone isn't Comparable.
            if (IsComparable && IsOptional)
                return null;
            // opt(T) is a composite — cannot satisfy a non-Any primitive Ancestor. No future
            // Pull can rescue (Gcd on non-Any primitive can't yield Any).
            if (IsOptional && Ancestor is StatePrimitive pa && pa != StatePrimitive.Any)
                return null;
            return this; // collecting constraints
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
                // Abstract types have no runtime representation — interval cannot collapse there.
                if (Ancestor.Name.HasFlag(PrimitiveTypeName._isAbstract))
                    return null;
                return IsOptional ? StateOptional.Of(Ancestor) : (ITicNodeState)Ancestor;
            }
            if (!(d is ITypeState descendant))
                return this;
            if (!descendant.CanBePessimisticConvertedTo(Ancestor))
                return null;
            // opt(T) composite cannot satisfy a non-Any primitive Ancestor.
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

        // StructBound structural equality with cycle guard (Amadio–Cardelli '93). Reference
        // equality fails on Gcd-produced bounds; StateStruct.Equals lacks a guard for anonymous
        // bounds (it relies on TypeName short-circuit).
        return StructBoundsEqual(StructBound, constrainsState.StructBound);
    }

    /// <summary>Structural F-bound equality with coinductive (S₁,S₂)-pair cycle guard
    /// (Amadio–Cardelli '93). Counter-examples surface during the field walk before cycle close.</summary>
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
                    // Recurse into StructBound directly to preserve the in-progress guard
                    // (ConstraintsState.Equals would create a fresh one).
                    if (csA.HasStructBound || csB.HasStructBound) {
                        if (!StructBoundsEqualInner(csA.StructBound, csB.StructBound, guard))
                            return false;
                    } else if (!csA.Equals(csB)) {
                        return false;
                    }
                } else if (sA is StateStruct ssA && sB is StateStruct ssB) {
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
        // Field names + arity only — recursing into field types would cycle on F-bound self-refs.
        unchecked {
            int h = 17;
            h = h * 31 + (Ancestor?.Name.GetHashCode() ?? 0);
            // Type-tag only on Descendant — cycle-safe.
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
        if (IsClearable)
            res += "@clr";
        if (Preferred != null)
            res += Preferred + "!";
        if (HasStructBound)
            res += $"⊆μ"; // F-bound; full shape would self-recurse
        return res;
    }
}
