using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using NFun.Exceptions;
using NFun.Tic.SolvingStates;

namespace NFun.Tic;

using System.Text;

public enum TicNodeType {
    /// <summary>
    /// input or output variable of expression
    /// TicNode's name equals to variable name
    /// </summary>
    Named = 2,

    /// <summary>
    /// Syntax node. TicNode's name equals to node's order number
    /// </summary>
    SyntaxNode = 4,

    /// <summary>
    /// Generic type from function/constant signature or created in process of solving.
    /// </summary>
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
    /// Deduplicate ancestor edges. With true graph cycles in named recursive types, the same
    /// root node's field can receive O(N) duplicate AddAncestor calls from N literal-struct
    /// call sites (all sharing the cycle root); without deduplication, _ancestors grows as
    /// O(N), each Pull iterates all entries → O(N²) Pull cost on AccessChain(N).
    /// PERF: ~8% on Simple|Build for non-recursive code — pays for safety on cyclic graphs.
    /// Targeted dedup at specific call sites was attempted (commit history) but proved too
    /// risky given the number of sites that can introduce duplicates during recursive
    /// resolution (Pull/Push/SetCall/MergeRefs paths). Reverted to global dedup for
    /// correctness; further perf wins should come from reducing M2-B unconditional overhead
    /// (IsContractiveCycleHead allocations, FreezeFunctionSignatureStructs, etc.).
    /// </summary>
    public void AddAncestor(TicNode node) {
        if(node == this)
            AssertChecks.Panic("Circular ancestor 0");
        for (int i = 0; i < _ancestors.Count; i++)
            if (_ancestors[i] == node) return;
        _ancestors.Add(node);
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
    /// True if this node is the element of a StateOptional composite.
    /// Used by PropagateOptionalUpward to avoid double-wrapping: if a node is already
    /// the element of opt(T), wrapping it again would create opt(opt(T)).
    /// </summary>
    internal bool IsOptionalElement { get; set; }
    /// <summary>
    /// True if this node's composite shape comes from a function signature parameter.
    /// Prevents Optional wrapping: Opt(T) ≤ T is invalid for signature-given types.
    /// </summary>
    internal bool IsSignatureParam { get; set; }

    /// <summary>
    /// Witness flag certifying that this node is the head of a contractive μ-cycle
    /// (Cardelli–Mitchell '89 §3 contractivity: every back-edge crosses a type constructor).
    /// Set by the SCC driver after a cyclic SCC has been verified contractive. Downstream cycle
    /// checks (ThrowIfRecursiveTypeDefinition, runtime Fit, coinductive Equals) treat this node
    /// as a contractive boundary — equivalent to <c>cs.StructBound != null</c> for
    /// short-circuiting purposes. Per Pottier–Rémy '92, μ-types are properties of the constraint
    /// graph, not first-class AST objects.
    /// </summary>
    public bool IsContractiveCycleHead { get; set; }

    public bool IsSolved => _state.IsSolved;
    public bool IsMutable => _state.IsMutable;

    public ITicNodeState State
    {
        get => _state;
        set
        {
            Debug.Assert(value != null);
            // Allowed mutations of a "solved" state (IsMutable=false):
            //  1. value.Equals(_state) — no-op (idempotent)
            //  2. value is StateRefTo — redirection (graph rewiring)
            //  3. value is StateOptional and _state is composite — the implicit-lift wrap
            //     T ≤ opt(T). The wrap creates innerNode holding _state by reference, so
            //     all structural identity (including TypeName, IsOptionalSourced) survives;
            //     only an Optional layer is added on the outer node. Universal algebraic
            //     postulate per TicTypeSystem §Optional. Previously this allowed
            //     `_state is StateOptional` only (Opt(Opt(T))→Opt(T) flatten path); GH #126
            //     followup adds the rest of ICompositeState (StateStruct, StateArray,
            //     StateFun) so F-bound recursive function returns can be widened.
            Debug.Assert(_state == null || IsMutable || value.Equals(_state)
                || value is StateRefTo
                || (value is StateOptional && _state is ICompositeState),
                "Node is already solved");
            if (value is StateArray array)
                array.ElementNode.IsMemberOfAnything = true;
            else if (value is StateOptional optional)
            {
                optional.ElementNode.IsMemberOfAnything = true;
                optional.ElementNode.IsOptionalElement = true;
                // Flatten nested optionals at assignment time: opt(opt(T)) → opt(T)
                // NFun doesn't support nested optionals, so any nesting is a solver artifact
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
                return; // Skip self-referencing (occurs with recursive struct types)
            }
            // If assigning an opt-sourced struct state would close a non-contractive cycle (one
            // of the struct's fields reaches this very node via composite traversal without an
            // Optional/Array break), restore the Optional break by wrapping the new state in
            // StateOptional. Yields the principal iso-recursive type μX. opt(struct{…X…})
            // instead of an invalid struct→struct loop. The IsOptionalSourced gate (set by
            // SetSafeFieldAccess and preserved through merges) distinguishes inferred recursion
            // through `?.` from a declared `type t = {self:t}` which must error.
            if (value is StateStruct ns
                && SolvingFunctions.StructHasFieldReaching(ns, this)
                && SolvingFunctions.StructSubgraphIsOptSourced(ns))
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
        // Path compression: find root, then flatten chain
        var root = refTo.Node;
        while (root.State is StateRefTo nextRef)
            root = nextRef.Node;
        // Compress: point directly to root (skip intermediate nodes)
        if (refTo.Node != root)
            State = new StateRefTo(root);
        return root;
    }

    public override int GetHashCode() => _uid;

    public void ClearAncestors() => _ancestors.Clear();

    /// <summary>
    /// If this node has nested optional state opt(opt(T)), flatten to opt(T).
    /// Bypasses the normal state setter assertion since the node may already be solved.
    /// NFun doesn't support nested optionals — any nesting is a solver artifact.
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
