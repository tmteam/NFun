using System;
using System.Collections.Generic;
using NFun.SyntaxParsing.Visitors;
using NFun.Tokenization;

namespace NFun.SyntaxParsing.SyntaxNodes;

public class BlockSyntaxNode : ISyntaxNode {
    public BlockSyntaxNode(IReadOnlyList<ISyntaxNode> statements, Interval interval) {
        Statements = statements;
        Interval = interval;
    }

    public IReadOnlyList<ISyntaxNode> Statements { get; }
    public FunnyType OutputType { get; set; }
    public int OrderNumber { get; set; }
    public int ParenthesesCount { get; set; }
    public Interval Interval { get; set; }
    public IEnumerable<ISyntaxNode> Children => Statements;
    public T Accept<T>(ISyntaxNodeVisitor<T> visitor) => visitor.Visit(this);
}
