using System.Collections.Generic;
using NFun.SyntaxParsing.SyntaxNodes;
using NFun.Tokenization;
using NFun.Types;

namespace NFun.SyntaxParsing
{
    public static class SyntaxNodeFactory
    {
        public static ISyntaxNode AnonymFun(ISyntaxNode definition, ISyntaxNode body)
            => new ArrowAnonymFunctionSyntaxNode(definition, body, new Interval(definition.Interval.Start, body.Interval.Finish));
        public static ISyntaxNode IfElse(IfCaseSyntaxNode[] ifThenNodes, ISyntaxNode elseResult, int start, int end) 
            => new IfThenElseSyntaxNode(ifThenNodes, elseResult, new Interval(start, end));
        public static IfCaseSyntaxNode IfThen(ISyntaxNode condition, ISyntaxNode expression, int start, int end)
            => new IfCaseSyntaxNode(condition,expression,new Interval(start, end));
        public static ISyntaxNode Var(Tok token) 
            => new NamedIdSyntaxNode(token.Value, token.Interval); 
        public static ISyntaxNode Constant(object value, VarType type, Interval interval) 
            => new ConstantSyntaxNode(value, type, interval);
        public static ISyntaxNode IntGenericConstant(ulong value, Interval interval)
            => new GenericIntSyntaxNode(value, false, interval);
        public static ISyntaxNode HexOrBinIntConstant(ulong value, Interval interval)
            => new GenericIntSyntaxNode(value, true, interval);
        public static ISyntaxNode Array(IList<ISyntaxNode> elements, int start, int end)
            => new ArraySyntaxNode(elements, new Interval(start,end));
        public static ISyntaxNode ListOf(ISyntaxNode[] elements, Interval interval, bool hasBrackets) 
            => new ListOfExpressionsSyntaxNode(elements, hasBrackets, interval);
        public static ISyntaxNode TypedVar(string name, VarType type, int start, int end)
            => new TypedVarDefSyntaxNode(name, type, new Interval(start,end));
        public static ISyntaxNode FunCall(string name, ISyntaxNode[] children, int start, int end) 
            => new FunCallSyntaxNode(name, children, new Interval(start,end));    
        public static ISyntaxNode OperatorFun(string name, ISyntaxNode[] children, int start, int end) 
            => new FunCallSyntaxNode(name, children, new Interval(start,end), true);
        public static ISyntaxNode Struct(List<EquationSyntaxNode> equations, Interval interval)
            => new StructSyntaxNode(equations, interval);

        public static ISyntaxNode FieldAccess( ISyntaxNode leftNode,Tok memberId)
        {
            return new StructFieldAccessSyntaxNode(leftNode, memberId.Value,
                new Interval(leftNode.Interval.Start, memberId.Finish));
        }
    }
}