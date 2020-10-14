using System;
using System.Collections.Generic;
using System.Linq;
using NFun.Tic.Stages;

namespace NFun.Tic.SolvingStates
{
    public class StateStruct : ICompositeState
    {
        public IEnumerable<KeyValuePair<string, TicNode>> StructMembers => _nodes;
        
        private Dictionary<string, TicNode> _nodes;

        public StateStruct(Dictionary<string, TicNode> fields) => _nodes = fields;

        public StateStruct() => _nodes = new Dictionary<string, TicNode>();

        public StateStruct(string name, StatePrimitive node) => _nodes = new Dictionary<string, TicNode>{{name, node}};

        public bool IsSolved { get; }
        public string Description => "Struct";
        public bool ApplyDescendant(IStateCombination2dimensionalVisitor visitor, TicNode ancestorNode, TicNode descendantNode) =>
            descendantNode.State.Apply(visitor, ancestorNode, descendantNode, this);
        public bool Apply(IStateCombination2dimensionalVisitor visitor, TicNode ancestorNode, TicNode descendantNode,
            StatePrimitive ancestor)
            => visitor.Apply(ancestor,this,ancestorNode, descendantNode);
        public bool Apply(IStateCombination2dimensionalVisitor visitor, TicNode ancestorNode, TicNode descendantNode, ConstrainsState ancestor)
            => visitor.Apply( ancestor,this,ancestorNode, descendantNode);
        public bool Apply(IStateCombination2dimensionalVisitor visitor, TicNode ancestorNode, TicNode descendantNode, ICompositeState ancestor)
            => visitor.Apply(ancestor,this,ancestorNode, descendantNode);

        public ICompositeState GetNonReferenced()
        {
            var nodeCopy =_nodes.ToDictionary(d => d.Key, d => d.Value.GetNonReference());
            return new StateStruct(nodeCopy);
        }

        public bool HasAnyReferenceMember => _nodes.Values.Any(v => v.State is StateRefTo);
        public IEnumerable<TicNode> Members => _nodes.Values;
        public IEnumerable<TicNode> AllLeafTypes => _nodes.Values;
        public ITypeState GetLastCommonAncestorOrNull(ITypeState otherType)
        {
            throw new NotImplementedException();
        }

        public bool CanBeImplicitlyConvertedTo(StatePrimitive type) => type == StatePrimitive.Any;

        public static ITypeState WithField(string name, StatePrimitive type) => new StateStruct(name, type);
    }
}