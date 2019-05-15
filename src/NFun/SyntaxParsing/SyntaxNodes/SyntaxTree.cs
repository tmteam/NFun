using System.Collections.Generic;
using System.Linq;
using NFun.SyntaxParsing.Visitors;
using NFun.Tokenization;
using NFun.Types;

namespace NFun.Parsing
{
    public class SyntaxTree: ISyntaxNode
    {
        public VarType OutputType { get; set; }
        public int NodeNumber { get; set; }

        public ISyntaxNode[] Nodes { get; }

        public SyntaxTree(ISyntaxNode[] nodes)
        {
            Nodes = nodes;
            
        }

        public bool IsInBrackets
        {
            get { return false; }
            set => throw new System.NotImplementedException();
        }

        public SyntaxNodeType Type => SyntaxNodeType.SyntaxTree;
        public Interval Interval
        {
            get { return Interval.Unite(Nodes.First().Interval, Nodes.Last().Interval); }
            set => throw new System.NotImplementedException();
        }
        public T Visit<T>(ISyntaxNodeVisitor<T> visitor) => visitor.Visit(this);

        public IEnumerable<ISyntaxNode> Children => Nodes;
    }
}