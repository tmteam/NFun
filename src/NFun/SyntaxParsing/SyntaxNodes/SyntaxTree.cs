using System.Collections.Generic;
using System.Linq;
using NFun.SyntaxParsing.Visitors;
using NFun.Tokenization;
using NFun.Types;

namespace NFun.SyntaxParsing.SyntaxNodes {

public class SyntaxTree : ISyntaxNode {
    public FunnyType OutputType { get; set; }
    public int OrderNumber { get; set; }

    public ISyntaxNode[] Nodes { get; }

    public SyntaxTree(ISyntaxNode[] nodes) { Nodes = nodes; }

    public bool IsInBrackets
    {
        get => false;
        set => throw new System.NotImplementedException();
    }
    public Interval Interval
    {
        get
        {
            var start = Nodes.Select(i => i.Interval.Start).Min();
            var finish = Nodes.Select(i => i.Interval.Finish).Max();
            return new Interval(start, finish);
        }
        set => throw new System.NotImplementedException();
    }
    public T Accept<T>(ISyntaxNodeVisitor<T> visitor) => visitor.Visit(this);

    public IEnumerable<ISyntaxNode> Children => Nodes;
    public int MaxNodeId { get; set; } = -1;
}

}