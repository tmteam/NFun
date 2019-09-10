using System.Collections.Generic;
using System.Linq;

namespace NFun.TypeInference.Solving
{
    public class GenericMap
    {
        public SolvingNode CreateSolvingNode(TiType type)
        {
            if (type is GenericType t) 
                return Get(t.GenericId);

            var args = type.Arguments.Select(MakeStrictNode).ToArray();
            
            return SolvingNode.CreateStrict(type.Name, args);
        }
        

        private List<SolvingNode> _map = new List<SolvingNode>();
        public IEnumerable<SolvingNode> Nodes => _map;

        /// <summary>
        /// Get (or Add then Get) reserved node for generic type Ti
        /// </summary>
        /// <param name="genericId">i</param>
        public SolvingNode Get(int genericId)
        {
            while (_map.Count <= genericId)
                _map.Add(null);

            var res = _map[genericId];
            if (res != null)
                return res;
            var newGeneric = new SolvingNode();
            _map[genericId] = newGeneric;
            return newGeneric;
        }
        
        private SolvingNode MakeStrictNode(SolvingNode node)
        {
            var actual = node.GetActualNode();
            
            if (actual.Behavior is GenericTypeBehaviour)
            {
                _map.Add(node);
                return node;
            }
            return CreateSolvingNode(actual.MakeType());
        }

    }
}