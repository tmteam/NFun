using System.Collections.Generic;
using NFun.Functions;
using NFun.Interpretation.Functions;
using NFun.SyntaxParsing.Visitors;
using NFun.Tokenization;

namespace NFun.SyntaxParsing.SyntaxNodes;

public class BinOperatorSyntaxNode : IFunCallSyntaxNode {
    public BinOperatorSyntaxNode(BinOp op, ISyntaxNode left, ISyntaxNode right, Interval interval) {
        Op = op;
        Left = left;
        Right = right;
        Interval = interval;
    }

    public BinOp Op { get; }
    public string Id => OperatorEnumHelper.ToName(Op);
    public ISyntaxNode Left { get; }
    public ISyntaxNode Right { get; }

    public FunnyType OutputType { get; set; }
    public int OrderNumber { get; set; }
    public int ParenthesesCount { get; set; }
    public Interval Interval { get; set; }

    /// <summary>Resolved function signature, set during TIC setup.</summary>
    public IFunctionSignature ResolvedSignature { get; set; }

    // IFunCallSyntaxNode — cold path (error handling, CreateFunctionCall)
    ISyntaxNode[] IFunCallSyntaxNode.Args => new[] { Left, Right };

    public T Accept<T>(ISyntaxNodeVisitor<T> visitor) => visitor.Visit(this);

    private ISyntaxNode[] _children;
    public IEnumerable<ISyntaxNode> Children => _children ??= new ISyntaxNode[] { Left, Right };
}
