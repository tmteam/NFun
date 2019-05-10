using System.Collections.Generic;
using NFun.SyntaxParsing.Visitors;
using NFun.Tokenization;

namespace NFun.Parsing
{
    public class ArraySyntaxNode : ISyntaxNode
    {
        public int NodeNumber { get; set; }

        public ISyntaxNode[] Expressions { get; }

        public ArraySyntaxNode(ISyntaxNode[] expressions, Interval interval)
        {
            Expressions = expressions;
            Interval = interval;
        }
        public bool IsInBrackets { get; set; }
        public SyntaxNodeType Type { get; }
        public Interval Interval { get; set; }
        public T Visit<T>(ISyntaxNodeVisitor<T> visitor) => visitor.Visit(this);
        public IEnumerable<ISyntaxNode> Children => Expressions;

        
    }
}