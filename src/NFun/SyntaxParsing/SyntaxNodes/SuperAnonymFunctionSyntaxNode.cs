using System.Collections.Generic;
using NFun.SyntaxParsing.Visitors;
using NFun.Tokenization;

namespace NFun.SyntaxParsing.SyntaxNodes; 

public class SuperAnonymFunctionSyntaxNode : ISyntaxNode {
    public SuperAnonymFunctionSyntaxNode(ISyntaxNode body) {
        Body = body;
        Interval = body.Interval;
    }
    public ISyntaxNode Body { get; }
    public FunnyType OutputType { get; set; }
    public int OrderNumber { get; set; }
    public int BracketsCount { get; set; }
    public Interval Interval { get; set; }
    public T Accept<T>(ISyntaxNodeVisitor<T> visitor) => visitor.Visit(this);
    public IEnumerable<ISyntaxNode> Children => new[] { Body };
}