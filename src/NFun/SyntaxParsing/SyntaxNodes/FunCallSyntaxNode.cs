using System;
using System.Collections.Generic;
using System.Linq;
using NFun.Interpretation.Functions;
using NFun.SyntaxParsing.Visitors;
using NFun.Tokenization;

namespace NFun.SyntaxParsing.SyntaxNodes;

/// <summary>Named argument in a function call: name = value</summary>
public readonly struct NamedCallArgument {
    public string Name { get; }
    public ISyntaxNode Value { get; }
    public Interval NameInterval { get; }

    public NamedCallArgument(string name, ISyntaxNode value, Interval nameInterval) {
        Name = name;
        Value = value;
        NameInterval = nameInterval;
    }
}

public interface IFunCallSyntaxNode : ISyntaxNode {
    ISyntaxNode[] Args { get; }
}

public class FunCallSyntaxNode : IFunCallSyntaxNode {
    public FunCallSyntaxNode(
        string id,
        ISyntaxNode[] args,
        Interval interval,
        bool isPipeForward,
        bool isOperator = false,
        NamedCallArgument[] namedArgs = null,
        int keywordOnlyNamedStartIndex = -1)
    {
        Id = id;
        Args = args;
        Interval = interval;
        IsPipeForward = isPipeForward;
        IsOperator = isOperator;
        NamedArgs = namedArgs ?? Array.Empty<NamedCallArgument>();
        KeywordOnlyNamedStartIndex = keywordOnlyNamedStartIndex >= 0
            ? keywordOnlyNamedStartIndex
            : NamedArgs.Length; // default: no keyword-only args
    }

    public FunnyType OutputType { get; set; }
    public int OrderNumber { get; set; }
    public int ParenthesesCount { get; set; }
    public string Id { get; }

    /// <summary>Positional arguments (in order)</summary>
    public ISyntaxNode[] Args { get; }

    /// <summary>Named arguments. Empty if none.</summary>
    public NamedCallArgument[] NamedArgs { get; }

    public bool HasNamedArgs => NamedArgs.Length > 0;

    /// <summary>
    /// Index into NamedArgs where keyword-only args start (args after ... in definition).
    /// Equals NamedArgs.Length if no keyword-only args.
    /// </summary>
    public int KeywordOnlyNamedStartIndex { get; }

    public Interval Interval { get; set; }
    public bool IsOperator { get; }

    /// <summary>Resolved function signature, set during TIC setup. Avoids dict lookup in ExpressionBuilder.</summary>
    public IFunctionSignature ResolvedSignature { get; set; }

    public T Accept<T>(ISyntaxNodeVisitor<T> visitor) => visitor.Visit(this);
    public IEnumerable<ISyntaxNode> Children =>
        HasNamedArgs ? Args.Concat(NamedArgs.Select(n => n.Value)).ToArray() : Args;
    public bool IsPipeForward { get; }
}
