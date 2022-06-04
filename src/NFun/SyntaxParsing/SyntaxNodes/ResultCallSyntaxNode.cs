using System.Collections.Generic;
using NFun.SyntaxParsing.Visitors;
using NFun.Tokenization;

namespace NFun.SyntaxParsing.SyntaxNodes; 

public class ResultFunCallSyntaxNode : IFunCallSyntaxNode {
    public ResultFunCallSyntaxNode(ISyntaxNode resultExpression, ISyntaxNode[] args, Interval interval) {
        ResultExpression = resultExpression;
        Args = args;
        Interval = interval;
    }
    public FunnyType OutputType { get; set; }
    public int OrderNumber { get; set; }
    public bool IsInBrackets { get; set; }
    public ISyntaxNode ResultExpression { get; }
    public ISyntaxNode[] Args { get; }
    public Interval Interval { get; set; }
    public T Accept<T>(ISyntaxNodeVisitor<T> visitor) => visitor.Visit(this);
    public IEnumerable<ISyntaxNode> Children
    {
        get
        {
            yield return ResultExpression;
            foreach (var node in Args)
                yield return node;
        }
    }
}