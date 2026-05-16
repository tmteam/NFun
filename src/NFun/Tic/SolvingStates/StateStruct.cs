namespace NFun.Tic.SolvingStates;

using System;
using System.Collections.Generic;
using System.Linq;
using Algebra;

public class StateStruct : ICompositeState {

    public int FieldsCount => _nodes.Count;
    public IEnumerable<KeyValuePair<string, TicNode>> Fields => _nodes;

    private readonly FieldMap _nodes;

    public TicNode GetFieldOrNull(string fieldName) =>
        _nodes.GetValueOrNull(fieldName);

    /// <summary>
    /// Adds a field in-place.
    /// </summary>
    public void AddField(string name, TicNode memberNode) {
        _nodes.Add(name, memberNode);
    }

    /// <summary>
    /// Replace an existing field's TicNode in-place. Used by SetStructInitType to insert
    /// Optional-wrapper intermediaries between a struct literal and its declared-Optional
    /// ancestor field. Caller's responsibility to ensure the field exists.
    /// </summary>
    internal void ReplaceField(string name, TicNode memberNode) {
        _nodes[name] = memberNode;
    }

    public static StateStruct Empty(bool isFrozen = true) => Of(isFrozen);
    public static StateStruct Of(IEnumerable<KeyValuePair<string, ITicNodeState>> fields, bool isFrozen, bool isOpen = false) {
        var nodeFields = new Dictionary<string, TicNode>();
        foreach (var (key, value) in fields)
        {
            var node = value switch {
                           ITypeState at   => TicNode.CreateTypeVariableNode(at),
                           StateRefTo aRef => aRef.Node,
                           _               => throw new InvalidOperationException()
                       };
            nodeFields.Add(key, node);
        }

        return new StateStruct(nodeFields, isFrozen, isOpen);
    }

    public static StateStruct Of(string fieldName, ITicNodeState fieldState) => Of(true, (fieldName, fieldState));
    public static StateStruct Of(params (string, ITicNodeState)[] fields) => Of(true, fields);

    public static StateStruct Of(bool isFrozen, params (string, ITicNodeState)[] fields) {
        var nodeFields = new Dictionary<string, TicNode>();
        foreach (var (key, value) in fields)
        {
            var node = value switch {
                ITypeState at   => TicNode.CreateTypeVariableNode(at),
                StateRefTo aRef => aRef.Node,
                ConstraintsState ct => TicNode.CreateInvisibleNode(ct),
                _               => throw new InvalidOperationException()
            };
            nodeFields.Add(key, node);
        }

        return new StateStruct(nodeFields, isFrozen);
    }

    /// <summary>Migration constructor: wraps existing Dictionary in FieldMap.</summary>
    public StateStruct(Dictionary<string, TicNode> fields, bool isFrozen, bool isOpen = false) {
        _nodes = new FieldMap(fields);
        IsFrozen = isFrozen;
        IsOpen = isOpen;
    }

    public StateStruct(bool isOpen = false) { _nodes = new FieldMap(); IsOpen = isOpen; }

    public StateStruct(string name, TicNode node, bool isFrozen, bool isOpen = false) {
        _nodes = new FieldMap(name, node.GetNonReference());
        IsFrozen = isFrozen;
        IsOpen = isOpen;
    }

    internal StateStruct(FieldMap fields, bool isFrozen, bool isOpen = false) {
        _nodes = fields;
        IsFrozen = isFrozen;
        IsOpen = isOpen;
    }

    /// <summary>
    /// Open struct (row polymorphism): has at least these fields, may have more.
    /// Created by SetFieldAccess/SetSafeFieldAccess. When combined with another struct
    /// in AddDescendant, uses field UNION instead of LCA (field intersection).
    /// Closed struct (IsOpen=false): has exactly these fields.
    /// </summary>
    public bool IsOpen { get; }

    /// <summary>
    /// True iff this struct was introduced via an Optional-typed constraint —
    /// i.e. it traces back to a SetSafeFieldAccess (`?.`) emission of
    /// `opt(struct{field:T})`. Carried on the struct state so every TicNode
    /// that shares the state observes the marker, including after merges that
    /// alias multiple nodes to the same StateStruct instance.
    ///
    /// Push width-propagation reads this flag to decide whether a self-closing
    /// struct cycle should be repaired by restoring an Optional break around
    /// the closure (yielding the contractive μX. opt(struct{…X…})), or — if
    /// the cycle came from a non-Optional source like a declared `type t =
    /// {self:t}` — should be left to throw a recursion error.
    /// </summary>
    public bool IsOptionalSourced { get; set; }

    // IsSolvedMark must be a CONSTANT shared across all recursive IsSolved calls
    // so that cycles are detected. Negative value avoids collision with incrementing _nextMark.
    private const int IsSolvedMark = -55000;
    public bool IsSolved {
        get {
            if (TypeName != null) return true; // Named types are always solved
            foreach (var n in _nodes) {
                var node = n.Value;
                if (node.VisitMark == IsSolvedMark) continue;
                var prev = node.VisitMark;
                node.VisitMark = IsSolvedMark;
                bool solved = node.IsSolved;
                node.VisitMark = prev;
                if (!solved) return false;
            }
            return true;
        }
    }
    public bool IsMutable => TypeName == null; // Named types are solved (declared, not inferred)
    public string Description => ToString();
    public bool IsFrozen { get; internal set; }

    /// <summary>
    /// Name of the declared type this struct represents (e.g. "node").
    /// null for anonymous struct literals. Named structs are always solved.
    /// </summary>
    public string TypeName { get; set; }

    /// <summary>
    /// Identity rule for combining two structs into a more-specific result
    /// (Pull merge, Gcd, Unify, MergeStructs, UnionStructFields). The result
    /// is "the most specific consistent identity":
    /// - one side null, other named → take the named one (no conflict)
    /// - both equal (case-insensitive) → that name
    /// - both differ → null (true conflict — caller must reject or downgrade)
    /// </summary>
    public static string MergedTypeName(string a, string b) {
        if (a == null) return b;
        if (b == null) return a;
        return a.Equals(b, StringComparison.OrdinalIgnoreCase) ? a : null;
    }

    /// <summary>
    /// Identity rule for the Lca (least-upper-bound) of two structs. The
    /// result is "the most specific identity COMMON to both":
    /// - both equal → that name
    /// - any other case → null (anonymous struct, since one side lacks the name)
    /// Stricter than <see cref="MergedTypeName"/> because LCA must not invent
    /// a name the other side never carried.
    /// </summary>
    public static string LcaTypeName(string a, string b) {
        if (a == null || b == null) return null;
        return a.Equals(b, StringComparison.OrdinalIgnoreCase) ? a : null;
    }

    /// <summary>
    /// IsOptionalSourced propagates with OR-semantics. If either side is
    /// optional-sourced, the result is too — the marker propagates through every
    /// algebraic combination so cycle-rescue gating sees the full subgraph.
    /// </summary>
    public static bool MergedIsOptionalSourced(bool a, bool b) => a || b;

    public virtual ICompositeState GetNonReferenced() {
        var nodeCopy = new FieldMap();
        foreach (var (key, value) in _nodes)
            nodeCopy.Add(key, value.GetNonReference());
        return new StateStruct(nodeCopy, IsFrozen, IsOpen) {
            TypeName = TypeName,
            IsOptionalSourced = IsOptionalSourced
        };
    }

    public bool HasAnyReferenceMember {
        get {
            foreach (var v in _nodes.Values)
                if (v.State is StateRefTo) return true;
            return false;
        }
    }

    public int MemberCount => _nodes.Count;
    public TicNode GetMember(int index) => _nodes.GetValueAt(index).GetNonReference();
    public IEnumerable<TicNode> Members {
        get {
            foreach (var m in _nodes.Values)
                yield return m.GetNonReference();
        }
    }

    private const int LeafMark = -56000;

    public IEnumerable<TicNode> AllLeafTypes {
        get {
            foreach (var member in Members) {
                if (member.State is ICompositeState composite) {
                    if (member.VisitMark == LeafMark) continue; // cycle guard
                    var prev = member.VisitMark;
                    member.VisitMark = LeafMark;
                    foreach (var leaf in composite.AllLeafTypes)
                        yield return leaf;
                    member.VisitMark = prev;
                } else {
                    yield return member;
                }
            }
        }
    }

    public int MembersCount => _nodes.Count;

    public ITypeState GetLastCommonAncestorOrNull(ITypeState otherType) =>
        // Delegate to StateExtensions.Lca which handles unsolved constraint fields
        // via UnifyOrNull (unlike the old inline logic which skipped ConstraintsState fields)
        this.Lca(otherType) as ITypeState;

    private const int PrintMark = -57000;
    public virtual string PrintState(int depth) {
        if (depth > 16)
            return "{...REQ...}";
        if (_nodes.Count == 0)
            return IsFrozen ? "{frozen}" : "{}";
        var parts = new List<string>(_nodes.Count);
        foreach (var n in _nodes) {
            var node = n.Value;
            if (node.VisitMark == PrintMark) {
                parts.Add($"{n.Key}:...");
                continue;
            }
            var prev = node.VisitMark;
            node.VisitMark = PrintMark;
            try {
                parts.Add($"{n.Key}:{node.State.PrintState(depth + 1)}");
            } finally {
                node.VisitMark = prev;
            }
        }
        return "{" + (IsFrozen ? "F|" : "") + string.Join("; ", parts) + "}";
    }

    public bool CanBePessimisticConvertedTo(StatePrimitive type) => type== StatePrimitive.Any;

    public static ITypeState WithField(string name, StatePrimitive type)
        => new StateStruct(name, TicNode.CreateNamedNode(type.ToString(), type), isFrozen: false);

    [ThreadStatic] private static HashSet<(StateStruct, StateStruct)> _equalsVisited;

    public override bool Equals(object obj) {
        if (obj is not StateStruct stateStruct) return false;
        if (ReferenceEquals(this, stateStruct)) return true;
        // Nominal-typing rule for named structs:
        // When BOTH sides have a declared TypeName, equality is determined by the name
        // alone (case-insensitive). The names match → equal (cycle-rescued recursive
        // types succeed without descending into the cyclic field graph). The names
        // differ → NOT equal, even if shapes coincide — `type t1 = {v:int}` and
        // `type t2 = {v:int}` are distinct types per nominal typing (Pierce TAPL §19).
        //
        // Previously a name-mismatch fell through to structural comparison, allowing
        // silent merge of two distinct named types — caught by bug-regression unit tests
        // and verified to cause `y:t2 = x` (where `x:t1`) to be accepted with TypeName
        // dropped. The structural fall-through is preserved only when at least one side
        // is anonymous (no TypeName) — that's row-polymorphism / structural-subtyping
        // territory, not nominal-vs-nominal.
        if (TypeName != null && stateStruct.TypeName != null)
            return TypeName.Equals(stateStruct.TypeName, StringComparison.OrdinalIgnoreCase);
        if (_nodes.Count != stateStruct._nodes.Count) return false;

        // Coinductive Equals for cyclic struct shapes (Amadio-Cardelli '93 §4.2). With true graph
        // cycles in named recursive types, descending through fields can recurse infinitely.
        // Visited-pair guard: assume equal under recursive subgoal, return true on cycle.
        // HashSet reused per-thread via Remove (one alloc for process lifetime) — see
        // SolvingFunctions.GetMergedStateOrNull for the same pattern.
        var visited = _equalsVisited ??= new HashSet<(StateStruct, StateStruct)>();
        var key = (this, stateStruct);
        if (!visited.Add(key)) return true;
        try {
            foreach (var (k, value) in _nodes)
            {
                var f = stateStruct.GetFieldOrNull(k);
                if (f == null)
                    return false;
                if (!f.State.Equals(value.State))
                    return false;
            }
            return true;
        } finally {
            visited.Remove(key);
        }
    }

    public string StateDescription => PrintState(0);

    public override string ToString() {
        if (_nodes.Count == 0)
            return IsFrozen ? "{frozen}" : "{}";
        return "{" + (IsFrozen ? "F|" : "") +
               string.Join("; ",
                   _nodes.Select(n => $"{n.Key}:{(n.Value.State is StatePrimitive p ? p.ToString() : "..")}"))
               + "}";
    }
}
