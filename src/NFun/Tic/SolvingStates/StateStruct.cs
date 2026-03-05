using System;
using System.Collections.Generic;
using System.Linq;

namespace NFun.Tic.SolvingStates;

public class StateStruct : ICompositeState {

    public int FieldsCount => _nodes.Count;

    public IEnumerable<KeyValuePair<string, TicNode>> Fields => _nodes;

    private readonly Dictionary<string, TicNode> _nodes;

    public TicNode GetFieldOrNull(string fieldName) {
        _nodes.TryGetValue(fieldName, out var res);
        return res;
    }

    /// <summary>
    /// Adds a field in-place and returns this.
    /// Safe because all callers discard the old reference immediately.
    /// </summary>
    public StateStruct With(string name, TicNode memberNode) {
        _nodes.Add(name, memberNode);
        return this;
    }

    public static StateStruct Empty(bool isFrozen = true) => Of(isFrozen);
    public static StateStruct Of(IEnumerable<KeyValuePair<string, ITicNodeState>> fields, bool isFrozen) {
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

        return new StateStruct(nodeFields, isFrozen);
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

    public StateStruct(Dictionary<string, TicNode> fields, bool isFrozen) {
        _nodes = fields;
        IsFrozen = isFrozen;
    }

    public StateStruct() => _nodes = new Dictionary<string, TicNode>();

    public StateStruct(string name, TicNode node, bool isFrozen) {
        _nodes = new Dictionary<string, TicNode> { { name, node.GetNonReference() } };
        IsFrozen = isFrozen;
    }

    public bool IsSolved {
        get {
            foreach (var n in _nodes)
                if (!n.Value.IsSolved) return false;
            return true;
        }
    }
    public bool IsMutable => true;
    public string Description => ToString();
    public bool IsFrozen { get; }

    public ICompositeState GetNonReferenced() {
        var nodeCopy = new Dictionary<string, TicNode>(_nodes.Count);
        foreach (var (key, value) in _nodes)
            nodeCopy.Add(key, value.GetNonReference());
        return new StateStruct(nodeCopy, IsFrozen);
    }

    public bool HasAnyReferenceMember {
        get {
            foreach (var v in _nodes.Values)
                if (v.State is StateRefTo) return true;
            return false;
        }
    }

    public IEnumerable<TicNode> Members {
        get {
            foreach (var m in _nodes.Values)
                yield return m.GetNonReference();
        }
    }

    public IEnumerable<TicNode> AllLeafTypes
    {
        get
        {
            foreach (var member in Members)
            {
                if (member.State is ICompositeState composite)
                {
                    foreach (var leaf in composite.AllLeafTypes)
                    {
                        yield return leaf;
                    }
                }
                else
                {
                    yield return member;
                }
            }
        }
    }

    public int MembersCount => _nodes.Count;

    public ITypeState GetLastCommonAncestorOrNull(ITypeState otherType) {
        if (otherType is not StateStruct otherStruct)
            return StatePrimitive.Any;

        // Struct LCA = intersection of fields where field types have a common ancestor
        var resultFields = new Dictionary<string, TicNode>();
        foreach (var (name, node) in _nodes)
        {
            var otherField = otherStruct.GetFieldOrNull(name);
            if (otherField == null)
                continue; // field not in other struct → not in LCA

            if (node.State is not ITypeState thisFieldType)
                continue;
            if (otherField.State is not ITypeState otherFieldType)
                continue;

            if (thisFieldType.Equals(otherFieldType))
            {
                resultFields.Add(name, TicNode.CreateTypeVariableNode(thisFieldType));
            }
            else
            {
                var fieldLca = thisFieldType.GetLastCommonAncestorOrNull(otherFieldType);
                if (fieldLca == null)
                    continue; // incompatible field types → field dropped from LCA
                resultFields.Add(name, TicNode.CreateTypeVariableNode(fieldLca));
            }
        }

        return new StateStruct(resultFields, true);
    }

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

    public bool CanBePessimisticConvertedTo(StatePrimitive type) => type.Equals(StatePrimitive.Any);

    public static ITypeState WithField(string name, StatePrimitive type)
        => new StateStruct(name, TicNode.CreateNamedNode(type.ToString(), type), isFrozen: false);

    public override bool Equals(object obj) {
        if (obj is not StateStruct stateStruct) return false;
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
                   _nodes.Select(n => $"{n.Key}:{(n.Value.State is StatePrimitive p ? p.ToString() : $"..")}"))
               + "}";
    }
}
