using System.Collections.Generic;
using NFun.SyntaxParsing.Visitors;
using NFun.Tokenization;

namespace NFun.SyntaxParsing.SyntaxNodes; 

public class StructFieldAccessSyntaxNode : ISyntaxNode {
    public StructFieldAccessSyntaxNode(ISyntaxNode source, string fieldName, Interval interval) {
        FieldName = fieldName;
        Source = source;
        Interval = interval;
    }
    public FunnyType OutputType { get; set; }
    public string FieldName { get; }
    public ISyntaxNode Source { get; }
    public int OrderNumber { get; set; }
    public int ParenthesesCount { get; set; }
    public Interval Interval { get; set; }
    public T Accept<T>(ISyntaxNodeVisitor<T> visitor) => visitor.Visit(this);
    public IEnumerable<ISyntaxNode> Children => new[] { Source };
}