using System.Collections.Generic;
using NFun.SyntaxParsing.Visitors;
using NFun.Tokenization;

namespace NFun.SyntaxParsing.SyntaxNodes; 

public class IfThenElseSyntaxNode : ISyntaxNode {
    public IfThenElseSyntaxNode(IfCaseSyntaxNode[] ifs, ISyntaxNode elseExpr, Interval interval) {
        Ifs = ifs;
        ElseExpr = elseExpr;
        Interval = interval;
    }
    public FunnyType OutputType { get; set; }
    public int OrderNumber { get; set; }
    public IfCaseSyntaxNode[] Ifs { get; }
    public ISyntaxNode ElseExpr { get; }
    public bool IsInBrackets { get; set; }
    public Interval Interval { get; set; }
    public T Accept<T>(ISyntaxNodeVisitor<T> visitor) => visitor.Visit(this);
    public IEnumerable<ISyntaxNode> Children
    {
        get
        {
            foreach (var ifThenSyntaxNode in Ifs)
                yield return ifThenSyntaxNode;
            yield return ElseExpr;
        }
    }
}