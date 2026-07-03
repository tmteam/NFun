using System.Collections.Generic;
using NFun.SyntaxParsing.Visitors;
using NFun.Tokenization;

namespace NFun.SyntaxParsing.SyntaxNodes;

public class ForSyntaxNode : ISyntaxNode {
    public string IteratorName { get; }
    public ISyntaxNode Collection { get; }
    public ISyntaxNode Body { get; }

    public ForSyntaxNode(string iteratorName, ISyntaxNode collection, ISyntaxNode body, Interval interval) {
        IteratorName = iteratorName;
        Collection = collection;
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
            yield return Collection;
            yield return Body;
        }
    }
}
