using System.Collections.Generic;
using NFun.Tokenization;

namespace NFun.Parsing
{
    public class IfThenSyntaxNode : ISyntaxNode
    {
        public ISyntaxNode Condition { get; }
        public ISyntaxNode Expr { get; }

        public IfThenSyntaxNode(ISyntaxNode condition, ISyntaxNode expr, Interval interval)
        {
            Condition = condition;
            Expr = expr;
            Interval = interval;
        }
        public bool IsInBrackets { get; set; }
        public SyntaxNodeType Type => SyntaxNodeType.IfThen;
        public Interval Interval { get; set; }

        public T Visit<T>(ISyntaxNodeVisitor<T> visitor) => visitor.Visit(this);
        public IEnumerable<ISyntaxNode> Children => new[] {Condition};
    }
}