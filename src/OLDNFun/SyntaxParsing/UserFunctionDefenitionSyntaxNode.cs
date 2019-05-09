using System.Collections.Generic;
using NFun.Tokenization;
using NFun.Types;

namespace NFun.Parsing
{
    public class UserFunctionDefenitionSyntaxNode: ISyntaxNode
    {
        public VarType OutputType;
        public FunCallSyntaxNode Head;

        public UserFunctionDefenitionSyntaxNode(IList<VarDefenitionSyntaxNode> arguments, 
            FunCallSyntaxNode headNode, 
            ISyntaxNode expression, 
            VarType outputType)
        {
            Args = arguments;
            Head = headNode;
            OutputType = outputType;
            Node = expression;
        }

        public string Id => Head.Value;
        public IList<VarDefenitionSyntaxNode>  Args { get; set; }
        public ISyntaxNode Node { get; set; }
        public bool IsBracket { get; set; }
        public LexNodeType Type => LexNodeType.UserFunctionDefenition;
        public Interval Interval { get; set; }
    }
}