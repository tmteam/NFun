using System.Collections.Generic;
using NFun.SyntaxParsing.Visitors;
using NFun.Tokenization;
using NFun.Types;

namespace NFun.SyntaxParsing.SyntaxNodes
{
    public class ListOfExpressionsSyntaxNode : ISyntaxNode
    {
        public FunnyType OutputType { get; set; }
        public int OrderNumber { get; set; }

        public ISyntaxNode[] Expressions { get; }

        public ListOfExpressionsSyntaxNode(ISyntaxNode[] expressions,bool hasBrackets, Interval interval)
        {
            Expressions = expressions;
            IsInBrackets = hasBrackets;
            Interval = interval;
        }
        public bool IsInBrackets { get; set; }
        public Interval Interval { get; set; }
        public T Accept<T>(ISyntaxNodeVisitor<T> visitor) => visitor.Visit(this);
        public IEnumerable<ISyntaxNode> Children => Expressions;
    }
}