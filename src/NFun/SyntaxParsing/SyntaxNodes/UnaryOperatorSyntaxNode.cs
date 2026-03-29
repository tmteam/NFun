using System.Collections.Generic;
using NFun.Functions;
using NFun.Interpretation.Functions;
using NFun.SyntaxParsing.Visitors;
using NFun.Tokenization;

namespace NFun.SyntaxParsing.SyntaxNodes;

public class UnaryOperatorSyntaxNode : IFunCallSyntaxNode {
    public UnaryOperatorSyntaxNode(UnOp op, ISyntaxNode operand, Interval interval) {
        Op = op;
        Operand = operand;
        Interval = interval;
    }

    public UnOp Op { get; }
    public string Id => OperatorEnumHelper.ToName(Op);
    public ISyntaxNode Operand { get; }

    public FunnyType OutputType { get; set; }
    public int OrderNumber { get; set; }
    public int ParenthesesCount { get; set; }
    public Interval Interval { get; set; }

    /// <summary>Resolved function signature, set during TIC setup.</summary>
    public IFunctionSignature ResolvedSignature { get; set; }

    // IFunCallSyntaxNode — cold path
    ISyntaxNode[] IFunCallSyntaxNode.Args => new[] { Operand };

    public T Accept<T>(ISyntaxNodeVisitor<T> visitor) => visitor.Visit(this);

    private ISyntaxNode[] _children;
    public IEnumerable<ISyntaxNode> Children => _children ??= new ISyntaxNode[] { Operand };
}
