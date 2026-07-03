using System;
using System.Collections.Generic;
using NFun.SyntaxParsing.Visitors;
using NFun.Tokenization;

namespace NFun.SyntaxParsing.SyntaxNodes;

public class DefaultValueSyntaxNode : ISyntaxNode {
    public DefaultValueSyntaxNode(Interval interval) => Interval = interval;
    public FunnyType OutputType { get; set; }
    public int OrderNumber { get; set; }
    public int ParenthesesCount { get; set; }
    public Interval Interval { get; set; }
    /// <summary>
    /// True for a sentinel inserted by the lang-mode parser as a stand-in for
    /// a missing `else` clause. Distinguishes the "no else" structural form
    /// (statement `if cond: ...` with no else block) from a user-written
    /// `else default` expression. Validators that flag "if used as expression
    /// without else" must check this flag — checking just the node type
    /// rejects the spec'd `if(x) a else default` pattern. (MR11Bug2.)
    /// </summary>
    public bool IsAutoInsertedElse { get; init; }
    public T Accept<T>(ISyntaxNodeVisitor<T> visitor) => visitor.Visit(this);
    public IEnumerable<ISyntaxNode> Children => Array.Empty<ISyntaxNode>();
    public override string ToString() => "default";
}
