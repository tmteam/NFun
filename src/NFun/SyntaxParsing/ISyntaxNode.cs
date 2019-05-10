using System.Collections.Generic;
using NFun.SyntaxParsing.Visitors;
using NFun.Tokenization;

namespace NFun.Parsing
{
    public interface ISyntaxNode
    {
        int NodeNumber { get; set; }
        bool IsInBrackets { get; set; }
        SyntaxNodeType Type { get; }
        Interval Interval { get; set; }
        T Visit<T>(ISyntaxNodeVisitor<T> visitor);
        IEnumerable<ISyntaxNode> Children { get; }
    }
}