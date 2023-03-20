using System;
using System.Collections.Generic;
using System.Linq;
using NFun.Tic.Stages;

namespace NFun.Tic.SolvingStates;

public class StateStruct : ICompositeState {

    public int FieldsCount => _nodes.Count;

    public IEnumerable<KeyValuePair<string, TicNode>> Fields => _nodes;

    /// <summary>
    /// Is it possible to add additional fields to this type
    /// </summary>
    public bool IsFrozen { get; }

    /// <summary>
    /// Is it possible to skip fields in this type
    /// </summary>
    public bool AllowDefaultValues { get; }

    public bool IsSolved => _nodes.All(n => n.Value.IsSolved);

    public bool IsMutable => true;

    public string Description =>
        "{"
        + (IsFrozen?"-frozen" :"")
        + (AllowDefaultValues?"-default " :"")
        + string.Join("; ", _nodes.Select(n => $"{n.Key}:{n.Value}"))
        + "}";

    private readonly Dictionary<string, TicNode> _nodes;

    public TicNode GetFieldOrNull(string fieldName) {
        _nodes.TryGetValue(fieldName, out var res);
        return res;
    }

    public StateStruct With(string name, TicNode memberNode, bool allowDefaultValues) {
        var newDic = new Dictionary<string, TicNode>(_nodes.Count + 1);
        foreach (var (key, value) in _nodes)
        {
            newDic.Add(key, value.GetNonReference());
        }

        newDic.Add(name, memberNode);
        return new StateStruct(newDic, isFrozen: IsFrozen, allowDefaultValues: allowDefaultValues);
    }

    public static StateStruct Of(IEnumerable<KeyValuePair<string, ITicNodeState>> fields, bool isFrozen, bool allowDefaultValues = false) {
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
        return new StateStruct(nodeFields, isFrozen, allowDefaultValues);
    }

    public StateStruct(Dictionary<string, TicNode> fields, bool isFrozen, bool allowDefaultValues = false) {
        _nodes = fields;
        IsFrozen = isFrozen;
        AllowDefaultValues = allowDefaultValues;
    }


    public StateStruct(bool allowDefaultValues = false) {
        _nodes = new Dictionary<string, TicNode>();
        AllowDefaultValues = allowDefaultValues;
    }

    public StateStruct(string name, TicNode node, bool isFrozen, bool allowDefaultValues = false) {
        _nodes = new Dictionary<string, TicNode> { { name, node.GetNonReference() } };
        IsFrozen = isFrozen;
        AllowDefaultValues = allowDefaultValues;
    }

    public bool ApplyDescendant(
        IStateCombination2dimensionalVisitor visitor, TicNode ancestorNode,
        TicNode descendantNode) =>
        descendantNode.State.Apply(visitor, ancestorNode, descendantNode, this);

    public bool Apply(
        IStateCombination2dimensionalVisitor visitor, TicNode ancestorNode, TicNode descendantNode,
        StatePrimitive ancestor)
        => visitor.Apply(ancestor, this, ancestorNode, descendantNode);

    public bool Apply(
        IStateCombination2dimensionalVisitor visitor, TicNode ancestorNode, TicNode descendantNode,
        ConstrainsState ancestor)
        => visitor.Apply(ancestor, this, ancestorNode, descendantNode);

    public bool Apply(
        IStateCombination2dimensionalVisitor visitor, TicNode ancestorNode, TicNode descendantNode,
        ICompositeState ancestor)
        => visitor.Apply(ancestor, this, ancestorNode, descendantNode);

    public ICompositeState GetNonReferenced() {
        var nodeCopy = _nodes.ToDictionary(d => d.Key, d => d.Value.GetNonReference());
        return new StateStruct(nodeCopy, IsFrozen, AllowDefaultValues);
    }

    public bool HasAnyReferenceMember => _nodes.Values.Any(v => v.State is StateRefTo);
    public IEnumerable<TicNode> Members => _nodes.Values.Select(m => m.GetNonReference());

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

    public ITypeState GetLastCommonAncestorOrNull(ITypeState otherType) =>
        otherType is StateStruct
            ? new StateStruct()
            : StatePrimitive.Any;

    public bool CanBeImplicitlyConvertedTo(StatePrimitive type) => type.Equals(StatePrimitive.Any);

    public static ITypeState WithField(string name, StatePrimitive type)
        => new StateStruct(name, TicNode.CreateNamedNode(type.ToString(), type), isFrozen: false);

    public override bool Equals(object obj) {
        if (obj is not StateStruct stateStruct) return false;

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

    public override string ToString()
        => "{"
           + (IsFrozen?"-frozen" :"")
           + (AllowDefaultValues?"-default " :"")
           + string.Join("; ", _nodes.Select(n => $"{n.Key}:{n.Value.State}"))
           + "}";
}
