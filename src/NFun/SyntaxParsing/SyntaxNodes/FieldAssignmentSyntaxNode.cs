using System.Collections.Generic;
using NFun.SyntaxParsing.Visitors;
using NFun.Tokenization;

namespace NFun.SyntaxParsing.SyntaxNodes;

/// <summary>
/// Represents a field assignment statement: s.field = expr
/// Desugars to copy-on-write: evaluates expr, copies the struct with the field replaced,
/// and reassigns the variable.
/// </summary>
public class FieldAssignmentSyntaxNode : ISyntaxNode {
    public FieldAssignmentSyntaxNode(string variableName, string fieldName,
        ISyntaxNode source, ISyntaxNode value, Interval interval) {
        VariableName = variableName;
        FieldName = fieldName;
        Source = source;
        Value = value;
        Interval = interval;
    }

    /// <summary>The variable being mutated (e.g., "s" in s.field = expr)</summary>
    public string VariableName { get; }

    /// <summary>The field being assigned (e.g., "field" in s.field = expr)</summary>
    public string FieldName { get; }

    /// <summary>The struct source expression (reads the current value of the variable)</summary>
    public ISyntaxNode Source { get; }

    /// <summary>The value expression to assign to the field</summary>
    public ISyntaxNode Value { get; }

    public FunnyType OutputType { get; set; }
    public int OrderNumber { get; set; }
    public int ParenthesesCount { get; set; }
    public Interval Interval { get; set; }

    public T Accept<T>(ISyntaxNodeVisitor<T> visitor) => visitor.Visit(this);

    public IEnumerable<ISyntaxNode> Children => new[] { Source, Value };
}
