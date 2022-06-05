using System.Collections.Generic;
using NFun.SyntaxParsing.Visitors;
using NFun.Tokenization;

namespace NFun.SyntaxParsing.SyntaxNodes; 

public class ListOfExpressionsSyntaxNode : ISyntaxNode {
    public ListOfExpressionsSyntaxNode(IList<ISyntaxNode> expressions, int bracketsCount, Interval interval) {
        Expressions = expressions;
        BracketsCount = bracketsCount;
        Interval = interval;
    }
    public FunnyType OutputType { get; set; }
    public int OrderNumber { get; set; }
    public int BracketsCount { get; set; }
    public IList<ISyntaxNode> Expressions { get; }
    public Interval Interval { get; set; }
    public T Accept<T>(ISyntaxNodeVisitor<T> visitor) => visitor.Visit(this);
    public IEnumerable<ISyntaxNode> Children => Expressions;
}