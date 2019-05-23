using System.Collections.Generic;
using NFun.SyntaxParsing.Visitors;
using NFun.Tokenization;
using NFun.Types;

namespace NFun.SyntaxParsing.SyntaxNodes
{
    public class IfCaseSyntaxNode : ISyntaxNode
    {
        public VarType OutputType { get; set; }
        public int NodeNumber { get; set; }

        public ISyntaxNode Condition { get; }
        public ISyntaxNode Expression { get; }

        public IfCaseSyntaxNode(ISyntaxNode condition, ISyntaxNode expression, Interval interval)
        {
            Condition = condition;
            Expression = expression;
            Interval = interval;
        }
        public bool IsInBrackets { get; set; }
        public Interval Interval { get; set; }

        public T Visit<T>(ISyntaxNodeVisitor<T> visitor) => visitor.Visit(this);
        public IEnumerable<ISyntaxNode> Children => new[] { Condition, Expression};
    }
}