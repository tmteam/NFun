using System.Collections.Generic;
using NFun.SyntaxParsing.Visitors;
using NFun.Tokenization;
using NFun.Types;

namespace NFun.Parsing
{
    public class UserFunctionDefenitionSyntaxNode: ISyntaxNode
    {
        public VarType OutputType { get; set; }
        public int NodeNumber { get; set; }

        public VarType SpecifiedType;
        public FunCallSyntaxNode Head;

        public UserFunctionDefenitionSyntaxNode(IList<TypedVarDefSyntaxNode> arguments, 
            FunCallSyntaxNode headNode, 
            ISyntaxNode expression, 
            VarType specifiedType)
        {
            Args = arguments;
            Head = headNode;
            SpecifiedType = specifiedType;
            Body = expression;
        }

        public string Id => Head.Id;
        public IList<TypedVarDefSyntaxNode>  Args { get;  }
        public ISyntaxNode Body { get; }
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

                yield return Body;
            }
        }

    }
}