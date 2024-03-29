using System;
using System.Collections.Generic;
using NFun.SyntaxParsing.Visitors;
using NFun.Tokenization;

namespace NFun.SyntaxParsing.SyntaxNodes; 

//todo unite with TypedVarDefSyntaxNode
public class VarDefinitionSyntaxNode : ISyntaxNode {
    public FunnyType OutputType { get; set; }
    public int OrderNumber { get; set; }
    public int ParenthesesCount { get; set; }
    public string Id { get; }
    public FunnyType FunnyType { get; }
    public FunnyAttribute[] Attributes { get; }
    public Interval Interval { get; set; }
    public override string ToString() => Id + ":" + OutputType;
    public T Accept<T>(ISyntaxNodeVisitor<T> visitor) => visitor.Visit(this);
    public IEnumerable<ISyntaxNode> Children => Array.Empty<ISyntaxNode>();

    public VarDefinitionSyntaxNode(TypedVarDefSyntaxNode node, FunnyAttribute[] attributes = null) {
        Id = node.Id;
        FunnyType = node.FunnyType;
        ParenthesesCount = node.ParenthesesCount;
        Attributes = attributes ?? Array.Empty<FunnyAttribute>();
        Interval = node.Interval;
    }
}