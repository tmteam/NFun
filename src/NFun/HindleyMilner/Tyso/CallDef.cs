using System;
using System.Linq;

namespace NFun.HindleyMilner.Tyso
{
    public class CallDef
    {
        public CallDef(FType type2, int[] nodesId)
        {
            Types = Enumerable.Repeat(type2,nodesId.Length).ToArray();
            NodesId = nodesId;
        }
        public CallDef(FType[] types, int[] nodesId)
        {
            Types = types;
            NodesId = nodesId;
        }
        
        public FType[] Types { get; }
        public int[] NodesId { get; }
        public override string ToString()
        {
            return
                $"({string.Join(",", Types.Skip(1).Select(t => t.ToString()))}):{Types[0]} " +
                $"{{{string.Join(",", NodesId.Select(n => n.ToString()))}}}";
        }
    }
}