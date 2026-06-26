using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using NFun.Exceptions;
using NFun.Tic.SolvingStates;
using NFun.Tic.Stages;

namespace NFun.Tic;

using System.Text;

public enum TicNodeType {
    /// <summary>Input or output variable — Name = variable name.</summary>
    Named = 2,

    /// <summary>Syntax node — Name = order number.</summary>
    SyntaxNode = 4,

    /// <summary>Generic type from a signature, or one created during solving.</summary>
    TypeVariable = 8
}

public class TicNode {
    internal int VisitMark = -1;
    internal bool Registered = false;

    private ITicNodeState _state;

    public static TicNode CreateTypeVariableNode(ITypeState type)
        => new(type.ToString(), type, TicNodeType.TypeVariable);

    private static int _interlockedId = 0;
    private readonly int _uid = 0;

    /// <summary>DIAGNOSTIC: count node allocations. Used by WorklistPullDriver to detect
    /// fresh-allocation churn during convergence-failure probes.</summary>
    public static int DiagAllocCount => _interlockedId;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static TicNode CreateSyntaxNode(int id, ITicNodeState state, bool registered = false)
        => new(id, state, TicNodeType.SyntaxNode) { Registered = registered };

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static TicNode CreateNamedNode(object name, ITicNodeState state)
        => new(name, state, TicNodeType.Named) { Registered = true };


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static TicNode CreateTypeVariableNode(string name, ITicNodeState state, bool registered = false)
        => new(name, state, TicNodeType.TypeVariable) { Registered = registered };

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static TicNode CreateInvisibleNode(ITicNodeState state)
        => new("invisible", state, TicNodeType.TypeVariable) { Registered = false };

    private TicNode(object name, ITicNodeState state, TicNodeType type) {
        _uid = ++_interlockedId;
        Name = name;
        State = state;
        Type = type;
    }

    public TicNodeType Type { get; }


    #region Ancestors

    /// <summary>
    /// Deduplicate ancestor edges. Cycles in named recursive types can trigger O(N) duplicate
    /// AddAncestor calls onto the cycle root — without dedup, each Pull becomes O(N²).
    /// </summary>
    public void AddAncestor(TicNode node) {
        if(node == this)
            AssertChecks.Panic("Circular ancestor 0");
        for (int i = 0; i < _ancestors.Count; i++)
            if (_ancestors[i] == node) return;
        _ancestors.Add(node);
        // Worklist Pull hook (debt #10): edge addition re-fires Pull on `this`.
        // No-op when driver inactive (default). Skip when `this` is an element of an
        // Optional — re-Pull on freshly-allocated inners drives the tower-of-wraps
        // cycle (WorklistPull_ConvergenceAnalysis.md wave-2.6). Their constraint
        // propagation goes through the outer Optional's element via the State setter.
        if (!IsOptionalElement)
            WorklistPullDriver.Enqueue(this);
    }

    public void AddAncestors(IEnumerable<TicNode> nodes) {
#if DEBUG
        if(nodes.Any(n => n == this))
            AssertChecks.Panic("Circular ancestor 1");
#endif
        foreach (var n in nodes) AddAncestor(n);
    }

    public void RemoveAncestor(TicNode node) =>
        _ancestors.Remove(node);

    public void SetAncestor(int index, TicNode node) {
        if(node == this)
            AssertChecks.Panic("Circular ancestor 2");
        _ancestors[index] = node;
    }

    private readonly SmallList<TicNode> _ancestors = new();
    public SmallList<TicNode> Ancestors => _ancestors;

    #endregion


    public bool IsMemberOfAnything { get; set; }
    /// <summary>
    /// True iff this node is the element of a <see cref="StateOptional"/>. Gates
    /// PropagateOptionalUpward to prevent <c>opt(opt(T))</c>.
    /// </summary>
    internal bool IsOptionalElement { get; set; }

    /// <summary>
    /// True iff this node's composite shape is a function signature parameter.
    /// Set in SetCallArgument(composite) and <see cref="GraphBuilder.SetCall(StateFun, int[])"/>.
    /// Read by <c>WrapAncestorInOptional</c> to reject <c>Opt(T) ≤ T</c> — signature shape is rigid.
    /// </summary>
    internal bool IsSignatureParam { get; set; }

    /// <summary>
    /// Negative-skolem flag (Pottier-Rémy ATTAPL §10.7): rigid signature element that REJECTS
    /// the implicit lift <c>T ≤ opt(T)</c>. Set on the U-node in <c>SetCoalesce</c> and
    /// <c>SetForceUnwrap</c>, where the outer Optional shell sits at the input and U must never
    /// absorb an Optional layer. Distinct from <see cref="IsSignatureParam"/> (generic params
    /// like <c>wrap: T → opt(T)</c>'s T SHOULD receive IsOptional from None descendants).
    /// Gates <c>IntersectIntervalsOrNull</c>'s IsOptional OR-fusion.
    /// </summary>
    internal bool IsForcedNonOptional { get; set; }

    /// <summary>
    /// Head of a contractive μ-cycle (Cardelli–Mitchell '89 §3: every back-edge crosses a type
    /// constructor). Set by the SCC driver after the cycle has been verified contractive.
    /// Cycle-aware checks treat this node as a contractive boundary, equivalent to
    /// <c>cs.StructBound != null</c>.
    /// </summary>
    public bool IsContractiveCycleHead { get; set; }

    /// <summary>
    /// Memo of the inner element node when this node is wrapped from CS into Opt(innerCS).
    /// Populated ONLY when <see cref="Stages.WorklistPullDriver.IsActive"/> — streaming Pull
    /// allocates fresh per cell fire as before. Worklist reuses the cached inner to prevent
    /// fresh-allocation churn on re-entry (debt #10 wave-1).
    /// </summary>
    internal TicNode WrapOptionalInner { get; set; }

    /// <summary>
    /// Memo of the element node created when this node was transformed from CS into a single-arg
    /// composite (StateArray / single-arg StateCollection). Populated ONLY when worklist driver
    /// is active. Separate from <see cref="WrapOptionalInner"/> because Opt-wrap and shape-
    /// transform carry semantically different element constraints (debt #10 wave-2).
    /// </summary>
    internal TicNode TransformElementInner { get; set; }

    public bool IsSolved => _state.IsSolved;
    public bool IsMutable => _state.IsMutable;

    public ITicNodeState State
    {
        get => _state;
        set
        {
            Debug.Assert(value != null);
            // Allowed transitions out of a solved (IsMutable=false) state:
            //  1. idempotent (Equals).
            //  2. StateRefTo — graph rewire.
            //  3. StateOptional over composite — implicit lift T ≤ opt(T) (TicTypeSystem §Optional).
            //  4. anonymous StateStruct — row-poly merges + LiftMuTypes promotion to CS{StructBound}.
            //     WORKAROUND: clause (4) is wider than the three legitimate transitions; narrowing
            //     re-triggers BugC_LcaOfRecursiveVarsInArray. Tracked in TicTechnicalDebt.md.
            Debug.Assert(_state == null || IsMutable || value.Equals(_state)
                || value is StateRefTo
                || (value is StateOptional && _state is ICompositeState)
                || (_state is StateStruct ss && ss.TypeName == null),
                "Node is already solved");
            if (value is StateArray array)
                array.ElementNode.IsMemberOfAnything = true;
            else if (value is StateOptional optional)
            {
                optional.ElementNode.IsMemberOfAnything = true;
                optional.ElementNode.IsOptionalElement = true;
                // Flatten opt(opt(T)) → opt(T): nested Optional is a solver artifact.
                var innerNonRef = optional.ElementNode.GetNonReference();
                if (innerNonRef.State is StateOptional innerOpt)
                {
                    value = innerOpt;
                    innerOpt.ElementNode.IsMemberOfAnything = true;
                    innerOpt.ElementNode.IsOptionalElement = true;
                }
            }
            else if (value is StateRefTo refTo && refTo.Node == this)
            {
                TraceLog.WriteLine($"  Skip self-referencing node {Name}");
                return; // self-ref arises with recursive struct types
            }
            // Assigning an opt-sourced struct that closes a non-contractive cycle through `this`:
            // restore the Optional break by wrapping in StateOptional. Yields the principal
            // iso-recursive type μX. opt(struct{…X…}) instead of an invalid struct→struct loop.
            // IsOptionalSourced (set by SetSafeFieldAccess) distinguishes inferred `?.` recursion
            // from declared <c>type t = {self:t}</c> (the latter must error).
            if (value is StateStruct ns
                && SolvingFunctions.StructSubgraphIsOptSourced(ns)
                && SolvingFunctions.StructHasFieldReaching(ns, this))
            {
                var inner = CreateTypeVariableNode("e" + Name + "'", ns);
                inner.IsOptionalElement = true;
                value = new StateOptional(inner);
            }
            _state = value;
        }
    }

    public object Name { get; }

    public override string ToString() =>
        Name.Equals(_state.ToString())
            ? Name.ToString()
            : $"{Name}:{_state}";

    public void PrintToConsole() {
        if (!TraceLog.IsEnabled)
            return;

#if DEBUG
        var sb = new StringBuilder($"{Name}");
        var nameD = 3 - sb.Length;
        if (nameD < 0) nameD = 0;
        sb.Append(new string(' ', nameD));
        sb.Append($"| {State.Description}");
        if (Ancestors.Count > 0)
            sb.Append(" --> " + string.Join(",", Ancestors.Select(a => a.Name)));
        var delta = 30 - sb.Length;
        if (delta < 0)
            delta = 0;
        sb.Append(new string(' ', delta));
        sb.Append("| state: " + State.StateDescription);

        TraceLog.Write(sb.ToString());
        TraceLog.WriteLine();
#endif
    }

    public bool TryBecomeConcrete(StatePrimitive primitiveState) {
        if (_state is StatePrimitive oldConcrete)
            return oldConcrete.Equals(primitiveState);
        if (_state is ConstraintsState constrains)
        {
            if (constrains.CanBeConvertedTo(primitiveState))
            {
                _state = primitiveState;
                return true;
            }
        }
        if (_state is StateRefTo refTo)
            return refTo.Node.TryBecomeConcrete(primitiveState);

        return false;
    }

    public bool TrySetAncestor(StatePrimitive anc) {
        if (anc== StatePrimitive.Any)
            return true;
        var node = this;
        if (node.State is StateRefTo)
            node = node.GetNonReference();

        if (node.State is StatePrimitive oldConcrete)
        {
            return oldConcrete.CanBePessimisticConvertedTo(anc);
        }
        else if (node.State is ConstraintsState constrains)
        {
            if (!constrains.TryAddAncestor(anc))
                return false;
            constrains.Preferred = anc;
            var optimized = constrains.SimplifyOrNull();
            if (optimized == null)
                return false;
            State = optimized;
            return true;
        }

        return false;
    }

    public TicNode GetNonReference() {
        if (State is not StateRefTo refTo)
            return this;
        // Union-find path compression.
        var root = refTo.Node;
        while (root.State is StateRefTo nextRef)
            root = nextRef.Node;
        if (refTo.Node != root)
            State = new StateRefTo(root);
        return root;
    }

    public override int GetHashCode() => _uid;

    public void ClearAncestors() => _ancestors.Clear();

    /// <summary>
    /// Flatten <c>opt(opt(T)) → opt(T)</c>, bypassing the state-setter assertion (node may be solved).
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void FlattenNestedOptional() {
        if (_state is StateOptional outerOpt)
        {
            var innerNonRef = outerOpt.ElementNode.GetNonReference();
            if (innerNonRef.State is StateOptional innerOpt)
            {
                _state = innerOpt;
                innerOpt.ElementNode.IsMemberOfAnything = true;
                innerOpt.ElementNode.IsOptionalElement = true;
            }
        }
    }
}
