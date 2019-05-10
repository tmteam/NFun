using System.Collections.Generic;
using NFun.SyntaxParsing.Visitors;
using NFun.Tokenization;

namespace NFun.Parsing
{
    public class ListOfExpressionsSyntaxNode : ISyntaxNode
    {
        public int NodeNumber { get; set; }

        public ISyntaxNode[] Expressions { get; }

        public ListOfExpressionsSyntaxNode(ISyntaxNode[] expressions,bool hasBrackets, Interval interval)
        {
            Expressions = expressions;
            IsInBrackets = hasBrackets;
            Interval = interval;
        }
        public bool IsInBrackets { get; set; }
        public SyntaxNodeType Type => SyntaxNodeType.ListOfExpressions;
        
        public Interval Interval { get; set; }
        public T Visit<T>(ISyntaxNodeVisitor<T> visitor) => visitor.Visit(this);
        public IEnumerable<ISyntaxNode> Children => Expressions;
    }
}