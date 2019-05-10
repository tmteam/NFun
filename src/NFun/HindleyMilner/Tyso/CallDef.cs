using System.Linq;

namespace NFun.HindleyMilner.Tyso
{
    public class CallDef
    {
        public CallDef(FType type2, int[] nodesId)
        {
            Types = Enumerable.Repeat(type2,nodesId.Length).ToArray();
            this.nodesId = nodesId;
        }
        public CallDef(FType[] types, int[] nodesId)
        {
            Types = types;
            this.nodesId = nodesId;
        }
        
        public FType[] Types { get; }
        public int[] nodesId { get; }
    }
}