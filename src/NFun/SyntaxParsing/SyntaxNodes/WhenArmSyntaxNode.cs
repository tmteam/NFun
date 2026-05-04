using System.Collections.Generic;
using NFun.SyntaxParsing.Visitors;
using NFun.Tokenization;

namespace NFun.SyntaxParsing.SyntaxNodes;

public class WhenArmSyntaxNode : ISyntaxNode {
    /// <summary>Value or boolean condition for this arm</summary>
    public ISyntaxNode Condition { get; }
    public ISyntaxNode Body { get; }

    public WhenArmSyntaxNode(ISyntaxNode condition, ISyntaxNode body, Interval interval) {
        Condition = condition;
        Body = body;
        Interval = interval;
    }

    public FunnyType OutputType { get; set; }
    public int OrderNumber { get; set; }
    public int ParenthesesCount { get; set; }
    public Interval Interval { get; set; }
    public T Accept<T>(ISyntaxNodeVisitor<T> visitor) => visitor.Visit(this);
    public IEnumerable<ISyntaxNode> Children {
        get {
            yield return Condition;
            yield return Body;
        }
    }
}
