using System;
using System.Collections.Generic;
using System.Linq;

namespace NFun.TypeInference.Solving
{
    public class TiResult
    {
        public const int NestedDepth = 100;
        private readonly IList<SolvingNode> _nodes;
        private readonly Dictionary<string, SolvingNode> _variablesMap;
        public string[] VarNames => _variablesMap.Keys.ToArray();
        public static TiResult NotSolvedResult() => new TiResult(false);

        private TiResult(bool isSolved)
        {
            _variablesMap = new Dictionary<string, SolvingNode>();
            IsSolved = isSolved;
        }

        public TiResult(IList<SolvingNode> nodes, IList<SolvingNode> allTypes,
            Dictionary<string, SolvingNode> variablesMap, Dictionary<int, TiFunctionSignature> overloads)
        {
            _overloads = overloads;
            IsSolved = true;
            _nodes = nodes;
            _variablesMap = variablesMap;
            int genericsCount = 0;
            _genMap = new Dictionary<SolvingNode, int>();
            foreach (var type in allTypes)
            {
                var concreteNode = GetConcrete(type, NestedDepth);
                if (concreteNode.Behavior is GenericTypeBehaviour)
                {
                    if (_genMap.ContainsKey(concreteNode))
                        continue;
                    _genMap.Add(concreteNode, genericsCount);
                    genericsCount++;
                }
            }

            GenericsCount = genericsCount;
        }

        public int GenericsCount { get; }
        public bool IsSolved { get; }

        private readonly Dictionary<SolvingNode, int> _genMap;
        private readonly Dictionary<int, TiFunctionSignature> _overloads;
        public TiType GetVarType(string varId) => ConvertToHmType2(_variablesMap[varId], NestedDepth);

        public TiType GetVarTypeOrNull(string varId)
        {
            if (!_variablesMap.TryGetValue(varId, out var solvingNode))
                return null;
            return ConvertToHmType2(solvingNode, NestedDepth);
        }


        public TiType GetNodeType(int nodeId) => ConvertToHmType2(_nodes[nodeId], NestedDepth);

        private SolvingNode GetConcrete(SolvingNode node, int nestedCount)
        {
            if (nestedCount < 0)
                throw new StackOverflowException("Get Concrete raise SO");
            if (node.Behavior is ReferenceBehaviour eq)
                return GetConcrete(eq.Node, nestedCount - 1);
            if (node.Behavior is LcaNodeBehaviour lca && lca.OtherNodes.Length == 1)
                return lca.OtherNodes.First();
            return node;
        }

        private TiType ConvertToHmType2(SolvingNode node, int nestedCount)
        {
            if (nestedCount < 0)
                throw new StackOverflowException("ConvertToHmType2 raise SO");
            var concreteNode = GetConcrete(node, nestedCount - 1);
            var beh = concreteNode.Behavior;


            if (beh is GenericTypeBehaviour)
            {
                if (!_genMap.TryGetValue(concreteNode, out var val))
                    throw new InvalidOperationException("Generic is not in the map");
                //Generic type there!
                return TiType.Generic(val);
            }

            var type = beh.MakeType(nestedCount - 1);

            SolvingNode[] arguments = type.Arguments
                .Select(a => SolvingNode.CreateStrict(ConvertToHmType2(a, nestedCount - 1)))
                .ToArray();
            if (type.Name.Equals(TiTypeName.SomeInteger))
                return new TiType(TiTypeName.Int32);

            return new TiType(type.Name, arguments);

        }

        public TiType MakeFunDefenition()
        {
            //maxNodeId is return type.
            if (!_nodes.Any())
                throw new InvalidOperationException();
            var outputNode = _nodes.LastOrDefault((n => n != null));
            if (outputNode == null)
                throw new InvalidOperationException();
            outputNode = GetConcrete(outputNode, NestedDepth);
            List<TiType> args = new List<TiType>();
            foreach (var solvingNode in _variablesMap)
            {
                var concrete = GetConcrete(solvingNode.Value, NestedDepth);
                if (concrete == outputNode)
                    continue;
                args.Add(ConvertToHmType2(concrete, NestedDepth));
            }

            return TiType.Fun(ConvertToHmType2(outputNode, NestedDepth), args.ToArray());
        }

        public TiType GetNodeTypeOrNull(int nodeId)
        {
            if (_nodes.Count <= nodeId)
                return null;
            var node = _nodes[nodeId];
            if (node == null)
                return null;
            return ConvertToHmType2(node, NestedDepth);
        }

        public TiFunctionSignature GetFunctionOverload(int nodeId)
        {
            if (_overloads.ContainsKey(nodeId)) 
                return _overloads[nodeId];
            return null;
        }
    }
}