using System;
using System.Collections.Generic;
using NFun.SyntaxParsing.Visitors;
using NFun.Tokenization;

namespace NFun.SyntaxParsing.SyntaxNodes;

public class ReturnSyntaxNode : ISyntaxNode {
    public ReturnSyntaxNode(ISyntaxNode expression, Interval interval) {
        Expression = expression;
        Interval = interval;
    }

    public ISyntaxNode Expression { get; }
    public FunnyType OutputType { get; set; }
    public int OrderNumber { get; set; }
    public int ParenthesesCount { get; set; }
    public Interval Interval { get; set; }
    public IEnumerable<ISyntaxNode> Children => Expression != null ? new[] { Expression } : Array.Empty<ISyntaxNode>();
    public T Accept<T>(ISyntaxNodeVisitor<T> visitor) => visitor.Visit(this);
}
