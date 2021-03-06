using System.Collections.Generic;
using NFun.Interpretation.Functions;
using NFun.SyntaxParsing.Visitors;
using NFun.Tokenization;
using NFun.Types;

namespace NFun.SyntaxParsing.SyntaxNodes
{
    public interface IFunCallSyntaxNode : ISyntaxNode
    {
         ISyntaxNode[] Args { get; }
    }
    public class FunCallSyntaxNode: IFunCallSyntaxNode
    {
        public FunCallSyntaxNode(string id, ISyntaxNode[] args, Interval interval, bool isOperator = false)
        {
            Id = id;
            Args = args;
            Interval = interval;
            IsOperator = isOperator;
        }
        public FunnyType OutputType { get; set; }
        public int OrderNumber { get; set; }

        public bool IsInBrackets { get; set; }
        public string Id { get; }
        public ISyntaxNode[] Args { get; }
        public IFunctionSignature FunctionSignature { get; set; } = null;
        public Interval Interval { get; set; }
        public bool IsOperator { get; }
        public T Accept<T>(ISyntaxNodeVisitor<T> visitor) => visitor.Visit(this);
        public IEnumerable<ISyntaxNode> Children => Args;
        
    }
}