using System;
using System.Collections.Generic;
using NFun.SyntaxParsing.Visitors;
using NFun.Tokenization;

namespace NFun.SyntaxParsing.SyntaxNodes;

public class ConstantSyntaxNode : ISyntaxNode {
    public ConstantSyntaxNode(object value, FunnyType funnyType, Interval interval) {
        OutputType = funnyType;
        Interval = interval;
        Value = value;
    }

    public FunnyType OutputType { get; set; }
    public int OrderNumber { get; set; }
    public int ParenthesesCount { get; set; }
    public object Value { get; }

    public Interval Interval { get; set; }

    // used in debug trace
    internal string ClrTypeName => Value?.GetType().Name;

    public IEnumerable<ISyntaxNode> Children => Array.Empty<ISyntaxNode>();
    public T Accept<T>(ISyntaxNodeVisitor<T> visitor) => visitor.Visit(this);
}
