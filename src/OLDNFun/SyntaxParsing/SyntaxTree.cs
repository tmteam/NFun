using System.Linq;
using NFun.Tokenization;

namespace NFun.Parsing
{
    public class SyntaxTree: ISyntaxNode
    {
        public ISyntaxNode[] Nodes { get; }

        public SyntaxTree(ISyntaxNode[] nodes)
        {
            Nodes = nodes;
            
        }

        public bool IsBracket
        {
            get { return false; }
            set => throw new System.NotImplementedException();
        }

        public LexNodeType Type => LexNodeType.SyntaxTree;
        public Interval Interval
        {
            get { return Interval.Unite(Nodes.First().Interval, Nodes.Last().Interval); }
            set => throw new System.NotImplementedException();
        }
    }
}