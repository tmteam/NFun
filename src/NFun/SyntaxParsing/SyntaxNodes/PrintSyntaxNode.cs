using System.Collections.Generic;
using NFun.SyntaxParsing.Visitors;
using NFun.Tokenization;

namespace NFun.SyntaxParsing.SyntaxNodes;

public class PrintSyntaxNode : ISyntaxNode {
    public ISyntaxNode Expression { get; }

    public PrintSyntaxNode(ISyntaxNode expression, Interval interval) {
        Expression = expression;
        Interval = interval;
    }

    public FunnyType OutputType { get; set; }
    public int OrderNumber { get; set; }
    public int ParenthesesCount { get; set; }
    public Interval Interval { get; set; }
    public T Accept<T>(ISyntaxNodeVisitor<T> visitor) => visitor.Visit(this);
    public IEnumerable<ISyntaxNode> Children {
        get { yield return Expression; }
    }
}
