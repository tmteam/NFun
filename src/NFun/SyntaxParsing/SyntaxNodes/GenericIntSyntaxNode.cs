using System;
using System.Collections.Generic;
using NFun.SyntaxParsing.Visitors;
using NFun.Tokenization;

namespace NFun.SyntaxParsing.SyntaxNodes; 

public class GenericIntSyntaxNode : ISyntaxNode {
    public GenericIntSyntaxNode(object value, bool isHexOrBin, Interval interval) {
        Interval = interval;
        Value = value;
        IsHexOrBin = isHexOrBin;
    }
    public FunnyType OutputType { get; set; }
    public int OrderNumber { get; set; }
    public int BracketsCount { get; set; }
    public object Value { get; }
    public bool IsHexOrBin { get; }
    public Interval Interval { get; set; }
    public IEnumerable<ISyntaxNode> Children => Array.Empty<ISyntaxNode>();
    public T Accept<T>(ISyntaxNodeVisitor<T> visitor) => visitor.Visit(this);
}