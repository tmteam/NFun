using System.Collections.Generic;
using NFun.SyntaxParsing.Visitors;
using NFun.Tokenization;

namespace NFun.SyntaxParsing.SyntaxNodes;

/// <summary>
/// Multiline if-elif-else block (statement form with indent-based bodies).
/// </summary>
public class IfBlockSyntaxNode : ISyntaxNode {
    public IfCaseSyntaxNode[] Ifs { get; }
    /// <summary>null if no else clause</summary>
    public ISyntaxNode ElseBody { get; }

    public IfBlockSyntaxNode(IfCaseSyntaxNode[] ifs, ISyntaxNode elseBody, Interval interval) {
        Ifs = ifs;
        ElseBody = elseBody;
        Interval = interval;
    }

    public FunnyType OutputType { get; set; }
    public int OrderNumber { get; set; }
    public int ParenthesesCount { get; set; }
    public Interval Interval { get; set; }
    public T Accept<T>(ISyntaxNodeVisitor<T> visitor) => visitor.Visit(this);
    public IEnumerable<ISyntaxNode> Children {
        get {
            foreach (var ifCase in Ifs) yield return ifCase;
            if (ElseBody != null) yield return ElseBody;
        }
    }
}
