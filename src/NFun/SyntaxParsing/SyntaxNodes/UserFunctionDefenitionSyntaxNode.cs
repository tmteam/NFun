using System.Collections.Generic;
using NFun.Tokenization;
using NFun.Types;

namespace NFun.Parsing
{
    public class UserFunctionDefenitionSyntaxNode: ISyntaxNode
    {
        public VarType OutputType;
        public FunCallSyntaxNode Head;

        public UserFunctionDefenitionSyntaxNode(IList<TypedVarDefSyntaxNode> arguments, 
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
        public IList<TypedVarDefSyntaxNode>  Args { get;  }
        public ISyntaxNode Node { get; }
        public bool IsInBrackets { get; set; }
        public SyntaxNodeType Type => SyntaxNodeType.UserFunctionDefenition;
        public Interval Interval { get; set; }
        public T Visit<T>(ISyntaxNodeVisitor<T> visitor) => visitor.Visit(this);

        public IEnumerable<ISyntaxNode> Children
        {
            get
            {
                yield return Head;
                foreach (var typedVarDefSyntaxNode in Args)
                {
                    yield return typedVarDefSyntaxNode;
                }
            }
        }

    }
}