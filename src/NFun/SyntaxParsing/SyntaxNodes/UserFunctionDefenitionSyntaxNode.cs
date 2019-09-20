using System.Collections.Generic;
using NFun.SyntaxParsing.Visitors;
using NFun.Tokenization;
using NFun.Types;

namespace NFun.SyntaxParsing.SyntaxNodes
{
    public class UserFunctionDefenitionSyntaxNode: ISyntaxNode
    {
        public VarType OutputType { get; set; }
        public int OrderNumber { get; set; }
        public VarType ReturnType { get; }
        public FunCallSyntaxNode Head { get; }

        public UserFunctionDefenitionSyntaxNode(
            IList<TypedVarDefSyntaxNode> arguments, 
            FunCallSyntaxNode headNode, 
            ISyntaxNode expression, 
            VarType returnType)
        {
            Args = arguments;
            Head = headNode;
            ReturnType = returnType;
            Body = expression;
        }

        public string Id => Head.Id;
        public IList<TypedVarDefSyntaxNode>  Args { get;  }
        public ISyntaxNode Body { get; }
        public bool IsInBrackets { get; set; }
        public Interval Interval { get; set; }
        public T Visit<T>(ISyntaxNodeVisitor<T> visitor) => visitor.Visit(this);

        public IEnumerable<ISyntaxNode> Children {
            get {
                foreach (var typedVarDefSyntaxNode in Args)
                    yield return typedVarDefSyntaxNode;
                yield return Body;
            }
        }
    }
}