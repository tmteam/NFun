using System.Collections.Generic;
using NFun.SyntaxParsing.Visitors;
using NFun.Tokenization;

namespace NFun.SyntaxParsing.SyntaxNodes;

public class TryCatchSyntaxNode : ISyntaxNode {
    public TryCatchSyntaxNode(ISyntaxNode tryExpr, ISyntaxNode catchExpr,
        string errorVariableName, Interval interval) {
        TryExpr = tryExpr;
        CatchExpr = catchExpr;
        ErrorVariableName = errorVariableName;
        Interval = interval;
    }

    public ISyntaxNode TryExpr { get; }
    public ISyntaxNode CatchExpr { get; }
    /// <summary>null for simple `catch expr`, non-null for `catch(e) expr`.</summary>
    public string ErrorVariableName { get; }
    public FunnyType OutputType { get; set; }
    public int OrderNumber { get; set; }
    public int ParenthesesCount { get; set; }
    public Interval Interval { get; set; }
    public T Accept<T>(ISyntaxNodeVisitor<T> visitor) => visitor.Visit(this);
    public IEnumerable<ISyntaxNode> Children {
        get {
            yield return TryExpr;
            yield return CatchExpr;
        }
    }
}
