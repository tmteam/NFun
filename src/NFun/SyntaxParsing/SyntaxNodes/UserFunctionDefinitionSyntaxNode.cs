using System.Collections.Generic;
using NFun.SyntaxParsing.Visitors;
using NFun.Tokenization;

namespace NFun.SyntaxParsing.SyntaxNodes; 

public class UserFunctionDefinitionSyntaxNode : ISyntaxNode {
    public UserFunctionDefinitionSyntaxNode(
        IList<TypedVarDefSyntaxNode> arguments,
        FunCallSyntaxNode headNode,
        ISyntaxNode expression,
        TypeSyntax returnTypeSyntax) {
        Args = arguments;
        Head = headNode;
        ReturnTypeSyntax = returnTypeSyntax;
        Body = expression;
    }
    public FunnyType OutputType { get; set; }
    public int OrderNumber { get; set; }
    public int ParenthesesCount { get; set; }
    public TypeSyntax ReturnTypeSyntax { get; }
    public FunCallSyntaxNode Head { get; }
    public bool IsRecursive { get; set; } = true;
    public string Id => Head.Id;
    public IList<TypedVarDefSyntaxNode> Args { get; }
    public ISyntaxNode Body { get; }
    public Interval Interval { get; set; }
    public T Accept<T>(ISyntaxNodeVisitor<T> visitor) => visitor.Visit(this);
    public IEnumerable<ISyntaxNode> Children
    {
        get
        {
            foreach (var typedVarDefSyntaxNode in Args)
                yield return typedVarDefSyntaxNode;
            yield return Body;
        }
    }
}