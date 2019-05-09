using System.Collections.Generic;
using System.Linq;
using NFun.Tokenization;

namespace NFun.Parsing
{
    public class IfThenElseSyntaxNode : ISyntaxNode
    {
        public IfThenSyntaxNode[] Ifs { get; }
        public ISyntaxNode ElseExpr { get; }

        public IfThenElseSyntaxNode(IList<IfThenSyntaxNode> ifs, ISyntaxNode elseExpr, Interval interval)
        {
            Ifs = ifs.ToArray();
            ElseExpr = elseExpr;
            Interval = interval;
        }
        public bool IsInBrackets { get; set; }
        public SyntaxNodeType Type => SyntaxNodeType.IfThanElse;
        public Interval Interval { get; set; }
        public T Visit<T>(ISyntaxNodeVisitor<T> visitor) => visitor.Visit(this);

        public IEnumerable<ISyntaxNode> Children
        {
            get
            {
                foreach (var ifThenSyntaxNode in Ifs)
                {
                    yield return ifThenSyntaxNode;
                }
                yield return ElseExpr;
            }
        }

    }
}