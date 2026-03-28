using System;
using System.Collections.Generic;
using NFun.SyntaxParsing.Visitors;
using NFun.Tokenization;

namespace NFun.SyntaxParsing.SyntaxNodes;

public class DefaultValueSyntaxNode : ISyntaxNode {
    public DefaultValueSyntaxNode(Interval interval) => Interval = interval;
    public FunnyType OutputType { get; set; }
    public int OrderNumber { get; set; }
    public int ParenthesesCount { get; set; }
    public Interval Interval { get; set; }

    /// <summary>
    /// Original default value expression from the function definition.
    /// When non-null, the expression builder should build this expression
    /// instead of using GetDefaultFunnyValue().
    /// </summary>
    public ISyntaxNode OriginalExpression { get; init; }

    public T Accept<T>(ISyntaxNodeVisitor<T> visitor) => visitor.Visit(this);
    public IEnumerable<ISyntaxNode> Children => Array.Empty<ISyntaxNode>();
    public override string ToString() => OriginalExpression != null ? $"default({OriginalExpression})" : "default";
}
