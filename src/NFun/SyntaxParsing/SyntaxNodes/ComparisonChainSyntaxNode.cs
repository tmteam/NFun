namespace NFun.SyntaxParsing.SyntaxNodes;

using System.Collections.Generic;
using Tokenization;
using Visitors;

public class ComparisonChainSyntaxNode : ISyntaxNode {
    public ComparisonChainSyntaxNode(IList<ISyntaxNode> operands, IList<Tok> operators) {
        Operands = operands;
        Operators = operators;
        Interval = new Interval(operators[0].Start, operators[^1].Finish);
    }
    public IList<ISyntaxNode> Operands { get; }
    public IList<Tok> Operators { get; }
    public FunnyType OutputType { get; set; } = FunnyType.Bool;
    public int OrderNumber { get; set; }
    public int ParenthesesCount { get; set; }
    public Interval Interval { get; set; }
    public T Accept<T>(ISyntaxNodeVisitor<T> visitor) => visitor.Visit(this);

    public IEnumerable<ISyntaxNode> Children => Operands;
}
