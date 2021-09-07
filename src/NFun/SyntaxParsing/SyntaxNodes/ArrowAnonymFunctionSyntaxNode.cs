using System.Collections.Generic;
using System.Linq;
using NFun.SyntaxParsing.Visitors;
using NFun.Tokenization;
using NFun.Types;

namespace NFun.SyntaxParsing.SyntaxNodes {

public class ArrowAnonymFunctionSyntaxNode : ISyntaxNode {
    public ISyntaxNode[] ArgumentsDefinition { get; }
    public ISyntaxNode Definition { get; }
    public ISyntaxNode Body { get; }
    /// <summary>
    /// Return type of anonymous function
    /// </summary>
    public FunnyType ReturnType { get; }
    public FunnyType OutputType { get; set; }
    public int OrderNumber { get; set; }
    public bool IsInBrackets { get; set; }
    public Interval Interval { get; set; }
    public T Accept<T>(ISyntaxNodeVisitor<T> visitor) => visitor.Visit(this);
    public IEnumerable<ISyntaxNode> Children => ArgumentsDefinition.Append(Body);

    public ArrowAnonymFunctionSyntaxNode(
        ISyntaxNode definition, ISyntaxNode body, FunnyType returnType, Interval interval) {
        if (definition is ListOfExpressionsSyntaxNode list)
            //it can be comlex: (x1,x2,x3)=>...
            ArgumentsDefinition = list.Expressions;
        else
            //or primitive: x1 => ...
            ArgumentsDefinition = new[] { definition };

        Definition = definition;
        Body = body;
        ReturnType = returnType;
        Interval = interval;
    }
}

}