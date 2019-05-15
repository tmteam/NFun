using System.Collections.Generic;
using NFun.SyntaxParsing.Visitors;
using NFun.Tokenization;
using NFun.Types;

namespace NFun.Parsing
{
    public interface ISyntaxNode
    {    
        VarType OutputType { get; set; }
        int NodeNumber { get; set; }
        bool IsInBrackets { get; set; }
        SyntaxNodeType Type { get; }
        Interval Interval { get; set; }
        T Visit<T>(ISyntaxNodeVisitor<T> visitor);
        IEnumerable<ISyntaxNode> Children { get; }
    }
}