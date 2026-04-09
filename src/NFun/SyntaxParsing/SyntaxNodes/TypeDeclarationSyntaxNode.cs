using System.Collections.Generic;
using System.Linq;
using NFun.SyntaxParsing.Visitors;
using NFun.Tokenization;

namespace NFun.SyntaxParsing.SyntaxNodes;

/// <summary>
/// Represents a named type declaration:
///   type name = {field defs}    — struct type (FieldDefinitions non-null)
///   type name = typeSyntax      — type alias (AliasTypeSyntax non-null)
/// </summary>
public class TypeDeclarationSyntaxNode : ISyntaxNode {
    public string TypeName { get; }
    /// <summary>Non-null for struct types: type name = {fields}</summary>
    public IReadOnlyList<TypeFieldDefinition> FieldDefinitions { get; }
    /// <summary>Non-null for type aliases: type name = int, type name = int[], etc.</summary>
    public TypeSyntax AliasTypeSyntax { get; }

    public bool IsAlias => AliasTypeSyntax is not null and not TypeSyntax.EmptyType;

    /// <summary>Struct type declaration</summary>
    public TypeDeclarationSyntaxNode(string typeName, List<TypeFieldDefinition> fields, Interval interval) {
        TypeName = typeName;
        FieldDefinitions = fields;
        AliasTypeSyntax = null;
        Interval = interval;
    }

    /// <summary>Type alias declaration</summary>
    public TypeDeclarationSyntaxNode(string typeName, TypeSyntax aliasType, Interval interval) {
        TypeName = typeName;
        FieldDefinitions = null;
        AliasTypeSyntax = aliasType;
        Interval = interval;
    }

    public FunnyType OutputType { get; set; }
    public int OrderNumber { get; set; }
    public int ParenthesesCount { get; set; }
    public Interval Interval { get; set; }
    public T Accept<T>(ISyntaxNodeVisitor<T> visitor) => visitor.Visit(this);

    public IEnumerable<ISyntaxNode> Children =>
        FieldDefinitions?.Where(f => f.DefaultValue != null).Select(f => f.DefaultValue)
        ?? Enumerable.Empty<ISyntaxNode>();
}

/// <summary>
/// A field in a named type definition.
/// Can be: name:type, name:type = default, or name = default (type inferred).
/// </summary>
public class TypeFieldDefinition {
    public string Name { get; }
    public TypeSyntax TypeSyntax { get; }
    public ISyntaxNode DefaultValue { get; }
    public Interval Interval { get; }

    public bool HasDefault => DefaultValue != null;
    public bool HasType => TypeSyntax is not TypeSyntax.EmptyType;

    public TypeFieldDefinition(string name, TypeSyntax typeSyntax, ISyntaxNode defaultValue, Interval interval) {
        Name = name.ToLower();
        TypeSyntax = typeSyntax;
        DefaultValue = defaultValue;
        Interval = interval;
    }
}
