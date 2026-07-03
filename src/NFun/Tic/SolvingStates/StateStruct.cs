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

    public void AddField(string name, TicNode memberNode) {
        _nodes.Add(name, memberNode);
    }

    /// <summary>Replace an existing field's TicNode in-place. Caller must ensure existence.</summary>
    public void ReplaceField(string name, TicNode memberNode) {
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

    /// <summary>Row polymorphism: open ≡ "at least these fields", closed ≡ "exactly these".</summary>
    public bool IsOpen { get; }

    /// <summary>Marker: this struct entered via a `?.` emission of <c>opt(struct{…})</c>.
    /// Push width-propagation uses it to decide whether to restore an Optional break around
    /// a self-closing cycle (contractive μX.opt(struct{…X…})) vs let it throw.</summary>
    public bool IsOptionalSourced { get; set; }

    public bool IsSolved {
        get {
            if (TypeName != null) return true; // Named types are always solved
            foreach (var n in _nodes) {
                var node = n.Value;
                if (node.VisitMark == Tic.TicVisitMarks.StructIsSolved) continue;
                var prev = node.VisitMark;
                node.VisitMark = Tic.TicVisitMarks.StructIsSolved;
                bool solved = node.IsSolved;
                node.VisitMark = prev;
                if (!solved) return false;
            }
            return true;
        }
    }
    // Immutable iff fully solved (mirrors StateArray/StateOptional/StateFun) — gives the F-bound
    // check a uniform "fully determined" probe.
    public bool IsMutable => !IsSolved;
    public string Description => ToString();
    public bool IsFrozen { get; internal set; }

    /// <summary>Name of the declared type ("node", …); null for anonymous literals.
    /// Named structs are always solved.</summary>
    public string TypeName { get; set; }

    /// <summary>Identity rule for refinement merges: pick the most specific consistent name
    /// (one-named wins; equal stays; conflict → null).</summary>
    public static string MergedTypeName(string a, string b) {
        if (a == null) return b;
        if (b == null) return a;
        return a.Equals(b, StringComparison.OrdinalIgnoreCase) ? a : null;
    }

    /// <summary>Identity rule for LCA: name common to both (else null). Stricter than
    /// <see cref="MergedTypeName"/> — LCA must not invent a name.</summary>
    public static string LcaTypeName(string a, string b) {
        if (a == null || b == null) return null;
        return a.Equals(b, StringComparison.OrdinalIgnoreCase) ? a : null;
    }

    /// <summary>IsOptionalSourced merges as OR so cycle-rescue gating sees the full subgraph.</summary>
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

    public IEnumerable<TicNode> AllLeafTypes {
        get {
            foreach (var member in Members) {
                if (member.State is ICompositeState composite) {
                    if (member.VisitMark == Tic.TicVisitMarks.StateLeaf) continue; // cycle guard
                    var prev = member.VisitMark;
                    member.VisitMark = Tic.TicVisitMarks.StateLeaf;
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
        // Delegate so unsolved ConstraintsState fields go through UnifyOrNull.
        this.Lca(otherType) as ITypeState;

    public virtual string PrintState(int depth) {
        if (depth > 16)
            return "{...REQ...}";
        if (_nodes.Count == 0)
            return IsFrozen ? "{frozen}" : "{}";
        var parts = new List<string>(_nodes.Count);
        foreach (var n in _nodes) {
            var node = n.Value;
            if (node.VisitMark == Tic.TicVisitMarks.StructPrint) {
                parts.Add($"{n.Key}:...");
                continue;
            }
            var prev = node.VisitMark;
            node.VisitMark = Tic.TicVisitMarks.StructPrint;
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

    /// <summary>Nominal equality when both sides carry TypeName (avoids cycle traversal and
    /// prevents same-shape-distinct-name silent unification). Null ⇒ rule N/A.</summary>
    internal static bool? NominalEquals(StateStruct a, StateStruct b) {
        if (a.TypeName == null || b.TypeName == null) return null;
        return a.TypeName.Equals(b.TypeName, StringComparison.OrdinalIgnoreCase);
    }

    [ThreadStatic] private static HashSet<(StateStruct, StateStruct)> _equalsVisited;

    public override bool Equals(object obj) {
        if (obj is not StateStruct stateStruct) return false;
        if (ReferenceEquals(this, stateStruct)) return true;
        if (NominalEquals(this, stateStruct) is bool nominal) return nominal;
        if (_nodes.Count != stateStruct._nodes.Count) return false;

        // Coinductive Equals for cyclic struct shapes (Amadio–Cardelli '93). Per-thread HashSet
        // reused via Remove.
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
