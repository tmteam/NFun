using System;
using System.Collections.Generic;
using System.Net;
using NFun.SyntaxParsing.Visitors;
using NFun.Tokenization;

namespace NFun.SyntaxParsing.SyntaxNodes;

public class IpAddressConstantSyntaxNode : ISyntaxNode {
    public IpAddressConstantSyntaxNode(IPAddress value, Interval interval) {
        Value = value;
        Interval = interval;
        OutputType = FunnyType.Ip;
    }
    public IPAddress Value { get; }
    public FunnyType OutputType { get; set; }
    public int OrderNumber { get; set; }
    public int BracketsCount { get; set; }
    public Interval Interval { get; set; }
    public IEnumerable<ISyntaxNode> Children => Array.Empty<ISyntaxNode>();
    public T Accept<T>(ISyntaxNodeVisitor<T> visitor) => visitor.Visit(this);
}