using System.Collections.Generic;
using NFun.SyntaxParsing.Visitors;
using NFun.Tokenization;

namespace NFun.SyntaxParsing.SyntaxNodes;

/// <summary>
/// When (pattern matching). Two forms:
/// 1. Value-based: when subject: value1: body1 ...
/// 2. Condition-based: when: cond1: body1 ...
/// </summary>
public class WhenSyntaxNode : ISyntaxNode {
    /// <summary>null for condition-based when</summary>
    public ISyntaxNode Subject { get; }
    public WhenArmSyntaxNode[] Arms { get; }
    /// <summary>null if no else</summary>
    public ISyntaxNode ElseBody { get; }

    public WhenSyntaxNode(ISyntaxNode subject, WhenArmSyntaxNode[] arms, ISyntaxNode elseBody, Interval interval) {
        Subject = subject;
        Arms = arms;
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
            if (Subject != null) yield return Subject;
            foreach (var arm in Arms) yield return arm;
            if (ElseBody != null) yield return ElseBody;
        }
    }
}
