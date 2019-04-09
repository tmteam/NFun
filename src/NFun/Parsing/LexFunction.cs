using NFun.Tokenization;
using NFun.Types;

namespace NFun.Parsing
{
    public class LexFunction
    {
        public LexFunction()
        {
            
        }
        public VarType OutputType;
        public LexNode Head;
        public string Id => Head.Value;
        public VariableInfo[] Args;
        public LexNode Node;
    }
}