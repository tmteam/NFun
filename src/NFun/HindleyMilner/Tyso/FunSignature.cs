using System.Linq;

namespace NFun.HindleyMilner.Tyso
{
    public class FunSignature
    {
        public CallDef ToCallDefenition(int returnNodeId, params int[] argIds)
        {
            return new CallDef(new[]{ReturnType}.Concat(ArgTypes).ToArray(), new[]{returnNodeId}.Concat(argIds).ToArray());
        }
        public readonly FType ReturnType;
        public readonly FType[] ArgTypes;

        public FunSignature(FType returnType, params FType[] argTypes)
        {
            ReturnType = returnType;
            ArgTypes = argTypes;
        }
    }
}