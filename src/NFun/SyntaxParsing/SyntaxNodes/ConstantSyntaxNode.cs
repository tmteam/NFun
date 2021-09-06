using System;
using System.Collections.Generic;
using NFun.SyntaxParsing.Visitors;
using NFun.Tokenization;
using NFun.Types;

namespace NFun.SyntaxParsing.SyntaxNodes {

public class ConstantSyntaxNode : ISyntaxNode {
    public FunnyType OutputType { get; set; }
    public int OrderNumber { get; set; }

    public ConstantSyntaxNode(object value, FunnyType funnyType, Interval interval) {
        OutputType = funnyType;
        Interval = interval;
        Value = value;
    }

    public string ClrTypeName => Value?.GetType().Name;
    public bool IsInBrackets { get; set; }
    public object Value { get; }
    public Interval Interval { get; set; }
    public IEnumerable<ISyntaxNode> Children => Array.Empty<ISyntaxNode>();
    public T Accept<T>(ISyntaxNodeVisitor<T> visitor) => visitor.Visit(this);
}

}