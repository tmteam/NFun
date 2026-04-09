using System.Collections.Generic;
using System.Linq;
using NFun.SyntaxParsing.Visitors;
using NFun.Tokenization;

namespace NFun.SyntaxParsing.SyntaxNodes;

/// <summary>
/// Represents a named type constructor call: Name{field = expr, ...}
/// This is desugared into a StructInitSyntaxNode during elaboration.
/// </summary>
public class NamedTypeConstructorSyntaxNode : ISyntaxNode {
    public string TypeName { get; }
    public Interval TypeNameInterval { get; }
    public IReadOnlyList<EquationSyntaxNode> ProvidedFields { get; }

    public NamedTypeConstructorSyntaxNode(
        string typeName, Interval typeNameInterval,
        List<EquationSyntaxNode> providedFields, Interval interval) {
        TypeName = typeName;
        TypeNameInterval = typeNameInterval;
        ProvidedFields = providedFields;
        Interval = interval;
    }

    public FunnyType OutputType { get; set; }
    public int OrderNumber { get; set; }
    public int ParenthesesCount { get; set; }
    public Interval Interval { get; set; }
    public T Accept<T>(ISyntaxNodeVisitor<T> visitor) => visitor.Visit(this);
    public IEnumerable<ISyntaxNode> Children => ProvidedFields.Select(f => f.Expression);
}
