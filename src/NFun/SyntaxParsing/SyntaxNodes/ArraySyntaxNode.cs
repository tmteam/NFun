using System.Collections.Generic;
using NFun.SyntaxParsing.Visitors;
using NFun.Tokenization;

namespace NFun.SyntaxParsing.SyntaxNodes;

/// <summary>
/// Bracket literal — `[a, b, c]`. The container kind (array vs list) is
/// stamped onto <see cref="Kind"/> by <c>TicSetupVisitor.Visit</c> based on
/// the active dialect, becoming the single source of truth for downstream
/// passes (TIC constraint setup AND `ExpressionBuilderVisitor.Visit`).
/// </summary>
public class ArraySyntaxNode : ISyntaxNode {
    public ArraySyntaxNode(IList<ISyntaxNode> expressions, Interval interval) {
        Expressions = expressions;
        Interval = interval;
    }

    public FunnyType OutputType { get; set; }
    public int OrderNumber { get; set; }
    public int ParenthesesCount { get; set; }
    public IList<ISyntaxNode> Expressions { get; }
    public Interval Interval { get; set; }
    /// <summary>
    /// Container kind for this literal — set by <c>TicSetupVisitor</c> based on
    /// the active dialect. Both TIC constraint setup and runtime expression
    /// building read this single field instead of redoing the dialect check.
    /// </summary>
    public ArrayLiteralKind Kind { get; set; }
    public T Accept<T>(ISyntaxNodeVisitor<T> visitor) => visitor.Visit(this);
    public IEnumerable<ISyntaxNode> Children => Expressions;
}

public enum ArrayLiteralKind {
    /// <summary>Default — ee-mode immutable covariant `T[]` (StateArray).</summary>
    Array = 0,
    /// <summary>Lang-mode mutable invariant `list&lt;T&gt;` (StateCollection.List).</summary>
    List = 1,
}
