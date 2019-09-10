using System.Linq;

namespace NFun.TypeInference.Solving
{
    public class CallDefenition
    {
        public CallDefenition(TiType sameTypeForAllArguments, int[] nodesId)
        {
            Types = Enumerable.Repeat(sameTypeForAllArguments,nodesId.Length).ToArray();
            NodesId = nodesId;
        }
        
        public CallDefenition(TiType[] types, int[] nodesId)
        {
            Types = types;
            NodesId = nodesId;
        }
        
        public TiType[] Types { get; }
        public int[] NodesId { get; }
        public override string ToString()
        {
            return
                $"({string.Join(",", Types.Skip(1).Select(t => t.ToString()))}):{Types[0]} " +
                $"{{{string.Join(",", NodesId.Select(n => n.ToString()))}}}";
        }
    }
}