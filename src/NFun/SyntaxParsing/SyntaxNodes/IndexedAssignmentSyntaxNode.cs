using System.Collections.Generic;
using NFun.SyntaxParsing.Visitors;
using NFun.Tokenization;

namespace NFun.SyntaxParsing.SyntaxNodes;

/// <summary>
/// Indexed-assignment statement: <c>a[i] = expr</c>. Lang-mode only — the
/// container type must satisfy the <c>IndexedMutable&lt;T&gt;</c> shape (today
/// only <see cref="NFun.Runtime.Lists.MutableFunnyList"/> qualifies; ee-mode
/// arrays and future <c>fixedArray</c> are rejected at TIC).
///
/// Mirrors <see cref="FieldAssignmentSyntaxNode"/>'s shape: the parser wraps
/// this node in an <c>Equation</c> so the surrounding mutable-variable
/// machinery (BuildEquationForLang's reassignment path) carries the in-place
/// mutation through.
/// </summary>
public class IndexedAssignmentSyntaxNode : ISyntaxNode {
    public IndexedAssignmentSyntaxNode(ISyntaxNode target, ISyntaxNode index, ISyntaxNode value, Interval interval) {
        Target = target;
        Index = index;
        Value = value;
        Interval = interval;
    }

    /// <summary>The container expression being mutated (`a` in `a[i] = expr`).</summary>
    public ISyntaxNode Target { get; }

    /// <summary>The index expression (`i` in `a[i] = expr`).</summary>
    public ISyntaxNode Index { get; }

    /// <summary>The value expression assigned at the index.</summary>
    public ISyntaxNode Value { get; }

    public FunnyType OutputType { get; set; }
    public int OrderNumber { get; set; }
    public int ParenthesesCount { get; set; }
    public Interval Interval { get; set; }

    public T Accept<T>(ISyntaxNodeVisitor<T> visitor) => visitor.Visit(this);

    public IEnumerable<ISyntaxNode> Children => new[] { Target, Index, Value };
}
