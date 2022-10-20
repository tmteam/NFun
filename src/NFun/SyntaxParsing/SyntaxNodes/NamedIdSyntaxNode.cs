using System;
using System.Collections.Generic;
using NFun.Interpretation.Functions;
using NFun.SyntaxParsing.Visitors;
using NFun.Tic.SolvingStates;
using NFun.Tokenization;

namespace NFun.SyntaxParsing.SyntaxNodes; 

public enum NamedIdNodeType {
    Unknown,
    Variable,
    GenericFunction,
    ConcreteFunction,
    Constant
}

/// <summary>
/// Variable or constant or function
/// </summary>
public class NamedIdSyntaxNode : ISyntaxNode {
    public NamedIdSyntaxNode(string id, Interval interval) {
        Id = id;
        Interval = interval;
    }
    
    /// <summary>
    /// type of id. Constant, variable or function
    /// </summary>
    public NamedIdNodeType IdType { get; set; } = NamedIdNodeType.Unknown;
    /// <summary>
    /// Content of id. Variable source, Function signature, or Constant value
    /// </summary>
    public object IdContent { get; set; }
    public FunnyType OutputType { get; set; }
    public FunnyType VariableType { get; set; }
    public int OrderNumber { get; set; }
    public int ParenthesesCount { get; set; }
    public string Id { get; }
    public Interval Interval { get; set; }
    public T Accept<T>(ISyntaxNodeVisitor<T> visitor) => visitor.Visit(this);
    public IEnumerable<ISyntaxNode> Children => Array.Empty<ISyntaxNode>();
    public override string ToString() => $"({OrderNumber}) {Id}:{OutputType}";
}