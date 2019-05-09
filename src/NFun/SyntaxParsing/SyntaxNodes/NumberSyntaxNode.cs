using System.Collections.Generic;
using NFun.Tokenization;

namespace NFun.Parsing
{
    public class NumberSyntaxNode : ISyntaxNode
    {
        public NumberSyntaxNode(string value, Interval interval)
        {
            Value = value;
            Interval = interval;
        }

        public bool IsInBrackets { get; set; }
        public SyntaxNodeType Type => SyntaxNodeType.Number;
        public string Value { get; }
        public Interval Interval { get; set; }
        public T Visit<T>(ISyntaxNodeVisitor<T> visitor) => visitor.Visit(this);
        public IEnumerable<ISyntaxNode> Children => new ISyntaxNode[0];
    }
}