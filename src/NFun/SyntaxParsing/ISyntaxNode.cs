using NFun.Tokenization;

namespace NFun.Parsing
{
    public interface ISyntaxNode
    {
        bool IsInBrackets { get; set; }
        SyntaxNodeType Type { get; }
        Interval Interval { get; set; }
        T Visit<T>(ISyntaxNodeVisitor<T> visitor);
    }
}