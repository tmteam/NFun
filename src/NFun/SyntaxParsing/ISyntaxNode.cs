using System.Collections.Generic;
using NFun.SyntaxParsing.Visitors;
using NFun.Tokenization;

namespace NFun.SyntaxParsing; 

public interface ISyntaxNode {
    FunnyType OutputType { get; set; }
    int OrderNumber { get; set; }
    /// <summary>
    /// The number of parenthesis in which the node is wrapped
    /// </summary>
    int ParenthesesCount { get; set; }
    Interval Interval { get; set; }
    T Accept<T>(ISyntaxNodeVisitor<T> visitor);
    IEnumerable<ISyntaxNode> Children { get; }
}