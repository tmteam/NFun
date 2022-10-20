using System.Collections.Generic;
using NFun.SyntaxParsing.Visitors;
using NFun.Tokenization;

namespace NFun.SyntaxParsing.SyntaxNodes; 

public class IfCaseSyntaxNode : ISyntaxNode {
    public IfCaseSyntaxNode(ISyntaxNode condition, ISyntaxNode expression, Interval interval) {
        Condition = condition;
        Expression = expression;
        Interval = interval;
    }
    public FunnyType OutputType { get; set; }
    public int OrderNumber { get; set; }
    public int ParenthesesCount { get; set; }
    public ISyntaxNode Condition { get; }
    public ISyntaxNode Expression { get; }
    public Interval Interval { get; set; }
    public T Accept<T>(ISyntaxNodeVisitor<T> visitor) => visitor.Visit(this);
    public IEnumerable<ISyntaxNode> Children => new[] { Condition, Expression };
}