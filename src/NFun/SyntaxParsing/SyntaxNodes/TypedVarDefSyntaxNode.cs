using System;
using System.Collections.Generic;
using NFun.SyntaxParsing.Visitors;
using NFun.Tokenization;

namespace NFun.SyntaxParsing.SyntaxNodes;

public class TypedVarDefSyntaxNode : ISyntaxNode {
    public TypedVarDefSyntaxNode(string id, TypeSyntax typeSyntax, Interval interval,
        ISyntaxNode defaultValue = null, bool isParams = false) {
        Id = id;
        TypeSyntax = typeSyntax;
        Interval = interval;
        DefaultValue = defaultValue;
        IsParams = isParams;
    }

    public int OrderNumber { get; set; }
    public int ParenthesesCount { get; set; }
    public FunnyType OutputType { get; set; }
    public string Id { get; }
    public TypeSyntax TypeSyntax { get; }

    /// <summary>Default value expression. Null if no default.</summary>
    public ISyntaxNode DefaultValue { get; }
    public bool HasDefault => DefaultValue != null;

    /// <summary>Whether this is a varargs/params parameter (prefixed with ...)</summary>
    public bool IsParams { get; }

    /// <summary>
    /// Precomputed default value and its type (evaluated at function compilation time).
    /// Used when parameter has type annotation and default expression resolves
    /// to a different preferred type (e.g. 10→Real but param says Int32).
    /// </summary>
    public object PrecomputedDefaultValue { get; set; }
    public FunnyType PrecomputedDefaultType { get; set; }

    public Interval Interval { get; set; }
    public T Accept<T>(ISyntaxNodeVisitor<T> visitor) => visitor.Visit(this);
    public IEnumerable<ISyntaxNode> Children =>
        DefaultValue != null ? new[] { DefaultValue } : Array.Empty<ISyntaxNode>();
    public override string ToString() =>
        (IsParams ? "..." : "") + Id + (TypeSyntax is TypeSyntax.EmptyType ? "" : ":" + TypeSyntax)
        + (HasDefault ? "=" + DefaultValue : "");
}
