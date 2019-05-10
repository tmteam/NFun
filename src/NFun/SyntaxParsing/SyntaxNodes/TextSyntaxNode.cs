using System.Collections.Generic;
using NFun.SyntaxParsing.Visitors;
using NFun.Tokenization;

namespace NFun.Parsing
{
    public class TextSyntaxNode : ISyntaxNode
    {
        public int NodeNumber { get; set; }

        public TextSyntaxNode (string value, Interval interval)
        {
            Value = value;
            Interval = interval;
        }
        public bool IsInBrackets { get; set; }
        public SyntaxNodeType Type => SyntaxNodeType.Text;
        public string Value { get; }
        public Interval Interval { get; set; }
        public T Visit<T>(ISyntaxNodeVisitor<T> visitor) => visitor.Visit(this);
        
        public IEnumerable<ISyntaxNode> Children => new ISyntaxNode[0];
    }
}