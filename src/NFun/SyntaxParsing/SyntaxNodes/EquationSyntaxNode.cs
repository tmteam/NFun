using System.Collections.Generic;
using NFun.SyntaxParsing.Visitors;
using NFun.Tokenization;

namespace NFun.SyntaxParsing.SyntaxNodes;

public class EquationSyntaxNode : ISyntaxNode {
    public EquationSyntaxNode(string id, int start, ISyntaxNode expression, FunnyAttribute[] attributes) {
        Id = id;
        Expression = expression;
        Attributes = attributes;
        Interval = new Interval(start, expression.Interval.Finish);
    }

    public string Id { get; }
    public ISyntaxNode Expression { get; }
    public FunnyAttribute[] Attributes { get; }
    public TypedVarDefSyntaxNode TypeSpecificationOrNull { get; set; }
    public bool OutputTypeSpecified => TypeSpecificationOrNull != null;
    public FunnyType OutputType { get; set; }
    public int OrderNumber { get; set; }
    public int ParenthesesCount { get; set; }
    public Interval Interval { get; set; }
    public T Accept<T>(ISyntaxNodeVisitor<T> visitor) => visitor.Visit(this);
    public IEnumerable<ISyntaxNode> Children => new[] { Expression };
}
