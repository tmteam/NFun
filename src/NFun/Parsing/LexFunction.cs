using NFun.Types;

namespace NFun.Parsing
{
    public class LexFunction: ILexRoot
    {
        public LexFunction()
        {
            
        }
        public VarType OutputType;
        public LexNode Head;
        public string Id => Head.Value;
        public LexVarDefenition[] Args;
        public LexNode Node;
    }
}