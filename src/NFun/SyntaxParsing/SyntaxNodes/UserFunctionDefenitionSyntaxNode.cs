using System.Collections.Generic;
using NFun.SyntaxParsing.Visitors;
using NFun.Tokenization;
using NFun.Types;

namespace NFun.Parsing
{
    public class UserFunctionDefenitionSyntaxNode: ISyntaxNode
    {
        public int NodeNumber { get; set; }

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
            BodyExpression = expression;
        }

        public string Id => Head.Id;
        public IList<TypedVarDefSyntaxNode>  Args { get;  }
        public ISyntaxNode BodyExpression { get; }
        public bool IsInBrackets { get; set; }
        public SyntaxNodeType Type => SyntaxNodeType.UserFunctionDefenition;
        public Interval Interval { get; set; }
        public T Visit<T>(ISyntaxNodeVisitor<T> visitor) => visitor.Visit(this);

        public IEnumerable<ISyntaxNode> Children
        {
            get
            {
                foreach (var typedVarDefSyntaxNode in Args)
                {
                    yield return typedVarDefSyntaxNode;
                }

                yield return BodyExpression;
            }
        }

    }
}