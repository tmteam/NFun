using System.Collections.Generic;
using System.Linq;
using NFun.SyntaxParsing.Visitors;
using NFun.Tokenization;

namespace NFun.SyntaxParsing.SyntaxNodes; 

public class SyntaxTree : ISyntaxNode {
    public SyntaxTree(ISyntaxNode[] nodes) { Nodes = nodes; }

    public FunnyType OutputType { get; set; }
    public int OrderNumber { get; set; }
    public int BracketsCount
    {
        get => 0;
        set => throw new System.InvalidOperationException();
    }
    
    public ISyntaxNode[] Nodes { get; }
    
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