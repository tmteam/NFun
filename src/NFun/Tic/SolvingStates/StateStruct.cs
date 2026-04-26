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
    public bool IsFrozen { get; }

    /// <summary>
    /// Name of the declared type this struct represents (e.g. "node").
    /// null for anonymous struct literals. Named structs are always solved.
    /// </summary>
    public string TypeName { get; set; }

    public ICompositeState GetNonReferenced() {
        var nodeCopy = new FieldMap();
        foreach (var (key, value) in _nodes)
            nodeCopy.Add(key, value.GetNonReference());
        return new StateStruct(nodeCopy, IsFrozen, IsOpen) { TypeName = TypeName };
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

    public string PrintState(int depth) {
        if (depth > 100)
            return "{...REQ...}";
        if (_nodes.Count == 0)
            return IsFrozen ? "{frozen}" : "{}";
        return
            "{"
            + (IsFrozen ? "F|" : "")
            + string.Join("; ", _nodes.Select(n => $"{n.Key}:{n.Value.State.PrintState(depth + 1)}"))
            + "}";
    }

    public bool CanBePessimisticConvertedTo(StatePrimitive type) => type== StatePrimitive.Any;

    public static ITypeState WithField(string name, StatePrimitive type)
        => new StateStruct(name, TicNode.CreateNamedNode(type.ToString(), type), isFrozen: false);

    public override bool Equals(object obj) {
        if (obj is not StateStruct stateStruct) return false;
        if (ReferenceEquals(this, stateStruct)) return true;
        if (_nodes.Count != stateStruct._nodes.Count) return false;

        foreach (var (key, value) in _nodes)
        {
            var f = stateStruct.GetFieldOrNull(key);
            if (f == null)
                return false;
            if (!f.State.Equals(value.State))
                return false;
        }
        return true;
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
