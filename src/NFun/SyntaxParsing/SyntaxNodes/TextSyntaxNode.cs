using NFun.Tokenization;

namespace NFun.Parsing
{
    public class TextSyntaxNode : ISyntaxNode
    {
        public TextSyntaxNode (string value, Interval interval)
        {
            Value = value;
            Interval = interval;
        }
        public bool IsInBrackets { get; set; }
        public SyntaxNodeType Type => SyntaxNodeType.Text;
        public string Value { get; }
        public Interval Interval { get; set; }
        public T Visit<T>(ISyntaxNodeVisitor<T> visitor) => visitor.Visit(this);    }
}